# Docelowa architektura CPU i maszyn

## Werdykt architektoniczny

Najlepsze cechy znalezionych projektow nalezy polaczyc tak:

1. Stan rejestrow procesora jako osobny, typowany obiekt.
2. Dispatch instrukcji przez tablice 256 opcode, nie przez wielki `switch`.
3. Warianty jako pochodne tablice opcode i konfiguracje zachowania.
4. CPU jako executor, bez wiedzy o PET, ekranie i konkretnych urzadzeniach.
5. Maszyna jako orkiestrator CPU, busa, zegara i urzadzen.
6. Testy jednostkowe opcode, testy cykli, testy binarne ROM i testy integracyjne maszyny.

Dziedziczenie powinno dotyczyc tablic instrukcji lub konfiguracji wariantu, a nie kopiowanych klas CPU. Dzieki temu dodanie 65C02, 6510 albo 2A03 nie wymaga przepisywania calego rdzenia.

## Rejestry

Najlepszy wzorzec obiektu rejestrow pochodzi z Z80 w `psudo-cpu` i `personal-003`:

- pojedynczy obiekt zawierajacy stan architektoniczny,
- rejestry 8-bitowe oraz wygodne pary 16-bitowe,
- reset w jednym miejscu,
- opcjonalny dostep po nazwie dla debuggera.

Dla 6502 docelowo:

```csharp
public sealed class Cpu6502Registers
{
    public byte A { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public byte SP { get; set; }
    public byte P { get; set; }
    public ushort PC { get; set; }

    public void Reset()
    {
        A = X = Y = 0;
        SP = 0xFD;
        P = 0x24;
        PC = 0;
    }
}
```

Obiekt rejestrow powinien byc zywszym stanem CPU. Snapshot/debugger powinien otrzymywac osobna niemutowalna kopie, np. `Cpu6502State`. Nie nalezy uzalezniac handlerow od kilkunastu prywatnych pol CPU.

Flagi pozostaja logicznie czescia rejestru `P`, ale warto udostepnic cienki `Cpu6502Flags` dla debuggera i testow. Nie tworzyc osobnego zrodla prawdy dla bitow flag.

## Tablica opcode

Najlepszy fundament daje `proc-vibe-001`:

- `OpcodeTable<TCpu>.Set()` do dodawania i podmiany opcode,
- `DeriveWith()` do budowania wariantu z tabeli bazowej,
- blad dla niezaimplementowanego opcode,
- test kompletności przestrzeni 0-255.

Docelowy wpis powinien przechowywac nie tylko delegate:

```csharp
public sealed record OpcodeDefinition(
    byte Opcode,
    string Mnemonic,
    AddressingMode AddressingMode,
    byte Length,
    byte BaseCycles,
    OpcodeHandler Handler,
    bool HasPageCrossPenalty = false);
```

Tabela powinna byc kopiowalna przez builder/factory, ale po zbudowaniu traktowana jako immutable. Nie wystawiac publicznego `RegisterOpcode()` do zmiany produkcyjnego CPU w dowolnym momencie. Rejestracja runtime jest przydatna tylko dla testow, eksperymentow i narzedzi.

Handler powinien zwracac wynik wykonania zawierajacy liczbe cykli, np. `InstructionResult`. Obecny rdzen jest cyklowy, wiec nie wolno zgubic page crossing, dodatkowych zapisow RMW ani cykli przerwan podczas migracji.

## Warianty procesora

Tablice budowac warstwowo:

```text
Official6502
  -> Nmos6502       + undocumented opcode + NMOS quirks
  -> Nes2A03        - BCD + zachowanie Ricoh
  -> Cmos65C02      - undocumented + poprawki + nowe opcode
  -> Commodore6510  + port I/O jako element maszyny/busa
```

Wariant powinien miec dwa zrodla roznic:

- tablica opcode: opcode dostepne, usuniete, dodane lub podmienione,
- `CpuBehavior`: BCD, JMP indirect bug, RMW write twice, zachowanie IRQ/NMI.

