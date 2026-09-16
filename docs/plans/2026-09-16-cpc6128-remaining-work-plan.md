# Plan dokończenia CPC6128 — braki po M0-M5/M7-częściowe

Bazuje na `docs/plans/2026-09-16-cpc6128-integration-plan.md` (tam jest
kontekst źródeł `personal-002`/`personal-003` i inwentaryzacja — nie
powtarzaj tamtej analizy, tylko wykonuj kroki poniżej).

Stan wejściowy (sprawdzony w repo przed napisaniem tego planu):

- `src/PetEmulator.Cpc6128/Cpc6128Machine.cs` istnieje, kompiluje się, ma
  `Cpu`, `Bus`, `Ports`, `GateArray`, `Crtc`, `Ay`, `Keyboard`, `Cassette`, `Fdc`.
- `roms/cpc6128/cpc6128.rom` i `roms/cpc6128/amsdos.rom` już są w repo.
- Menu Desktop ma wpis "Amstrad CPC6128", `MainWindow.axaml` ma
  `DataTemplate` dla `Cpc6128MachineViewModel`/`Cpc6128MachineView`.
- `lib/PetEmulator.CpcFdc/I8272Chip.cs` ma już `CaptureState()`/`RestoreState()`
  zwracające/przyjmujące `I8272Snapshot` (`CpcFdcSnapshot.cs`) — **nie
  pisz tego od nowa**, tylko użyj w kroku 3.
- `Cpc6128MemoryBus.cs:88` ma `CapturePhysicalRam()` (tylko odczyt, bez
  odpowiednika do przywracania).
- Żaden z: `Cpc464GateArray`, `MT6545`, `Ay38910`, `Cpc464Keyboard`,
  `Cpc464Cassette`, `Cpc6128Ports` **nie ma** `CaptureState`/`RestoreState`.
  To jest nowa praca, nie port z `personal-002` — nazwy pól tam się różnią.

Zasady wykonania (obowiązują na każdym kroku, patrz `CLAUDE.md` w root repo):

1. Przed edycją symbolu uruchom impact analysis (`impact` MCP lub
   `node .gitnexus/run.cjs impact "<symbol>" --direction upstream --repo .`).
2. Przed commitem uruchom `detect_changes` (`scope: "all"` albo CLI
   `detect-changes --scope all --repo .`). `risk: UNKNOWN` traktuj jako
   nierozstrzygnięte — potwierdź ręcznym grepem, nie commituj na go dalej.
3. Po każdym kroku: `dotnet build PetEmulator.slnx --nologo` (0 błędów,
   0 ostrzeżeń) i `dotnet test` na zmienionych projektach testowych.
4. Jeden krok = jeden commit. Nie mieszaj kroków w jednym commicie.
5. Nie kopiuj kodu 1:1 z `personal-002`/`personal-003` — inne przestrzenie
   nazw i kontrakty. Wzorce do kopiowania są wskazane niżej z dokładną
   ścieżką pliku w **tym** repo.

---

## Krok 1 — M6: test realnego bootu firmware (`Cpc6128BootTests`)

Cel: potwierdzić, że prawdziwy ROM CPC6128 rzeczywiście bootuje i wykonuje
BASIC, nie tylko że maszyna się konstruuje.

Wzorzec do skopiowania i zaadaptowania: `tests/PetEmulator.Cpc464.Tests/Cpc464BootTests.cs`
(cały plik — technika: boot, wpisz program przez matrycę klawiatury,
porównaj sylwetkę/glif renderowanego tekstu, bez zaglądania w pamięć
BASIC-a). Pomocniczy plik `tests/PetEmulator.Cpc464.Tests/Cpc464ScreenOcr.cs`
też możesz wykorzystać (sprawdź, czy jest reużywalny wprost, czy trzeba
skopiować do nowego pliku w `tests/PetEmulator.Cpc6128.Tests/`).

Różnice do uwzględnienia względem CPC464:

- `Cpc6128Machine` ma 128 KB RAM i domyślną konfigurację banków `0` —
  `Cpc6128Machine(rom)` już to ustawia w konstruktorze, nie trzeba nic
  dodatkowo wybierać dla samego boot-testu.
- Klawiatura (`Keyboard` property) to ten sam `Cpc464Keyboard` — matryca
  i `SetKey(row, column, down)` identyczne jak w CPC464BootTests, łącznie
  z uwagą o Shift+(3,4) dla `+`.
- Ścieżka ROM-u: `Path.Combine(root, "roms", "cpc6128", "cpc6128.rom")`.
- Liczba instrukcji do bootu może się różnić (128 KB RAM inicjalizacja jest
  dłuższa niż 64 KB CPC464) — jeśli `Boot()` z 2_000_000 nie starcza,
  zwiększaj w krokach x2 aż `RowText` pokaże "Ready", nie zgaduj z góry.

