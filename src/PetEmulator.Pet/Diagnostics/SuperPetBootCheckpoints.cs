using System.Text;

namespace PetEmulator.Pet.Diagnostics;

/// <summary>One named point of interest in the SuperPET 6809 Waterloo boot ROM, identified by hand
/// disassembly (see docs/pet/superpet-6809-boot-hang.md) - never a guess. Ordered roughly in the
/// sequence a healthy boot is expected to pass through them; a run's "never reached" list (see
/// <see cref="BootCheckpointReport"/>) is what pinpoints exactly which stage a boot dies before,
/// instead of re-deriving it by hand from a raw trace every time.</summary>
public sealed record BootCheckpoint(string Stage, ushort Pc, string Description, IReadOnlyList<ushort> WatchAddresses)
{
    public BootCheckpoint(string stage, ushort pc, string description, params ushort[] watchAddresses)
        : this(stage, pc, description, (IReadOnlyList<ushort>)watchAddresses)
    {
    }
}

/// <summary>One observed arrival at a <see cref="BootCheckpoint"/>'s address - full register
/// snapshot plus whatever that checkpoint's <see cref="BootCheckpoint.WatchAddresses"/> held at
/// that exact instant, so a hit is self-contained enough to explain without re-running anything.</summary>
public sealed record CheckpointHit(
    string Stage,
    int HitNumber,
    ulong InstructionIndex,
    ulong CycleCount,
    byte A,
    byte B,
    ushort X,
    ushort Y,
    ushort U,
    ushort S,
    byte DirectPage,
    byte Flags,
    IReadOnlyDictionary<ushort, byte> WatchedMemory);

/// <summary>Result of one <see cref="SuperPetBootCheckpoints.Run"/> - which stages were reached
/// (and when, and with what state), and - the actionable part - which were not, in checkpoint
/// declaration order so a "boot dies between stage N and stage N+1" reading is immediate.</summary>
public sealed record BootCheckpointReport(
    ulong InstructionsRun,
    ushort FinalProgramCounter,
    bool Halted,
    IReadOnlyList<BootCheckpoint> Checkpoints,
    IReadOnlyList<CheckpointHit> Hits,
    IReadOnlyList<BootCheckpoint> NeverReached)
{
    public string Render()
    {
        var output = new StringBuilder()
            .AppendLine($"instructions-run={InstructionsRun} pc-final=${FinalProgramCounter:X4} halted={Halted}")
            .AppendLine("hits (in the order first reached):");

        foreach (var hit in Hits)
        {
            var checkpoint = Checkpoints.First(c => c.Stage == hit.Stage);
            output.AppendLine(
                $"  [{hit.Stage}] hit #{hit.HitNumber} at instr={hit.InstructionIndex} cycles={hit.CycleCount} - " +
                checkpoint.Description);
            output.AppendLine(
                $"    A=${hit.A:X2} B=${hit.B:X2} X=${hit.X:X4} Y=${hit.Y:X4} U=${hit.U:X4} S=${hit.S:X4} " +
                $"DP=${hit.DirectPage:X2} P=${hit.Flags:X2}");
            foreach (var (address, value) in hit.WatchedMemory)
                output.AppendLine($"    ${address:X4}=${value:X2}");
        }

        output.AppendLine("never reached:");
        foreach (var checkpoint in NeverReached)
            output.AppendLine($"  [{checkpoint.Stage}] ${checkpoint.Pc:X4} - {checkpoint.Description}");

        return output.ToString();
    }
}

