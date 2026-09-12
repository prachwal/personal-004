using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using PetEmulator.Core;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Memory;

namespace PetEmulator.CpuZ80.Tests;

public sealed class BusTests
{
    [Fact]
    public void SystemBusTraceLoggingIsOffByDefaultAndOnWhenAnEnabledLoggerIsGiven()
    {
        var silentLogger = new RecordingLogger<SystemBus>(LogLevel.None);
        var silentBus = new SystemBus(new RamMemory(), silentLogger);
        silentBus.ReadMemory(0x1234);
        Assert.Empty(silentLogger.Messages);

        var tracingLogger = new RecordingLogger<SystemBus>(LogLevel.Trace);
        var tracingBus = new SystemBus(new RamMemory(), tracingLogger);
        tracingBus.ReadMemory(0x1234);

        Assert.Contains(tracingLogger.Messages, m => m.Contains("1234", StringComparison.Ordinal));
    }


    [Fact]
    public void SystemBusDelegatesMemoryAndIoOperations()
    {
        var memory = new RecordingMemoryBus();
        var io = new RecordingIoBus();

        WriteThroughBus(new SystemBus(memory, io));

        Assert.Equal((ushort)0x1234, memory.LastAddress);
        Assert.Equal((byte)0x56, memory.LastValue);
        Assert.Equal((byte)0xA0, io.LastPort);
        Assert.Equal((byte)0x78, io.LastValue);
    }

    [Fact]
    public void SystemBusUsesFFForUnmappedPortsByDefault()
    {
        var bus = new SystemBus(new RamMemory());

        bus.WritePort(0xFF, 0x12);

        Assert.Equal((byte)0xFF, bus.ReadPort(0xFF));
    }

    [Fact]
    public void SystemBusPreservesFullIoPortAddresses()
    {
        var io = new FullAddressIoBus();
        var bus = new SystemBus(new RamMemory(), io);

        bus.WritePort(0xA512, 0x78);

        Assert.Equal((ushort)0xA512, io.LastPort);
        Assert.Equal((byte)0x78, io.LastValue);

        Assert.Equal((byte)0x78, bus.ReadPort(0xA512));
        Assert.Equal((ushort)0xA512, io.LastReadPort);
    }

    [SuppressMessage("Performance", "CA1859", Justification = "This helper verifies the IBus contract.")]
    private static void WriteThroughBus(IBus bus)
    {
        bus.WriteMemory(0x1234, 0x56);
        bus.WritePort(0xA0, 0x78);
    }

    private sealed class RecordingMemoryBus : IMemoryBus
    {
        public ushort LastAddress { get; private set; }
        public byte LastValue { get; private set; }

        public byte Read(ushort address) => LastValue;

        public void Write(ushort address, byte value)
        {
            LastAddress = address;
            LastValue = value;
        }
    }

    private sealed class RecordingIoBus : IIoBus
    {
        public byte LastPort { get; private set; }
        public byte LastValue { get; private set; }

        public byte Read(byte port) => LastValue;

        public void Write(byte port, byte value)
        {
            LastPort = port;
            LastValue = value;
        }
    }

    private sealed class FullAddressIoBus : IIoBus
    {
        public ushort LastPort { get; private set; }
        public ushort LastReadPort { get; private set; }
        public byte LastValue { get; private set; }

        public byte Read(byte port) => LastValue;

        public byte Read(ushort port)
        {
            LastReadPort = port;
            return LastValue;
        }

        public void Write(byte port, byte value)
        {
            LastPort = port;
            LastValue = value;
        }

        public void Write(ushort port, byte value)
        {
            LastPort = port;
            LastValue = value;
        }
    }
}
