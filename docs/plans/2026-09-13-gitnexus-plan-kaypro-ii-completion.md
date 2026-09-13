# GitNexus Engineering Plan

> Task: dokończenie implementacji komputera Kaypro II
>
> Evidence verified at commit `a731cf1324eeec716b18c3af45f1252f17b8706a` on 2026-09-13. GitNexus index was refreshed with `--index-only --pdg` and is current for the baseline commit.
>
> Evidence provenance: schema 2; global dirty digest `sha256:0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd`.

## 1. Cel i kryterium sukcesu

Celem jest doprowadzenie obecnej bazy Kaypro II do emulacji całej ścieżki uruchomienia CP/M, bez hostowego skrótu omijającego Z80, pamięć, SIO, PIO, FDC i video.

Kryterium sukcesu:

- [ ] maszyna startuje z właściwym ROM-em monitora Kaypro II;
- [ ] dysk raw/TD0 jest zamontowany przez kontroler FDC i pozwala przejść przez bootloader;
- [ ] system dochodzi do stabilnego promptu `A>` w teście binarnym;
- [ ] klawiatura, konsola SIO, ekran 80x24, odczyt sektora, zapis sektora, reset i ponowne zamontowanie obrazu działają;
- [ ] wszystkie nowe urządzenia mają testy jednostkowe, kontraktowe i integracyjne;
- [ ] build, testy CPU/chipów, testy Kaypro i pełna regresja rozwiązania przechodzą.

## 2. Stan bazowy [verified]

Obecny commit zawiera fundament w `src/PetEmulator.Kaypro`:

- [x] projekt `PetEmulator.Kaypro` jest dodany do `PetEmulator.slnx`;
- [x] `KayproMachine` implementuje `IMachine`, tworzy `Z80Cpu`, magistralę i reset/run/step;
- [x] `KayproBus` ma 64 KiB RAM, 2 KiB ROM, VRAM, porty SIO/PIO/FDC i port systemowy;
- [x] istnieją szkielety `KayproSio`, `KayproPio`, `KayproVideo`;
- [x] `KayproDiskImage` obsługuje podstawowy obraz 40 ścieżek × 10 sektorów × 512 bajtów;
- [x] wspólny `FD1793` i `IFD1793DiskImage` pozostają w `lib/PetEmulator.Chips`;
- [x] istnieją testy resetu, mapowania pamięci, portów, obrazu dysku i cyklu maszyny.

Braki, które plan musi zamknąć:

- [ ] pełne zachowanie wariantu sprzętowego Kaypro II i jawny profil płyty/FDC;
- [ ] dokładna bankowana pamięć/ROM, latch systemowy i sygnały przerwań;
- [ ] pełne kanały SIO z timingiem, FIFO/rejestrem statusu i IM2;
- [ ] używalny PIO oraz model klawiatury, drukarki i linii napędu;
- [ ] loader TD0, w tym skompresowane rekordy, oraz trwały zapis zmian;
- [ ] font/ROM znaków i renderowanie 80x24;
- [ ] ROM monitora, obraz CP/M i deterministyczny test bootu;
- [ ] integracja CLI/Desktop i dokumentacja sprzętu/testów.

## 3. Granice architektury [verified][inferred]

Docelowy podział odpowiedzialności:

| Warstwa | Lokalizacja | Odpowiedzialność |
| --- | --- | --- |
| wspólne kontrakty | `lib/PetEmulator.Core` | `IMachine`, `IBus`, pamięć, zegar, debug/obserwacja |
| CPU | `lib/PetEmulator.CpuZ80` | Z80 i jego istniejące cykle/IRQ/NMI |
| układy współdzielone | `lib/PetEmulator.Chips` | FD1793 oraz ewentualne generyczne kontrakty nośnika |
| maszyna | `src/PetEmulator.Kaypro` | mapa pamięci, dekoder portów, wariant płyty i połączenia urządzeń |
| testy | `tests/PetEmulator.Kaypro.Tests` | testy urządzeń, magistrali, bootu i end-to-end |
| aplikacje | `src/PetEmulator.Cli`, `src/PetEmulator.Desktop` | wybór maszyny, wejście, obraz ekranu, status debuggera |

