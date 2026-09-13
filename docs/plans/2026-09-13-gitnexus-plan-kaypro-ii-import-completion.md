# GitNexus Engineering Plan

> Task: import verified Kaypro II assets and plan the complete machine build
> Evidence verified at commit `a731cf1324eeec716b18c3af45f1252f17b8706a`; GitNexus index refreshed this session (`--index-only`), but the analyzer reports bounded C# flow truncation, so graph claims are lower-bound.
> Evidence provenance schema 2; global dirty digest and cited-path manifest are embedded in §11; the generated plan path is excluded.

## 1. Objective

Import the reusable Kaypro II ROM/font/disk assets and define an executable, test-first sequence that ends with a documented, selectable Kaypro II profile booting CP/M from the real monitor ROM to `A>`, with correct video, SIO/PIO, floppy timing, storage formats and CLI/Desktop integration.

The selected first target is the early `81-110A`/`81-111` board: Z80 ~2.5 MHz, `81-149C` monitor, `81-146` character ROM and FD1791-compatible floppy path. FD1793 remains a separate later-board profile, not an implicit substitute.

## 2. Current Behaviour

- [verified] `KayproMachine` composes `Z80Cpu`, `KayproBus`, shared Core contracts and exposes reset, instruction stepping and run (`src/PetEmulator.Kaypro/KayproMachine.cs:6-52`).
- [verified] `KayproBus` currently maps SIO `$04-$07`, FDC `$10-$13`, PIO-G `$08-$0B`, PIO-S/system `$1C-$1F`, ROM at `$0000-$07FF`, and a video window beginning at `$3000` (`src/PetEmulator.Kaypro/KayproBus.cs:8-143`).
- [verified] Current raw `KayproDiskImage` accepts only 40×10×512-byte images; there is no TD0 loader in the current Kaypro project (`src/PetEmulator.Kaypro/KayproDiskImage.cs:5-63`).
- [verified] Current video is a 2 KiB, 80×24 text buffer without a loaded Kaypro character ROM (`src/PetEmulator.Kaypro/KayproVideo.cs:3-33`).
- [verified] Z80 SIO/PIO are now independent chip implementations with Kaypro adapters; remaining work is machine wiring and real-guest validation (`src/PetEmulator.Kaypro/KayproSioWiring.cs:5-41`, `src/PetEmulator.Kaypro/KayproPioWiring.cs:5-117`).
- [verified] Current Kaypro tests cover contracts, basic mapping, SIO keyboard flow, PIO interrupt acknowledgement and raw disk geometry, but not real assets, TD0 or CP/M boot (`tests/PetEmulator.Kaypro.Tests/KayproMachineTests.cs:7-129`).
- [verified] `docs/kaypro/README.md` still lists ROM/font, TD0, real boot and CLI/Desktop as future work.

## 3. Relevant Architecture

- [verified] `PetEmulator.Kaypro` owns machine composition and adapter policy; `PetEmulator.Chips` owns generic FD1793, Z80 SIO and Z80 PIO contracts. Keep board-specific bits outside chip implementations.
- [verified] `KayproBus` is both `IBus` and `IMemoryBus`; changing its concrete mapping has interface-dispatch impact, so profile mapping should be introduced behind explicit machine/profile objects and preserved by map tests.
- [verified] The current solution already includes Kaypro source and test projects in `PetEmulator.slnx`; no current asset project or Kaypro CLI/Desktop application registration was found.
- [source-derived, previous checkout] `cpu-vibe-010` contains a working real-ROM/real-raw-disk boot path, CP/M filesystem helpers, Kaypro video/font adapters, GUI ViewModel and extensive Kaypro tests.
- [source-derived, previous checkout] `cpu-vibe-011` contains a separate early-board design, embedded ROM/font resources, a Kaypro motherboard, native video adapter and a `kpii-149.td0` test asset. Its implementation is a donor, not a drop-in because its Core and machine contracts differ.

## 4. GitNexus Findings

