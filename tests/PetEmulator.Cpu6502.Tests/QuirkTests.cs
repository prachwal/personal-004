using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests
{
    [TestFixture]
    public class QuirkTests
    {
        private FlatMemory _memory;
        private Cpu6502 _cpu;

        [SetUp]
        public void Setup()
        {
            _memory = new FlatMemory();
            _cpu = new Cpu6502(_memory);
            _memory.Write(0xFFFE, 0x00);
            _memory.Write(0xFFFF, 0x00);
            _cpu.Reset();
        }

        private void LoadProgram(params byte[] program)
        {
            ushort baseAddr = 0x8000;
            for (int i = 0; i < program.Length; i++)
                _memory.Write((ushort)(baseAddr + i), program[i]);
            _cpu.PC = baseAddr;
            var state = _cpu.GetState();
            state.Sync = true;
            _cpu.SetState(state);
        }

        private void ExecuteOne()
        {
            do
            {
                _cpu.Tick();
            }
            while (!_cpu.GetState().Sync);
        }

        private static void ExecuteOne(Cpu6502 cpu)
        {
            do
            {
                cpu.Tick();
            }
            while (!cpu.GetState().Sync);
        }

        private sealed class CountingMemory : IMemoryBus
        {
            private readonly byte[] ram = new byte[65536];

            public int WriteCount { get; private set; }
            public int WriteCountAtTarget { get; private set; }
            public ushort TargetAddress { get; set; }

            public byte Read(ushort address) => ram[address];

            public void Write(ushort address, byte value)
            {
                WriteCount++;
                if (address == TargetAddress)
                {
                    WriteCountAtTarget++;
                }
                ram[address] = value;
            }
        }

        #region R-M-W Double Write Tests

        [Test]
        public void IncAbs_DoubleWrite_WritesTwiceToSameAddress()
        {
            // Arrange
            var memory = new CountingMemory();
            var cpu = new Cpu6502(memory);
            memory.Write(0xFFFC, 0x00);
            memory.Write(0xFFFD, 0x00);
            cpu.Reset();
            memory.Write(0x8000, 0xEE);
            memory.Write(0x8001, 0x00);
            memory.Write(0x8002, 0x10);
            memory.Write(0x1000, 0x42); // original value
            memory.TargetAddress = 0x1000;
            cpu.PC = 0x8000;
            var state = cpu.GetState();
            state.Sync = true;
            cpu.SetState(state);

            // Act - execute all cycles
            ExecuteOne(cpu);

            // Assert - INC absolute: dummy write + actual write = 2 writes
            Assert.That(memory.WriteCountAtTarget, Is.EqualTo(2));
            Assert.That(memory.Read(0x1000), Is.EqualTo(0x43));
        }

        [Test]
        public void AslAbs_DoubleWrite_WritesTwiceToSameAddress()
        {
            // Arrange
            var memory = new CountingMemory();
            var cpu = new Cpu6502(memory);
            memory.Write(0xFFFC, 0x00);
            memory.Write(0xFFFD, 0x00);
            cpu.Reset();
            memory.Write(0x8000, 0x0E);
            memory.Write(0x8001, 0x00);
            memory.Write(0x8002, 0x10);
            memory.Write(0x1000, 0x41); // original value (01000001)
            memory.TargetAddress = 0x1000;
            cpu.PC = 0x8000;
            var state = cpu.GetState();
            state.Sync = true;
            cpu.SetState(state);

            // Act
            ExecuteOne(cpu);

            // Assert - ASL absolute: dummy write + actual write = 2 writes
            Assert.That(memory.WriteCountAtTarget, Is.EqualTo(2));
            Assert.That(memory.Read(0x1000), Is.EqualTo(0x82)); // ASL result
        }

        [Test]
        public void DecAbs_DoubleWrite_WritesTwiceToSameAddress()
        {
            // Arrange
            var memory = new CountingMemory();
            var cpu = new Cpu6502(memory);
            memory.Write(0xFFFC, 0x00);
            memory.Write(0xFFFD, 0x00);
            cpu.Reset();
            memory.Write(0x8000, 0xCE);
            memory.Write(0x8001, 0x00);
            memory.Write(0x8002, 0x10);
            memory.Write(0x1000, 0x42); // original value
            memory.TargetAddress = 0x1000;
            cpu.PC = 0x8000;
            var state = cpu.GetState();
            state.Sync = true;
            cpu.SetState(state);

            // Act
            ExecuteOne(cpu);

            // Assert - DEC absolute: dummy write + actual write = 2 writes
            Assert.That(memory.WriteCountAtTarget, Is.EqualTo(2));
            Assert.That(memory.Read(0x1000), Is.EqualTo(0x41)); // Decrement result
        }

        #endregion

        #region JMP Indirect Bug Tests

        [Test]
        public void JmpIndirect_BugAtPageBoundary_ReadsHighByteFromSamePage()
        {
            // Arrange - JMP ($01FF) where $01FF contains $12 and $0100 contains $34
            LoadProgram(0x6C, 0xFF, 0x01); // JMP ($01FF)
            _memory.Write(0x01FF, 0x12); // low byte of target
            _memory.Write(0x0100, 0x34); // high byte of target (NMOS bug: read from $0100 instead of $0200)

            // Act
            ExecuteOne(); // Execute JMP indirect

            // Assert - PC should be $3412 (high byte from $0100, low from $01FF)
            Assert.That(_cpu.PC, Is.EqualTo(0x3412));
        }

        [Test]
        public void JmpIndirect_NormalCase_ReadsHighByteFromNextAddress()
        {
            // Arrange - JMP ($0200) where $0200 contains $56 and $0201 contains $78
            LoadProgram(0x6C, 0x00, 0x02); // JMP ($0200)
            _memory.Write(0x0200, 0x56); // low byte of target
            _memory.Write(0x0201, 0x78); // high byte of target

            // Act
            ExecuteOne(); // Execute JMP indirect

            // Assert - PC should be $7856
            Assert.That(_cpu.PC, Is.EqualTo(0x7856));
        }

        #endregion

        #region CLI Latency Tests

        [Test]
        public void Cli_ClearsIFlagImmediately()
        {
            // Arrange - CLI with IRQ pending
            LoadProgram(0x58, 0xEA); // CLI, NOP
            _cpu.SetFlag(Cpu6502.FlagI, true); // IRQ disabled

            // Act - execute CLI
            ExecuteOne(); // CLI completes

            // Assert - I flag should be cleared immediately
            Assert.That(_cpu.GetFlag(Cpu6502.FlagI), Is.False);
            Assert.That(_cpu.PC, Is.EqualTo(0x8001));
        }

        [Test]
        public void Cli_DelaysIRQByOneInstruction()
        {
            // Arrange - CLI followed by NOP with IRQ pending
            LoadProgram(0x58, 0xEA, 0xEA); // CLI, NOP, NOP
            _cpu.SetFlag(Cpu6502.FlagI, true); // IRQ disabled

            // Set up IRQ
            _cpu.SetIRQ(true);
            _memory.Write(0xFFFE, 0x00);
            _memory.Write(0xFFFF, 0x60);

            // Act - execute CLI
            ExecuteOne(); // CLI completes, I flag cleared

            // Assert - I flag should be clear, but IRQ not yet fired
            Assert.That(_cpu.GetFlag(Cpu6502.FlagI), Is.False);
            Assert.That(_cpu.PC, Is.EqualTo(0x8001));

            // Execute NOP - IRQ should NOT fire yet (delayed by 1 instruction)
            ExecuteOne(); // NOP completes

            // Assert - IRQ still not fired
            Assert.That(_cpu.PC, Is.EqualTo(0x8002));

            // Execute next NOP - IRQ should fire here
            ExecuteOne(); // NOP completes - IRQ should fire here

            // Assert - IRQ should have fired
            Assert.That(_cpu.PC, Is.EqualTo(0x6000)); // Should be at IRQ vector
        }

        #endregion

        #region Branch Interrupt Timing Tests

        [Test]
        public void BranchNotTaken_DoesNotAllowIRQOnSameInstruction()
        {
            // Arrange - BCC with carry set (not taken) and IRQ pending
            LoadProgram(0x90, 0x02, 0xEA); // BCC +2, NOP
            _cpu.SetFlag(Cpu6502.FlagC, true); // Carry set, so BCC not taken
            _cpu.SetFlag(Cpu6502.FlagI, false); // IRQ enabled

            // Set up IRQ
            _cpu.SetIRQ(true);
            _memory.Write(0xFFFE, 0x00);
            _memory.Write(0xFFFF, 0x50);

            // Act - execute BCC (not taken, 2 cycles)
            ExecuteOne(); // BCC completes

            // Assert - PC should be at NOP, IRQ not fired during BCC
            Assert.That(_cpu.PC, Is.EqualTo(0x8002));

            // Execute NOP - IRQ should fire here
            ExecuteOne(); // NOP completes - IRQ should fire here

            // Assert - IRQ should have fired after NOP
            Assert.That(_cpu.PC, Is.EqualTo(0x5000));
        }

        [Test]
        public void BranchTakenSamePage_DoesNotAllowIRQOnSameInstruction()
        {
            // Arrange - BCC with carry clear (taken, same page) and IRQ pending
            LoadProgram(0x90, 0x02, 0xEA); // BCC +2, NOP at target
            _cpu.SetFlag(Cpu6502.FlagC, false); // Carry clear, so BCC taken
            _cpu.SetFlag(Cpu6502.FlagI, false); // IRQ enabled

            // Set up IRQ
            _cpu.SetIRQ(true);
            _memory.Write(0xFFFE, 0x00);
            _memory.Write(0xFFFF, 0x50);

            // Act - execute BCC (taken, same page, 3 cycles)
            ExecuteOne(); // BCC completes

            // Assert - PC should be at target
            Assert.That(_cpu.PC, Is.EqualTo(0x8004));

            // Execute NOP - IRQ should fire here
            ExecuteOne(); // NOP completes - IRQ should fire here

            // Assert - IRQ should have fired
            Assert.That(_cpu.PC, Is.EqualTo(0x5000));
        }

        #endregion
    }
}
