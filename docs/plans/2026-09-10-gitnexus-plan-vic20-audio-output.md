# GitNexus Engineering Plan

> Task: Add VIC-20 audio output - MOS6560's built-in 3-oscillator + noise square-wave generator, played through PetEmulator.Audio's IAudioOutput/AudioOutputFactory (verified working on this WSL host, docs/desktop/audio-wsl-backend.md).
> Evidence verified at commit e20c105667b9cbd8f6f0bdb3caad868b7ccfca95; GitNexus index refreshed this session (--index-only, no --pdg) after impact() showed it 7 commits behind - re-read after refresh.
> Evidence provenance schema 2; global dirty digest sha256:ff5012ae903afa6e767a06a933ee16792830d5e71a096722a8d9bbaeced73d12; cited-path manifest 9 sorted entries; exact generated plan path excluded.

## Objective (§1)

Give `MOS6560` (the VIC-20's video *and* sound chip) a working `IAudioSource`
implementation, and have `Vic20MachineViewModel` play it through the
already-verified `PetEmulator.Audio` backend - so a booted VIC-20 running a
real program that pokes its sound registers is actually audible, the same way
`docs/desktop/audio-wsl-backend.md`'s own sine-wave test was heard on this machine.

## Current Behaviour (§2-3) - architecture folded in

**MOS6560 today** [verified, `lib/PetEmulator.Chips/MOS6560.cs:1-123`]: 16
memory-mapped registers (`_registers` byte array), raster-line counter driven
by `Tick(ulong cycles)`, video-only decode properties (`Columns`/`Rows`/
`ScreenAddr`/etc., `AuxColor`/`Volume`-adjacent nibbles at `_registers[0x0E]`/
`[0x0F]` already decoded for *video* color, not sound). The class doc says so
explicitly: "no audio oscillators (nothing in 'boot to BASIC READY + render
text' needs them - a documented gap, not a guess)". `Write(ushort, byte)`
already latches any register value generically (line 95-99) - registers
$900A-$900E a sound implementation needs are already stored, just never read
for audio.

**Real hardware model to port** [external prior art, not part of this repo -
`personal-002/lib/Chips/MosTechnology/Vic6560/Vic6560Chip.cs:70-219`, fully
read this session, same donor lineage (cpu-vibe-006) as this repo's own VIC-20
port]:

- Osc1/2/3: reg `$900A`/`$900B`/`$900C`, bit7 = enable, bits0-6 = 7-bit raw
  frequency; dividers 256/128/64 respectively.
- Noise: reg `$900D`, bit7 = enable, bits0-6 = raw freq, divider 32; a 16-bit
  LFSR clocked when its own phase accumulator overflows, taps `bit14^bit13`.
- Volume: reg `$900E` **low nibble** (this repo's `AuxColor` already reads
  the *high* nibble of the same register for video - the two coexist on one
  register, confirmed no conflict).
- `CalcFreq(raw, divider) = Phi2Ntsc / divider / (255 - raw + 1)` -
  `Phi2Ntsc` already exists in this repo's `MOS6560` (`MOS6560.cs:18`),
  identical constant.
- Continuous-time phase accumulators (`double[4]`, seconds), advanced by
  `dt = 1/SampleRate` inside `Render`, **not** by `Tick(cycles)`/CPU cycles -
  the donor's own `Render` loop (`Vic6560Chip.cs:181-191`) calls
  `AdvanceOscillators(dt)` + samples per output frame, entirely decoupled
  from the raster/CPU tick cadence this repo's `MOS6560.Tick` already owns.
  **This means zero changes to `Tick`, `Read`, `Write`, or raster logic.**

**PetEmulator.Audio today** [verified, all three files fully read this
session]: `IAudioSource` (`Format` + `Render(Span<AudioFrame>)`,
`AudioContracts.cs`), `IAudioOutput`/`AudioOutputFactory.CreateDefault()`
(`PulseAudioSink`/`AudioOutputFactory.cs`) - real-playback-verified on this
exact host (`docs/desktop/audio-wsl-backend.md`'s "Verification performed on this
host" section, captured `RDPSink.monitor` independently). Zero changes needed
there; this task is purely "implement `IAudioSource`, then call `Start`".

**Vic20Machine today** [verified, `src/PetEmulator.Vic20/Vic20Machine.cs`,
re-read this session - file is currently mid-edit by unrelated, uncommitted
disk work in this working tree (IEC serial bus), reflected accurately below]:
`Vic => _vic` already public (line 94), `StepInstruction()` (lines 167-186)
ticks `_vic.Tick(cycles)` for video/raster only. **No change needed here** -
confirmed by the current-behavior finding above (audio is self-clocked by
sample rate, not CPU cycles).

**Vic20MachineViewModel today** [verified,
`src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs`]: constructor
(lines 52-63) builds `_machine`/`_display`; `Dispose()` (line 145) is
currently an empty no-op - a clean, already-existing hook for `Stop()`.

## Findings (§4-5) - only load-bearing, each tagged + tool-named

- `impact({target:"MOS6560", direction:"upstream"})`: **risk CRITICAL**, 53
  impacted, d=1: 24 (incl. `Vic20Machine` ctor, `Vic20MachineViewModel` ctor,
  9 `MOS6560Tests` methods, `Vic20DebuggerSession`, several
  `Vic20*BootTests`/`Vic20MemoryBusTests`). Entirely expected for a
  shared, widely-consumed chip - **every d=1 item stays green under an
  additive-only change** (new interface implementation, new members; no
  existing signature touched). Index was 7 commits behind at call time
  (unrelated concurrent disk-feature commits) - refreshed via
  `analyze --index-only` before relying on this list, per this plan's
  `freshness:accept`-but-load-bearing escalation rule.
- `lib/PetEmulator.Chips/PetEmulator.Chips.csproj` [verified, read this
  session]: references only `PetEmulator.Core` today - adding
  `PetEmulator.Audio` is a new, zero-dependency-of-its-own reference (Audio
  itself only references Core transitively via nothing - it's dependency-free
  per its own `.csproj`).
- `tests/PetEmulator.Chips.Tests/MOS6560Tests.cs` [verified, read this
  session, 90 lines]: NUnit/FluentAssertions, `PetEmulator.Chips.Tests`
  namespace - existing extension point for oscillator/noise/volume tests.

## Proposed Changes (§6)

1. **`lib/PetEmulator.Chips/PetEmulator.Chips.csproj`**: add
   `<ProjectReference Include="../PetEmulator.Audio/PetEmulator.Audio.csproj" />`.
2. **`lib/PetEmulator.Chips/MOS6560.cs`**: add `using PetEmulator.Audio;`,
   `: IAudioSource` to the class declaration, and:
   - `SampleRate` (double, default 44100 - mirrors donor's own property name/
     default) + `Format => new((uint)Math.Round(SampleRate), 2)` (2 channels,
     mirrors this repo's `PulseAudioSink`'s stereo-capable `AudioFrame`
     shape - mono would also work since `AudioFrame.Right` is defined as
     ignorable by a mono format per `AudioContracts.cs`, but stereo matches
     the donor and needs no extra decision).
   - Osc1/2/3 enable+freq+`Osc*Frequency` properties, `NoiseEnabled`/
     `NoiseFrequency`, private `CalcFreq` - same shape as the donor
     (§2-3), reading this class's own existing `_registers[]` array (no new
     storage needed beyond the 4 phase-accumulator doubles + 1 LFSR ushort
     the donor also needs).
   - `Render(Span<AudioFrame>)`: per-frame `dt = 1.0/SampleRate`, advance
     enabled oscillators'/noise's phase, mix (sum enabled generators / count,
     scaled by `Volume/15`), write `new AudioFrame(sample, sample)`.
   - `Reset()` (existing method, `MOS6560.cs:101-106`): add clearing the 4
     phase accumulators + LFSR reset to its non-zero seed value (donor:
     `0xFFFF` - an all-zero LFSR would lock up, same class of bug this
     repo's own AY-analysis flagged for a different chip's noise generator).
3. **`src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs`**: add a
   `private readonly PetEmulator.Audio.IAudioOutput _audioOutput` field,
   `AudioOutputFactory.CreateDefault()` in the constructor (after `_display`
   is built, alongside the existing `_machine`/`_display` setup), then
   `_audioOutput.Start(_machine.Vic)`. `Dispose()` becomes
   `_audioOutput.Dispose()` (which internally calls `Stop()` - see
   `PulseAudioSink.Dispose`). No UI/mute control in this pass (YAGNI - the
   task is "have sound", not "control sound"; a volume/mute toggle is a
   natural §12 follow-up, not blocking this).

## Implementation Sequence (§7)

1. Add the `PetEmulator.Chips` -> `PetEmulator.Audio` project reference.
   Build to confirm no circular reference (Audio has zero project references
   of its own, so none possible).
2. Implement `IAudioSource` on `MOS6560` per §6.2. `impact()` on `MOS6560`
   already run and additive-safety confirmed (§4-5) - re-run only if this
   step ends up touching an existing signature (it shouldn't).
3. `MOS6560Tests.cs` additions (see §8) - oscillator frequency accuracy,
   silence when all disabled, volume scaling, noise non-silence. Run
   `dotnet test tests/PetEmulator.Chips.Tests` before touching the ViewModel.
4. Wire `Vic20MachineViewModel` per §6.3.
5. Manual/real verification: mirror `docs/desktop/audio-wsl-backend.md`'s own
   methodology - run the actual Desktop app (or a small script poking
   `$900A`/`$900E` directly through `MOS6560.Write`) and confirm audible
   sound, ideally cross-checked with a `parec`/`RDPSink.monitor` capture the
   same way that document did for the sink itself, not just "no exception."
6. `detect_changes({scope:"all"})`, full `dotnet test`, then update
   `MOS6560.cs`'s own class doc (currently says "no audio oscillators" - now
   false) and `docs/vic20/migration-plan.md`'s "Poza zakresem v1: ... audio"
   line before commit.

## Test Strategy (§8)

- `tests/PetEmulator.Chips.Tests/MOS6560Tests.cs` (extend): tone frequency
  accuracy for a known raw register value (mirrors
  `Ay38910ChipTests.RenderClockedProducesTheConfiguredToneFrequency`'s
  rising-edge-count technique, adapted to this chip's continuous-time
  `Render`); `Render` returns silence (all-zero frames) when Osc1-3 and
  noise are all disabled; volume register scales peak amplitude
  monotonically 0..15 (mirrors `Ay38910ChipTests.MeasuredVolumeCurveIs...`
  in spirit, simpler - no measured DAC curve here, linear `Volume/15`);
  noise-enabled produces non-silent, non-tonal output (peak-to-peak > 0,
  distinct from a pure square wave's exact ±amplitude values).
- Regression: full `dotnet test` - confirms zero behavior change to
  `MOS6560`'s existing video-register/raster tests (all 8 in `MOS6560Tests.cs`
  today, per §4-5) and to `PetEmulator.Audio`'s own suite.
- Verification commands: `dotnet build`, `dotnet test`,
  `node .gitnexus/run.cjs detect-changes --scope all --repo .`.

## Implementation Context (§11)

```yaml
implementation_context:
  task_summary: 'Implement IAudioSource on MOS6560 (VIC-20''s built-in 3-oscillator + noise square-wave sound, registers $900A-$900E), reusing PetEmulator.Audio''s already WSL-verified IAudioOutput/AudioOutputFactory, and start it from Vic20MachineViewModel.'
  acceptance_criteria:
    - 'MOS6560 implements PetEmulator.Audio.IAudioSource (Format + Render)'
    - 'Osc1/2/3 (regs $900A/$900B/$900C) and noise (reg $900D) each independently enable/disable and set frequency via their existing raw-register bits, matching CalcFreq(raw,divider)=Phi2Ntsc/divider/(255-raw+1) with dividers 256/128/64/32'
    - 'Volume (reg $900E low nibble) scales output amplitude 0..15, coexisting with the existing AuxColor (same register''s high nibble) with no conflict'
    - 'Vic20MachineViewModel starts audio via AudioOutputFactory.CreateDefault().Start(machine.Vic) and stops it in Dispose()'
    - 'Zero changes to MOS6560.Tick/Read/Write or Vic20Machine.StepInstruction (audio is self-clocked by sample rate, confirmed decoupled from CPU-cycle raster ticking)'
    - 'Full dotnet test stays green, including all existing MOS6560Tests and PetEmulator.Audio tests unchanged'

  evidence_provenance:
    schema_version: 2
    head_commit: 'e20c105667b9cbd8f6f0bdb3caad868b7ccfca95'
    generated_plan_path: 'docs/plans/2026-09-10-gitnexus-plan-vic20-audio-output.md'
    global_dirty_digest:
      algorithm: 'sha256'
      canonicalization: 'gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records'
      value: 'ff5012ae903afa6e767a06a933ee16792830d5e71a096722a8d9bbaeced73d12'
    cited_path_manifest:
      - path: 'docs/vic20/migration-plan.md'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:09d788c2cbc6c727c1c3e2a0a9cf7fb822487fc5151d2cf6f83b35f052ac02af'
        index_digest: 'sha256:09d788c2cbc6c727c1c3e2a0a9cf7fb822487fc5151d2cf6f83b35f052ac02af'
        worktree_digest: 'sha256:09d788c2cbc6c727c1c3e2a0a9cf7fb822487fc5151d2cf6f83b35f052ac02af'
        untracked_digest: absent
      - path: 'lib/PetEmulator.Audio/AudioContracts.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:b95b607ceb67cf49e37a7456cf0ac7825eefb5d18a31e57c63095d2e232d8533'
        index_digest: 'sha256:b95b607ceb67cf49e37a7456cf0ac7825eefb5d18a31e57c63095d2e232d8533'
        worktree_digest: 'sha256:b95b607ceb67cf49e37a7456cf0ac7825eefb5d18a31e57c63095d2e232d8533'
        untracked_digest: absent
      - path: 'lib/PetEmulator.Audio/AudioOutputFactory.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:839dacf41608aaa2ebed71eebbf9f29e09d42d4e9ca05d72ca49dd09e21fbad8'
        index_digest: 'sha256:839dacf41608aaa2ebed71eebbf9f29e09d42d4e9ca05d72ca49dd09e21fbad8'
        worktree_digest: 'sha256:839dacf41608aaa2ebed71eebbf9f29e09d42d4e9ca05d72ca49dd09e21fbad8'
        untracked_digest: absent
      - path: 'lib/PetEmulator.Audio/PulseAudioSink.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:4eb282e37c6105566b42802230abe5056ad35014eaf41f6db31c452fc3f00a22'
        index_digest: 'sha256:4eb282e37c6105566b42802230abe5056ad35014eaf41f6db31c452fc3f00a22'
        worktree_digest: 'sha256:4eb282e37c6105566b42802230abe5056ad35014eaf41f6db31c452fc3f00a22'
        untracked_digest: absent
      - path: 'lib/PetEmulator.Chips/MOS6560.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:a8e23982b81e9cd791358eaddac23d3e60474ea5826a473503bc2a76276eb291'
        index_digest: 'sha256:a8e23982b81e9cd791358eaddac23d3e60474ea5826a473503bc2a76276eb291'
        worktree_digest: 'sha256:a8e23982b81e9cd791358eaddac23d3e60474ea5826a473503bc2a76276eb291'
        untracked_digest: absent
      - path: 'lib/PetEmulator.Chips/PetEmulator.Chips.csproj'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:e1901faf2475c81a620619fa83935281a59b406fa5255d54916221ed498c0efb'
        index_digest: 'sha256:e1901faf2475c81a620619fa83935281a59b406fa5255d54916221ed498c0efb'
        worktree_digest: 'sha256:e1901faf2475c81a620619fa83935281a59b406fa5255d54916221ed498c0efb'
        untracked_digest: absent
      - path: 'src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:e94eda74bc4f55d0b63dcdb3a640efbb823770abf3323001dc51c4a5922e037e'
        index_digest: 'sha256:e94eda74bc4f55d0b63dcdb3a640efbb823770abf3323001dc51c4a5922e037e'
        worktree_digest: 'sha256:e94eda74bc4f55d0b63dcdb3a640efbb823770abf3323001dc51c4a5922e037e'
        untracked_digest: absent
      - path: 'src/PetEmulator.Vic20/Vic20Machine.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: unstaged
        rename_from: null
        rename_to: null
        head_digest: 'sha256:8c69ca980aa3157f81acd3a4ae1beab394784340d7ecce2acea127cfc6c5742d'
        index_digest: 'sha256:8c69ca980aa3157f81acd3a4ae1beab394784340d7ecce2acea127cfc6c5742d'
        worktree_digest: 'sha256:5791bdaea2fada08af4e93588a97778c43d3c2485dec1d53cd72087cd639375c'
        untracked_digest: absent
      - path: 'tests/PetEmulator.Chips.Tests/MOS6560Tests.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:77c1ac64f8258048f96df784b81cd3890f681ad04d4767de334d3accbf6f4be9'
        index_digest: 'sha256:77c1ac64f8258048f96df784b81cd3890f681ad04d4767de334d3accbf6f4be9'
        worktree_digest: 'sha256:77c1ac64f8258048f96df784b81cd3890f681ad04d4767de334d3accbf6f4be9'
        untracked_digest: absent

  files_to_modify:
    - file: 'lib/PetEmulator.Chips/PetEmulator.Chips.csproj'
      symbols: []
      intended_change: 'Add ProjectReference to lib/PetEmulator.Audio/PetEmulator.Audio.csproj'
    - file: 'lib/PetEmulator.Chips/MOS6560.cs'
      symbols: ['MOS6560']
      intended_change: 'Implement IAudioSource: Format/SampleRate, Osc1-3 + noise enable/frequency decode from existing _registers, phase accumulators, Render(), Reset() extension for the new state - additive only, no existing member signature changes'
    - file: 'src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs'
      symbols: ['Vic20MachineViewModel']
      intended_change: 'Add IAudioOutput field, AudioOutputFactory.CreateDefault().Start(machine.Vic) in the constructor, Stop/Dispose in Dispose()'
    - file: 'lib/PetEmulator.Chips/MOS6560.cs'
      symbols: []
      intended_change: "Update class doc comment - remove the now-false 'no audio oscillators' claim"
    - file: 'docs/vic20/migration-plan.md'
      symbols: []
      intended_change: 'Remove audio from the "Poza zakresem v1" exclusion list'

  tests:
    - file: 'tests/PetEmulator.Chips.Tests/MOS6560Tests.cs'
      scenarios:
        - 'Known raw Osc1 register value -> ToneHz-equivalent frequency within expected range (mirrors Ay38910ChipTests''s rising-edge-count technique)'
        - 'All oscillators and noise disabled -> Render produces all-zero (silent) frames'
        - 'Volume register 0 vs 15 -> peak amplitude scales monotonically, 0 is silent'
        - 'Noise enabled alone -> non-silent, non-periodic output distinct from a pure tone'

  verification_commands:
    - 'dotnet build'
    - 'dotnet test'
    - 'node .gitnexus/run.cjs detect-changes --scope all --repo .'

  risks:
    - 'MOS6560 is a CRITICAL-risk upstream target (impact(): 53 symbols, 24 direct callers) - must stay strictly additive; re-run impact() after the diff if any existing member''s signature changed.'
    - 'This repo''s Vic20Machine.cs is currently mid-edit (uncommitted IEC disk work in the same working tree) - re-verify it still builds/tests clean before starting this task if that work has landed more commits since this plan was written.'
    - 'Audio correctness (real square-wave timing/frequency accuracy) is only as good as the donor model reused here - no independent hardware-timing verification of the donor itself was done this session beyond reading its source; treat the frequency-accuracy test in §8 as the real gate, not the act of porting alone.'

  assumptions:
    - 'PetEmulator.Audio (lib/PetEmulator.Audio) needs zero changes to serve a second IAudioSource implementer - IAudioSource/IAudioOutput are already implementer-agnostic by design (re-verify: AudioContracts.cs has no MOS6560/chip-specific coupling, confirmed this session by full read).'
    - 'Stereo (2-channel) output for a source that is inherently mono (VIC-20 audio is a single mixed output on real hardware) is fine - AudioFrame''s Right field exists for exactly this and PulseAudioSink handles channels=2 already (used by this session''s own sine-wave verification test).'

  open_questions:
    - 'Whether Vic20DebuggerSession (CLI) should also start audio for its own real-KERNAL scripts, or whether audio stays Desktop-only for this pass - not addressed here, default to Desktop-only (YAGNI) unless the user asks for CLI audio specifically.'
    - 'No volume/mute UI control in this pass - explicitly deferred (see Proposed Changes §6.3''s own note), add if the user asks after hearing it play automatically feels wrong for e.g. a headless CLI run.'

  avoid:
    - 'Do not touch MOS6560.Tick/Read/Write or Vic20Machine.StepInstruction - audio here is proven self-clocked by sample rate, not CPU cycles (see Current Behaviour).'
    - 'Do not add a chip-specific coupling to PetEmulator.Audio (e.g. a MOS6560-shaped overload) - IAudioSource is already the right, implementer-agnostic seam.'
    - 'Do not repeat full repository discovery already captured in §2-5 above.'
```

## Assumptions and Open Questions (§12)

**Assumptions:** `PetEmulator.Audio` needs no changes (already
implementer-agnostic, confirmed by full read this session). Stereo output for
an inherently-mono chip is fine (`AudioFrame.Right` exists for this,
`PulseAudioSink` already handles 2-channel).

**Open questions:** Should the CLI (`Vic20DebuggerSession`) also get audio, or
stay Desktop-only for this pass? Default: Desktop-only (YAGNI). No mute/volume
UI in this pass - deferred, add only if requested after hearing it play.

**Explicitly deferred (not in scope):** Volume/mute UI control, CLI audio,
PAL-frequency audio (this repo is NTSC-only throughout, per
`docs/vic20/migration-plan.md`), any attempt at cycle-accurate (vs.
continuous-time-resampled) oscillator timing beyond what the donor model
already provides.

## Definition of Done (§13)

- `MOS6560` implements `IAudioSource`; `Render` produces correct-frequency,
  correctly-silenced-when-disabled, correctly-volume-scaled samples per §8's
  tests.
- `Vic20MachineViewModel` starts/stops audio via
  `AudioOutputFactory.CreateDefault()`.
- A real, unmodified VIC-20 program that pokes `$900A`-`$900E` produces
  audible sound when run through the Desktop app on this WSL host - ideally
  cross-checked with a real `parec`/`RDPSink.monitor` capture, mirroring
  `docs/desktop/audio-wsl-backend.md`'s own verification methodology, not just "the
  test passed."
- Full `dotnet test` green, including every existing `MOS6560Tests` case and
  the entire `PetEmulator.Audio` suite unchanged.
- `detect_changes({scope:"all"})` reviewed before commit; `MOS6560.cs`'s
  class doc and `docs/vic20/migration-plan.md` updated to drop the "no audio"
  claim.