Nie kodowac takich roznic przez serie `if (variant == ...)` w kazdym handlerze. Jesli wariant zmienia caly opcode, podmienic wpis w tabeli. Jesli zmienia semantyke wspolnej instrukcji, uzyc jawnej polityki zachowania przekazanej do CPU.

## Maszyna

Obecny `IMachine` w `lib/PetEmulator.Core/IMachine.cs` jest dobrym kontraktem lifecycle. Docelowa implementacja powinna zachowac jego prostote i dodac dopiero wtedy, gdy beda konsumenci:

```text
IMachine
  ├── IProcessor / Cpu6502
  ├── IMemoryBus
  ├── IClock
  └── IReadOnlyList<IDevice>
```

`StepInstruction()` oznacza krok calej maszyny, nie tylko CPU. Maszyna wykonuje instrukcje CPU, a potem zasila urzadzenia liczba zuzytych cykli. `StepCycle()` mozna dodac pozniej jako osobny kontrakt, gdy debugger lub video bedzie wymagal granularity cyklu.

Core (`lib/PetEmulator.Core`) definiuje kontrakty busa, procesora, zegara, urządzeń, maszyny oraz globalne pojęcia niezależne od komputera, takie jak katalog klawiszy AT. PET implementuje mapowanie adresowe, klawiaturę i kolejność taktowania swoich chipów; VIC-20 robi to samo według własnego profilu.

## Status migracji Z80 do wspólnego Core

Z80 jest obecnie przykładem rodziny korzystającej ze wspólnej abstrakcji procesora:

- `Z80Cpu` dziedziczy po `CpuProcessorBase<Z80State>`.
- Stan, snapshot, lifecycle, liczniki, debugger i `IProcessor` są obsługiwane przez Core, z zachowaniem adapterów kompatybilności Z80.
- Opcode’y Base, CB, ED, DD i FD są przechowywane w `Core.OpcodeTable<Z80State>`; rozszerzenia wariantów używają `ConfigureOpcodes` i chronionego `RegisterOpcode`.
- Pamięć, I/O, WAIT, refresh, acknowledge przerwań, `BusCycle`, watchpointy i obserwator wykonania mają testy kontraktowe.
- Metadane opcode’ów obejmują mnemonic, długość, tryb adresowania i timing; testy sprawdzają kompletność wpisów oraz zgodność reprezentatywnych timingów z `Step()`.

Do zamknięcia migracji pozostają pełne przebiegi `ZEXDOC` i `z80doc.tap`, pełny build/regresja rozwiązania oraz końcowy przegląd martwych adapterów. Szczegółowy status i liczba pozostałych czynności są prowadzone w [checkliście migracji Z80](../plans/2026-09-12-z80-core-migration-checklist.md).

## Jak zbudować komputer z klocków

Każdą emulowaną maszynę składaj z tych samych elementów, a różnice sprzętowe trzymaj w
profilu i implementacjach konkretnego komputera:

```text
Machine
├── Processor      CPU + wariant zachowania
├── MemoryBus      dekoder adresów
├── Memory         RAM, ROM i open bus
├── Chips          urządzenia mapowane pod adresami I/O
├── Input          klawiatura AT → macierz komputera
├── Display        stan ekranu → framebuffer
└── Timing         cykle CPU → Tick(cycles) urządzeń
```

Kolejność budowy:

1. Zdefiniuj profil sprzętu: wariant CPU, rozmiar RAM, adresy ROM/RAM/I/O, układ klawiatury,
   geometrię ekranu i wymagane ROM-y.
2. Zaimplementuj CPU jako komponent niezależny od maszyny. CPU zna tylko `IProcessor`, zegar i
   `IMemoryBus`; nie zna PET, VIC-20 ani ich chipów.
3. Zbuduj `MemoryBus`, który dla każdego adresu wybiera RAM, ROM, chip albo open bus. Najpierw
   testuj odczyt/zapis rejestrów bez uruchamiania ROM-u.
4. Dodaj chipy jako niezależne urządzenia (`IMemoryMappedDevice`) i podłącz je do dekodera.
   Maszyna przekazuje im liczbę cykli zużytych przez CPU.