- [graph] `impact KayproMachine --direction upstream`: LOW lower-bound; direct callers are current Kaypro tests, with interface dispatch not fully resolved.
- [graph] `impact KayproBus --direction upstream`: MEDIUM lower-bound; direct constructor/test consumers are visible, but `IBus`/`IMemoryBus` dispatch may hide more callers.
- [graph] `impact KayproVideo --direction upstream`: HIGH exact; direct consumers include `KayproBus`, `KayproMachine` and current machine tests. This is the highest-risk migration boundary and must be changed behind compatibility-preserving tests.
- [graph] `impact KayproDiskImage --direction upstream`: LOW lower-bound; current direct consumer is the Kaypro machine test, while `IFD1793DiskImage` dispatch may hide additional consumers.
- [graph] `impact KayproPioWiring`: UNKNOWN/not indexed as a symbol; confirm all callers textually before changing it. Current `rg` shows `KayproBus` and Kaypro tests as consumers.
- [graph] `query 'KayproBus KayproMachine KayproVideo KayproDiskImage FD1793'`: current machine, bus, disk and FD1793 definitions are indexed, but process output is truncated; do not infer missing execution paths from absent graph flows.

## 5. Statement-Level PDG Findings

No usable PDG slice was run for this planning task. The refreshed analyzer reported candidate-flow caps and truncated process walks; therefore ordering and state-transfer constraints below are source-derived and must be rechecked during implementation.

Critical ordering constraints:

- [inferred] CPU memory writes must be flushed into the Kaypro bus before device-side video/FDC observations; reset must clear device state before the CPU starts at the monitor reset vector.
- [inferred] FDC DRQ/INTRQ/WAIT/NMI state must be latched until the guest acknowledges or reads the corresponding register; a host tick must not discard a pending guest-visible event.
- [inferred] ROM bank selection, hidden RAM under the monitor ROM and video address translation must be represented in one profile-aware bus, not by copying bytes into ordinary RAM.
- [inferred] SIO channel-B keyboard input must pass through the public SIO adapter and its timing/interrupt path; tests must not write internal chip state.

## 6. Proposed Changes

### 6.1 Asset import and provenance

| Source evidence | Destination | Validation | Use |
| --- | --- | --- | --- |
| `/home/prachwal/source/emulators/cpu-vibe-011/src/CpuVibe.CpmSystem.Frontend/Roms/kaypro-81-149c.bin` | `roms/kaypro-ii/81-149c.rom` | 2048 B; SHA-256 `5231e5dd0f6fb64a8ec50951269c248e00875906e391d044918e212d14b08f55`; archive provenance and legal status recorded | early monitor ROM |
| `/home/prachwal/source/emulators/cpu-vibe-011/src/CpuVibe.CpmSystem.Frontend/Roms/kaypro-81-146.bin` | `roms/fonts/kaypro-ii/81-146.bin` | 2048 B; SHA-256 `9df2034c2bf38ea218ae87c7c5669f689d3e70f86eaade8c51949d216936d777`; upper-half 128 glyph layout | early character ROM |
| `/home/prachwal/source/emulators/cpu-vibe-010/roms/fonts/kaypro-ii/81-146a.rom` | `roms/fonts/kaypro-ii/81-146a.rom` only if retained as a named alternate | 2048 B; SHA-256 `cde431c9e506edf11261f7e7d52fd60b1a43fe2d9ea0d3e06f271d4cab4884fa`; bytewise differs from `81-146` | documented alternate dump, never silently defaulted |
| `/home/prachwal/source/emulators/cpu-vibe-011/tests/CpuVibe.CpmSystem.Tests/Floppies/kpii-149.td0` | `tests/PetEmulator.Kaypro.Tests/Assets/kpii-149.td0` or a documented test-data location | 112024 B; SHA-256 `6cba0285fd22126970ff46ee19a35ec20576980627496becc5f69aca5b7d926f` | authentic boot fixture |
| `/home/prachwal/source/emulators/cpu-vibe-010/roms/disks/kaypro-ii/cpm22-rom149.dsk` | `tests/.../Assets/cpm22-rom149.dsk` if licensing policy permits | 204800 B; SHA-256 `1f3a95255d417175a2bec33a7e1deacebf04a744373df7648a00f045d0b941e6` | raw-path regression and TD0 cross-check |

