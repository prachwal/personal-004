# Plan: rekonesans CPC + plan importu prostszego modelu (CPC464) do personal-004

## Context

Użytkownik poprosił o zlecenie haiku rekonesansu wcześniejszych iteracji projektu w poszukiwaniu implementacji Amstrad CPC, sprawdzenie wszystkich wariantów, i przygotowanie szczegółowego planu importu **prostszego modelu** do bieżącego repo (`personal-004`) w pliku `.md`. Cel: nie przepisywać CPC od zera — wykorzystać najlepiej przetestowaną wcześniejszą implementację jako źródło portu, trzymając się konwencji modułów maszyn już ustalonej w `personal-004` (PET, VIC-20, TRS-80, Kaypro).

## Recon — wynik (3 agenty haiku + bezpośrednia weryfikacja)

Warianty CPC znalezione w `/home/prachwal/source/emulators/`:

| Repo | Status | Warianty | Ocena |
| --- | --- | --- | --- |
| **personal-002** | ✅ production-grade | CPC464 (prosty, boot+gry), **CPC6128** (główny, CP/M 2.2/Plus, FDC, banking) | 1534 LOC emulacji + 900 LOC testów, 97+15+8 testów zielonych, pełny hardware-freeze plan (`docs/machines/amstrad/amstrad-cpc6128-implementation-plan.md`, fazy M0–M7 DONE) |
| personal-003 | ⚠️ port zablokowany | CPC464, CPC6128 (kopia z personal-002) | Blokada architektoniczna: generyczny `IMachine`/rejestr jeszcze nie wyekstrahowany z TRS-80-specyficznego `ITrs80Machine`; brak integracji z UI |
| cpu-vibe-010 | 📄 tylko dokumentacja | — | Świetne spec doc (CRTC 6845 598 linii, Gate Array 180 linii, mapa portów) + font/keyboard adapter, **zero działającego kodu maszyny** |
| proc-vibe-001 | 🔶 częściowy kod | — | MC6845 CRTC ✅ i AY-3-8910 ✅ zaimplementowane i przetestowane (207 testów), ale brak Gate Array, PPI, FDC, brak klasy maszyny łączącej |
| web-emulator | 📄 TS, nie .NET | — | Tylko tryb tekstowy/demo wideo, brak emulacji CPU — nieistotne do portu |
| psudo-cpu | 🔹 izolowany fragment | — | Wyłącznie `AmstradCpcBankSwitching.cs` (strategia bankowania pamięci) |

**Wniosek:** `personal-002` jest jedynym źródłem z kompletną, przetestowaną maszyną. W nim **CPC464 jest prostszym wariantem** niż CPC6128 — stały układ 64 KB RAM (bez 8-konfiguracyjnego bankowania), **brak FDC/dyskietek** (tylko kaseta), mniej portów. To naturalny kandydat na "prostszy model" do importu.

### personal-002 → CPC464, pliki źródłowe (bez `obj`/`bin`)

- `lib/Machines/Amstrad/Cpc464/Cpc464Machine.cs` — kompozycja maszyny (własny Z80, nie ten z personal-004!)
- `lib/Machines/Amstrad/Cpc464/Cpc464MemoryBus.cs` — mapa pamięci 64 KB, overlay ROM dolny (0000–3FFF) / górny (C000–FFFF)
- `lib/Machines/Amstrad/Cpc464/Cpc464Ports.cs` — dekodowanie portów I/O → Gate Array / CRTC / AY / klawiatura / taśma
- `lib/Machines/Amstrad/Cpc464/Cpc464Keyboard.cs` — matryca 10×8, active-low
- `lib/Machines/Amstrad/Cpc464/Cpc464Snapshot.cs`, `Cpc464SnapshotCodec.cs`, `Cpc464PortsSnapshot.cs`, `Cpc464KeyboardSnapshot.cs` — serializacja stanu (opcjonalne przy imporcie)
- `lib/Chips/Amstrad/CpcGateArray/CpcGateArrayChip.cs` (+ `CpcGateArrayPalette.cs`, `CpcGateArraySnapshot.cs`) — **serce CPC**: wybór trybu (0/1/2), ROM enable, konfiguracja RAM, IRQ co 52 HSYNC (licznik 6-bit), render 320×200
- CRTC: `Crtc6545Chip` (mimo nazwy — to model rejestrów zgodny z MC6845, użyty jako `new Crtc6545Chip(0xBC00)` w `Cpc464Machine.cs:27`)
- AY-3-8910: `Ay38910Chip` (clock=1MHz dla CPC)
- Kaseta: `CassetteTape` (Chips/Cassette)
- Testy referencyjne: `tests/Cpc464.Tests/Cpc464MachineTests.cs`, `Cpc464RomBootTests.cs`
- ROM: `lib/Machines/Amstrad/Cpc464/roms/cpc464.rom` (32 KB, firmware+BASIC)

### personal-004 — konwencja modułu maszyny (na przykładzie Kaypro/TRS-80)