Istniejące pliki pozostają w bieżącej przestrzeni nazw. Podfoldery `Devices/`, `Storage/`, `Video/` należy wprowadzać dopiero wtedy, gdy liczba plików uzasadni podział; sam fizyczny ruch plików nie jest celem migracji.

## 4. Ustalenia GitNexus [graph]

- `KayproMachine` jest punktem wejścia przez `IMachine`; jego aktualnymi callerami są przede wszystkim testy Kaypro.
- `impact(IMachine, upstream)` zwróciło `HIGH`, 21 elementów i 7 bezpośrednich implementacji. To dolna granica z powodu dispatchu interfejsów, więc zmian w `IMachine` nie należy mieszać z implementacją Kaypro.
- `FD1793` ma szeroki wpływ (`CRITICAL` w poprzedniej analizie); nie kopiować układu do Kaypro i nie zmieniać jego kontraktu bez osobnej regresji wszystkich użytkowników.
- `KayproBus.Read/Write` rozdziela VRAM, ROM i RAM; `ReadPort/WritePort` rozdziela SIO, FDC, PIO i latch systemowy. Te gałęzie muszą pozostać rozłączne.
- `KayproMachine.Run` wykonuje kolejno `StepInstruction`, a dopiero potem `Bus.Tick`. Timing urządzeń i przerwań musi zachować tę kolejność.

## 5. Wyniki analizy przepływu PDG [graph]

Analiza PDG po ścieżkach plików potwierdziła:

- `Run`: warunek limitu instrukcji, inkrementacja indeksu i wywołanie `StepInstruction` są kontrolowane w jednej pętli;
- pamięć: zakres VRAM ma pierwszeństwo przed ROM/RAM, a ROM jest wybierany tylko przy aktywnym latchu;
- porty: pojedynczy adres trafia do dokładnie jednej gałęzi dekodera, a nieznany port zwraca `0xff`;
- zapis portu systemowego zmienia zarówno latch, jak i wybór napędu FDC;
- `Bus.Tick` jest właściwym miejscem do postępu urządzeń i zgłaszania NMI/DRQ.

Wniosek implementacyjny: nie przenosić timingów do `Z80Cpu`, nie skracać dekodera przez wspólny stan globalny i nie łączyć DRQ z IRQ bez jawnego modelu linii.

## 6. Decyzje sprzętowe do zamknięcia przed kodem

- [ ] wybrać emulowany wariant płyty: rekomendowany pierwszy cel to Kaypro II 81-110A/81-149C z kontrolerem zgodnym z FD1791; alternatywą jest późniejszy wariant z FD1793, który lepiej pasuje do obecnego układu;
- [ ] udokumentować różnice wariantu: adresy portów, aktywność bitów latcha, typ FDC, mapa ROM/VRAM, format dyskietki, linie SIO/PIO;
- [ ] ustalić źródło i licencję ROM-u monitora oraz fontu; binaria bez potwierdzonej proweniencji nie mogą być przedstawione jako oficjalne;
- [ ] ustalić, czy obraz CP/M i test TD0 mogą być przechowywane w repozytorium, czy test ma używać assetu opcjonalnego z hash-checkiem;
- [ ] ustalić docelową częstotliwość CPU i skalę zegara urządzeń na podstawie dokumentacji wariantu.

Do czasu decyzji kod powinien używać profilu `KayproIIHardwareProfile`, a nie rozsianych stałych. Profil domyślny ma być jawny i testowalny.

## 7. Sekwencja implementacji

### Etap 0 — baseline i profil sprzętowy

- [ ] dodać `KayproIIHardwareProfile` z mapą pamięci, portów, polaryzacją sygnałów i typem kontrolera;
- [ ] zamienić magiczne adresy w `KayproBus` na wartości profilu;
- [ ] dodać test snapshotu mapy portów i pamięci;
- [ ] zapisać wybraną rewizję płyty w `docs/kaypro/README.md`.

