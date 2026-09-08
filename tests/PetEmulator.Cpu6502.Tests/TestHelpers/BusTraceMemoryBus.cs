using System.Collections.Generic;
using Cpu6502;

namespace Cpu6502.Tests.TestHelpers;

public sealed class BusTraceMemoryBus : IMemoryBus
{
    private readonly byte[] _memory = new byte[65536];
    private readonly List<BusAccess> _accesses = new();

    public IReadOnlyList<BusAccess> Accesses => _accesses;

    public byte Read(ushort address)
    {
        byte value = _memory[address];
        _accesses.Add(BusAccess.Read(address, value));
        return value;
    }

    public void Write(ushort address, byte value)
    {
        _memory[address] = value;
        _accesses.Add(BusAccess.Write(address, value));
    }

    public void Load(ushort address, params byte[] data)
    {
        for (var i = 0; i < data.Length; i++)
        {
            _memory[(ushort)(address + i)] = data[i];
        }
    }

    public byte this[ushort address]
    {
        get => _memory[address];
        set => _memory[address] = value;
    }

    public void ClearTrace() => _accesses.Clear();
}

public sealed record BusAccess(bool IsWrite, ushort Address, byte Value)
{
    public static BusAccess Read(ushort address, byte value) => new(false, address, value);
    public static BusAccess Write(ushort address, byte value) => new(true, address, value);
}