- Folder `src/PetEmulator.<Nazwa>/` — klasy maszyny/urządzeń specyficzne dla modelu
- `IMachine` (`lib/PetEmulator.Core/Abstractions/IMachine.cs`): `Name`, `IsReady`, `CycleCount`, `Processor:IProcessor`, `Memory:IMemoryBus`, `Reset()`, `StepInstruction()`, `Run(ulong)`
- CPU Z80 **już istnieje i jest reużywalny**: `lib/PetEmulator.CpuZ80` (`Z80Cpu`, `IBus`, `IIoBus`, `InterruptLines`/`IInterruptLines`) — używany już przez TRS-80 i Kaypro. **Nie wolno** kopiować własnego Z80 z personal-002 (inny, niekompatybilny kontrakt).
- Wzorzec tick: `StepInstruction()` robi `Processor.StepInstruction()`, potem `Bus.Tick(cyklesDelta, halted)` popycha urządzenia (patrz `KayproMachine.cs:41-46`, `Trs80Machine.cs`).
- Brak fabryki/rejestru — maszyny instancjonowane wprost w `src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs` przez `ModuleMenuEntry`, każda ma osobny `<Nazwa>MachineViewModel`.
- Chipy generyczne (reużywalne między maszynami) żyją płasko w `lib/PetEmulator.Chips/` (np. `FD1791.cs`, `FD1793.cs`, `MC6850.cs`, `MOS6522.cs`). Chip specyficzny dla jednej maszyny (np. `KayproFdcWiring.cs`) zostaje w folderze maszyny.
- **CRTC 6845 już częściowo istnieje**: `lib/PetEmulator.Chips/MT6545.cs` — generyczny, renderer-independent model rejestrów 18×8845/6545, dokumentacja mówi wprost że jest to "minimal Motorola 6545 CRT controller" z niezamodelowanymi tylko PET-specyficznymi trybami Update/Transparent. **Do zweryfikowania w pierwszym kroku implementacji**, czy nadaje się wprost dla CPC (adres MA/RA, R-wartości trybu 1: R0=63,R1=40,R4=38 z dok. cpu-vibe-010) czy wymaga nowego, równoległego `MC6845.cs`.
- **FDC nie jest potrzebny dla CPC464** (brak stacji dysków) — różnica względem Kaypro/TRS-80, upraszcza import.
- **AY-3-8910 nie istnieje jeszcze w `lib/PetEmulator.Chips/`** — trzeba dodać nowy plik; najlepsze źródło to `Ay38910Chip` z personal-002 (już zintegrowany i przetestowany z CPC464) lub przetestowana wersja z proc-vibe-001 (299 linii, pełna specyfikacja: obwiednie, szum LFSR 17-bit, R7 negacja bitów) — do wyboru w kroku 2, preferencja dla personal-002 (spójność z resztą portowanego kodu).
- **Gate Array nie ma odpowiednika** — musi być nowy plik, port 1:1 logiki z `CpcGateArrayChip.cs` (adaptacja do `IMemoryMappedDevice`/`IDevice` z personal-004 zamiast `Abstraction.Devices.IDevice` z personal-002).

## Proponowane podejście

Import **CPC464** jako nowy moduł `src/PetEmulator.Cpc464/`, źródło = **personal-002** (jedyna kompletna, przetestowana implementacja), z przepisaniem warstwy CPU/bus na istniejące kontrakty `PetEmulator.CpuZ80` i `PetEmulator.Core.IMachine` (wzorzec 1:1 z `KayproMachine`/`Trs80Machine`). CPC6128 (banking, FDC, CP/M) świadomie **poza zakresem** tego importu — to następny krok po ustabilizowaniu CPC464.

### Docelowa struktura plików

- `src/PetEmulator.Cpc464/Cpc464Machine.cs` — implementuje `IMachine`, komponuje `Z80Cpu` (z `PetEmulator.CpuZ80`), `Cpc464Bus`, urządzenia
- `src/PetEmulator.Cpc464/Cpc464Bus.cs` — `IBus`+`IIoBus`, mapa pamięci 64 KB (ROM lower/upper overlay, bez bankowania), routing portów do Gate Array/CRTC/AY/klawiatura/taśma — port logiki z `Cpc464MemoryBus.cs` + `Cpc464Ports.cs`
- `src/PetEmulator.Cpc464/Cpc464Keyboard.cs` — matryca 10×8 (port z `Cpc464Keyboard.cs`)
- `src/PetEmulator.Cpc464/Cpc464GateArray.cs` — port 1:1 z `CpcGateArrayChip.cs`+`CpcGateArrayPalette.cs`: tryby 0/1/2, ROM enable/RAM config, IRQ licznik HSYNC, render 320×200
- `src/PetEmulator.Cpc464/Cpc464Cassette.cs` (opcjonalnie na start: stub/no-op, real .CDT loader jako krok późniejszy)
- `lib/PetEmulator.Chips/AY38910.cs` — nowy generyczny chip (nie CPC-specyficzny — reużywalny dla ZX Spectrum/MSX w przyszłości), port z personal-002 `Ay38910Chip`
- `lib/PetEmulator.Chips/MC6845.cs` **lub** rozszerzenie `MT6545.cs` — CRTC, decyzja po weryfikacji zgodności rejestrów w kroku 1
- `src/PetEmulator.Desktop/ViewModels/Cpc464MachineViewModel.cs` + wpis w `MainWindowViewModel.cs` (`ModuleMenuEntry("Amstrad CPC464", ...)`)
- `tests/PetEmulator.Cpc464.Tests/` — port testów z `Cpc464.Tests` (mapa pamięci, boot ROM, Gate Array tryby/paleta/IRQ)
- `roms/cpc464/cpc464.rom` — import ROM (32 KB) z weryfikacją SHA-256 i notatką o pochodzeniu/licencji (wzorzec z `docs/plans/2026-09-13-gitnexus-plan-kaypro-ii-import-completion.md` §6.1)