Akceptacja: istniejące testy Kaypro pozostają zielone, a zmiana profilu nie wymaga edycji kodu dekodera.

### Etap 1 — pamięć, reset i sygnały systemowe

- [ ] zaimplementować dokładne okno ROM/RAM i latch systemowy zgodnie z profilem;
- [ ] rozdzielić reset CPU, reset urządzeń i reset napędu;
- [ ] modelować WAIT, NMI, INT, DRQ i INTRQ jako jawne linie magistrali;
- [ ] zapewnić obserwację rejestrów i liczników cykli przez istniejące kontrakty Core.

Akceptacja: testy resetu, ROM-disable, zapisu RAM/VRAM, linii przerwań i kolejności `StepInstruction` → `Tick`.

### Etap 2 — nośnik i formaty dyskowe

- [ ] wydzielić wspólny interfejs sektora od formatów plikowych;
- [ ] zachować raw `KayproDiskImage` jako format testowy;
- [ ] dodać `KayproTd0Image` w warstwie `Storage`;
- [ ] zaimplementować nagłówek TD0, nagłówki ścieżek/sektorów, warianty kodowania i dekompresję LZHUF, jeśli występuje;
- [ ] odrzucać uszkodzone nagłówki, złą geometrię, skrócone sektory i nieobsługiwane flagi z czytelnym błędem;
- [ ] zapewnić zapis zmienionych sektorów do kopii roboczej, bez nadpisywania źródła bez jawnej zgody.

Akceptacja: round-trip raw/TD0 dla sektorów pustych, pełnych, uszkodzonych i zmodyfikowanych.

### Etap 3 — kontroler FDC i napęd

- [ ] nie duplikować `FD1793`; użyć go przez adapter profilu Kaypro;
- [ ] zweryfikować różnice FD1791/FD1793: komendy, status, DRQ, INTRQ, motor, side/drive select i timing;
- [ ] dodać kolejkę sygnałów od FDC do magistrali oraz właściwą polaryzację NMI/IRQ;
- [ ] obsłużyć odczyt/zapis sektora, seek, restore, force interrupt i błędy nośnika;
- [ ] dodać testy komend na poziomie chipu i end-to-end przez porty Kaypro.

Akceptacja: monitor potrafi wykonać seek, odczytać boot sector i zgłosić błąd dla brakującego napędu.

### Etap 4 — SIO, konsola i klawiatura

- [ ] zastąpić szkic pełnym modelem Z80 SIO: kanały A/B, data/control registers, RR/WR, reset, baud/timing i status;
- [ ] zaimplementować RX/TX FIFO oraz bezpieczne zachowanie overrun/underrun;
- [ ] dodać kanał konsoli, kolejkę wejściową klawiatury i wyjście znaków;
- [ ] zaimplementować przerwania IM2 zgodne z konfiguracją wektora SIO;
- [ ] dodać host-independent API do wstrzykiwania i odbierania bajtów;
- [ ] nie mieszać wejścia Avalonia z urządzeniem: UI ma wywoływać serwis/kontrakt maszyny.

Akceptacja: testy rejestrów, resetu, RX/TX, statusu, opóźnienia i przerwania; test bootu odbiera prompt bez ręcznego zapisu do RAM.

### Etap 5 — PIO i linie urządzeń

- [ ] zaimplementować tryby portów Z80 PIO, kierunek, maskę, handshake i przerwania;
- [ ] podłączyć klawiaturę, drukarkę, wybór napędu i sygnały stanu do profilu;
- [ ] rozdzielić urządzenia rzeczywiście obecne w wariancie od niepodłączonych bitów;
- [ ] dodać testy odczytu/zapisu portu, maski i przerwań.

Akceptacja: program diagnostyczny widzi właściwe bity PIO, a niepodłączone linie mają udokumentowane wartości.

### Etap 6 — video i font