5. Dodaj wejście: użyj globalnego `AtKeyboardKey`, a mapowanie AT → macierz trzymaj w klasie
   profilu. Klawisz nieobecny w danym modelu musi być jawnie niemapowalny, nie „zgadywany”.
6. Dodaj wyjście: ekran czyta pamięć/chip w formacie właściwym dla sprzętu i renderuje własny
   framebuffer. Nie zakładaj, że tekstowy PET i pikselowy VIC-20 mają ten sam renderer.
7. Zdefiniuj reset i krok maszyny: reset CPU/chipów, `StepInstruction()`, przekazanie cykli do
   urządzeń, aktualizacja IRQ/NMI i dopiero potem wyższe funkcje.
8. Weryfikuj warstwami: chipy i bus, reset wektor, boot z prawdziwym ROM-em, klawiatura przez
   macierz, ekran, a na końcu urządzenia zewnętrzne i UI.

### Przykład: PET

`PetMachine` składa `Cpu6502Classic`, `PetMemoryBus`, RAM/ROM oraz `MT6520`, `MOS6522` i
opcjonalny `MT6545`. Profil ROM-u wybiera także layout klawiatury: `Pet2001Graphics`, `Cbm4032`
albo `Cbm8032`. To ważne, bo BASIC 1/2/4 nie opisuje całego okablowania klawiatury — profil jest
źródłem zweryfikowanego mapowania dla konkretnego zestawu ROM-ów.

### Przykład: VIC-20

`Vic20Machine` używa tego samego CPU i kontraktów Core, ale własnego busa, `MOS6560`, `MOS2114`,
dwóch `MOS6522`, 8×8 macierzy klawiatury i renderera pikselowego. `Vic20KeyboardMap` nadpisuje
globalny katalog klawiszy AT; klawisze bez odpowiednika w VIC-20 pozostają `null`.

Ta sama zasada pozwala dodać kolejny komputer bez kopiowania CPU, debuggera, zegara ani katalogu
klawiatury: nowa maszyna dostarcza tylko własny profil, bus, chipy, mapy wejścia i renderer.

## Testowalnosc

Warstwy testow powinny byc rozdzielone:

1. `OpcodeTableTests`: wszystkie 256 wpisow, partycjonowanie official/undocumented/undefined i poprawne tabele wariantow.
2. Testy instrukcji: rejestry, flagi, adresowanie, zapis do busa.
3. Testy timing: bazowe cykle, page crossing, RMW double-write, IRQ/NMI.
4. Testy ROM: Klaus Dormann, `nestest` i inne niezalezne ROM-y referencyjne.
5. Testy maszyny: reset vector PET, mapowanie RAM/ROM/I/O, taktowanie PIA/VIA/CRTC.
6. Testy integracyjne: boot BASIC, klawiatura, ekran, IEEE-488, tape i snapshot.

Testy ROM nie powinny byc jedynym dowodem poprawnosci. Test ROM moze przejsc mimo blednego API publicznego, zlego trace albo niepoprawnych cykli urzadzen.

## Debugger i snapshot

Najlepsze elementy debuggera pochodza z `personal-002`, `personal-003` i `cpu-vibe-012`:

- odczyt rejestrow po nazwie,
- memory peek bez efektow ubocznych,
- trace instrukcji i busa,
- snapshot CPU i maszyny,
- zegar jako osobny komponent,
- warstwa adapterow `Registers`, `Flags`, `AddressSpace`, `Clock`.

Adaptery sa granica dla UI/API, ale nie powinny zastapic typowanego stanu wewnatrz CPU. Snapshot musi obejmowac stan CPU, pamiec, urzadzenia, zegar i pending IRQ/NMI.

## Plan migracji obecnego rdzenia

1. Zachowac obecny cyklowy rdzen jako referencje, poniewaz przechodzi testy ROM i testy cykli.
2. Wydzielic `Cpu6502Registers` i przepiac properties CPU na ten obiekt.
3. Dodac `OpcodeDefinition`, builder tablicy i testy przestrzeni opcode.
4. Przenosic grupy instrukcji z `ExecuteCycle...` do handlerow tabeli, jedna rodzina naraz.
5. Zbudowac `Official6502`, `Nmos6502`, `Nes2A03` i `Cmos65C02` przez `Derive`.
6. Porownywac po kazdym kroku testy jednostkowe, trace i ROM-y.
7. Dopiero po zgodnosci usunac stary dispatch.

