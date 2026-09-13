# Plan implementacji Intel 8080

## Stan realizacji

- [x] Etap 0 — utworzenie projektu biblioteki, projektu testów, wpisów solution i szkieletu Core.
- [x] Etap 1 — podstawowy stan, reset, snapshot/restore i lifecycle z działającym `NOP`.
- [x] Etap 2 — pełna tabela opcode’ów i metadane wszystkich 256 wartości.
- [x] Etapy 3–6 — zaimportowane wykonanie ISA, flagi, sterowanie, stos, I/O, `HLT` i podstawowa obsługa przerwań.
- [ ] Etapy 7–8 — ekstrakcja podzbioru dla Z80, pełne macierze testów i test binarny.

Weryfikacja wykonana po etapie 1:

- `dotnet build PetEmulator.slnx --no-restore --disable-build-servers` — PASS, 0 ostrzeżeń, 0 błędów.
- `dotnet test tests/PetEmulator.Cpu8080.Tests/PetEmulator.Cpu8080.Tests.csproj --no-restore --disable-build-servers` — PASS, 30/30.
- Macierz dispatchu obejmuje wszystkie 256 wartości opcode — PASS.
- Macierz flag obejmuje ADD/SUB/INR/DCR/ANA/DAA — PASS.
- Własny test binarny `.COM` ładowany pod `0x0100` — PASS; obejmuje ALU, skok warunkowy, zapis pamięci i `HLT`.
- Testy I/O, `EI/DI`, `INTE` i interrupt acknowledge — PASS.
- Snapshot/restore z aktywnym opóźnieniem `EI` — PASS.
- Długi test ZEXDOC Z80 pozostaje uruchomiony niezależnie od tej zmiany.

## Cel

Dodać pełną implementację Intel 8080 w oparciu o wspólne kontrakty `PetEmulator.Core`, a następnie wykorzystać ją jako źródło wspólnego podzbioru instrukcji dla Z80.

8080 jest bazą historyczną i częściowo ISA-bazą Z80, ale Z80 nie powinien dziedziczyć bezpośrednio po klasie wykonującej cały procesor 8080. Różnią się stanem, flagami, przerwaniami, timingiem, I/O i dekoderem prefiksów. Dziedziczenie powinno dotyczyć dopiero małych, zweryfikowanych elementów wspólnych albo generowanych definicji opcode’ów.

## Zasady architektoniczne

- [ ] `Cpu8080` używa `IProcessor`, `IDebuggableProcessor`, `CpuProcessorBase<TState>`, `OpcodeTable<TState>` i wspólnego `IMemoryBus`.
- [ ] Stan 8080 jest niezależny: `Cpu8080State : CpuState`; nie używa bezpośrednio `Z80State` ani pełnego rejestru flag Z80.
- [ ] Rejestracja opcode’ów odbywa się w jednym standardzie Core, w partialach CPU, bez lokalnego słownika dispatchu.
- [ ] Wspólne helpery ALU i adresowania są wydzielane dopiero po przejściu testów 8080; nie przenosić zachowania Z80 metodą kopiuj-wklej.
- [ ] Z80 może później korzystać ze wspólnych definicji podzbioru 8080, lecz zachowuje własny decoder prefiksów, stan i timing.
- [ ] Funkcje specyficzne dla 8080 są capability, a nie obowiązkową częścią bazowego `IProcessor`: 8-bitowe I/O, `INTE`, `HLT`, interrupt acknowledge i exact bus timing.
- [ ] Każdy etap kończy się testami fragmentu; migracja nie kończy się na kompilacji.

## Model procesora 8080

### Stan architektoniczny

- [ ] `A` — akumulator.
- [ ] `B`, `C`, `D`, `E`, `H`, `L` — rejestry 8-bitowe.
- [ ] `BC`, `DE`, `HL` — pary 16-bitowe złożone z rejestrów 8-bitowych.
- [ ] `SP` — wskaźnik stosu.
- [ ] `PC` — licznik programu.
- [ ] Flagi: `S`, `Z`, `AC`, `P`, `CY`.
- [ ] Stan sterowania: `Halted`, `InterruptsEnabled` (`INTE`), opóźnienie `EI`, licznik cykli i instrukcji.
- [ ] Snapshot/restore obejmuje wszystkie rejestry, flagi, `HLT`, `INTE`, opóźnienie `EI` i stan potrzebny do deterministycznego wznowienia.
- [ ] Nie dodawać do stanu 8080 rejestrów Z80: `IX`, `IY`, `I`, `R`, rejestrów alternatywnych, `IFF2` i `IM`.

