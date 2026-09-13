# Zilog Z80 SIO — checklista niezależnego układu

Dokument definiuje implementację standardowego układu Zilog Z80 SIO/SIO-O jako
niezależnego chipu. Kaypro, TRS-80 i inne komputery mają dostarczać osobny
adapter okablowania, mapowania portów i źródeł zegara. Kod układu nie może
zawierać nazw ani reguł Kaypro.

Stan implementacji: `PetEmulator.Chips.Z80Sio` został wydzielony z poprzedniego
`KayproSio`. Punkty nieobsługiwane są oznaczone jawnie; wartości profilu i
adaptera nie są ukrywane przez stałe zależne od konkretnej maszyny.

Ostatnia paczka: bitowy codec async, sync 8/16/external, auto-echo/local loopback,
CRC-X25 i codec SDLC/HDLC; cykl życia odbiornika z jawnym flush bufora, BREAK/ABORT dla Tx/Rx,
status RR1 i zdarzenia Ext/Status,
buforowany i taktowany nadajnik Tx z `TxEmpty`/`AllSent`,
rzeczywisty timing asynchronicznej ramki na podstawie WR3/WR4,
RR3 i statusowe przerwania Ext/Status dla zmian CTS/DCD oraz
snapshot/restore pełnego stanu układu, obejmujący rejestry obu kanałów, bufor Rx,
timing, pending IRQ oraz linie modemowe. Wcześniejsza paczka
objęła konfigurację asynchroniczną WR3–WR5, enable Rx/Tx, status DCD/CTS,
wyjścia RTS/DTR i blokowanie Tx przez CTS.

Kanały mogą działać na dwóch poziomach: `IZ80SioChannel` przenosi gotowe
znaki, a opcjonalny `IZ80SioBitChannel` przenosi pojedyncze bity. Rdzeń używa
drugiego kontraktu do składania ramek async, taktowania Tx oraz obsługi ramek
SDLC; postęp częściowej ramki jest częścią snapshotu.

## Zakres układu

- [x] Obsłużyć wariant `Z8440 SIO/O` jako jawny profil rewizji przez
  `Z80SioRevision.Z8440` i `Z80SioProfile`.
- [x] Nie mieszać SIO z UART-em hosta ani z kolejką klawiatury; kanał hosta jest
  opcjonalnym kontraktem `IZ80SioChannel`.
- [x] Udostępnić dwa niezależne kanały A i B.
- [x] Rozdzielić interfejs CPU, kanał szeregowy, timing i przerwania od logiki Kaypro.
- [x] Zaprojektować adapter `KayproSioWiring` nad gotowym chipem; alias
  `KayproSio` zachowuje kompatybilność istniejących wywołań.

## Model i kontrakty

- [x] Umieścić niezależny układ w `PetEmulator.Chips`.
- [x] Zdefiniować `Z80Sio` bez znajomości `KayproMachine`.
- [x] Zdefiniować `IZ80SioChannel` dla RxD/TxD, zegarów Rx/Tx i linii modemowych.
- [x] Zdefiniować opcjonalny `IZ80SioBitChannel` dla RxD/TxD na poziomie bitów.
- [x] Zdefiniować `IZ80SioInterruptSource`/wspólny kontrakt daisy-chain.
- [x] Zdefiniować `Z80SioState` obejmujący oba kanały, rejestry, bufory, zegar i IRQ.
- [x] Zdefiniować `ReadPort`/`WritePort` dla czterech kombinacji data/control × A/B.
- [x] Zdefiniować `Tick(tStates)` dla postępu czasu odbiornika.
- [x] Zdefiniować `Reset()` i reset kanału zgodny z komendą WR0.
- [x] Zdefiniować snapshot/debug bez udostępniania prywatnych kolejek jako mutowalnych obiektów.

## Interfejs CPU i dekoder

- [x] Obsłużyć sygnały `CE`, `IORQ`, `RD`, `M1`, `C/D`, `B/A`, `RESET` na poziomie
  `Z80SioBusAdapter`; są aktywne logicznie w `Z80SioBusSignals`.