Nie wykonywac pelnego przepisywania CPU i PET jednoczesnie. Najmniejsza bezpieczna jednostka zmiany to jedna rodzina opcode plus jej testy i niezmienione testy ROM.

## Czego nie kopiowac

- Wielkiego `switch`/dispatchu z obecnego `personal-001` jako docelowego modelu.
- Mutowalnej globalnej tablicy opcode wspoldzielonej przez wszystkie instancje.
- Uniwersalnego slownika rejestrow jako jedynego stanu CPU.
- Kontraktu maszyny zawierajacego szczegoly PET albo konkretnego UI.
- Pelnego frameworka konfiguracji maszyn, zanim pojawi sie druga realna maszyna.

## Znany bug w referencyjnym rdzeniu (personal-001)

`personal-001` — trzymany jako referencja testow ROM/cykli (patrz tabela ponizej) — ma niebezpieczny wzorzec w glownej petli instrukcji, ktory **nie wolno kopiowac**:

```csharp
// personal-001, Cpu6502.CycleStepped.Core.cs:84-90
while (!_sync)
{
    var key = (ushort)((_currentOpcode << 3) | _cycleCount);
    ExecuteCycle(key);
    _cycleCount++;
    _cycle++;
}
```

`_cycleCount` to `byte` bez maskowania do 3 bitow. Jesli ktorykolwiek cycle-handler (typowo illegal/unstable opcode) nie ustawi `_sync=true` na danym cyklu, licznik rosnie ponad 7 i zaczyna kolidowac z bitami opcode w kluczu dispatch (`<<3`) — CPU wpada w cudzy handler bez gwarancji powrotu. Skutek: nieskonczona petla wewnatrz jednego kroku instrukcji, niewidoczna dla zewnetrznego limitu instrukcji maszyny (`PetMachine.Run(n)` nigdy nie wraca). Ujawnia sie tylko na realnym kodzie KERNAL/BASIC (integration testy `PetMachineTests`/`PetDesktopIntegrationTests` w personal-001 wieszaja sie z tego powodu), nie na synthetic unit testach pojedynczych opcode. Szczegoly: [deep-analysis.md](deep-analysis.md).

**Wlasny rdzen ma ten sam ksztalt ryzyka** (bez collision na kluczu dispatch, bo handler jest tabelowy per-opcode, ale bez capa na iteracje):

```csharp
// ten projekt, Cpu6502.CycleStepped.Core.cs:117-122
while (!_sync)
{
    _currentDefinition!.Handler(this, _currentOpcode, _cycleCount);
    _cycleCount++;
    _clock.Advance(1);
}
```

Jesli jakikolwiek handler w tabeli (zwlaszcza przy dodawaniu nowych wariantow/illegal opcode) zapomni ustawic `_sync=true` na ktoryms cyklu, petla nie ma gornej granicy. Przed uznaniem rdzenia za finalny (krok 6 planu migracji) dodac defensywny cap, np. `if (_cycleCount > 7) throw` albo assert w trybie debug — degraduje cichy hang do szybkiego bledu przy pisaniu nowego handlera.

## Zrodla wzorcow

| Cecha | Zrodlo | Decyzja |
| --- | --- | --- |
| Tablica opcode i pochodne warianty | `proc-vibe-001` | adoptowac |
| Obiekt rejestrow z parami i shadow registers | `psudo-cpu`, Z80 | adaptowac do 6502 |
| Rozdzielenie CPU, flags, address space i clock | `cpu-vibe-012` | adoptowac jako adaptery |
| Grupowanie handlerow opcode | `cpu-vibe-003` | adoptowac selektywnie |
| Cyklowy CPU i testy ROM | `personal-001`, `6502` | zachowac jako referencje |
| Maszyna PET i kolejnosc taktowania urzadzen | `personal-001` | adoptowac po stabilizacji CPU |