Do not import private/proprietary dumps without a manifest stating source, size, hash, board/ROM pairing and redistribution status. If policy rejects binary distribution, keep hash-only manifests and require an external asset directory; tests must report an explicit skip, never a false pass.

### 6.2 Profile and machine boundary

- Add a `KayproIIHardwareProfile`/configuration describing board `81-110A`, ROM/font identity, FD1791-compatible controller, 80×25 native display, 40×1×10×512 media and all port/system-latch polarity.
- Keep `KayproBus` as the adapter boundary, but move magic constants into the profile and add map snapshots for memory, PIO-G, PIO-S, SIO and FDC.
- Add a separate FD1791 compatibility adapter or explicit profile mode over the shared controller only after register polarity, data-bus inversion and timing differences are tested. Do not label the current FD1793 path as FD1791-accurate.

### 6.3 Video/font

- Replace the current placeholder video with a profile-selected 80×25/128-byte virtual-row model and the actual Kaypro VRAM address translation required by the selected ROM.
- Add a font loader/decoder for the chosen 81-146 dump, active-low five-bit glyph rows, blink/attribute handling and deterministic 480×250 rendering.
- Preserve the existing `KayproVideo` API where feasible; if the shape changes, update `KayproBus`, machine tests and any future UI adapter together because GitNexus marks this boundary HIGH impact.

### 6.4 Storage and CP/M

- Keep raw `KayproDiskImage` for fixed geometry and add `KayproTd0Image` with strict TD0 header, track/sector records, normal and advanced-compressed LZHUF decoding, CRC/length validation and copy-on-write persistence.
- Expose one `IKayproDiskImage`/adapter contract to FD1791/FD1793 while preserving sector IDs and Kaypro geometry; do not alias Kaypro media to IBM 3740.
- Add CP/M directory/file helpers only as host-side inspection tools; the acceptance boot path must execute the guest CCP/BDOS/CBIOS, not substitute a host `DIR` implementation.

### 6.5 Device wiring and timing

- Connect PIO-G to printer/parallel signals and PIO-S to system port, drive select, density, motor, ready and unconnected bits according to the selected board.
- Connect SIO channel B to keyboard/console and channel A to the documented serial role; use public chip APIs for RX/TX, baud timing, modem lines and IM2 daisy-chain.
- Define one machine tick order: CPU instruction/T-states → SIO timing → FDC timing → PIO edge/handshake propagation → INT/NMI/WAIT recomputation.
- Add latched diagnostics for CPU PC, bank, FDC command/status/DRQ/INTRQ, SIO status and PIO interrupt state so long boots can be checkpointed without changing behavior.

### 6.6 Guest boot and frontends

- Load the verified monitor ROM, mount the matching `kpii-149` image, reset and run with a bounded instruction/cycle budget until monitor output and CP/M `A>` are observed through emulated console/video.
- Add guest-level `DIR`, file read, file write, reset, remount and second boot tests; include progress checkpoints every 100 million instructions and a fail-fast diagnostic snapshot.
- Add CLI registration for profile/ROM/font/disk paths and a Desktop ViewModel that uses machine services, not direct chip state; display screen, console, drive activity, CPU and FDC status.

## 7. Implementation Sequence