- [ ] załadować font Kaypro z walidacją rozmiaru i hash/proweniencji;
- [ ] zaimplementować format znaków, atrybuty, kursora i scrollowania;
- [ ] odwzorować właściwe 80×24 oraz adresowanie VRAM profilu;
- [ ] udostępnić stabilny snapshot ekranu dla CLI/testów i model klatek dla Desktop;
- [ ] dodać renderowanie kontrolowanych znaków bez zależności od Avalonia.

Akceptacja: testy fontu i VRAM dają deterministyczny obraz znaków, a ekran startowy jest zgodny z oczekiwanym układem.

### Etap 7 — ROM, CP/M i test binarny

- [ ] dodać loader ROM monitora oraz jawne źródło assetu;
- [ ] dodać loader obrazu CP/M/boot disk przez raw lub TD0;
- [ ] dodać `KayproBootTests` z limitem kroków, fail-fast dla błędów CPU/FDC i checkpointami postępu;
- [ ] wykrywać prompt `A>` przez odczyt kanału konsoli, nie przez dopisanie tekstu do wyjścia;
- [ ] rozszerzyć test o `DIR`, odczyt pliku, zapis pliku, reset i ponowny boot;
- [ ] przechowywać wynik, liczbę instrukcji, cykle i ostatni stan CPU przy timeout/błędzie.

Akceptacja: test binarny przechodzi z dostępnym assetem i nieprzekraczalnym limitem bezpieczeństwa; brak assetu kończy test jako jawnie pominięty, nie jako fałszywy sukces.

### Etap 8 — CLI/Desktop i diagnostyka

- [ ] dodać Kaypro do rejestru/aplikacji CLI;
- [ ] dodać wybór obrazu dysku, ROM-u i profilu przez istniejące ustawienia;
- [ ] dodać ViewModel maszyny, ekran, konsolę, status napędu i przyciski reset/mount;
- [ ] zachować MVVM: code-behind tylko widok, bez logiki emulacji;
- [ ] dodać monitor rejestrów CPU, portów, FDC i linii przerwań.

Akceptacja: `apps` pokazuje Kaypro, `run` uruchamia maszynę z konfiguracją, a Desktop nie wymaga zmian w Core.

### Etap 9 — finalna regresja i dokumentacja

- [ ] uzupełnić `docs/kaypro/README.md` o mapę, wariant, ROM, formaty i ograniczenia;
- [ ] dodać checklistę pokrycia urządzeń i testów;
- [ ] uruchomić build rozwiązania, testy Kaypro, testy Z80/FD1793 i pełną regresję;
- [ ] uruchomić `gitnexus detect-changes --scope all` oraz porównać zmiany do `main`;
- [ ] sprawdzić `git diff --check`, zakres staged i brak assetów wygenerowanych;
- [ ] dopiero po tym wykonać commit/merge.

## 8. Macierz testów

### Jednostkowe

- [ ] profil: każda mapa adresów i bitów ma test;
- [ ] pamięć: ROM/RAM/VRAM, granice i reset;
- [ ] SIO: RR/WR, FIFO, timing, overrun, TX/RX i reset;
- [ ] PIO: tryby, kierunek, maska, handshake i przerwania;
- [ ] FDC: komendy, status, DRQ/INTRQ, seek i błędy;
- [ ] TD0/raw: geometria, dekodowanie, uszkodzenia i zapis kopii;
- [ ] video: font, atrybuty, kursor, scroll i snapshot 80×24.

### Kontraktowe i integracyjne

- [ ] każdy port ma test odczytu/zapisu przez `KayproBus`;
- [ ] CPU wykonuje instrukcję przed tickiem urządzeń;
- [ ] DRQ/INTRQ nie giną między FDC a CPU;
- [ ] SIO/Pio nie kolidują z dekoderem portów;
- [ ] mount/eject/reset nie zostawia stanu starego obrazu;
- [ ] debug snapshot jest spójny z maszyną zatrzymaną na granicy instrukcji.

### Testy binarne

