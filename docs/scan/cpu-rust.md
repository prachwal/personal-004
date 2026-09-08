# cpu-rust

- Zrodlo: `/home/prachwal/source/emulators/cpu-rust`.
- Stos: Rust workspace; `Cargo.toml`.
- Rdzen 6502: `crates/6502/core`, `memory`, `config`; testy ROM-ow funkcjonalnych i `nestest`.
- PET: `crates/machines/pet/` z ROM-ami `editor.bin`, `kernal.bin`, `basic-c000.bin`, `basic-d000.bin`, `chargen.bin`.
- Apple 1/KIM-1: `crates/machines/apple1/`, `crates/machines/kim1/`; narzedzia `pet-window`, `pet-term`, `apple1-window`, `apple1-term`.
- Chipy: PIA 6520, VIA 6522, ACIA 6551; dokumentacja zawiera plany zgodnosci PET.
- Kandydat reuse: architektura Rust, podzial CPU/bus/machine i komplet ROM-ow PET; sprawdzic licencje i zgodnosc z docelowym stosem.