### Kolejność wykonania

1. **Weryfikacja CRTC** — sprawdzić `MT6545.cs` vs wymagania CPC (adresowanie MA/RA z formuły `((MA&0x3000)<<2)|((RA&7)<<11)|((MA&0x3FF)<<1)`, wartości R0=63/R1=40/R4=38 dla trybu 1, zachowanie readback statusu) → decyzja: reuse czy nowy plik.
2. **Import AY-3-8910** — nowy `lib/PetEmulator.Chips/AY38910.cs`, testy jednostkowe (rejestry, obwiednie, LFSR) — niezależny od reszty, można zweryfikować w izolacji.
3. **Port Gate Array** — `Cpc464GateArray.cs`, z testami trybów/palety/IRQ (source: `CpcGateArray.Tests` w personal-002, 8 testów jako baza).
4. **Bus + pamięć + porty + klawiatura** — `Cpc464Bus.cs`, `Cpc464Keyboard.cs`, testy mapy pamięci i dekodowania portów.
5. **Cpc464Machine + integracja CPU** — wpięcie `PetEmulator.CpuZ80.Cpu.Z80Cpu`, pętla `StepInstruction`→`Bus.Tick`, IRQ z Gate Array (IM1, wektor 0xFF — sprawdzić zgodność z `IInterruptLines`/`AcknowledgeInterrupt()` z `IBus`).
6. **Boot ROM real firmware** — import `cpc464.rom`, test bootujący do ekranu BASIC (wzorzec `Cpc464RomBootTests.cs`).
7. **Desktop integracja** — `Cpc464MachineViewModel`, wpis w menu, klawiatura ekranowa (opcjonalnie na start).
8. **Dokumentacja + finalizacja** — build+testy+`detect_changes` przed commitem.

## Ryzyka

- **Różne kontrakty CPU**: personal-002 ma własny `Z80Cpu`/`Z80State`/`IM2`/`BusCycleObserver` — nie kopiować bezpośrednio, tylko logikę urządzeń; sprawdzić czy `PetEmulator.CpuZ80.Z80Cpu` obsługuje IM1 z wektorem 0xFF tak jak personal-002 (`_cpu.RequestInterrupt(0xFF)`).
- **CRTC**: `MT6545.cs` ma w komentarzu explicite "NOT modeled: ... 6545-specific Update/Transparent modes" — trzeba potwierdzić że bazowy model rejestrów wystarcza dla CPC (który używa standardowego MC6845, nie Rockwell-specyficznych trybów), inaczej dodać osobny plik zamiast ryzykować regresję w PET.
- **Timing Gate Array**: IRQ co 52 zbocza HSYNC (licznik 6-bit) musi być podpięty do prawidłowego zliczania cykli Z80→CRTC w nowej pętli tick — źródło błędów przy pierwszym porcie.
- **Licencja ROM**: `cpc464.rom` to oryginalny firmware Amstrad — powtórzyć politykę z planu Kaypro (hash + notatka o pochodzeniu, bez cichej redystrybucji jeśli status prawny niejasny).

## Weryfikacja

- `dotnet build PetEmulator.slnx --no-restore --disable-build-servers -m:1`
- `dotnet test tests/PetEmulator.Cpc464.Tests/PetEmulator.Cpc464.Tests.csproj --no-restore --disable-build-servers -m:1`
- `dotnet test tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj` (nowy AY-3-8910, ew. CRTC)
- Test boot: uruchomienie maszyny z realnym `cpc464.rom` do ekranu BASIC (checksum ramki lub porównanie znaków w text-equivalent VRAM)
- `node .gitnexus/run.cjs impact "Cpc464Machine" --direction upstream --repo .` przed edycją istniejących współdzielonych plików (`MainWindowViewModel.cs`, ew. `MT6545.cs` jeśli rozszerzany)
- `node .gitnexus/run.cjs detect-changes --scope all --repo .` przed commitem