- [x] Odwzorować cztery adresy: A-data, B-data, A-control, B-control.
- [x] Nie kodować konkretnego adresu portu; adres bazowy jest parametrem układu, a mapowanie Kaypro należy do adaptera.
- [x] Odrzucać zwykły odczyt/zapis podczas `M1`; osobna ścieżka
  `TryAcknowledgeInterrupt` obsługuje cykl `M1+IORQ`.
- [x] Zdefiniować wartości dla nieprawidłowych portów i nieobsługiwanych odczytów
  RR w `Z80SioProfile`; domyślnie jest to odpowiednio `0xFF` i `0x00`.

## Rejestry zapisu WR0–WR7

- [x] WR0: implementować wybór rejestru, komendy kanału i reset błędów.
- [x] WR0: implementować `Channel Reset`, `Enable Int on Next Rx Character`,
      `Reset Tx Interrupt Pending` i `Error Reset`; pozostałe komendy są jeszcze otwarte.
- [x] WR1: implementować Tx/Rx interrupt enable oraz tryby Rx używane przez model.
- [x] WR1: implementować `Status Affects Vector` dla modelowanych źródeł.
- [x] WR2: implementować wspólny wektor przerwań układu zgodnie z rewizją SIO.
- [x] WR3: implementować enable Rx i dekodowanie liczby bitów Rx; auto enable/CRC pozostają otwarte.
- [x] WR4: implementować rozpoznanie async, parity, bity stopu i mnożnik zegara; synchronizacja pozostaje otwarta.
- [x] WR5: implementować Tx enable, DTR, RTS i dekodowanie liczby bitów Tx; break/CRC pozostają otwarte.
- [x] WR6/WR7: zapisywać znaki synchronizacji 8/16-bit; tryb i dekoder SDLC/HDLC
  są jawnie modelowane przez `Z80SioFrameMode` i codec.
- [x] Zapewnić prawdziwy protokół wyboru rejestru: pierwszy zapis wybiera rejestr, następny go programuje.
- [x] Testować pointer rejestru niezależnie dla kanałów oraz jego reset po dostępie.

## Rejestry odczytu RR0–RR3

- [x] RR0: Rx character available, Tx buffer empty, DCD i CTS; pozostałe bity linii/statusu są jeszcze otwarte.
- [x] RR1: all sent, parity, Rx overrun, framing error i break detected; CRC/EOM/special receive są jeszcze otwarte.
- [x] RR2: interrupt vector oraz podstawowa modyfikacja wektora przez status/channel/źródło IRQ.
- [x] RR3: pending interrupt bits dla modelowanych Rx/Tx/Ext-Status obu kanałów.
- [x] Udokumentować różnice profili SIO/0, SIO/1, SIO/2 i Z8440 zamiast maskować
  je stałym `0x00`.

### Profile rewizji

| Profil | RR3 | Daisy-chain IEI/IEO | Nieprawidłowy port | Nieobsługiwany RR |
| --- | ---: | ---: | ---: | ---: |
| SIO/0 | nie | nie | `0xFF` | `0x00` |
| SIO/1 | nie | tak | `0xFF` | `0x00` |
| SIO/2 | tak | tak | `0xFF` | `0x00` |
| Z8440 SIO/O | tak | tak | `0xFF` | `0x00` |

Tabela opisuje politykę emulacji dostępną przez `Z80SioProfile`. Nie oznacza,
że każdy detal maski rejestrów i zachowania przerwań konkretnej rewizji został
już zaimplementowany; nieobsługiwane rejestry są zwracane jawnie według profilu.

- [x] Testować kasowanie bufora i błędu overrun przez odczyt danych/komendę WR0.

## Kanał odbiorczy i nadawczy

- [x] Zaimplementować niezależne API RxD/TxD dla kanału A i B.
- [x] Zaimplementować ograniczony model bufora odbiornika oraz nadajnika z eventem `Transmitted`.
- [x] Rozróżnić buforowany bajt dostępny dla CPU i czas jego udostępnienia.
- [x] Wykrywać overrun, parity error i framing error; CRC/break są jeszcze otwarte.
- [x] Implementować enable/disable receiver, zachowanie po wyłączeniu Rx oraz `FlushReceiveBuffer`.
- [x] Implementować transmisję bitową; `Z80SioBitCodec` koduje/dekoduje start/stop,
  parity i liczbę bitów znaku, a `IZ80SioBitChannel` jest podłączony do timingów
  Rx/Tx rdzenia.
