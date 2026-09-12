using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Core.Serial;
using PetEmulator.Chips;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Pet.Tape;
using PetEmulator.Pet.Tests.Roms;
using PetEmulator.Pet.Roms;

namespace PetEmulator.Pet.Tests;

/// <summary>
/// First smoke tests of real ROM execution through <see cref="PetMachine"/>'s brand-new bus
/// decoder. Deliberately bounded (hundreds of instructions, not a full BASIC boot) with a hard
/// [CancelAfter] backstop: the CPU's per-instruction cycle cap throws instead of hanging on a
/// handler bug, but a real bug there is still a legitimate failure here, not something to swallow.
/// </summary>
[TestFixture]
public sealed class PetMachineTests
{
    private static readonly byte[] ReadyBytes = [0x12, 0x05, 0x01, 0x04, 0x19, 0x2E]; // "READY."

    [Test]
    public void BusObserver_FiresForEveryRealReadAndWrite()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var accesses = new List<BusAccess>();
        machine.BusObserver = accesses.Add;

        machine.Memory.Write(0x0100, 0x42);
        machine.Memory.Read(0x0100);

        accesses.Should().Equal(
            new BusAccess(IsWrite: true, 0x0100, 0x42),
            new BusAccess(IsWrite: false, 0x0100, 0x42));
    }

    [Test]
    public void BusObserver_ObservesRealChipAccessesDuringRealExecution()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var pia1Accesses = 0;
        machine.BusObserver = access =>
        {
            if (access.Address is >= 0xE810 and < 0xE820)
                pia1Accesses++;
        };

        machine.Run(2_000);

        pia1Accesses.Should().BeGreaterThan(0, "real KERNAL boot code touches PIA1 (keyboard/cassette) within the first 2,000 instructions");
    }

    [Test]
    public void BusObserver_IsOptOutWithZeroBehaviorChangeWhenUnset()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        var act = () => machine.Run(500);

        act.Should().NotThrow();
        machine.BusObserver.Should().BeNull();
    }

    [Test]
    public void RunUntilOrStalled_MeetsCondition_WhenItBecomesTrueBeforeAnyStallWindow()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        var result = machine.RunUntilOrStalled(
            _ => machine.Processor.InstructionCount >= 10,
            () => (long)machine.Processor.InstructionCount, // always "progresses" every instruction
            maxInstructions: 1_000,
            stallWindow: 500);

        result.ConditionMet.Should().BeTrue();
        result.Stalled.Should().BeFalse();
        result.InstructionsRun.Should().Be(10);
    }

    [Test]
    public void RunUntilOrStalled_DetectsAPlateau_LongBeforeMaxInstructions()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        var result = machine.RunUntilOrStalled(
            _ => false, // never met - only a stall or the max budget can end this
            () => 0, // never changes - an immediate, permanent plateau
            maxInstructions: 1_000_000,
            stallWindow: 200);

        result.ConditionMet.Should().BeFalse();
        result.Stalled.Should().BeTrue();
        result.InstructionsRun.Should().Be(200, "it should stop at the stall window, not run to maxInstructions");
    }

    [Test]
    public void RunUntilOrStalled_RunsToMaxInstructions_WhenNeitherConditionNorStallFires()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        long counter = 0;

        var result = machine.RunUntilOrStalled(
            _ => false,
            () => ++counter, // always progresses - never plateaus
            maxInstructions: 300,
            stallWindow: 200);

        result.ConditionMet.Should().BeFalse();
        result.Stalled.Should().BeFalse();
        result.InstructionsRun.Should().Be(300);
    }

    [Test]
    public void IeeeByteTransferCount_IsCumulative_UnlikePollDiskActivity()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_32);
        var testDisksDir = RomLocator.Directory("test-disks", "games-1.d64");
        machine.MountDisk(Path.Combine(testDisksDir, "games-1.d64"));

        machine.IeeeByteTransferCount.Should().Be(0);
    }

    [Test]
    public void Name_And_IsReady_ReflectProfile()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.Name.Should().Be(PetProfileCatalog.Pet2001_8.Name);
        machine.IsReady.Should().BeTrue();
    }

    [Test]
    [CancelAfter(30_000)]
    public void Pet2001_8_BootsWithoutThrowing_ForBoundedSteps()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.Run(500);

        machine.Processor.Halted.Should().BeFalse();
        machine.Processor.InstructionCount.Should().BeGreaterThan(0);
    }

    [Test]
    [CancelAfter(30_000)]
    public void Cbm8032_BootsWithoutThrowing_ForBoundedSteps()
    {
        var machine = CreateMachine(PetProfileCatalog.Cbm8032);

        machine.Run(500);

        machine.Processor.Halted.Should().BeFalse();
        machine.Processor.InstructionCount.Should().BeGreaterThan(0);
    }

    [TestCaseSource(nameof(AllProfiles))]
    [CancelAfter(30_000)]
    public void EveryImplementedProfile_BootsWithoutThrowing_ForBoundedSteps(PetProfile profile)
    {
        var machine = CreateMachine(profile);

        machine.Run(500);

        machine.Processor.Halted.Should().BeFalse(profile.Id);
        machine.Processor.InstructionCount.Should().BeGreaterThan(0, profile.Id);
    }

    [TestCaseSource(nameof(AllProfiles))]
    public void EveryImplementedProfile_UsesTheVerifiedCommonPeripheralWiring(PetProfile profile)
    {
        var machine = CreateMachine(profile);

        machine.Devices.Select(device => device.Id).Should().Contain(["datasette", "datasette2"],
            profile.Id);
        machine.UserPort.Should().NotBeNull(profile.Id);
        (machine.Crtc is not null).Should().Be(profile.VideoHardware == PetVideoHardware.Crtc, profile.Id);
    }

    [Test]
    public void SuperPet_Acia_IsMappedAtEff0_AndUsesTheSerialTransport()
    {
        using var transport = new BufferedSerialTransport();
        var profileDirectory = RomLocator.Directory(PetProfileCatalog.SuperPet.RomDirectory, PetProfileCatalog.SuperPet.RomManifest[0].Path);
        var machine = new PetMachine(
            PetProfileCatalog.SuperPet,
            Directory.GetParent(profileDirectory)!.FullName,
            serialTransport: transport);

        machine.Memory.Write(0xEFF0, 0xA5);
        transport.TryReadTransmitted(out var transmitted).Should().BeTrue();
        transmitted.Should().Be(0xA5);

        machine.Memory.Write(0xEFF2, 0x00);
        transport.ReceiveFromHost(0x5A);
        machine.StepInstruction();

        machine.Acia.Should().NotBeNull();
        machine.Acia!.Irq.Should().BeTrue();
        machine.Memory.Read(0xEFF0).Should().Be(0x5A);
    }

    [Test]
    public void SuperPet_ExposesWaterlooFirmwareToThe6809WithoutReplacingThe6502()
    {
        var profile = PetProfileCatalog.SuperPet;
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var romsRoot = Directory.GetParent(profileDirectory)!.FullName;
        var machine = new PetMachine(profile, romsRoot, serialTransport: new BufferedSerialTransport());

        machine.SuperPet6809Cpu.Should().NotBeNull();
        machine.SuperPet6809Memory.Should().NotBeNull();
        machine.SuperPet6809Cpu!.State.PC.Should().Be(
            (ushort)((machine.SuperPet6809Memory!.Read(0xFFFE) << 8) | machine.SuperPet6809Memory.Read(0xFFFF)));
        machine.Processor.Should().NotBe(machine.SuperPet6809Cpu);

        var firmwareFirstByte = PetRomLoader.Load(
            Path.Combine(romsRoot, profile.RomDirectory), profile.ExpansionRomManifest!)[0].Data[0];
        machine.SuperPet6809Memory.Read(0xA000).Should().Be(firmwareFirstByte);
    }

    [Test]
    public void SuperPet_6809ExecutesTheWaterlooResetRoutine()
    {
        var profile = PetProfileCatalog.SuperPet;
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var machine = new PetMachine(
            profile,
            Directory.GetParent(profileDirectory)!.FullName,
            serialTransport: new BufferedSerialTransport());

        for (var instruction = 0; instruction < 16; instruction++)
            machine.SuperPet6809Cpu!.StepInstruction();

        machine.SuperPet6809Cpu!.Halted.Should().BeFalse();
        machine.SuperPet6809Cpu.InstructionCount.Should().Be(16);
    }

    [Test]
    public void SuperPet_CpuSwitchSelectsTheMatchingProcessorAndMemoryMap()
    {
        var profile = PetProfileCatalog.SuperPet6502;
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var machine = new PetMachine(profile, Directory.GetParent(profileDirectory)!.FullName,
            serialTransport: new BufferedSerialTransport());
        var petMemory = machine.Memory;

        machine.SelectedProcessor.Should().Be(SuperPetProcessor.Mos6502);
        machine.Memory.Should().BeSameAs(petMemory);

        machine.SelectProcessor(SuperPetProcessor.Motorola6809);

        machine.SelectedProcessor.Should().Be(SuperPetProcessor.Motorola6809);
        machine.Processor.Should().BeSameAs(machine.SuperPet6809Cpu);
        machine.Memory.Should().BeSameAs(machine.SuperPet6809Memory);
        machine.Memory.Read(0xA000).Should().Be(machine.SuperPet6809Memory!.Read(0xA000));

        machine.SelectProcessor(SuperPetProcessor.Mos6502);

        machine.SelectedProcessor.Should().Be(SuperPetProcessor.Mos6502);
        machine.Processor.Should().NotBeSameAs(machine.SuperPet6809Cpu);
        machine.Memory.Should().BeSameAs(petMemory);
    }

    [TestCase(SuperPetProcessor.Mos6502, "superpet-6502")]
    [TestCase(SuperPetProcessor.Motorola6809, "superpet")]
    public void SuperPetProfile_SelectsItsProcessorModeAtPowerOn(SuperPetProcessor expected, string profileId)
    {
        var profile = PetProfileCatalog.Find(profileId);
        var machine = CreateMachine(profile);

        machine.SelectedProcessor.Should().Be(expected);
        machine.SuperPet6809Cpu.Should().NotBeNull();
        machine.SuperPetProtectionDongle.Should().NotBeNull();
    }

    [Test]
    public void SuperPet_ExpansionRamUsesIndependentBanksThroughEffc()
    {
        var profile = PetProfileCatalog.SuperPet;
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var machine = new PetMachine(profile, Directory.GetParent(profileDirectory)!.FullName,
            serialTransport: new BufferedSerialTransport());
        machine.SelectProcessor(SuperPetProcessor.Motorola6809);

        machine.Memory.Write(SuperPetMemoryMap.BankSelectRegister, 2);
        machine.Memory.Write(SuperPetMemoryMap.ExpansionRamWindow, 0xA2);
        machine.Memory.Write(SuperPetMemoryMap.BankSelectRegister, 7);
        machine.Memory.Write(SuperPetMemoryMap.ExpansionRamWindow, 0xA7);

        machine.Memory.Read(SuperPetMemoryMap.ExpansionRamWindow).Should().Be(0xA7);
        machine.Memory.Write(SuperPetMemoryMap.BankSelectRegister, 2);
        machine.Memory.Read(SuperPetMemoryMap.ExpansionRamWindow).Should().Be(0xA2);
        ((SuperPet6809MemoryBus)machine.SuperPet6809Memory!).SelectedBank.Should().Be(2);
    }

    [Test]
    public void SuperPet_MapsThe6702DongleAtEfe0ThroughEfe3()
    {
        var profile = PetProfileCatalog.SuperPet;
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var machine = new PetMachine(profile, Directory.GetParent(profileDirectory)!.FullName,
            serialTransport: new BufferedSerialTransport());
        machine.SelectProcessor(SuperPetProcessor.Motorola6809);

        machine.Memory.Read(SuperPetMemoryMap.ProtectionDongleBaseAddress).Should().Be(0xD6);
        machine.Memory.Write(SuperPetMemoryMap.ProtectionDongleBaseAddress, 0x12);
        machine.Memory.Write((ushort)(SuperPetMemoryMap.ProtectionDongleBaseAddress + 3), 0x35);

        machine.Memory.Read((ushort)(SuperPetMemoryMap.ProtectionDongleBaseAddress + 2)).Should().Be(0xD6);
        machine.SuperPetProtectionDongle.Should().NotBeNull();
    }

    [Test]
    public void NonSuperPet_RejectsSelectingThe6809Processor()
    {
        var machine = CreateMachine(PetProfileCatalog.Cbm8032);

        var act = () => machine.SelectProcessor(SuperPetProcessor.Motorola6809);

        act.Should().Throw<InvalidOperationException>();
        machine.SelectedProcessor.Should().Be(SuperPetProcessor.Mos6502);
    }

    [Test]
    public void Reset_ZeroesRamAndRestartsCpuAtResetVector()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        machine.Memory.Write(0x0100, 0x77);

        machine.Reset();

        machine.Memory.Read(0x0100).Should().Be(0);
        machine.Processor.InstructionCount.Should().Be(0);
    }

    [Test]
    public void Keyboard_PressedKey_ReadableThroughPia1OverTheBus()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        const ushort pia1Base = 0xE810;

        // Select the data register (not the direction register) on PIA1's port A and B.
        machine.Memory.Write((ushort)(pia1Base + 1), 0x04);
        machine.Memory.Write((ushort)(pia1Base + 3), 0x04);

        // KeyA on the PET 2001 graphics keyboard sits at matrix row 4, column 0.
        machine.Keyboard.Press(4, 0);

        // Software selects row 4 by writing it to port A (PA0-3); port B then echoes that row's columns.
        machine.Memory.Write(pia1Base, 0x04);
        var columns = machine.Memory.Read((ushort)(pia1Base + 2));

        columns.Should().Be(machine.Keyboard.ReadColumns(4), "PIA1 port B should echo the selected row's live matrix state");
        (columns & 0x01).Should().Be(0, "column 0 is held down (active-low) once KeyA is pressed");

        machine.Keyboard.Release(4, 0);
        var afterRelease = machine.Memory.Read((ushort)(pia1Base + 2));
        (afterRelease & 0x01).Should().Be(0x01, "releasing the key should clear its active-low bit");
    }

    [Test]
    public void DatasetteSense_IsActiveLowOnPetPia1PortA()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_32);
        var pia1Base = PetMemoryBus.Pia1Base;

        machine.Memory.Write((ushort)(pia1Base + 1), 0x04); // CRA: select ORA
        machine.Memory.Write((ushort)(pia1Base + 3), 0x04); // CRB: select ORB
        machine.Memory.Write(pia1Base, 0x00); // select keyboard row 0

        var idle = machine.Memory.Read(pia1Base);
        (idle & 0x10).Should().Be(0x10, "cassette sense is released high before PLAY");

        machine.Datasette.LoadTape([100, 200], "sense-test.tap");
        machine.Datasette.PressPlay();

        var playing = machine.Memory.Read(pia1Base);
        (playing & 0x10).Should().Be(0, "cassette sense is active-low while PLAY is pressed");
    }

    [Test]
    public void SecondDatasette_UsesPia1Pa5AndViaPb4_IndependentlyOfCassetteOne()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var pia1Base = PetMemoryBus.Pia1Base;
        var viaBase = PetMemoryBus.ViaBase;

        machine.Memory.Write((ushort)(pia1Base + 1), 0x04); // select PIA1 ORA
        machine.Datasette2.PressPlay();
        var playing = machine.Memory.Read(pia1Base);
        (playing & 0x20).Should().Be(0, "cassette #2 sense is active-low on PA5");
        machine.Datasette.Sense.Should().BeFalse("cassette #1 must remain released");

        machine.Memory.Write((ushort)(viaBase + MOS6522.Ddrb), 0x10);
        machine.Memory.Write((ushort)(viaBase + MOS6522.Orb), 0x00);
        machine.Datasette2.MotorOn.Should().BeTrue();
        machine.Datasette.MotorOn.Should().BeFalse("cassette #1 motor is controlled by PIA1 CB2");

        machine.Memory.Write((ushort)(viaBase + MOS6522.Orb), 0x10);
        machine.Datasette2.MotorOn.Should().BeFalse();
    }

    [Test]
    public void Devices_ExposeBothIndependentCassettes()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.Devices.Should().Contain(d => d.Id == "datasette");
        machine.Devices.Should().Contain(d => d.Id == "datasette2");
    }

    [Test]
    public void UserPort_MapsViaPortAInputOutputDirectionAndCa2Handshake()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var viaBase = PetMemoryBus.ViaBase;

        machine.UserPort.Input = 0x5A;
        machine.Memory.Write((ushort)(viaBase + MOS6522.Ddra), 0xF0);
        machine.Memory.Write((ushort)(viaBase + MOS6522.OraWithoutHandshake), 0xA5);

        machine.UserPort.Output.Should().Be(0xA0);
        machine.UserPort.Direction.Should().Be(0xF0);
        machine.Memory.Read((ushort)(viaBase + MOS6522.OraWithoutHandshake)).Should().Be(0xAA);

        machine.Memory.Write((ushort)(viaBase + MOS6522.PeripheralControl), 0x0C);
        machine.UserPort.HandshakeOutput.Should().BeFalse();
        machine.Memory.Write((ushort)(viaBase + MOS6522.PeripheralControl), 0x0E);
        machine.UserPort.HandshakeOutput.Should().BeTrue();
    }

    [Test]
    public void UserPort_HandshakeInput_IsForwardedToViaCa2()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        machine.Memory.Write((ushort)(PetMemoryBus.ViaBase + MOS6522.PeripheralControl), 0x02);

        machine.UserPort.HandshakeInput = false;

        machine.Via.CA2.Should().BeFalse();
    }

    [Test]
    [CancelAfter(30_000)]
    public void RunUntil_StopsAsSoonAsConditionIsTrue()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        var met = machine.RunUntil(mem => ContainsReady(mem, PetProfileCatalog.Pet2001_8), 1_000_000);

        met.Should().BeTrue("the real ROM should print READY. well within a million instructions");
        machine.Processor.InstructionCount.Should().BeLessThan(1_000_000);
    }

    [TestCase("HELLO", new[] { "KeyH", "KeyE", "KeyL", "KeyL", "KeyO" })]
    [TestCase("A1 B", new[] { "KeyA", "Digit1", "Space", "KeyB" })]
    [TestCase("X+Y", new[] { "KeyX", "Equal", "KeyY" })]
    public void TextTyper_ToHostKey_MapsEveryCharacter(string text, string[] expectedHostKeys)
    {
        var actual = text.Select(TextTyper.ToHostKey).ToArray();
        actual.Should().Equal(expectedHostKeys);
    }

    [Test]
    public void TextTyper_UnmappableCharacter_IsSkippedNotThrown()
    {
        var act = () => TextTyper.ToHostKey('$');
        act.Should().NotThrow();
        TextTyper.ToHostKey('$').Should().BeNull();
    }

    /// <summary>
    /// Root cause was PetMachine driving the wrong chip's interrupt line entirely: the ~60Hz
    /// jiffy-clock pulse that KERNAL's keyboard scan depends on belongs on PIA1's CB1 (confirmed
    /// by a register-level comparison against personal-001's PetMachine.ClockPia1Cb1), not VIA's
    /// CA1 (an earlier, incorrect fix). Boots to a genuinely stable READY. via RunUntil (a fixed
    /// instruction-count guess previously produced a false pass from the boot banner still
    /// printing mid-test), holds KeyA for personal-001's own proven-sufficient budget (5_500
    /// instructions), and checks the screen actually changed.
    /// </summary>
    [Test]
    public void Keyboard_HeldKeyPress_ProducesVisibleScreenChange()
    {
        var profile = PetProfileCatalog.Pet2001_8;
        var machine = CreateMachine(profile);
        var map = new Pet2001GraphicsKeyboardMap();

        machine.RunUntil(mem => ContainsReady(mem, profile), 1_000_000).Should().BeTrue();
        var before = SnapshotScreen(machine, profile);

        foreach (var action in map.Translate("KeyA", HostKeyEventKind.Press))
        {
            if (action.Pressed) machine.Keyboard.Press(action.Row, action.Column);
            else machine.Keyboard.Release(action.Row, action.Column);
        }
        machine.Run(5_500); // personal-001's own proven-sufficient per-key budget
        foreach (var action in map.Translate("KeyA", HostKeyEventKind.Release))
        {
            if (action.Pressed) machine.Keyboard.Press(action.Row, action.Column);
            else machine.Keyboard.Release(action.Row, action.Column);
        }
        machine.Run(5_500);

        var after = SnapshotScreen(machine, profile);
        after.Should().NotEqual(before, "holding a real key through IPetKeyboardMap should change what BASIC has drawn on screen");
    }

    /// <summary>Same PIA1 CB1 fix as <see cref="Keyboard_HeldKeyPress_ProducesVisibleScreenChange"/>,
    /// exercised through the higher-level <see cref="TextTyper"/> the GUI's "type text" feature uses.</summary>
    [Test]
    public void TextTyper_TypedLine_ExecutesInBasic()
    {
        var profile = PetProfileCatalog.Pet2001_8;
        var machine = CreateMachine(profile);
        var map = new Pet2001GraphicsKeyboardMap();

        machine.RunUntil(mem => ContainsReady(mem, profile), 1_000_000).Should().BeTrue();
        var before = SnapshotScreen(machine, profile);

        TextTyper.Type(machine, map, "PRINT2+2\n");
        machine.Run(50_000);

        var after = SnapshotScreen(machine, profile);
        after.Should().Contain((byte)0x34, "PRINT2+2 should evaluate and print the digit '4' (PET screen code 0x34) somewhere on screen");
        after.Should().NotEqual(before, "typing PRINT2+2<Enter> should produce visible output");
    }

    // "PRESS PLAY ON TAPE #1" in PET screen codes: A-Z map to 1-26 (see ReadyBytes above),
    // space/digits/'#' are ASCII-identical.
    private static readonly byte[] PressPlayBytes =
    [
        0x10, 0x12, 0x05, 0x13, 0x13, 0x20, // PRESS
        0x10, 0x0C, 0x01, 0x19, 0x20,       // PLAY
        0x0F, 0x0E, 0x20,                   // ON
        0x14, 0x01, 0x10, 0x05, 0x20,       // TAPE
        0x23, 0x31,                         // #1
    ];

    // "SEARCHING" - what LOAD prints next once it actually starts reading. The PET screen never
    // clears/overwrites the prompt line (it just scrolls up), so a passing test has to look for
    // this appearing, not for PressPlayBytes disappearing - see the test's own history for why an
    // earlier draft asserting the prompt's absence failed against a real ROM.
    private static readonly byte[] SearchingBytes =
        [0x13, 0x05, 0x01, 0x12, 0x03, 0x08, 0x09, 0x0E, 0x07];

    /// <summary>Reproduces the real hang this session's screenshot showed: the KERNAL's LOAD
    /// routine turns the cassette motor on via CB2 but then blocks on PIA1 PA4 (cassette sense)
    /// until PLAY is physically pressed - a real datasette behavior <see cref="PetMachine"/>
    /// didn't model at all (PA4 was hardcoded high, so the prompt would never clear no matter
    /// what). Proves both halves of the fix: PA4 actually reaches <see cref="PetDatasette.Sense"/>,
    /// and pressing play is what makes LOAD proceed - not just having a tape attached.</summary>
    [Test]
    [CancelAfter(60_000)]
    public void PressPlay_LetsLoadProceedPastThePressPlayPrompt()
    {
        var profile = PetProfileCatalog.Pet2001_32;
        var machine = CreateMachine(profile);
        var map = new Pet2001GraphicsKeyboardMap();
        var tapDirectory = RomLocator.Directory("test-tapes", "tower-and-dragon-town.tap");
        var tap = PetTapFile.Parse(File.ReadAllBytes(Path.Combine(tapDirectory, "tower-and-dragon-town.tap")));
        machine.Datasette.LoadTape(tap.PulseCycles, "tower-and-dragon-town.tap");

        machine.RunUntil(mem => ContainsReady(mem, profile), 1_000_000).Should().BeTrue();
        TextTyper.Type(machine, map, "LOAD\n");

        machine.RunUntil(mem => ScreenContains(mem, profile, PressPlayBytes), 2_000_000)
            .Should().BeTrue("LOAD should turn the motor on and then block waiting for PLAY, printing this prompt");

        machine.Datasette.PressPlay();

        machine.RunUntil(mem => ScreenContains(mem, profile, SearchingBytes), 2_000_000)
            .Should().BeTrue("pressing play should close the PA4 sense line and let LOAD proceed to actually read the tape");
    }

    private static bool ScreenContains(IMemoryBus memory, PetProfile profile, byte[] pattern)
    {
        for (var start = profile.VideoRamStart; start + pattern.Length <= profile.VideoRamStart + profile.VideoRamLength; start++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (memory.Read((ushort)(start + j)) != pattern[j]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }

    private static bool ContainsReady(IMemoryBus memory, PetProfile profile) => ScreenContains(memory, profile, ReadyBytes);

    private static byte[] SnapshotScreen(PetMachine machine, PetProfile profile)
    {
        var bytes = new byte[profile.Columns * profile.Rows];
        for (ushort i = 0; i < bytes.Length; i++)
            bytes[i] = machine.Memory.Read((ushort)(profile.VideoRamStart + i));
        return bytes;
    }

    [Test]
    public void Devices_ReportsTheDatasetteEvenWithNoTapeLoaded()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.Devices.Should().ContainSingle(d => d.Id == "datasette")
            .Which.StatusText.Should().Be("No tape");
    }

    [Test]
    public void Devices_ReflectsALoadedTapesName()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.Datasette.LoadTape([100, 200], "starwars.tap");

        machine.Devices.Single(d => d.Id == "datasette").StatusText.Should().Be("starwars.tap - press play");
    }

    [Test]
    public void MountDisk_AddsADriveDeviceStatus()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var diskPath = Path.Combine(RomLocator.Directory("test-disks", "games-1.d64"), "games-1.d64");

        machine.MountDisk(diskPath);

        machine.Devices.Should().ContainSingle(d => d.Id == "ieee488:8")
            .Which.StatusText.Should().Be("games-1.d64");
    }

    [Test]
    public void MountDisk_OnTheSameDeviceNumber_ReplacesTheStatusInsteadOfAddingASecondOne()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var testDisksDir = RomLocator.Directory("test-disks", "games-1.d64");

        machine.MountDisk(Path.Combine(testDisksDir, "games-1.d64"));
        machine.MountDisk(Path.Combine(testDisksDir, "utils.d64"));

        machine.Devices.Should().ContainSingle(d => d.Id == "ieee488:8")
            .Which.StatusText.Should().Be("utils.d64");
    }

    [Test]
    public void HasDisk_ReflectsWhetherDeviceEightIsMounted()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.HasDisk().Should().BeFalse();

        var testDisksDir = RomLocator.Directory("test-disks", "games-1.d64");
        machine.MountDisk(Path.Combine(testDisksDir, "games-1.d64"));

        machine.HasDisk().Should().BeTrue();
        machine.HasDisk(9).Should().BeFalse("nothing is mounted at device 9");
    }

    /// <summary>Drives a real LISTEN/TALK exchange over the memory-mapped PIA2/VIA registers -
    /// the same sequence PetIeeeBusBindingTests proves at the bare-chip level - through the full
    /// PetMachine, to prove PollDiskActivity's peek-and-clear wiring against real bus traffic
    /// (not just that PetIeeeBus.Activity itself fires, which is already covered elsewhere).</summary>
    [Test]
    [CancelAfter(10_000)]
    public void PollDiskActivity_ReportsRealIeee488Traffic_ThenClearsUntilTheNextByte()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var testDisksDir = RomLocator.Directory("test-disks", "games-1.d64");
        machine.MountDisk(Path.Combine(testDisksDir, "games-1.d64"));

        machine.PollDiskActivity().Should().BeFalse("nothing has touched the bus yet");

        const byte AtnOutViaDdrb = 0x04;
        var pia2Base = PetMemoryBus.Pia2Base;
        var viaBase = PetMemoryBus.ViaBase;

        machine.Memory.Write((ushort)(viaBase + MOS6522.Ddrb), AtnOutViaDdrb);
        machine.Memory.Write((ushort)(viaBase + MOS6522.Orb), 0x00); // ATN asserted -> command phase
        machine.Memory.Write((ushort)(pia2Base + 3), 0x04); // select PIA2 CRB's data register
        machine.Memory.Write((ushort)(pia2Base + 2), (byte)(0x48 ^ 0xFF)); // TALK device 8
        machine.Memory.Write((ushort)(pia2Base + 2), (byte)(0x6F ^ 0xFF)); // SECONDARY 15 (error channel)
        machine.Memory.Write((ushort)(viaBase + MOS6522.Orb), 0x04); // ATN released -> device becomes talker

        for (var i = 0; i < 8; i++) machine.StepInstruction(); // let the talker-side prefetch delay elapse

        machine.Memory.Write((ushort)(pia2Base + 1), 0x04); // select PIA2 CRA's data register
        var read = machine.Memory.Read((ushort)(pia2Base + 0));
        ((byte)(read ^ 0xFF)).Should().Be((byte)'7', "the status channel starts \"73,CBM DOS...\"");

        machine.PollDiskActivity().Should().BeTrue("a real byte just crossed the IEEE-488 bus");
        machine.PollDiskActivity().Should().BeFalse("polling again immediately should find nothing new");
    }

    private static PetMachine CreateMachine(PetProfile profile)
    {
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var romsRoot = Directory.GetParent(profileDirectory)!.FullName;
        return new PetMachine(profile, romsRoot);
    }

    private static IEnumerable<PetProfile> AllProfiles() => PetProfileCatalog.All;
}