Nowy plik: `tests/PetEmulator.Cpc6128.Tests/Cpc6128BootTests.cs`.

Test minimalny (jeden wystarczy na ten krok, można dodać więcej później):

- `RealFirmwareBootsAndExecutesPrintOnePlusOne` — analogiczny do CPC464:
  wpisz `PRINT 1+1`, sprawdź że po Enter linia "Ready" wraca i że wynik
  renderuje cyfrę '2'.

Weryfikacja: `dotnet test tests/PetEmulator.Cpc6128.Tests/PetEmulator.Cpc6128.Tests.csproj`
— nowy test zielony, stare 12 nadal zielone.

Commit: `test: verify CPC6128 real-firmware boot to Ready`

---

## Krok 2 — M7 dokończenie: CLI debugger session

Cel: `cpc6128-debug` w `PetEmulator.Cli`, analogicznie do `cpc464-debug`.

### 2a. Nowy plik `src/PetEmulator.Cli/Cpc6128DebuggerSession.cs`

Skopiuj **całość** `src/PetEmulator.Cli/Cpc464DebuggerSession.cs` i zmień:

- namespace zostaje `PetEmulator.Cli`, `using PetEmulator.Cpc464;` →
  `using PetEmulator.Cpc6128;`.
- `Cpc464Machine` → `Cpc6128Machine` we wszystkich sygnaturach i polach.
- `CreateMachine()`: ROM z `Path.Combine(_romsRoot, "cpc6128", "cpc6128.rom")`,
  konstruktor `new Cpc6128Machine(rom)` (identyczny kształt co CPC464 —
  jeden `byte[]` ROM, bez dodatkowych parametrów).
- `LoadTape`: w CPC6128 kaseta wisi pod `_machine.Cassette` (property na
  `Cpc6128Machine`, nie `_machine.Bus.Cassette` jak w CPC464 — sprawdź
  aktualne property zanim wkleisz, `Cpc6128Machine.cs` nie ma `Bus.Cassette`,
  ma `Cassette` bezpośrednio).
- `Key`: `_machine.Keyboard.SetKey(row, column, down)` (property `Keyboard`
  bezpośrednio na `Cpc6128Machine`, tak samo jak `Cassette`).
- Dodaj komendę `disk <path>`: `_machine.LoadDisk(0, DskDiskImage.Load(File.ReadAllBytes(path)))`
  — wzoruj się na `LoadTape` dla kształtu metody i komunikatu zwrotnego;
  `DskDiskImage` jest w `PetEmulator.CpcFdc`, dodaj `using PetEmulator.CpcFdc;`.
- `Status()`: dorzuć `cycles=`/`instructions=`/`halted=` tak jak w CPC464,
  ewentualnie `frame={machine.FrameCount}` skoro `Cpc6128Machine` to ma
  a `Cpc464Machine` nie miał (sprawdź czy `Cpc464Machine` ma `FrameCount`
  zanim uznasz to za różnicę — jeśli ma, zachowaj identyczny format).

### 2b. Wpięcie w `src/PetEmulator.Cli/Program.cs`

Skopiuj blok `cpc464Script`/`cpc464Debug` (linie z `var cpc464Script = ...`
do `cpc464Debug.SetAction(...)`) i zduplikuj ze zmianą `cpc464` → `cpc6128`
wszędzie (nazwa zmiennej, tekst opisu komendy, nazwa metody
`RunCpc6128DebugScript`). Dodaj też:

- `root.Subcommands.Add(cpc6128Debug);` obok istniejącego
  `root.Subcommands.Add(cpc464Debug);`.
- Nową metodę `RunCpc6128DebugScript(string? scriptPath)` — skopiuj ciało
  `RunCpc464DebugScript` 1:1, zmień tylko `new Cpc464DebuggerSession()` →
  `new Cpc6128DebuggerSession()`.
- Dodaj `using PetEmulator.Cpc6128;` jeśli kompilator się poskarży (obecnie
  `Program.cs` nie ma tego usingu, bo debugger sessions żyją w tym samym
  namespace `PetEmulator.Cli` — sprawdź, czy w ogóle potrzebny).

Weryfikacja: `dotnet build PetEmulator.slnx --nologo`, potem ręcznie:

```
echo "roms roms
status" | dotnet run --project src/PetEmulator.Cli -- cpc6128-debug
```

(uruchom z katalogu repo; `status` powinien zwrócić `profile=Amstrad CPC6128 ...`
bez wyjątku).

Commit: `feat: add cpc6128-debug CLI session`

---

## Krok 3 — M8: snapshot/debug