/// <summary>Runs a SuperPET 6809 <see cref="PetMachine"/> up to <paramref name="maxInstructions"/>
/// steps, recording an instruction-level snapshot every time PC lands on one of
/// <paramref name="checkpoints"/> (default: <see cref="WellKnown"/>) - up to
/// <paramref name="maxHitsPerCheckpoint"/> times each, so a checkpoint sitting inside a tight loop
/// doesn't blow memory the way <see cref="PetMachine.DiagnoseSuperPet6809Startup"/> and
/// <c>MachineDebugger.Trace</c> both do at large instruction counts (see
/// docs/pet/superpet-6809-boot-hang.md's "Tooling gap" section) - this is bounded by construction
/// regardless of how many instructions are requested.</summary>
public static class SuperPetBootCheckpoints
{
    /// <summary>Every checkpoint found while diagnosing the SuperPET 6809 boot hang, in the order a
    /// healthy boot is expected to reach them. Extend this list as new stages get disassembled -
    /// it's the running map of "what this ROM actually does", not a one-off debugging script.</summary>
    public static IReadOnlyList<BootCheckpoint> WellKnown { get; } =
    [
        new("Reset", 0xFF80,
            "6809 reset vector entry - the very first instruction executed."),
        new("ExpansionBankClear", 0xBC29,
            "W $EFFC=$00 - SuperPET expansion RAM bank-select register cleared.",
            0xEFFC),
        new("KeyboardRingBufferInit", 0xDD60,
            "Ring buffer head/tail ($012C/$012E) set to $0130 (empty); PIA1 armed " +
            "(PA=$0F, CRA=$3C, CRB=$3D - CRB bit0 enables the CB1/jiffy-clock IRQ).",
            0x012C, 0x012D, 0x012E, 0x012F, 0xE811, 0xE813),
        new("OldDeadPollLoop", 0xD6B4,
            "'Wait for CR/Ctrl-C/high-bit key from the ring buffer' loop entry - the exact PC the " +
            "6809 boot used to freeze on forever before the I/O-hole fix.",
            0x012C, 0x012E),
        new("ChecksCb1FlagAtDE0B", 0xDE0B,
            "Reads PIA1 CRB, checks bit7 (the CB1 flag); if set, chains toward the ring-buffer " +
            "enqueue routine. NOT the FIRQ soft-vector target ($0106) despite an earlier " +
            "disassembly pass assuming so - live checkpoint data showed S varying wildly between " +
            "hits (inconsistent with a fixed-size interrupt frame), so this is reached by a plain " +
            "JSR from whatever the current mainline call depth is, most likely a periodic " +
            "'check keyboard status' poll - not an interrupt handler at all.",
            0xE813),
        new("Address09FD", 0x09FD,
            "Later observed installed as the FIRQ soft-vector slot's target ($0100+6 = $0106), but " +
            "live data showed it first reached at instr~23240 while $0106 still read the default " +
            "$FFB1 stub - i.e. this first hit is reached by ordinary call flow (a JSR from " +
            "somewhere in the a000-bfff ROM), not via a fired FIRQ; whether it's ever reached AS " +
            "the FIRQ target (which needs M6809Cpu.SetFIRQ/RequestFirq - PetMachine never calls " +
            "either) is a separate, still-open question worth its own checkpoint on $0106's value.",
            0x0106, 0x0107),
        new("SwiSoftVectorTarget", 0xBC73,
            "The 6809 SWI soft-vector slot's real target ($0100+10 = $010A, read from RAM once " +
            "boot init runs) - only reachable via an explicit SWI instruction.",
            0x010A, 0x010B),
        new("RingBufferEnqueueCallA", 0xC6C0,
            "A JSR site into the ring-buffer enqueue routine ($DDAD)."),
        new("RingBufferEnqueueCallB", 0xDE3F,
            "A JSR site into the ring-buffer enqueue routine ($DDAD), reached from the FIRQ handler chain."),
        new("RingBufferEnqueueCallC", 0xDE68,
            "A JSR site into the ring-buffer enqueue routine ($DDAD)."),
        new("RingBufferEnqueue", 0xDDAD,
            "The ring-buffer enqueue routine itself - a keyboard byte is about to be pushed."),
        new("KeyboardMatrixScanLoop", 0xDF2F,
            "Direct PIA1 port poll for the physical keyboard matrix (W $E810=row-select, " +
            "R $E812=column-read) - polled, not interrupt-driven; wired through PetMachine's " +
            "_pia1.PortAWritten/PortBInput.",
            0xE810, 0xE812),
        new("IrqDefaultStub", 0xFFB1,
            "Default (never customized by this ROM) target for the IRQ/SWI2/SWI3 soft-vector slots."),
        new("MenuDispatchTrampoline", 0xA9F0,
            "Reads the boot-phase pointer at DP $2A and jumps through it - the sole entry point " +
            "into the whole menu subsystem (banner + SETUP/MONITOR/APL/BASIC/EDIT/FORTRAN/PASCAL/" +
            "DEVELOPMENT dispatch).",
            0x002A, 0x002B),
        new("MenuBannerPrint", 0xAA66,
            "Prints 'Waterloo microSystems ... Select:' plus the module-choice menu text."),
        new("MenuChoiceDispatch", 0xAA09,
            "Dereferences the menu jump table by the pressed letter's index and JSRs to the chosen module."),
    ];

    public static BootCheckpointReport Run(
        PetMachine machine,
        ulong maxInstructions,
        IReadOnlyList<BootCheckpoint>? checkpoints = null,
        int maxHitsPerCheckpoint = 3)
    {
        ArgumentNullException.ThrowIfNull(machine);
        var cpu = machine.SuperPet6809Cpu
            ?? throw new InvalidOperationException("The selected machine has no Waterloo 6809 board.");
        checkpoints ??= WellKnown;
        var byPc = new Dictionary<ushort, BootCheckpoint>();
        foreach (var checkpoint in checkpoints)
            byPc[checkpoint.Pc] = checkpoint;

        var hits = new List<CheckpointHit>();
        var hitCounts = new Dictionary<string, int>();

        ulong i;
        for (i = 0; i < maxInstructions && !cpu.Halted; i++)
        {
            var pc = cpu.State.PC;
            if (byPc.TryGetValue(pc, out var checkpoint))
            {
                var count = hitCounts.GetValueOrDefault(checkpoint.Stage);
                if (count < maxHitsPerCheckpoint)
                {
                    var watched = checkpoint.WatchAddresses.ToDictionary(a => a, machine.Memory.Read);
                    hits.Add(new CheckpointHit(
                        checkpoint.Stage,
                        count + 1,
                        i,
                        cpu.CycleCount,
                        cpu.State.A,
                        cpu.State.B,
                        cpu.State.X,
                        cpu.State.Y,
                        cpu.State.U,
                        cpu.State.S,
                        cpu.State.DP,
                        cpu.State.Flags.ToByte(),
                        watched));
                }
                hitCounts[checkpoint.Stage] = count + 1;
            }

            machine.StepInstruction();
        }

        var neverReached = checkpoints.Where(c => !hitCounts.ContainsKey(c.Stage)).ToList();
        return new BootCheckpointReport(i, cpu.State.PC, cpu.Halted, checkpoints, hits, neverReached);
    }
}