1. **Import manifest and legal gate** — add assets only after copying bytes from the identified previous checkout, verify sizes/hashes, document provenance in `docs/kaypro/README.md` or a consolidated asset document, and add tests for manifest validation. Stop if redistribution status is unacceptable.
2. **Hardware profile and map baseline** — introduce the early-board profile, explicit port/memory constants and snapshot tests; preserve current public machine construction.
3. **ROM banking and video/font** — implement the selected 81-146 decoder, real VRAM geometry and renderer; test glyph bytes, row stride, cursor/blink policy, reset fill and ROM/RAM bank switching.
4. **Raw disk contract and FD1791 compatibility** — retain current raw image tests, add sector ID/geometry/write-protect cases, then implement/test the board-specific controller adapter and signal polarity.
5. **TD0 loader** — parse uncompressed and advanced-compressed records, reject malformed/truncated/duplicate/missing sectors, expose read/write behavior and compare decoded sectors with the known raw image where both are available.
6. **PIO/SIO physical wiring** — attach keyboard, printer and drive/system lines, test every mapped port, interrupt acknowledge/RETI, reset, edge/level behavior, baud timing and no direct internal-state access.
7. **Real monitor boot** — run `81-149C` from reset against `kpii-149.td0`, capture checkpoints, diagnose until the boot monitor and first system-sector transfer are deterministic, then assert `A>`.
8. **CP/M E2E and persistence** — execute guest `DIR`, read and write a file, reset, remount and boot again; verify bytes through the disk image and guest-visible output.
9. **CLI/Desktop integration** — add profile and asset selection, mounted-disk status, reset/boot controls and screen/console/debug views; cover construction and error paths without requiring Avalonia rendering in chip tests.
10. **Final verification and documentation** — update all Kaypro/chip checklists, run targeted and full tests, run `dotnet build PetEmulator.slnx --no-restore`, run GitNexus `detect-changes`, then stage/commit only the intended files.

## 8. Test Strategy

- Asset tests: exact size/hash, missing external asset, wrong hash, alternate font explicitly selected.
- Profile/map tests: every memory/port range, ROM banking, VRAM alias/stride, reset defaults, unknown port fallback and unconnected PIO bits.
- Video tests: 80×25 geometry, 128-byte row stride, active-low glyph `A`/space rows, blink/attribute handling and deterministic framebuffer checksum.
- Disk tests: raw geometry, sector IDs 1–10, write-protect, TD0 normal/advanced compression, bad header/CRC/length, missing or duplicate records, persistence and raw-vs-TD0 equivalence.
- Device tests: FD1791/FD1793 profile differences, WAIT/DRQ/INTRQ/INT/NMI latching, SIO RX/TX timing and IM2, PIO handshakes and interrupt daisy-chain.
- Guest tests: monitor reset vector, banner, first sector transfer, `A>`, `DIR`, read/write, reset and second boot. Tests must use bus ports and public adapter APIs only.
- Verification commands: `dotnet build PetEmulator.slnx --no-restore --disable-build-servers -m:1`; targeted Kaypro, Chips and CPU Z80 tests; full solution tests; `git diff --check`; GitNexus `detect-changes --scope all --repo .`.

## 9. Risk and Impact Analysis

- **High:** video replacement touches `KayproBus`, `KayproMachine` and tests; retain compatibility or update all callers in one coherent change.
- **High:** ROM/font pairing is ambiguous: `81-146a` and `81-146` differ bytewise. The early-board default must be pinned to one hash and the alternate must remain explicit.
- **High:** FD1791 versus FD1793 differences can make a plausible boot pass the wrong hardware model; use profile-specific tests and do not hide bus inversion/polarity in generic chip code.
- **Medium:** TD0 advanced compression and malformed-record handling; use bounded buffers and no partial image publication.
- **Medium:** long guest boot runtime; add checkpoints and diagnostics rather than weakening correctness or using host-side CP/M shortcuts.
- **Medium:** current analyzer has unresolved interface/dynamic-dispatch boundaries and truncated flows; repeat impact and textually inspect callers before implementation/deletion.
- **Legal:** ROM/font/disk dumps have source references but not uniformly explicit redistribution grants; make asset policy configurable and documented.

## 10. Files Expected to Change