### Magistrala i przerwania

- [ ] Pamięć ma adres 16-bitowy i dane 8-bitowe.
- [ ] I/O 8080 używa 8-bitowego numeru portu; nie zakładać z góry z80-owego 16-bitowego adresowania portów.
- [ ] Zdefiniować capability `I8080Bus`/adapter dla `ReadOpcode`, pamięci, I/O i obserwacji cyklu, bez rozszerzania minimalnego Core o szczegóły 8080.
- [ ] `INTE` blokuje maskowalne przerwania; 8080 nie ma odpowiednika Z80 `NMI`, `IFF1/IFF2` ani trybów `IM0/IM1/IM2`.
- [ ] Acknowledge przerwania przyjmuje dostarczony opcode lub wektor z urządzenia; domyślny model musi być jawnie opisany w kontrakcie.
- [ ] `HLT` zatrzymuje fetch instrukcji i pozostaje zatrzymany do resetu albo zaakceptowanego przerwania.
- [ ] Timing cyklowy jest capability 8080 i nie może być udawany samą liczbą cykli instrukcji.

## Sekwencja implementacji

### Etap 0 — kontrakt, baseline i zakres

- [x] Potwierdzić bieżące kontrakty Core i wzorce z `Cpu6502`, `Cpu6800` oraz `CpuZ80`.
- [x] Utworzyć projekt `lib/PetEmulator.Cpu8080` oraz `tests/PetEmulator.Cpu8080.Tests`.
- [x] Dodać projekty do `PetEmulator.slnx` i ustalić referencje wyłącznie do Core oraz pakietów testowych.
- [ ] Zapisać baseline: build rozwiązania, testy Core i istniejące testy Z80.
- [x] Zdefiniować roboczą politykę nielegalnych opcode’ów jako brak wpisu i jawny wyjątek z tabeli; finalna decyzja pozostaje w etapie 2.
- [x] Zdefiniować zakres jako Intel 8080-compatible, bez rozszerzeń 8085.

Kryterium: projekt jest pustym, kompilującym się szkieletem z publicznym procesorem i stanem.

### Etap 1 — stan i lifecycle

- [x] Dodać `Cpu8080State` i typowany widok rejestrów dla debuggera.
- [x] Dodać reset z wartościami `PC=0`, `SP=0`, `Flags=0`, `INTE=false` i `Halted=false`.
- [x] Podłączyć `StepInstruction`, `Reset`, `CycleCount`, `InstructionCount` i `Halted` do `CpuProcessorBase`.
- [x] Zaimplementować fetch opcode’u i inkrementację `PC` w jednym kontekście wykonania.
- [x] Dodać snapshot/restore oraz testy round-trip dla rejestrów i bitu `INTE`.
- [ ] Dodać pełny test obserwatora: before-step, opcode fetch, after-step i exception.

Kryterium: `NOP`, reset i zatrzymanie procesora działają bez opcode’ów rodziny.

### Etap 2 — standard opcode i metadane

- [x] Zdefiniować standardową stronę `OpcodeKey.Base(opcode)` dla wszystkich 256 wartości.
- [x] Dodać partial `Cpu8080.OpcodeRegistration.Base.cs` i rejestrację przez `OpcodeTable<Cpu8080State>`.
- [x] Dodać metadane: mnemonic, długość, tryb adresowania i nominalny timing.
- [x] Dodać test kompletności 256 wpisów oraz jawną politykę dla niezaimplementowanych opcode’ów.
- [ ] Dodać test braku duplikatów i test rozszerzenia/zmiany opcode’u przez klasę potomną bez mutowania tabeli bazowej.
- [ ] Nie tworzyć stron CB/ED/DD/FD — są elementem Z80, nie 8080.

Kryterium: debugger może odczytać opis każdej wartości opcode, a dispatch nie używa drugiej tablicy runtime.

### Etap 3 — transfer danych i adresowanie

- [x] `MOV r,r` dla wszystkich kombinacji, z `MOV M,r` i `MOV r,M` przez adres `HL`.
- [x] `MVI r,data`, `MVI M,data`, `LXI B/D/H/SP`.
- [x] `LDAX B/D`, `STAX B/D`, `LDA`, `STA`, `LHLD`, `SHLD`.
- [x] `XCHG`, `SPHL`, `PCHL`.
- [ ] Obsłużyć alias `M` jako pamięć pod adresem `HL`, bez tworzenia sztucznego rejestru.
- [ ] Testy: PC, odczyt/zapis pamięci, cykle, przypadki graniczne `0xFFFF` i wrap argumentu 16-bitowego.