- [ ] monitor ROM — reset vector i pierwszy prompt;
- [ ] CP/M — `A>`;
- [ ] `DIR`/odczyt pliku;
- [ ] zapis pliku i ponowne odczytanie;
- [ ] reset z zamontowanym obrazem;
- [ ] pełny dłuższy test z raportem co 100 mln instrukcji i checkpointem co 1 mld, zgodnie z mechanizmem używanym w pozostałych CPU.

## 9. Pliki i symbole

Pliki istniejące, które będą rozwijane:

- `src/PetEmulator.Kaypro/KayproMachine.cs` — lifecycle, reset, run, obserwacja;
- `src/PetEmulator.Kaypro/KayproBus.cs` — mapa pamięci i portów;
- `src/PetEmulator.Kaypro/KayproSio.cs` — SIO;
- `src/PetEmulator.Kaypro/KayproPio.cs` — PIO;
- `src/PetEmulator.Kaypro/KayproVideo.cs` — VRAM i obraz;
- `src/PetEmulator.Kaypro/KayproDiskImage.cs` — raw image;
- `tests/PetEmulator.Kaypro.Tests/KayproMachineTests.cs` — testy bazowe.

Pliki planowane:

- `src/PetEmulator.Kaypro/KayproIIHardwareProfile.cs`;
- `src/PetEmulator.Kaypro/KayproFdcAdapter.cs`;
- `src/PetEmulator.Kaypro/Storage/KayproTd0Image.cs` i ewentualnie decoder TD0;
- `src/PetEmulator.Kaypro/Video/KayproCharacterRom.cs`;
- testy `KayproSioTests`, `KayproPioTests`, `KayproDiskImageTests`, `KayproVideoTests`, `KayproBootTests`, `KayproEndToEndTests`;
- CLI/Desktop tylko po ustabilizowaniu kontraktu maszyny.

## 10. Komendy weryfikacyjne

```bash
dotnet build PetEmulator.slnx --no-restore --disable-build-servers
dotnet test tests/PetEmulator.Kaypro.Tests/PetEmulator.Kaypro.Tests.csproj --no-restore --disable-build-servers
dotnet test tests/PetEmulator.CpuZ80.Tests/PetEmulator.CpuZ80.Tests.csproj --no-restore --disable-build-servers
dotnet test tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj --no-restore --disable-build-servers --filter FullyQualifiedName~FD1793
dotnet test PetEmulator.slnx --no-restore --disable-build-servers
node .gitnexus/run.cjs detect-changes --scope all --repo .
git diff --check
```

Test binarny musi mieć konfigurowalny limit instrukcji, fail-fast dla nielegalnego stanu oraz raport postępu; nie wolno używać bezlimitowego procesu w automatycznej regresji.

## 11. Ryzyka i decyzje

- [ ] wariant FD1791 kontra FD1793: największe ryzyko dokładności; rozwiązanie — profil i adapter, bez cichego udawania zgodności;
- [ ] brak legalnego ROM-u/fontu: rozwiązanie — loader assetów z hash-checkiem i testy strukturalne niezależne od binariów;
- [ ] TD0 z kompresją: rozwiązanie — osobne testy dekodera i fixtures z błędami;
- [ ] przerwania Z80 SIO/PIO: rozwiązanie — testy IM2 i przejścia linii, nie tylko testy rejestrów;
- [ ] wydajność długiego bootu: rozwiązanie — checkpointy, licznik instrukcji i test diagnostyczny uruchamiany osobno;
- [ ] globalne kontrakty Core: `IMachine` ma HIGH impact; rozwiązanie — nie zmieniać interfejsu bez osobnego planu i pełnej regresji;
- [ ] UI może wymusić niewłaściwą zależność: rozwiązanie — adaptery ViewModel/serwisy i brak logiki Avalonia w maszynie.

## 12. Implementation Context Pack

