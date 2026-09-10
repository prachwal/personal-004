# Audio abstraction and the WSL host backend

This repo has no sound chip yet (VIC-20 v1 explicitly cut audio - see
`docs/vic20-migration-plan.md`'s "Czego NIE robić w v1"; PET's own hardware has
at best a PIA2 CB2 buzzer, not modeled either). `lib/PetEmulator.Audio/` exists
so a future SID/AY/PET-beeper implementation has a ready, WSL-proven place to
plug into, instead of re-deriving host audio output from scratch.

## What was gathered, and from where

Every sibling project under `~/source/emulators/` that actually got sound
working on this machine's real host (WSL2 + WSLg) was surveyed. None of their
code was copied into this repo directly (different projects, different
licenses/history) - this document distills what they proved, and
`lib/PetEmulator.Audio/PulseAudioSink.cs` reimplements the proven design.

| Project | What it has | Approach |
| --- | --- | --- |
| `personal-002/lib/Abstraction/Audio/AudioContracts.cs` | `IAudioSource`/`IAudioOutput`/`AudioFormat`/`AudioFrame` + speculative `IBitAudioSource`/`IDmaAudioSource`/`IAudioMixer` | The abstraction this repo's `IAudioSource`/`AudioFormat`/`AudioFrame` are ported from (trimmed - see below) |
| `personal-002/lib/Terminal.Avalonia/Audio/Sdl3AudioSink.cs` | SID6581 + other chips playing through it | SDL3 `SDL_OpenAudioDevice`/`SDL_AudioStream`, priming + backpressure sleep loop |
| `personal-003/src/Retro.Desktop/Audio/Sdl3AudioSink.cs` (name is legacy - no SDL3 calls in the file) | AY-3-8910 | Direct `libpulse-simple` P/Invoke, no SDL at all |
| `personal-003/TRS-80_COMPLETE_DOCUMENTATION/OtherMachines/19_MSX_AUDIO_CAPTURE_AND_WSLG_ANALYSIS.md` | - | The actual WSLg root-cause investigation (see below) |
| `personal-003/scripts/record-wsl-audio.sh` | - | `parec`-based capture of the real WSLg output, for verifying a fix actually reaches the speaker rather than trusting the emulator-side signal alone |
| `proc-vibe-001/src/ProcVibe.Gui/PulseAudio.cs` | PIT/SID-adjacent one-bit sources | Same direct-`libpulse-simple` approach, independently arrived at |

## The two backend options, and why this repo picked one

**SDL3's audio API** (`personal-002`) works, but WSL has a landmine: the
distro ships a pipewire *client* library with no pipewire *server* actually
running, so SDL3's default driver auto-probe silently selects pipewire and
opens a device that accepts writes and plays into nothing - no error, no
exception, just silence. The fix that project found was forcing the driver
before `SDL_Init`:

```csharp
if (OperatingSystem.IsLinux() && Sdl.GetEnv("SDL_AUDIO_DRIVER") is null)
    Sdl.SetEnv("SDL_AUDIO_DRIVER", "pulseaudio");
```

and critically, that has to go through a **native** `setenv()` (P/Invoke to
`libc`) - `Environment.SetEnvironmentVariable` from managed code does not
reach SDL3's own native `getenv()` call (confirmed by that project: managed
reads its own write back fine, SDL3 doesn't see it).

**Direct PulseAudio** (`personal-003`, `proc-vibe-001`) skips this class of bug
entirely by never going through SDL's driver selection at all. WSLg's actual
audio server *is* PulseAudio - `pactl list short sinks`/`sources` reports a
`RDPSink`/`RDPSink.monitor` pair, and `libpulse-simple`'s three-call API
(`pa_simple_new`/`pa_simple_write`/`pa_simple_free`) talks to it directly. This
repo's `PulseAudioSink` uses this approach: one fewer native dependency (no
`libSDL3.so` needed at all) and one fewer WSL-specific landmine to hit.

## The buffering lesson (from personal-003's MSX investigation)

A clean-sounding emulator signal can still crackle/stutter on real WSLg
playback, and the two failure modes look identical to the ear but have
different fixes and different diagnosis:

1. **Wrong samples** (bad chip emulation, e.g. the investigation's own
   `RenderClocked()` divisor bug producing half the correct frequency) -
   diagnosed by a **direct capture** that bypasses the whole audio backend
   (chip -> `WavWriter` -> file) and inspecting that WAV.
2. **Transport stalls** (WSLg/RDP scheduling jitter starving an undersized
   PulseAudio buffer) - a *correct* signal that still audibly crackles.
   Diagnosed by recording `RDPSink.monitor` (`scripts/record-wsl-audio.sh` in
   that project) - a clean monitor capture proves the samples reached
   PulseAudio correctly, which rules out (1) and narrows the fault to
   everything downstream of that point.

The fix for (2) was buffer sizing, not signal changes: PulseAudio's
`pa_buffer_attr` target length and prebuffer set to roughly **one second**,
minimum request to roughly **one quarter second** - thin enough to not add
noticeable latency, thick enough to absorb WSLg's scheduling jitter.
`PulseAudioSink` in this repo carries that sizing forward unchanged (see its
own doc comment).

**Takeaway for whoever wires a chip into `PulseAudioSink` next:** if playback
crackles, capture `RDPSink.monitor` with `parec` (or copy
`personal-003/scripts/record-wsl-audio.sh`) before touching the chip's sample
generation - most "audio bug" reports at this layer turn out to be transport,
not signal, and the direct-capture-vs-monitor-capture split is how to tell
which one you actually have.

## What this repo's abstraction deliberately leaves out

`personal-002`'s `AudioContracts.cs` also defines `IAudioOutput`,
`IBitAudioSource`, `IDmaAudioSource<TAddress>`, and `IAudioMixer`. None of
them had a real implementer in that project either (grep found no callers
beyond the interface declaration itself) - they were speculative shape for
chip kinds that project never actually got to. This repo's
`lib/PetEmulator.Audio/AudioContracts.cs` ports only `IAudioSource` +
`AudioFormat` + `AudioFrame` - the part that `PulseAudioSink` and a real
consumer chip actually need. Add the rest back only when a specific chip
needs it (a PET CB2 buzzer is genuinely edge-timed and would want something
`IBitAudioSource`-shaped; nothing in this repo needs that yet).

## How a future chip plugs in

1. Implement `PetEmulator.Audio.IAudioSource` on the chip (`Format` +
   `Render(Span<AudioFrame>)`), same shape `Ay38910Chip`/`Sid6581Chip` used in
   the surveyed projects.
2. `new PulseAudioSink().Start(chip)` from the machine's/ViewModel's audio
   lifecycle; `Stop()`/`Dispose()` on teardown. Check `LastError`/`IsPlaying`
   for UI feedback - `Start` never throws even with no PulseAudio server
   present (CI, non-WSL dev boxes), it just disables audio and logs why.
3. If it crackles on real WSLg playback, see the buffering lesson above
   before assuming the chip emulation is wrong.