| File | Symbols | Reason |
| --- | --- | --- |
| `src/PetEmulator.Kaypro/KayproHardwareProfile.cs` | new profile records/enums | board, ROM, FDC, timing and map identity |
| `src/PetEmulator.Kaypro/KayproBus.cs` | `KayproBus` | profile-aware memory/I/O and signal wiring |
| `src/PetEmulator.Kaypro/KayproMachine.cs` | `KayproMachine` | asset loading, reset/tick/diagnostics |
| `src/PetEmulator.Kaypro/KayproVideo.cs` and new font/renderer files | `KayproVideo` | real VRAM/font/native raster |
| `src/PetEmulator.Kaypro/KayproDiskImage.cs` and new TD0 files | disk adapters | raw/TD0 media and persistence |
| `lib/PetEmulator.Chips/` only if required by a proven FD1791 difference | shared FDC contract/adapter | keep generic chip independent of Kaypro |
| `tests/PetEmulator.Kaypro.Tests/` | new asset, TD0, map, boot and E2E tests | regression and acceptance coverage |
| `roms/kaypro-ii/`, `roms/fonts/kaypro-ii/`, test asset folder | binary assets/manifests | verified ROM/font/disk inputs |
| `docs/kaypro/README.md`, `docs/chips/README.md`, `docs/README.md` | status/index/checklists | consolidated documentation and provenance |
| `src/PetEmulator.Cli/Program.cs`, Desktop ViewModels/Views | new Kaypro registration/UI | frontend integration after machine path is stable |

## 11. Reusable Implementation Context