To jest **nowa funkcjonalność**, nie port — w repo nie ma jeszcze żadnego
pełnego snapshotu maszyny (żadna z PET/VIC-20/Kaypro/TRS-80/CPC464 go nie
ma). Jedyny gotowy kawałek to `I8272Chip.CaptureState()/RestoreState()`
(→ `I8272Snapshot`, plik `lib/PetEmulator.CpcFdc/CpcFdcSnapshot.cs`) —
użyj go wprost, nie przepisuj.

### 3a. Dodaj `CaptureState()`/`RestoreState()` po kolei do

| Komponent | Plik | Co musi wejść do stanu |
| --- | --- | --- |
| `Cpc6128MemoryBus` | `src/PetEmulator.Cpc6128/Cpc6128MemoryBus.cs` | pełne 128 KB fizycznego RAM-u (już jest `CapturePhysicalRam()` — dodaj brakujący `RestorePhysicalRam(byte[])`), aktualna konfiguracja banków |
| `Cpc464GateArray` | `src/PetEmulator.Cpc464/Cpc464GateArray.cs` | tryb wideo, paleta, pen index, border, latch trybu, `LowerRomEnabled`/`UpperRomEnabled`, `RamConfiguration`, stan HSync/IRQ licznika |
| `MT6545` | `lib/PetEmulator.Chips/MT6545.cs` | wszystkie 18 rejestrów, wybrany rejestr, stan raster/HSync/VSync — **uwaga**: to współdzielony chip z PET, zmiana musi zostać ogólna (nie CPC-specific), zrób impact analysis przed edycją bo ma innych callerów (PET) |
| `Ay38910` | `lib/PetEmulator.Chips/AY38910.cs` | rejestry PSG, wybrany rejestr, stan obwiedni/szumu jeśli jest modelowany |
| `Cpc464Keyboard` | `src/PetEmulator.Cpc464/Cpc464Keyboard.cs` | stan matrycy 10x8 |
| `Cpc464Cassette` | `src/PetEmulator.Cpc464/Cpc464Cassette.cs` | pozycja odtwarzania/nagrywania, `MotorOn`, bufor pulsów/bajtów — sprawdź `HasTape`/`AtEndOfTape` pola z poprzedniego refaktoru mediów, muszą przetrwać snapshot |
| `Cpc6128Ports` | `src/PetEmulator.Cpc6128/Cpc6128Ports.cs` | latch-e PPI A/B/C, rejestr sterujący, wybrany numer expansion ROM (`DFxx`) |

Dla każdego: metoda `CaptureState()` zwraca nowy, prosty, niemutowalny
snapshot-typ (wzór: klasa `I8272Snapshot` w `CpcFdcSnapshot.cs` — `public
init`-only properties, `Version` pole), `RestoreState(TSnapshot)` ustawia
pola z powrotem. Nie dodawaj JSON-a na tym etapie, tylko surowe obiekty C#.

### 3b. Agregat `src/PetEmulator.Cpc6128/Cpc6128Snapshot.cs`

Jedna klasa spinająca wszystkie powyższe snapshoty plus:

- stan CPU: sprawdź co `Z80Cpu` już eksponuje (`Registers`, `CycleCount`,
  `Halted` używane gdzie indziej w repo) — jeśli `Z80Cpu` nie ma
  `CaptureState`, sprawdź najpierw czy jest współdzielony z CPC464/Kaypro/
  TRS-80 (tak, `PetEmulator.CpuZ80.Cpu.Z80Cpu`) i zrób impact analysis
  przed dotykaniem go — to najbardziej ryzykowna zmiana w tym kroku,
  bo dotyka wszystkich maszyn Z80 w repo.
- `I8272Snapshot` z `_fdc.CaptureState()` (gotowe, patrz wyżej).
- `FrameCount` z `Cpc6128Machine`.

### 3c. `Cpc6128Machine.CaptureState()`/`RestoreState(Cpc6128Snapshot)`

Woła capture/restore każdego składnika po kolei. `RestoreState` musi
odtworzyć kolejność zależności: RAM/ROM overlay przed GateArray (GateArray
czyta video RAM przez callback), FDC po dysku (dysk już musi być
zamontowany przed przywróceniem stanu transferu).

### 3d. `Cpc6128SnapshotCodec.cs` (serializacja)

Nie zgaduj formatu — sprawdź, czy jakakolwiek inna maszyna w repo ma już
JSON/binarny kodek do skopiowania stylu (na dzień pisania tego planu: nie
ma, patrz research wyżej). Jeśli nadal nie ma, użyj `System.Text.Json`
z `JsonSerializerOptions { WriteIndented = true }`, wersjonuj przez pole
`Version` na każdym poziomie (już masz to w `I8272Snapshot`/`DskSectorSnapshot`
itd. jako wzór), waliduj wersję przy deserializacji i rzucaj czytelny
wyjątek przy niezgodności zamiast cichego zepsutego stanu.

### 3e. Testy

`tests/PetEmulator.Cpc6128.Tests/Cpc6128SnapshotTests.cs`:

- capture → mutuj żywą maszynę (np. wykonaj kilka instrukcji, zmień bank
  RAM, załaduj dysk) → restore na **innej** instancji `Cpc6128Machine` →
  porównaj widoczny stan (rejestry CPU, zawartość konkretnych komórek RAM
  w kilku bankach, `Cylinder`/`Phase` FDC, zawartość ekranu po `RenderFrame()`).
- osobny test na "restore mid-transfer FDC" — zacznij komendę Read Data,
  nie dokończ, snapshot, restore, dokończ transfer — potwierdź że dane
  się zgadzają (to jest dokładnie przypadek, który `I8272Snapshot` już
  wspiera polami `Phase`/`TransferBytesRemaining`/`TransferBuffer`).

Commit: jeden na komponent z sekcji 3a (7 commitów) + jeden na 3b/3c
razem + jeden na 3d + jeden na 3e. Nie zwijaj w jeden wielki commit —
`detect_changes` po każdym musi zostać uruchomiony na mniejszym, czytelnym
diffie.

---

## Krok 4 — dokumentacja

### 4a. `docs/desktop/multi-machine.md`

Dodaj do sekcji "Media capabilities" (jest już tam opis capability
interfaces z poprzedniego refaktoru) zdanie o CPC6128: implementuje
`IDatasetteViewModel` + `IDiskDriveViewModel`, w przeciwieństwie do CPC464
które ma tylko `IDatasetteViewModel`. Do sekcji "Implementations" dodaj
wpis `Cpc6128MachineViewModel` (jedno zdanie, wzoruj się na formacie
istniejących wpisów dla `Cpc464MachineViewModel`).

### 4b. `docs/chips/I8272.md` (nowy plik)

Wzorzec: `docs/chips/MT6545.md` (checklist + sekcja "Implementacja i testy").
Wypełnij checklistę na podstawie tego, co `I8272Chip.cs` faktycznie robi
(komendy z `ExecuteCommand`: Read/Write Data, Seek, Recalibrate, Sense
Interrupt Status, Sense Drive Status, Read ID, Specify, Format Track,
Invalid) i co pokrywają `tests/PetEmulator.CpcFdc.Tests/I8272ChipTests.cs`
(8 testów — patrz plik, nie zgaduj z nazwy klasy). Zaznacz `[x]` tylko to,
co ma realny test; resztę `[ ]`. Dopisz do `docs/chips/README.md` wpis
w tabeli/liście chipów (sprawdź format istniejących wpisów w tym pliku
przed edycją).

Commit: `docs: document CPC6128 media capabilities and I8272 controller`

---

## Krok 5 — M9: regresja i zamknięcie planu

1. `dotnet build PetEmulator.slnx --nologo` — 0/0.
2. `dotnet test PetEmulator.slnx --filter "TestCategory!=BinaryBoot"` na
   **całym** solution (nie pojedynczych projektach) — wszystkie zwykłe testy
   zielone, zanotuj łączną liczbę testów w commit message. Testy oznaczone
   `BinaryBoot` są długotrwałe i pozostają opt-in; uruchamiaj je osobno przez
   `dotnet test PetEmulator.slnx --filter "TestCategory=BinaryBoot"`.
3. `detect_changes({scope: "compare", base_ref: "main"})` (albo CLI
   odpowiednik) na całej gałęzi zmian od `2ffacea` (pierwszy commit
   CPC6128) do HEAD — potwierdź brak nieoczekiwanych efektów ubocznych
   na CPC464/PET/inne maszyny.
4. Zaktualizuj `docs/plans/2026-09-16-cpc6128-integration-plan.md`:
   status na "M0-M9 zaimplementowane", zaktualizuj sekcję "Kryteria
   zakończenia" zaznaczając, co jest spełnione.
5. Ten plik (`2026-09-16-cpc6128-remaining-work-plan.md`) możesz zostawić
   jako log wykonania — nie usuwaj, dopisz na końcu "Status: wykonane
   w commitach X..Y" z realnymi hashami po zamknięciu.

Commit: `docs: close CPC6128 integration plan (M0-M9 complete)`

## Status wykonania — 2026-09-16

- Zrealizowane: M0-M8 oraz M9 build/domyślna regresja solution z filtrem
  `TestCategory!=BinaryBoot`; testy `BinaryBoot` pozostają opt-in zgodnie z
  `AGENTS.md`.
- Potwierdzone firmware: CPC6128 boot do `Ready` i `PRINT 1+1`; CPC464 boot
  oraz real-firmware SAVE/LOAD pozostają zielone.
- Otwarte: brak automatycznych testów firmware dla `CAT`, `|CPM` i odczytu DSK
  przez AMSDOS, więc plan nie jest jeszcze zamknięty jako pełne M0-M9.