- [x] Implementować bufor Tx, `Tx empty`, `All sent` i kolejność zdarzeń przy zapisie kolejnych bajtów; underrun/EOM pozostają otwarte.
- [x] Implementować `Send Break` w WR5, zdarzenie zmiany linii i wykrycie break po stronie Rx; abort/SDLC pozostają otwarte.
- [x] Implementować auto echo i local loopback jako funkcje kanału, nie testu hostowego.

## Tryby synchroniczne

- [x] Implementować sync character 8/16-bit oraz tryb external sync.
- [x] Implementować SDLC/HDLC: flag, zero insertion/deletion, abort, CRC i end-of-frame
  w `Z80SioSdlcCodec`, z integracją Rx/Tx przez `ReceiveBit` i `TransmitSdlcFrame`.
- [x] Implementować CRC generator/checker z presetem X-25 i kolejnością LSB-first.
- [x] Oddzielić tryb asynchroniczny od synchronicznego w stanie kanału oraz snapshot/restore trybu.
- [x] Dodać testy krótkich ramek, ramek z zerami, abortu i błędnego CRC.

## Zegary i timing

- [x] Obsłużyć osobne zegary Rx i Tx dla każdego kanału przez `IZ80SioChannel`.
- [x] Dekodować mnożniki x1, x16, x32 i x64 w konfiguracji kanału; ich pełne użycie w generatorze bitów pozostaje otwarte.
- [x] Nie zastępować zegara ramkowego stałym czasem: async Rx uwzględnia `BaudRate`, mnożnik WR4 i format ramki.
- [x] Określić zachowanie przy zegarze 0, zbyt wolnym zegarze i zmianie konfiguracji
  w trakcie ramki: zatrzymany zegar lub `BaudRate=0` wstrzymuje postęp bez utraty
  danych, a zmiana baud rate działa od następnej ramki.
- [x] Testować dostępność bajtu dopiero po czasie jednej modelowanej ramki.
- [x] Testować zmianę baud rate, zatrzymanie zegara i kolejkę kilku ramek;
  nadajnik przechowuje do `TxBufferDepth` oczekujących bajtów.

## Linie modemowe i handshake

- [x] Modelować RTS, CTS, DTR i DCD jako linie kanału; SYNC/WAIT/READY pozostają otwarte.
- [x] Udostępnić stan linii bez narzucania polaryzacji adaptera.
- [x] Zablokować Tx przy nieaktywnym CTS.
- [x] Generować Ext/Status interrupt dla zmian DCD/CTS; break/sync pozostają otwarte.
- [x] Testować przejścia CTS/DCD przed transmisją i po jej odblokowaniu.

## Przerwania i daisy-chain

- [x] Zaimplementować model `IEI`/`IEO` i priorytet układu w
  `IZ80SioInterruptSource`.
- [x] Rozróżnić modelowane źródła Rx, Tx i Ext/Status; special receive condition pozostaje otwarte.
- [x] Zaimplementować priorytet kanału A/B dla modelowanych źródeł.
- [x] Zwracać wektor IM2 z podstawową modyfikacją status/channel.
- [x] Zaimplementować blokowanie niższego źródła podczas obsługi wyższego;
  nowe ACK jest odrzucane do czasu zakończenia bieżącego ISR.
- [x] Obsłużyć zakończenie ISR przez jawne `NotifyReti()` w kontrakcie daisy-chain
  (`CompleteInterrupt()` pozostaje aliasem kompatybilności).
- [x] Dodać testy bez `EI`, z `EI`, priorytetu, maskowania i kilku jednoczesnych źródeł.

## Testy jednostkowe i integracyjne