```yaml
implementation_context:
  task_summary: Import verified Kaypro II assets and complete the early-board machine path from monitor ROM to CP/M A>.
  acceptance_criteria:
    - Early 81-110A/81-111 profile explicitly selects 81-149C, 81-146 and FD1791-compatible behavior.
    - ROM/font/disk provenance and SHA-256 manifests are verified.
    - Raw and TD0 media pass strict format and persistence tests.
    - Real guest boot reaches A> and executes DIR/read/write/reset/remount/reboot.
    - PIO/SIO/FDC/video/CLI/Desktop integrations use public contracts and pass regressions.
  evidence_provenance:
    schema_version: 2
    head_commit: a731cf1324eeec716b18c3af45f1252f17b8706a
    generated_plan_path: docs/plans/2026-09-13-gitnexus-plan-kaypro-ii-import-completion.md
    global_dirty_digest:
      algorithm: sha256
      canonicalization: gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records
      value: 5cbeef29d52cb7a4604f66026d9de96edaac2e1b1151f67561ab9d230a57be9c
    cited_path_manifest:
      - path: docs/README.md
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:ce4773c1e66ee22bfeabfc751d0fea7e1829a930f28c10b1f7a639107ab18eae
        index_digest: sha256:ce4773c1e66ee22bfeabfc751d0fea7e1829a930f28c10b1f7a639107ab18eae
        worktree_digest: sha256:ce4773c1e66ee22bfeabfc751d0fea7e1829a930f28c10b1f7a639107ab18eae
        untracked_digest: absent
      - path: docs/chips/Z80PIO.md
        object_kind: {head: absent, index: regular, worktree: regular, untracked: absent}
        state: staged
        rename_from: null
        rename_to: null
        head_digest: absent
        index_digest: sha256:5307da8cea900766f3fc2d47c06926748d4516944c85ff9a7be252e59a93b558
        worktree_digest: sha256:5307da8cea900766f3fc2d47c06926748d4516944c85ff9a7be252e59a93b558
        untracked_digest: absent
      - path: docs/chips/Z80SIO.md
        object_kind: {head: absent, index: regular, worktree: regular, untracked: absent}
        state: staged
        rename_from: null
        rename_to: null
        head_digest: absent
        index_digest: sha256:940869ab8c2552be6d3b36b8062e8a6b8dad7e79b03ab45e6404f888eda45165
        worktree_digest: sha256:940869ab8c2552be6d3b36b8062e8a6b8dad7e79b03ab45e6404f888eda45165
        untracked_digest: absent
      - path: docs/kaypro/README.md
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:145147a5c11ce2b9ea0853d224f3f3c9e80404b8cc7c5c5391400c78358f14ab
        index_digest: sha256:145147a5c11ce2b9ea0853d224f3f3c9e80404b8cc7c5c5391400c78358f14ab
        worktree_digest: sha256:145147a5c11ce2b9ea0853d224f3f3c9e80404b8cc7c5c5391400c78358f14ab
        untracked_digest: absent
      - path: src/PetEmulator.Kaypro/KayproBus.cs
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: staged
        rename_from: null
        rename_to: null
        head_digest: sha256:c76772bd2f000c1d766feedc2716f8f2cfe5d2aaf6adbbf70ab84b97eb3097ac
        index_digest: sha256:199eaf17644369793de2534ba045e7b1bf05d232c4e079058d6edeccfc8c0b9f
        worktree_digest: sha256:199eaf17644369793de2534ba045e7b1bf05d232c4e079058d6edeccfc8c0b9f
        untracked_digest: absent
      - path: src/PetEmulator.Kaypro/KayproDiskImage.cs
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:ed4aeadb72a663ea89e7d310e38faa2a639e4f4673b6bcb07405d0378cd52914
        index_digest: sha256:ed4aeadb72a663ea89e7d310e38faa2a639e4f4673b6bcb07405d0378cd52914
        worktree_digest: sha256:ed4aeadb72a663ea89e7d310e38faa2a639e4f4673b6bcb07405d0378cd52914
        untracked_digest: absent
      - path: src/PetEmulator.Kaypro/KayproMachine.cs
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:b0906faa33d0e9492963f3785213d28bbace3595bef440890384b5c1605edaa2
        index_digest: sha256:b0906faa33d0e9492963f3785213d28bbace3595bef440890384b5c1605edaa2
        worktree_digest: sha256:b0906faa33d0e9492963f3785213d28bbace3595bef440890384b5c1605edaa2
        untracked_digest: absent
      - path: src/PetEmulator.Kaypro/KayproPioWiring.cs
        object_kind: {head: absent, index: regular, worktree: regular, untracked: absent}
        state: staged
        rename_from: null
        rename_to: null
        head_digest: absent
        index_digest: sha256:b238797acc034bf4ccb63684a82687fd320c608afb03bfd26c4e88f07f4d24d9
        worktree_digest: sha256:b238797acc034bf4ccb63684a82687fd320c608afb03bfd26c4e88f07f4d24d9
        untracked_digest: absent
      - path: src/PetEmulator.Kaypro/KayproSioWiring.cs
        object_kind: {head: absent, index: regular, worktree: regular, untracked: absent}
        state: staged
        rename_from: null
        rename_to: null
        head_digest: absent
        index_digest: sha256:3dbdf9bee9d386c22f7cfe2643ca05e346c68f6be6ceb6f9da6aca717b3a44e2
        worktree_digest: sha256:3dbdf9bee9d386c22f7cfe2643ca05e346c68f6be6ceb6f9da6aca717b3a44e2
        untracked_digest: absent
      - path: src/PetEmulator.Kaypro/KayproVideo.cs
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:8dc2825097e8dc31d73464d879d00b01685dff3e58c99c4ee21006fec62874b8
        index_digest: sha256:8dc2825097e8dc31d73464d879d00b01685dff3e58c99c4ee21006fec62874b8
        worktree_digest: sha256:8dc2825097e8dc31d73464d879d00b01685dff3e58c99c4ee21006fec62874b8
        untracked_digest: absent
      - path: src/PetEmulator.Kaypro/PetEmulator.Kaypro.csproj
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:1ff91794a12e81c631aedf61ea565af83d7218efbe13dfba0628d4ad973480e0
        index_digest: sha256:1ff91794a12e81c631aedf61ea565af83d7218efbe13dfba0628d4ad973480e0
        worktree_digest: sha256:1ff91794a12e81c631aedf61ea565af83d7218efbe13dfba0628d4ad973480e0
        untracked_digest: absent
      - path: tests/PetEmulator.Kaypro.Tests/KayproMachineTests.cs
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: staged
        rename_from: null
        rename_to: null
        head_digest: sha256:2d76947edf12e5266865eb757c5328883a711b7b4566d1e8fba83cebcf8568a7
        index_digest: sha256:3acc6d3808b07bb6c87c557da6276e9bd1ef6cde2df6093d9400f4826632c454
        worktree_digest: sha256:3acc6d3808b07bb6c87c557da6276e9bd1ef6cde2df6093d9400f4826632c454
        untracked_digest: absent
      - path: tests/PetEmulator.Kaypro.Tests/KayproPioEndToEndTests.cs
        object_kind: {head: absent, index: regular, worktree: regular, untracked: absent}
        state: staged
        rename_from: null
        rename_to: null
        head_digest: absent
        index_digest: sha256:bd97b891b16242eac4f9ed442b6ef778db2046da8330ef6eecd7cd3a114b2990
        worktree_digest: sha256:bd97b891b16242eac4f9ed442b6ef778db2046da8330ef6eecd7cd3a114b2990
        untracked_digest: absent
      - path: tests/PetEmulator.Kaypro.Tests/KayproPioWiringTests.cs
        object_kind: {head: absent, index: regular, worktree: regular, untracked: absent}
        state: staged
        rename_from: null
        rename_to: null
        head_digest: absent
        index_digest: sha256:a5ee16fbf20de5ae3f9a3bf4d955c36aa051c79d17473119abdaa3758635b570
        worktree_digest: sha256:a5ee16fbf20de5ae3f9a3bf4d955c36aa051c79d17473119abdaa3758635b570
        untracked_digest: absent
      - path: tests/PetEmulator.Kaypro.Tests/PetEmulator.Kaypro.Tests.csproj
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:08c77d990330f6b87324762890e8f8529c84955a57a0f96edc8e7dde6dc11aae
        index_digest: sha256:08c77d990330f6b87324762890e8f8529c84955a0f96edc8e7dde6dc11aae
        worktree_digest: sha256:08c77d990330f6b87324762890e8f8529c84955a57a0f96edc8e7dde6dc11aae
        untracked_digest: absent
  primary_symbols:
    - symbol: KayproMachine
      file: src/PetEmulator.Kaypro/KayproMachine.cs
      lines: 6-52
      role: machine composition and CPU/device tick boundary
    - symbol: KayproBus
      file: src/PetEmulator.Kaypro/KayproBus.cs
      lines: 8-143
      role: memory, I/O, ROM bank and interrupt routing
    - symbol: KayproVideo
      file: src/PetEmulator.Kaypro/KayproVideo.cs
      lines: 3-33
      role: current video boundary to replace compatibly
    - symbol: KayproDiskImage
      file: src/PetEmulator.Kaypro/KayproDiskImage.cs
      lines: 5-63
      role: raw media implementation
    - symbol: KayproPioWiring
      file: src/PetEmulator.Kaypro/KayproPioWiring.cs
      lines: 5-117
      role: machine PIO adapter
  related_symbols:
    - symbol: KayproSioWiring
      relationship: adapter used by KayproBus
      relevance: keyboard/console timing and interrupt path
    - symbol: FD1793
      relationship: shared chip dependency
      relevance: profile-specific FDC compatibility boundary
    - symbol: KayproMachineTests
      relationship: direct test consumer
      relevance: map and contract regression
  execution_path:
    - CPU accesses KayproBus memory and ports.
    - Bus maps SIO/PIO/FDC and exposes banked ROM/video.
    - Bus tick advances serial and floppy timing and recomputes interrupt lines.
    - Monitor ROM reads disk sectors through the selected FDC profile.
    - Guest CP/M uses SIO/PIO/video and reaches A>.
  pdg_constraints:
    - description: PDG unavailable for a load-bearing Kaypro slice; use source-derived ordering and rerun if implementation introduces central state machines.
      affected_statements: []
      implementation_consequence: Preserve explicit CPU-to-device tick order and add state-transition tests.
  architectural_patterns:
    - pattern: generic chip plus machine adapter
      example_location: src/PetEmulator.Kaypro/KayproPioWiring.cs
      usage_guidance: keep Kaypro semantics out of lib/PetEmulator.Chips
    - pattern: Core IMachine/IBus/IMemoryBus composition
      example_location: src/PetEmulator.Kaypro/KayproMachine.cs
      usage_guidance: preserve public machine contracts
  files_to_modify:
    - file: src/PetEmulator.Kaypro/KayproHardwareProfile.cs
      symbols: [new profile types]
      intended_change: encode selected board and asset identity
    - file: src/PetEmulator.Kaypro/KayproBus.cs
      symbols: [KayproBus]
      intended_change: profile-aware mapping and timing
    - file: src/PetEmulator.Kaypro/KayproDiskImage.cs
      symbols: [KayproDiskImage]
      intended_change: preserve raw path while adding common adapter
    - file: tests/PetEmulator.Kaypro.Tests/
      symbols: [new tests]
      intended_change: asset, TD0, device and guest regressions
  tests:
    - file: tests/PetEmulator.Kaypro.Tests/KayproMachineTests.cs
      scenarios: [reset/map/ROM banking -> documented values, SIO/PIO -> public contract behavior]
    - file: tests/PetEmulator.Kaypro.Tests/KayproTd0ImageTests.cs
      scenarios: [valid/invalid TD0 -> decoded sectors or deterministic error]
    - file: tests/PetEmulator.Kaypro.Tests/KayproBootTests.cs
      scenarios: [81-149C + matching image -> banner, A>, DIR, write, reset, second A>]
  verification_commands:
    - dotnet build PetEmulator.slnx --no-restore --disable-build-servers -m:1
    - dotnet test tests/PetEmulator.Kaypro.Tests/PetEmulator.Kaypro.Tests.csproj --no-restore --disable-build-servers -m:1 --verbosity minimal
    - dotnet test tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj --no-restore --disable-build-servers -m:1 --verbosity minimal
    - dotnet test PetEmulator.slnx --no-restore --disable-build-servers -m:1 --verbosity minimal
    - node .gitnexus/run.cjs detect-changes --scope all --repo .
  risks: [FD1791/FD1793 mismatch, ROM/font licensing and pairing, TD0 compression, long guest runtime, hidden interface callers]
  assumptions:
    - what: early-board target is 81-110A/81-111
      how: confirm against selected ROM/disk provenance and board documentation before coding
    - what: kpii-149.td0 is the matching boot image
      how: verify TD0 metadata and guest boot, not filename alone
  open_questions:
    - Is binary redistribution of the selected ROM/font/disk allowed in this repository?
    - Should FD1791 inversion be implemented as a new shared profile-capable adapter or as a Kaypro-only wrapper?
    - Is 80×25 required by the selected ROM while the current code/documentation says 80×24?
  avoid:
    - Do not copy old CpuVibe classes without adapting Core contracts.
    - Do not use host CP/M commands as proof of guest boot.
    - Do not silently substitute 81-146A for 81-146 or FD1793 for FD1791.
    - Do not put Kaypro port semantics into generic chip implementations.
```