Kryterium: pełna grupa transferu danych działa i ma testy wszystkich trybów adresowania.

### Etap 4 — arytmetyka, logika i flagi

- [x] Rejestry i pamięć: `ADD`, `ADC`, `SUB`, `SBB`, `ANA`, `XRA`, `ORA`, `CMP`.
- [x] Immediate: `ADI`, `ACI`, `SUI`, `SBI`, `ANI`, `XRI`, `ORI`, `CPI`.
- [x] `INR`, `DCR`, `INX`, `DCX`, `DAD`.
- [x] `RLC`, `RRC`, `RAL`, `RAR`, `DAA`, `CMA`, `STC`, `CMC`.
- [ ] Zaimplementować parzystość jako even parity dla wyniku 8-bitowego.
- [ ] Dokładnie rozdzielić zachowanie `AC` od `CY`; `INR`/`DCR` nie zmieniają `CY`.
- [ ] Ustalić i przetestować zachowanie bitów nieobecnych w 8080; nie mapować automatycznie flag `N`, `H`, `X`, `Y` z Z80.
- [x] Dodać macierze wartości: zero, znak, przepełnienie bez znaku, przeniesienie po bitach 3/7, parzystość i `DAA`.

Kryterium: każda instrukcja ALU ma test wyniku i pełnego zestawu pięciu flag 8080.

### Etap 5 — skoki, wywołania, stos i restart

- [x] Warunkowe i bezwarunkowe `JMP`: `JZ`, `JNZ`, `JC`, `JNC`, `JP`, `JM`, `JPE`, `JPO`.
- [x] `CALL` i wszystkie warianty warunkowe.
- [x] `RET` i wszystkie warianty warunkowe.
- [x] `PUSH`/`POP` dla `B`, `D`, `H` i `PSW`, z poprawną kolejnością bajtów.
- [x] `XTHL`, `SPHL`, `PCHL` oraz `RST 0..7`.
- [ ] Przetestować stos na początku, końcu i poza granicą pamięci zgodnie z polityką magistrali.
- [ ] Zweryfikować, że instrukcje warunkowe pobierają argumenty w poprawnej kolejności także wtedy, gdy warunek jest fałszywy.

Kryterium: kontrola przepływu i stos mają testy śladu PC/SP oraz cykli dla gałęzi spełnionej i niespełnionej.

### Etap 6 — I/O, HLT i przerwania

- [x] `IN port` i `OUT port` przez opcjonalny port bus 8080.
- [x] Brak port bus ma jawne zachowanie (`IN` zwraca `0xFF`, `OUT` jest ignorowane).
- [x] `EI` i `DI` aktualizują `INTE` z opóźnieniem `EI`.
- [x] `HLT` zatrzymuje normalne wykonywanie i nie zwiększa `InstructionCount` podczas bezczynnego kroku.
- [x] Przerwanie jest akceptowane tylko przy `INTE`; acknowledge dostarcza opcode RST.
- [ ] Po zaakceptowaniu przerwania `INTE` jest wyłączane, a stan stosu/PC odpowiada dokumentowanemu modelowi 8080.
- [x] Dodać testy reset → `EI` → interrupt i `DI` → interrupt.
- [x] Dodać test `HLT` → interrupt.
- [ ] Dodać test interrupt podczas oczekiwania.

Kryterium: pełna ścieżka urządzenie → acknowledge → wykonanie opcode’u przerwania jest deterministyczna.

### Etap 7 — ekstrakcja wspólnego podzbioru dla Z80

- [ ] Porównać definicje opcode’ów 8080 i bazowej strony Z80 na poziomie semantyki, a nie tylko mnemoniców.
- [ ] Wydzielić wyłącznie bezpieczne helpery: rejestry 8-bitowe, pary 16-bitowe, warunki, ALU i operandy pamięci.
- [ ] Wydzielić współdzielone definicje opcode tylko wtedy, gdy metadane i callback nie wymagają Z80-owego stanu.
- [ ] Zostawić osobne warstwy flag: 8080 (`S/Z/AC/P/CY`) i Z80 (`S/Z/H/PV/N/C` plus bity undocumented).
- [ ] Zostawić osobne timing adapters: 8080 nie może odziedziczyć refresh, prefiksów ani Z80-owych portów.
- [ ] Dodać test zgodności wspólnego podzbioru: dla tych samych rejestrów, pamięci i instrukcji wyniki są równe, a różnice timingowe są jawnie oczekiwane.