- [x] `Z80SioTests`: podstawowy WR/RR, pointer, reset i dwa kanały.
- [x] `Z80SioTests`: parity, framing, timing znakowy, format 8N1/8E2 i bufor Tx.
- [x] `Z80SioTests`: głębokość bufora RX i overrun.
- [x] `Z80SioSyncTests`: sync, SDLC/HDLC, CRC, abort i end-of-frame w codec oraz
  testach bitowego kanału.
- [x] `Z80SioTests`: CTS/RTS/DCD/DTR.
- [x] `Z80SioTests`: RR3, statusowe IRQ, niezależne bity kanałów i priorytet źródeł.
- [x] `Z80SioTests`: Send Break, BreakDetected, RR1 i snapshot statusu.
- [x] `Z80SioTests`: flush bufora Rx, kasowanie overrun i ponowne rozpoczęcie timingu.
- [x] `Z80SioWiringTests`: kanał zewnętrzny, zegary Rx/Tx, linie modemowe oraz IEI/IEO/RETI.
- [x] `Z80SioTests`: reset, snapshot, restore i brak aliasowania kolejek.
- [x] `Z80SioInterruptTests`: IM2, wektor, priorytet, IEI/IEO i RETI.
- [x] `Z80SioTests`: reset, snapshot, restore i brak aliasowania kolejek.
- [x] Test adaptera Kaypro: mapowanie `$04–$07` i klawiatura na kanale B.
- [x] Test adaptera innej maszyny, aby potwierdzić brak zależności od Kaypro
  (`Z80Sio` z bazą portu `0x40`).
- [x] Przenieść tylko zachowania potwierdzone testami z `cpu-vibe-010/Z80SioController`; ograniczenia pozostają jawne w tej checkliście.

## Przykłady użycia

- [x] Przykład polling RX/TX dla dwóch kanałów (`Z80SioExamplesTests`).
- [x] Przykład konfiguracji 8N1, x16, z CTS/RTS (`Z80SioExamplesTests`).
- [x] Przykład konfiguracji kanału B jako klawiatury Kaypro przez adapter
  (`KayproMachineTests`).
- [x] Przykład odtworzenia konfiguracji kanału i bufora Rx ze snapshotu ma test wykonywalny.
- [x] Przykład IM2 z wektorem dla Rx i odczytem Rx data (`Z80SioExamplesTests`).
- [x] Przykład synchronicznej ramki SDLC z CRC (`Z80SioExamplesTests`).
- [x] Każdy przykład ma test wykonywalny, a nie tylko komentarz lub kod demonstracyjny.

## Definition of Done

- [x] Chip nie ma referencji do Kaypro, UI ani konkretnego adresu portu.
- [x] Wszystkie rejestry i tryby wybrane dla profilu Z8440 są zaimplementowane albo jawnie oznaczone jako nieobsługiwane; macierz profili i wartości fallback ma test w `Z80SioProfileTests`.
- [x] Testy rejestrów, async, sync, timingów, linii, bitowego kanału, IM2
  i daisy-chain przechodzą; test binarny Kaypro pozostaje końcowym etapem integracji.
- [x] Co najmniej jeden adapter maszyny korzysta wyłącznie z publicznego kontraktu chipu.
- [x] Dokumentacja zawiera tabelę różnic wariantów i znane uproszczenia.
- [ ] Test binarny Kaypro przechodzi bez bezpośredniego zapisu do stanu SIO.

## Źródła

- [ ] [Zilog Z80 Family Product Specifications Handbook — Z8440 SIO](https://www.bitsavers.org/components/zilog/z80/Z80_Family_Product_Specifications_Handbook_Feb84.pdf)
- [ ] [Z80 SIO/0 and SIO/1 programming information](https://www.z80.de/z80/)
- [ ] [MAME Kaypro II SIO wiring](https://github.com/mamedev/mame/blob/master/src/mame/kaypro/kaypro.cpp#L2371-L2421)
- [ ] Lokalny donor: `/home/prachwal/source/emulators/cpu-vibe-010/src/CpuVibe.Kaypro/Z80SioController.cs` — źródło zachowań, nie autorytatywna specyfikacja.
