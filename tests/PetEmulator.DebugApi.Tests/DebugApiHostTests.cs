using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace PetEmulator.DebugApi.Tests;

[TestFixture]
public sealed class DebugApiHostTests
{
    private WebApplication _app = null!;
    private FakeMachine _machine = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _machine = new FakeMachine();
        // port: 0 lets the OS pick an ephemeral port, so tests can run in parallel without
        // clashing on a fixed port.
        _app = DebugApiHost.Create(_machine, port: 0);
        await _app.StartAsync();

        IServerAddressesFeature addressesFeature = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!;
        string address = addressesFeature.Addresses.First();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    [TearDown]
    public async Task TearDown()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Test]
    public async Task Status_reports_machine_and_processor_state()
    {
        HttpResponseMessage response = await _client.GetAsync("/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("name").GetString().Should().Be("fake");
        json.GetProperty("isReady").GetBoolean().Should().BeTrue();
        json.GetProperty("processor").GetProperty("halted").GetBoolean().Should().BeFalse();
    }

    [Test]
    public async Task Root_lists_endpoints()
    {
        HttpResponseMessage response = await _client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("endpoints").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Test]
    public async Task Step_advances_instruction_count_by_count()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/step", new { count = 5 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("processor").GetProperty("instructionCount").GetUInt64().Should().Be(5);
        _machine.Processor.InstructionCount.Should().Be(5);
    }

    [Test]
    public async Task Trace_collects_bounded_samples_and_stops_at_steps()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/trace", new { steps = 100, maxSamples = 10 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("stepsExecuted").GetInt32().Should().Be(100);
        int samplesCollected = json.GetProperty("samplesCollected").GetInt32();
        samplesCollected.Should().BeLessThanOrEqualTo(10);
        json.GetProperty("samples").GetArrayLength().Should().Be(samplesCollected);
    }

    [Test]
    public async Task Memory_write_then_read_round_trips()
    {
        HttpResponseMessage write = await _client.PostAsJsonAsync("/memory", new { address = 0x1000, hex = "0102FF" });
        write.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage read = await _client.GetAsync("/memory?address=4096&length=3");
        JsonElement json = await read.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("hex").GetString().Should().Be("0102FF");
    }

    [Test]
    public async Task Memory_read_rejects_out_of_range_length()
    {
        HttpResponseMessage response = await _client.GetAsync("/memory?address=65530&length=100");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Reset_zeroes_counters()
    {
        await _client.PostAsJsonAsync("/step", new { count = 10 });

        HttpResponseMessage response = await _client.PostAsync("/reset", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("processor").GetProperty("instructionCount").GetUInt64().Should().Be(0);
        _machine.Processor.Halted.Should().BeFalse();
    }

    [Test]
    public async Task Concurrent_step_and_trace_requests_do_not_corrupt_machine_state()
    {
        // Regression test for the bug fixed while porting this from personal-002: that source ran
        // its step/trace loops inline (marshaled only onto Avalonia's UI thread) with no lock
        // around the machine at all. FakeProcessor.StepInstruction deliberately does a
        // non-atomic read/yield/write specifically so a missing lock would lose updates under
        // concurrency. If DebugApiHost's Lock is doing its job, every one of these concurrent
        // requests' steps lands — none lost to interleaving.
        const int requestCount = 8;
        const int stepsPerRequest = 200;

        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(i % 2 == 0
                ? _client.PostAsJsonAsync("/step", new { count = stepsPerRequest })
                : _client.PostAsJsonAsync("/trace", new { steps = stepsPerRequest, maxSamples = 5 }));
        }

        HttpResponseMessage[] responses = await Task.WhenAll(tasks);

        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        _machine.Processor.InstructionCount.Should().Be((ulong)(requestCount * stepsPerRequest));
        _machine.Processor.CycleCount.Should().Be((ulong)(requestCount * stepsPerRequest * 2));
    }
}