Kryterium: Z80 korzysta z kodu wspólnego tylko tam, gdzie test zgodności potwierdza brak regresji.

### Etap 8 — pełna regresja i testy binarne

- [ ] Testy kontraktów Core: lifecycle, snapshot, observer, debug registers, counters.
- [ ] Testy 8080: rejestracja, każda rodzina opcode, flags matrix, memory matrix, stack, I/O, HLT, interrupts i timing.
- [ ] Testy negatywne: nielegalny opcode, brak I/O, przerwanie przy `INTE=0`, restore niepełnego/niezgodnego snapshotu.
- [ ] Dodać testy parametrów i testy losowe/differential dla ALU względem niezależnego modelu referencyjnego.
- [x] Dodać krótką, własną walidację binarną 8080-compatible uruchamianą w CI.
- [ ] Dodać dłuższą walidację zewnętrznym diagnostycznym `TST8080.COM`/`8080PRE.COM`; w poprzednich iteracjach znaleziono tylko programy demonstracyjne `.com`, brak zweryfikowanego obrazu w checkoutach.
- [ ] Uruchomić regresję Z80 po ekstrakcji wspólnych helperów.
- [ ] Wykonać `dotnet build PetEmulator.slnx --no-restore`, testy projektów CPU, pełne testy rozwiązania i analizę zmian GitNexus przed commitem.

Kryterium: implementacja przechodzi testy jednostkowe, kontraktowe, integracyjne i binarne; wynik każdego testu jest zapisany w checkliście.

## Proponowany układ plików

```text
lib/PetEmulator.Cpu8080/
  Cpu8080.cs
  Cpu8080State.cs
  Cpu8080Registers.cs
  Cpu8080Bus.cs
  Cpu8080.OpcodeRegistration.Base.cs
  Cpu8080.OpcodeRegistration.DataTransfer.cs
  Cpu8080.OpcodeRegistration.Arithmetic.cs
  Cpu8080.OpcodeRegistration.ControlFlow.cs
  Cpu8080.OpcodeRegistration.Stack.cs
  Cpu8080.OpcodeRegistration.IoInterrupts.cs
  Cpu8080OpcodeMetadata.cs

tests/PetEmulator.Cpu8080.Tests/
  Cpu8080StateTests.cs
  Cpu8080OpcodeRegistrationTests.cs
  Cpu8080DataTransferTests.cs
  Cpu8080ArithmeticTests.cs
  Cpu8080FlagTests.cs
  Cpu8080ControlFlowTests.cs
  Cpu8080StackTests.cs
  Cpu8080IoInterruptTests.cs
  Cpu8080TimingTests.cs
  Cpu8080BinaryValidationTests.cs
```

## Ryzyka i decyzje

- [ ] Nie zakładać, że `Z80Cpu : Cpu8080`; najpierw udowodnić, że wspólny executor nie przenosi błędnych flag, timingów ani przerwań.
- [ ] Nie używać pełnego `Z80State` jako stanu 8080; prowadziłoby to do fałszywej zgodności i niejawnych zależności.
- [ ] Nie scalać I/O 8080 i Z80 do jednego kontraktu bez pola określającego szerokość adresu portu.
- [ ] Nie traktować zgodności opcode’ów jako zgodności cykli; testować oba poziomy osobno.
- [ ] Największe ryzyko regresji Z80 wystąpi przy ekstrakcji ALU, flag i rejestrów; etap 7 musi nastąpić dopiero po zamknięciu testów 8080.

## Definition of Done

- [ ] Istnieje działający `Cpu8080` oparty o wspólny Core.
- [ ] Wszystkie 256 wartości opcode mają jawny wpis, metadane i politykę wykonania.
- [ ] Wszystkie legalne instrukcje 8080 są zaimplementowane i pokryte testami.
- [ ] Snapshot, debugger, observer, timing, I/O, HLT i przerwania są przetestowane.
- [ ] Z80 korzysta ze wspólnych elementów tylko po przejściu testów zgodności i zachowuje swoje rozszerzenia.
- [ ] Test binarny 8080 oraz pełna regresja rozwiązania przechodzą.
- [ ] Dokumentacja i checklista zawierają rzeczywiste wyniki, a nie tylko planowane zadania.
