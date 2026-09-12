using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PetEmulator.CpuZ80.Bus;

public sealed partial class SystemBus : IBus
{
    private readonly IMemoryBus memory;
    private readonly IIoBus io;
    private readonly ILogger logger;

    /// <summary>Address-keyed diagnostic watchpoints - see <see cref="MemoryWatch"/>. Empty by default; add to it, don't replace it.</summary>
    public MemoryWatch Watch { get; } = new();

    public SystemBus(IMemoryBus memory, ILogger<SystemBus>? logger = null)
        : this(memory, new UnmappedIoBus(), logger)
    {
    }

    /// <param name="logger">
    /// Optional per-access Trace source ("Bus" category per the plan's
    /// logging doc). Omit it and every call is a no-op - a bus trace that
    /// logs unconditionally would flood the log and wreck timing, so
    /// there is no default logger, only an opt-in one.
    /// </param>
    public SystemBus(IMemoryBus memory, IIoBus io, ILogger<SystemBus>? logger = null)
    {
        this.memory = memory ?? throw new ArgumentNullException(nameof(memory));
        this.io = io ?? throw new ArgumentNullException(nameof(io));
        this.logger = logger ?? NullLogger<SystemBus>.Instance;
    }

    public byte ReadMemory(ushort address)
    {
        var value = memory.Read(address);
        LogMemoryRead(logger, address, value);
        if (Watch.HasAny)
            Watch.OnRead(address, value);
        return value;
    }

    public void WriteMemory(ushort address, byte value)
    {
        LogMemoryWrite(logger, address, value);
        if (Watch.HasAny)
            Watch.OnWrite(address, value);
        memory.Write(address, value);
    }

    public byte ReadPort(byte port)
    {
        var value = io.Read(port);
        LogPortRead(logger, port, value);
        return value;
    }

    // Not a byte-overload passthrough: some IIoBus implementations key off
    // the full 16-bit port (real Z80 IN/OUT drive A0-A15, not just A0-A7),
    // so this must reach io.Read(ushort) directly - truncating to byte
    // first would lose the high byte before it ever got there.
    public byte ReadPort(ushort port)
    {
        var value = io.Read(port);
        LogPortRead(logger, port, value);
        return value;
    }

    public void WritePort(byte port, byte value)
    {
        LogPortWrite(logger, port, value);
        io.Write(port, value);
    }

    public void WritePort(ushort port, byte value)
    {
        LogPortWrite(logger, port, value);
        io.Write(port, value);
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "MEM R {Address:X4}={Value:X2}")]
    private static partial void LogMemoryRead(ILogger logger, ushort address, byte value);

    [LoggerMessage(Level = LogLevel.Trace, Message = "MEM W {Address:X4}={Value:X2}")]
    private static partial void LogMemoryWrite(ILogger logger, ushort address, byte value);

    [LoggerMessage(Level = LogLevel.Trace, Message = "IO R {Port:X4}={Value:X2}")]
    private static partial void LogPortRead(ILogger logger, ushort port, byte value);

    [LoggerMessage(Level = LogLevel.Trace, Message = "IO W {Port:X4}={Value:X2}")]
    private static partial void LogPortWrite(ILogger logger, ushort port, byte value);
}