```yaml
schema_version: 1
task: "Complete Kaypro II implementation"
repo: personal-004
head_commit: a731cf1324eeec716b18c3af45f1252f17b8706a
generated_plan_path: docs/plans/2026-09-13-gitnexus-plan-kaypro-ii-completion.md
evidence_provenance:
  schema_version: 2
  global_dirty_digest: sha256:0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd
  cited_paths:
    - PetEmulator.slnx
    - docs/README.md
    - docs/kaypro/README.md
    - lib/PetEmulator.Chips/FD1793.cs
    - lib/PetEmulator.Chips/IFD1793DiskImage.cs
    - lib/PetEmulator.Core/Abstractions/IMachine.cs
    - lib/PetEmulator.CpuZ80/Cpu/Z80Cpu.cs
    - src/PetEmulator.Kaypro/PetEmulator.Kaypro.csproj
    - src/PetEmulator.Kaypro/KayproMachine.cs
    - src/PetEmulator.Kaypro/KayproBus.cs
    - src/PetEmulator.Kaypro/KayproSio.cs
    - src/PetEmulator.Kaypro/KayproPio.cs
    - src/PetEmulator.Kaypro/KayproVideo.cs
    - src/PetEmulator.Kaypro/KayproDiskImage.cs
    - tests/PetEmulator.Kaypro.Tests/PetEmulator.Kaypro.Tests.csproj
    - tests/PetEmulator.Kaypro.Tests/KayproMachineTests.cs
    - src/PetEmulator.Pet/PetMachine.cs
    - src/PetEmulator.Vic20/Vic20Machine.cs
    - src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs
    - src/PetEmulator.Desktop/PetEmulator.Desktop.csproj
    - src/PetEmulator.Cli/Program.cs
primary_symbols:
  - KayproMachine
  - KayproBus
  - KayproSio
  - KayproPio
  - KayproVideo
  - KayproDiskImage
  - FD1793
  - IMachine
execution_path:
  - IMachine
  - KayproMachine.Run
  - KayproMachine.StepInstruction
  - Z80Cpu.StepInstruction
  - KayproBus.Read/Write/ReadPort/WritePort
  - KayproBus.Tick
pdg_constraints:
  - "CPU step precedes peripheral tick"
  - "VRAM branch precedes ROM/RAM memory branch"
  - "Each port address selects one device branch"
  - "System latch also controls FDC drive select"
patterns:
  - "PET/VIC-20 machine lifecycle through IMachine"
  - "shared chip implementation in PetEmulator.Chips"
  - "thin Desktop ViewModels and services"
verification:
  - "dotnet build PetEmulator.slnx --no-restore --disable-build-servers"
  - "targeted Kaypro, Z80 and FD1793 tests"
  - "full solution test"
  - "GitNexus detect-changes before commit"
risks:
  - "hardware variant mismatch"
  - "ROM/font provenance"
  - "TD0 compression"
  - "Z80 interrupt timing"
  - "IMachine high impact"
assumptions:
  - "existing Z80 and FD1793 remain shared implementations"
  - "Kaypro machine-specific behavior stays in src/PetEmulator.Kaypro"
open_questions:
  - "which exact Kaypro II board revision is the acceptance target"
  - "which ROM/font and CP/M assets may be committed"
avoid:
  - "do not duplicate FD1793"
  - "do not change Core interfaces in the first implementation phases"
  - "do not fake CP/M output in the boot test"
```

## 13. Definition of Done

- [ ] wybrany i opisany wariant Kaypro II;
- [ ] kompletna mapa pamięci, portów, przerwań i timingów;
- [ ] działające SIO, PIO, FDC, nośniki raw/TD0, video/font i klawiatura;
- [ ] ROM i CP/M uruchamiają się przez prawdziwy Z80 do `A>`;
- [ ] testy jednostkowe, kontraktowe, integracyjne i binarne są zielone lub jawnie oznaczone jako pominięte z powodu braku assetu;
- [ ] CLI/Desktop obsługują Kaypro bez logiki sprzętowej w UI;
- [ ] `docs/kaypro/README.md` opisuje ograniczenia i proweniencję assetów;
- [ ] build i pełna regresja rozwiązania przechodzą;
- [ ] analiza GitNexus zmian jest czysta, a zakres commita zawiera tylko planowaną funkcję.
