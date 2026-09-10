using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PetEmulator.Core;

namespace PetEmulator.DebugApi;

// TODO: endpoints intentionally dropped when porting this from
// personal-002/lib/Terminal.Avalonia/DebugApi.cs — none of these have a backing concept on this
// repo's IMachine yet. Add them back once IMachine (or a machine-specific extension of it) grows
// the relevant device model:
//   - GET /ws                          - no change-notification hook on IMachine to push from.
//   - GET /snapshot, POST /snapshot    - no snapshot/restore contract here.
//   - GET /screen.png                  - no display/raster device on IMachine.
//   - POST /key, POST /text            - no keyboard/input device on IMachine.
//   - POST /tape, POST /tape/save,
//     POST /disk, GET|POST /fdc        - no storage/FDC device model on IMachine.
//   - GET|POST /keyboard,
//     GET|POST /display                - no matrix-keyboard / segment-display device model.
//   - POST /frames                     - no notion of "frame" on IMachine (only
//                                         StepInstruction/Run at the instruction level).

/// <summary>
/// Localhost-only debug/scripted-testing HTTP API for a running <see cref="IMachine"/>: read
/// status, single-step or run instructions, trace execution, and peek/poke memory.
///
/// <para><b>Not authenticated, not exposed beyond 127.0.0.1</b> — binding Kestrel to loopback only
/// (see <see cref="Create"/>) is the entire security model, adequate for a local diagnostic tool,
/// never for production use.</para>
///
/// <para><b>Threading.</b> The personal-002 source this was ported from ran its whole step/trace
/// loop inside <c>Dispatcher.UIThread.InvokeAsync(...)</c> because it needed to touch a
/// non-thread-safe machine from Kestrel's thread pool while an Avalonia UI thread owned that same
/// machine — a large <c>steps</c> value blocked the UI for the whole trace. This host has no UI
/// thread at all: nothing in <see cref="PetEmulator.Core"/> guarantees <see cref="IMachine"/> is
/// thread-safe, but the fix is just a single <see cref="Lock"/> serializing access, with the actual
/// stepping done via <see cref="Task.Run(Action)"/> on a thread-pool thread instead of inline on
/// whichever Kestrel thread accepted the request. That keeps every request's I/O thread free and
/// guarantees two concurrent requests (e.g. <c>/step</c> and <c>/trace</c>) can never step the
/// machine at the same time.</para>
/// </summary>
public static class DebugApiHost
{
    private const int DefaultTraceSteps = 1_000;
    private const int MaxTraceSteps = 100_000;
    private const int DefaultTraceMaxSamples = 200;
    private const int MaxTraceMaxSamples = 500;
    private const int MaxMemoryLength = 65536;