## 12. Assumptions and Open Questions

### Confirmed

- Previous assets and their hashes are present in `cpu-vibe-010`/`cpu-vibe-011` as listed in §6.
- Current repo has generic Z80 SIO/PIO and FD1793, but no current Kaypro ROM/font/TD0 assets or TD0 loader.
- PIO/SIO chip-level checklists are complete; machine-level binary boot remains open.

### Assumptions to verify before implementation

- The first supported machine is the early `81-110A`/`81-111` profile with FD1791-compatible behavior.
- The `81-149C` + `81-146` + `kpii-149.td0` tuple is the intended acceptance tuple.
- Asset redistribution is permitted, or the repository will use external asset paths with hash validation.

### Deferred decisions

- Later `81-110B1/B2`/FD1793 and `81-240` profiles should be added only after the first profile is green.
- Full physical CRT timing and exact keyboard electrical matrix are separate from the first deterministic CP/M boot gate unless the selected ROM requires them.

## 13. Definition of Done

- [ ] A named early Kaypro II profile selects board, ROM, font, FDC variant, timing, map and disk geometry together.
- [ ] ROM/font/TD0/raw assets have verified hashes, provenance and explicit legal policy.
- [ ] Video uses the selected Kaypro font and correct guest-visible geometry/stride.
- [ ] PIO/SIO/FDC wiring and WAIT/DRQ/INTRQ/INT/NMI behavior are tested through public buses.
- [ ] TD0 and raw images load, reject malformed input and preserve writes as specified.
- [ ] Real monitor ROM boots the matching CP/M image to `A>`; `DIR`, read, write, reset, remount and second boot pass.
- [ ] CLI/Desktop can select the profile and assets and expose screen, console, drive and debug state.
- [ ] Targeted, chip/CPU, full solution tests, build, diff check and GitNexus change analysis are green before commit.