    /// <summary>Builds a configured, not-yet-started <see cref="WebApplication"/> bound to
    /// <c>127.0.0.1:port</c> (pass <c>port: 0</c> for an OS-assigned ephemeral port, e.g. from
    /// tests). The caller decides when/how to run it (<c>Run()</c>, <c>RunAsync()</c>, or
    /// <c>StartAsync()</c> for in-process testing).</summary>
    public static WebApplication Create(IMachine machine, int port = 5099)
    {
        ArgumentNullException.ThrowIfNull(machine);

        // ponytail: a plain lock is enough here — every handler either finishes fast (status,
        // single step, small memory read/write) or is the one long-running trace, and there's no
        // reentrancy or async work inside the critical section, so there's nothing a
        // SemaphoreSlim would buy over Lock. Revisit only if a handler needs to await while
        // holding the machine.
        var gate = new Lock();

        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port));

        WebApplication app = builder.Build();
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (JsonException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Request body is not valid JSON." });
            }
        });

        MapEndpoints(app, machine, gate);
        return app;
    }

    private static void MapEndpoints(WebApplication app, IMachine machine, Lock gate)
    {
        app.MapGet("/", () => Results.Json(new
        {
            endpoints = new[]
            {
                "GET / - this list",
                "GET /status - machine + processor status snapshot",
                "POST /step {count=1} - execute count instructions, returns status afterward",
                "POST /trace {steps=1000,maxSamples=200} - single-step up to steps times (or until " +
                    "halted), returns a bounded sample list of {cycle,instructionCount,halted}",
                "GET /memory?address=0&length=256 - read raw memory as hex",
                "POST /memory {address,hex} - write raw memory from hex, starting at address",
                "POST /reset - power-cycle the machine",
            },
        }));

        app.MapGet("/status", async () => Results.Json(await RunLocked(gate, machine, GetStatus)));

        app.MapPost("/step", async (HttpContext ctx) =>
        {
            StepRequest req = await ReadJsonAsync(ctx, new StepRequest());
            return await RunStepAsync(gate, machine, req.Count);
        });
        app.MapGet("/step", () => RunStepAsync(gate, machine, 1));

        app.MapPost("/trace", async (HttpContext ctx) =>
        {
            TraceRequest req = await ReadJsonAsync(ctx, new TraceRequest());
            int steps = Math.Clamp(req.Steps <= 0 ? DefaultTraceSteps : req.Steps, 1, MaxTraceSteps);
            int maxSamples = Math.Clamp(req.MaxSamples <= 0 ? DefaultTraceMaxSamples : req.MaxSamples, 1, MaxTraceMaxSamples);
            TraceResult result = await RunLocked(gate, machine, m => RunTrace(m, steps, maxSamples));
            return Results.Json(result);
        });

        app.MapGet("/memory", async (int address = 0, int length = 256) =>
        {
            if (address < 0 || address > 0xFFFF)
                return Results.BadRequest(new { error = "address must be within 0-65535." });
            if (length < 0 || length > MaxMemoryLength || address + length > 0x10000)
                return Results.BadRequest(new { error = "length must be non-negative and stay within the 64KB address space." });

            byte[] bytes = await RunLocked(gate, machine, m =>
            {
                var buffer = new byte[length];
                for (int i = 0; i < length; i++)
                    buffer[i] = m.Memory.Read((ushort)(address + i));
                return buffer;
            });
            return Results.Json(new { address, length, hex = Convert.ToHexString(bytes) });
        });

        app.MapPost("/memory", async (HttpContext ctx) =>
        {
            WriteMemoryRequest req = await ReadJsonAsync(ctx, new WriteMemoryRequest());
            if (req.Address < 0 || req.Address > 0xFFFF)
                return Results.BadRequest(new { error = "address must be within 0-65535." });
            if (req.Hex.Length % 2 != 0)
                return Results.BadRequest(new { error = "hex must have an even length." });

            byte[] data;
            try
            {
                data = string.IsNullOrEmpty(req.Hex) ? [] : Convert.FromHexString(req.Hex);
            }
            catch (FormatException)
            {
                return Results.BadRequest(new { error = "hex contains invalid characters." });
            }
            if (req.Address + data.Length > 0x10000)
                return Results.BadRequest(new { error = "write would exceed the 64KB address space." });

            await RunLocked(gate, machine, m =>
            {
                for (int i = 0; i < data.Length; i++)
                    m.Memory.Write((ushort)(req.Address + i), data[i]);
                return true;
            });
            return Results.Json(new { ok = true, address = req.Address, length = data.Length });
        });

        app.MapPost("/reset", async () => Results.Json(await RunLocked(gate, machine, m =>
        {
            m.Reset();
            return GetStatus(m);
        })));
    }

    private static async Task<IResult> RunStepAsync(Lock gate, IMachine machine, int count)
    {
        if (count < 1)
            return Results.BadRequest(new { error = "count must be at least 1." });

        MachineStatus status = await RunLocked(gate, machine, m =>
        {
            m.Run((ulong)count);
            return GetStatus(m);
        });
        return Results.Json(status);
    }

    /// <summary>Single-steps up to <paramref name="steps"/> times, stopping early once the
    /// processor halts, and records a sample every <c>steps / maxSamples</c>-th step (plus the
    /// final step) so the response stays bounded regardless of how large <paramref name="steps"/>
    /// is. There's no PC or register snapshot here — <see cref="IProcessor"/> is deliberately
    /// CPU-agnostic and exposes only Halted/CycleCount/InstructionCount, so that's all a sample
    /// can carry; add richer per-step data only behind a CPU-specific capability, not by
    /// fabricating fields here.</summary>
    private static TraceResult RunTrace(IMachine machine, int steps, int maxSamples)
    {
        var samples = new List<TraceSample>();
        int interval = Math.Max(1, steps / maxSamples);
        int executed = 0;

        while (executed < steps && !machine.Processor.Halted)
        {
            machine.StepInstruction();
            executed++;

            bool isSampleTick = executed % interval == 0;
            bool isLastStep = executed == steps || machine.Processor.Halted;
            if ((isSampleTick || isLastStep) && samples.Count < maxSamples)
                samples.Add(new TraceSample(machine.Processor.CycleCount, machine.Processor.InstructionCount, machine.Processor.Halted));
        }

        return new TraceResult(executed, samples.Count, samples);
    }

    private static MachineStatus GetStatus(IMachine machine) => new(
        machine.Name,
        machine.IsReady,
        machine.CycleCount,
        new ProcessorStatus(machine.Processor.Halted, machine.Processor.CycleCount, machine.Processor.InstructionCount));

    /// <summary>Runs <paramref name="action"/> on a thread-pool thread while holding
    /// <paramref name="gate"/>, so it never runs concurrently with any other locked machine
    /// access — this is the whole fix for the source's UI-thread blocking: no dispatcher, just a
    /// lock plus <see cref="Task.Run(Action)"/> instead of running inline on the calling
    /// (Kestrel I/O) thread.</summary>
    private static Task<T> RunLocked<T>(Lock gate, IMachine machine, Func<IMachine, T> action) =>
        Task.Run(() =>
        {
            lock (gate)
                return action(machine);
        });

    /// <summary>Reads the raw request body and deserializes it by hand instead of minimal API's
    /// own <c>[FromBody]</c> binding, which requires a <c>Content-Type: application/json</c>
    /// header that plain <c>curl -d '{...}'</c> doesn't send by default. An empty body falls back
    /// to <paramref name="fallback"/> so every POST endpoint above also works with no body at
    /// all.</summary>
    private static async Task<T> ReadJsonAsync<T>(HttpContext context, T fallback)
    {
        using var reader = new StreamReader(context.Request.Body);
        string body = await reader.ReadToEndAsync();
        return string.IsNullOrWhiteSpace(body)
            ? fallback
            : JsonSerializer.Deserialize<T>(body, RequestJsonOptions) ?? fallback;
    }

    private static readonly JsonSerializerOptions RequestJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record StepRequest(int Count = 1);
    private sealed record TraceRequest(int Steps = DefaultTraceSteps, int MaxSamples = DefaultTraceMaxSamples);
    private sealed record WriteMemoryRequest(int Address = 0, string Hex = "");
    private sealed record ProcessorStatus(bool Halted, ulong CycleCount, ulong InstructionCount);
    private sealed record MachineStatus(string Name, bool IsReady, ulong CycleCount, ProcessorStatus Processor);
    private sealed record TraceSample(ulong Cycle, ulong InstructionCount, bool Halted);
    private sealed record TraceResult(int StepsExecuted, int SamplesCollected, IReadOnlyList<TraceSample> Samples);
}
