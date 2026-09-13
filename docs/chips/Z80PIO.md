# Zilog Z80 PIO — checklista niezależnego układu

Dokument definiuje implementację standardowego Zilog Z80 PIO jako niezależnego
układu. Kaypro używa dwóch instancji PIO o różnych połączeniach z resztą
maszyny; ich wiring, role system/printer i dekodowanie portów muszą być
adapterami poza chipem.

## Plan migracji i implementacji E2E

Plan jest podzielony na szerokie paczki funkcjonalne. Każda paczka kończy się
działającą ścieżką od CPU przez rejestry PIO do portu zewnętrznego oraz testem
regresyjnym. Nie zamykamy paczki samym pokryciem rejestrów: wymagany jest test
przejścia stanu i test użycia przez adapter.

### Grupa 0 — kontrakt układu, profil i baseline

Status: **ukończona**. Kontrakt i baseline są zaimplementowane w
`lib/PetEmulator.Chips/Z80PIO/`, a testy grupy znajdują się w
`Z80PioGroup0Tests` oraz wspólnym `Z80PioGroup1Tests`.

Zakres:

- ustalić model `Z8420` jako bazę oraz jawne różnice NMOS/CMOS tylko tam, gdzie
  zmieniają wynik odczytu, reset, poziom linii lub IRQ;
- utworzyć `Z80PioDevice`, `IZ80PioPort`, `IZ80PioInterruptSource` oraz
  `Z80PioState` w `lib/PetEmulator.Chips/Z80PIO/`;
- zdefiniować dwa porty logiczne, cztery operacje CPU, reset i snapshot;
- ustalić wartości odczytu nieprawidłowych adresów i niepełnych sekwencji;
- zachować istniejący `KayproPio` jako baseline do czasu gotowego adaptera.

Testy i kryterium wyjścia:

- test niezależności od Kaypro/UI, instancjonowania dwóch układów i braku
  współdzielenia stanu;
- test resetu, domyślnego trybu, wartości fallback i pełnego kształtu snapshotu;
- publiczny kontrakt nie zawiera adresów Kaypro ani semantyki drukarki/napędu.

### Grupa 1 — dekoder CPU i programowanie słów sterujących

Zakres:

- zaimplementować dekoder A/B data/control niezależny od adresu maszyny;
- dodać parser słów: vector, mode, I/O select, interrupt control, mask follows
  i interrupt disable;
- utrzymywać parser oraz oczekiwane słowo następujące osobno dla A i B;
- zaimplementować wyrównanie wektora IM2 i walidację komend po bitach, nie po
  adresie portu;
- obsłużyć reset w połowie programowania i ponowne programowanie bez resetu.

Testy i kryterium wyjścia:

- `Z80PioRegisterTests` pokrywa cztery operacje, wszystkie słowa, kolejność
  następnych zapisów i błędne sekwencje;
- testy potwierdzają niezależność parsera A/B oraz zgodność wektora z IM2;
- adapter magistrali nie przecieka do modelu chipu.

### Status grupy 2

Grupa 2 jest zaimplementowana w `Z80PioDevice` i pokryta przez
`Z80PioMode01Tests`: Mode 0 obsługuje latch wyjściowy, oczekiwanie na
`READY`, kolejne zapisy i output IRQ; Mode 1 obsługuje strobe, pełny latch,
odczyt CPU, input IRQ i overrun. Mode 2/3 oraz pełna semantyka linii PIO
pozostają kolejnymi grupami.

### Status grupy 3

Grupa 3 jest zaimplementowana i pokryta przez `Z80PioMode23Tests`: Port A
obsługuje zmianę kierunku Mode 2, Port B jawnie odrzuca Mode 2, a Mode 3
obsługuje mieszany odczyt, zapis tylko bitów wyjściowych oraz warunki IRQ
AND/OR i high/low.

### Grupa 2 — ścieżki danych Mode 0 i Mode 1

Zakres:

- Mode 0: latch wyjściowy, zapis CPU, aktualizacja ośmiu linii danych,
  ARDY/BRDY, strobe i output interrupt;
- Mode 1: wejściowy latch, przyjęcie danych wyłącznie przez jawny strobe,
  blokowanie nadpisania pełnego latcha, odczyt CPU i input interrupt;
- zdefiniować zachowanie zapisu/transferu przed `READY`, kolejnych transferów
  i urządzenia zewnętrznego, które nie odpowiada;
- nie dodawać fikcyjnego zegara: przejścia wynikają z operacji CPU i krawędzi
  sygnałów portu.

Testy i kryterium wyjścia:

- `Z80PioMode0Tests` sprawdza pojedynczy, wielokrotny i przedwczesny zapis oraz
  kolejność latch → ready → strobe → potwierdzenie urządzenia;
- `Z80PioMode1Tests` sprawdza strobe przy ready aktywnym/nieaktywnym, pełny
  latch, odczyt i ponowne uzbrojenie;
- każda ścieżka CPU↔PIO i PIO↔peripheral ma asercję wartości oraz kolejności.

### Grupa 3 — Mode 2 i Mode 3 jako kompletne ścieżki logiczne

Zakres:

- Mode 2 wyłącznie na Port A: zmiana kierunku, fazy handshake, tri-state i
  współpraca Portu B bez udawania drugiego kanału bidirectional;
- Mode 3: I/O Select, mieszany odczyt wejść/latchów wyjściowych, zapis tylko
  bitów wyjściowych, maska, AND/OR, high/low i `Mask Follows`;
- określić zachowanie kolizji kierunku i zachować wartości wejść podczas zapisu
  wyjść;
- generować IRQ dopiero po spełnieniu warunku zaprogramowanego dla portu.

Testy i kryterium wyjścia:

- `Z80PioMode2Tests` obejmuje input → output → input, strobe, kolizję i Port B;
- `Z80PioMode3Tests` obejmuje każdy bit, maskę pustą/pełną, AND/OR, aktywny
  poziom oraz ponowne programowanie maski;
- testy dowodzą, że odczyt mieszany nie niszczy latcha ani wejść.

### Grupa 4 — linie zewnętrzne, handshake i adaptery maszyn

Status: **ukończona**. Adapter magistrali, tłumaczenie polaryzacji, detekcja
krawędzi strobe oraz wiring dwóch instancji Kaypro są zaimplementowane i
pokryte testami `Z80PioBusAdapterTests`, `Z80PioSignalAdapterTests` oraz
`KayproPioWiringTests`.

Zakres:

- ustalić jawny model polaryzacji `/STB`, `ARDY`, `/ASTB`, `BRDY` i reakcji na
  krawędzie;
- zbudować `Z80PioBusAdapter` dla sygnałów CPU oraz `KayproPioWiring` dla ról
  PIO-G/PIO-S;
- przenieść mapowanie portów i semantykę urządzeń do adaptera, w tym Centronics,
  system/napęd i ewentualną klawiaturę;
- rozstrzygnąć rzeczywistą mapę PIO-S: obecny `$1C` jest zajęty przez port
  systemowy, a obecny `KayproPio` obsługuje tylko `$08–$0B`;
- zastąpić latchowy `KayproPio` dopiero po przejęciu wszystkich jego callerów
  przez publiczny kontrakt układu.

Testy i kryterium wyjścia:

- test adaptera z inną mapą niż Kaypro potwierdza brak zależności chipu od
  maszyny;
- test Kaypro obejmuje PIO-G, PIO-S, niepodłączone linie, polaryzację i reset;
- test kolejności sygnałów CPU↔PIO↔peripheral wykrywa utratę lub podwójne
  potwierdzenie transferu.

### Grupa 5 — IRQ, IM2, daisy-chain i integracja RETI

Status: **ukończona**. PIO ma niezależne wektory Port A/B, priorytet
wewnętrzny, łańcuch IEI/IEO dla wielu instancji oraz integrację RETI z CPU
przez magistralę Kaypro.

Zakres:

- rozdzielić IRQ Port A/B, ustalić priorytet A nad B i osobny wyrównany wektor
  dla każdego portu;
- zaimplementować IEI/IEO, blokowanie niższego źródła i acknowledge tylko przy
  aktywnym IEI;
- podłączyć acknowledge do magistrali Z80 i zwolnienie źródła do rzeczywistego
  `RETI`, analogicznie do zakończonej integracji SIO;
- uwzględnić brak `EI`, kilka jednoczesnych warunków i reset podczas ISR.

Testy i kryterium wyjścia:

- `Z80PioInterruptTests` obejmuje IM2, niezależne wektory A/B, maskowanie,
  IEI/IEO i RETI;
- dwa niezależne PIO przechodzą acknowledge w prawidłowej kolejności;
- test CPU wykonuje `ED 4D`, a nie tylko wywołuje metodę adaptera ręcznie.

### Grupa 6 — snapshot, debug, przykłady i migracja Kaypro

Status: **ukończona**. Snapshot urządzenia i łańcucha, immutable debug view,
wykonywalne przykłady trybów oraz test E2E programu Z80 przez wiring Kaypro są
zaimplementowane i zweryfikowane.

Zakres:

- snapshot/restore obejmuje tryb, parser słów, latchy, maski, poziomy linii,
  pending IRQ i stan daisy-chain;
- udostępnić diagnostyczny opis portów bez wystawiania mutowalnych struktur;
- dodać wykonywalne przykłady Mode 0, Mode 1, Mode 2, Mode 3, IM2 oraz dwóch
  instancji z adapterem Kaypro;
- wymienić `KayproPio` w `KayproBus`, zachowując mapowanie dopiero po decyzji
  o konflikcie `$1C` i testach portów;
- wykonać test diagnostyczny/binarny Kaypro bez bezpośredniej manipulacji
  prywatnym stanem PIO.

Testy i kryterium wyjścia:

- `Z80PioGroup1Tests` sprawdza snapshot round-trip, brak aliasowania i reset;
- `Z80PioExamplesTests` sprawdza wartości linii, rejestrów i IRQ dla każdego
  przykładu;
- test E2E Kaypro wykonuje program Z80 zapisany jako bajty i przekazuje dane
  przez PIO-G do portu zewnętrznego;
- test E2E przechodzi od programu Z80 przez porty PIO do urządzenia Kaypro;
- pełny build, testy chipów, CPU, Kaypro i regresja rozwiązania są zielone.

### Kolejność i zasady wykonania

1. Grupa 0 musi zakończyć się kontraktem i baseline; dopiero wtedy można
   tworzyć tryby pracy.
2. Grupy 1–3 są rdzeniem układu i powinny być realizowane kolejno, każda jako
   osobny większy pakiet z testami regresyjnymi.
3. Grupa 4 może rozpocząć się po stabilizacji słów sterujących, ale nie może
   wprowadzać logiki Kaypro do `lib/PetEmulator.Chips`.
4. Grupa 5 wymaga wcześniejszego potwierdzenia kontraktu `IBus`/RETI CPU Z80;
   nie wolno zastąpić go ręcznym `NotifyReti()` w teście.
5. Grupa 6 zamyka migrację dopiero po testach E2E. Do tego czasu stary
   `KayproPio` pozostaje adapterem przejściowym, nie jest rozbudowywany o kolejne
   funkcje PIO.

### Ryzyka i decyzje do zamknięcia przed kodem

- [x] Potwierdzić wariant płyty Kaypro i rzeczywiste role PIO-G/PIO-S w
  dokumentacji/MAME; Kaypro II mapuje PIO-G na `$08–$0B`, a PIO-S na
  `$1C–$1F`, przy czym `$1C` jest portem systemowym PIO-S.
- [x] Potwierdzić polaryzację i krawędzie sygnałów handshake; kontrakt używa
  poziomów logicznych, `Z80PioSignalAdapter` tłumaczy aktywny wysoki/niski,
  a przyjęcie danych następuje na krawędzi aktywacji strobe.
- [x] Ustalić, które zachowania NMOS/CMOS są wymagane przez ROM/CP/M: różnice
  profili ograniczają się do technologii, zegara i cech elektrycznych; model
  programowy portów pozostaje wspólny. Ograniczenia są jawne w `Z80PioProfile`.
- [x] Ustalić źródło testu binarnego: test E2E używa własnego, jawnie zapisanego
  programu Z80 w `KayproPioEndToEndTests`; nie kopiuje zewnętrznego ROM-u.

## Zakres układu

- [x] Obsłużyć profil `Z8420/Z84C20` z jawnym wariantem NMOS/CMOS, jeśli wpływa na zachowanie; wariant i wartości fallback są w `Z80PioProfile`.
- [x] Udostępnić dwa porty po 8 bitów: Port A i Port B.
- [x] Rozdzielić interfejs CPU, rejestry portów, linie handshake/ready/strobe i IRQ.
- [x] Nie kodować drukarki, napędu, klawiatury ani Kaypro w implementacji PIO.
- [x] Udostępnić osobny adapter `KayproPioWiring` dla PIO-S i PIO-G.

## Model i kontrakty

- [x] Utworzyć `Z80PioDevice` w warstwie układów, bez zależności od `KayproMachine`.
- [x] Zdefiniować `IZ80PioPort` dla ośmiu linii danych i sygnałów STB/READY.
- [x] Zdefiniować `Z80PioState` z trybem, kierunkiem, latchami, maską, wektorem i IRQ obu portów.
- [x] Zdefiniować `ReadPort`/`WritePort` dla A-data, B-data, A-control i B-control.
- [x] Zdefiniować `Tick` tylko dla rzeczywiście wymaganych przejść handshake; baseline nie dodaje fikcyjnego zegara.
- [x] Zdefiniować reset sprzętowy oraz reset stanu przerwań.
- [x] Zdefiniować snapshot/debug z widocznym trybem i poziomami linii.

## Interfejs CPU i dekoder

- [x] Obsłużyć selekcję Port A/B oraz data/control przez linie `A0` i `C/D` adaptera.
- [x] Nie zakładać adresów `$08–$0B` w chipie; Kaypro ma mapować je na instancję PIO-G.
- [x] Nie zakładać adresów `$1C–$1F` w chipie; Kaypro ma mapować je na instancję PIO-S.
- [x] Zdefiniować bazowy odczyt/zapis portu danych; semantyka trybów pozostaje w grupach 2–3.
- [x] Zdefiniować odczyt/zapis portu kontrolnego z sekwencją słów następujących.
- [x] Testować nieprawidłowe sekwencje programowania i reset w połowie sekwencji.

## Słowa sterujące i programowanie

- [x] Implementować Interrupt Vector Word i wyrównanie wektora zgodne z IM2.
- [x] Implementować rozpoznanie Mode Control Word:
      Mode 0 output, Mode 1 input, Mode 2 bidirectional, Mode 3 bit control.
- [x] Implementować I/O Select Word dla Mode 3: bit `1` = input, bit `0` = output.
- [x] Implementować rozpoznanie Interrupt Control Word: enable, AND/OR, high/low i mask follows.
- [x] Implementować Interrupt Disable Word.
- [x] Rozpoznawać słowa komend po bitach identyfikujących, nie po adresie maszyny.
- [x] Obsłużyć wymagane słowo następujące po Mode 3 i po `Mask Follows`.
- [x] Zachować stan parsera sekwencji osobno dla Port A i Port B.
- [x] Testować ponowne programowanie trybu bez resetu.

## Mode 0 — output

- [x] Zapisywać latch wyjściowy i aktualizować osiem linii danych.
- [x] Modelować bazową gotowość urządzenia i strobe przez `IZ80PioPort`; pełna polaryzacja ARDY/BRDY pozostaje grupą 4.
- [x] Określić moment transferu po zapisie CPU i po zaakceptowaniu danych przez urządzenie.
- [x] Obsłużyć output interrupt po transferze, jeśli przerwania są włączone.
- [x] Testować jeden i wiele kolejnych zapisów, w tym zapis przed ready.

## Mode 1 — input

- [x] Przyjmować dane z portu tylko przez jawny sygnał strobe/transfer.
- [x] Przechowywać bajt wejściowy do odczytu CPU i blokować nadpisanie, gdy rejestr jest pełny.
- [x] Aktualizować stan latcha po odczycie danych przez CPU.
- [x] Generować input interrupt po zaakceptowaniu danych, jeśli przerwanie jest włączone.
- [x] Testować strobe przy ready aktywnym i nieaktywnym oraz utratę/odrzucenie kolejnego bajtu.

## Mode 2 — bidirectional

- [x] Ograniczyć Mode 2 do Port A zgodnie ze specyfikacją.
- [x] Połączyć kierunek danych z właściwą fazą strobe i handshake; pełne krawędzie pozostają grupą 4.
- [x] Ustalić relację Port B do Mode 3 i jego wpływ na wektor/przerwania.
- [x] Testować przejście input → output → input bez utraty stanu.
- [x] Testować kolizję kierunku i zachowanie linii tri-state w modelu logicznym.

## Mode 3 — bit control

- [x] Zastosować I/O Select Word dla każdego bitu portu.
- [x] Odczyt zwraca wejścia dla bitów wejściowych i latch wyjściowy dla bitów wyjściowych.
- [x] Zapis zmienia tylko bity wyjściowe, nie niszczy wartości wejściowych.
- [x] Implementować mask register i warunek przerwania na monitorowanych bitach.
- [x] Implementować AND/OR oraz aktywny poziom high/low.
- [x] Implementować `Mask Follows` i przejście parsera do maski.
- [x] Testować każdy bit osobno, maskę pustą/pełną i kombinacje AND/OR.

## Przerwania i daisy-chain

- [x] Zaimplementować osobny wyrównany wektor PIO dla Port A i Port B.
- [x] Rozdzielić źródła IRQ Port A i Port B.
- [x] Zaimplementować priorytet Port A nad Port B.
- [x] Zaimplementować `IEI`/`IEO` dla łańcucha wielu urządzeń Z80.
- [x] Zablokować niższy priorytet podczas obsługi wyższego źródła.
- [x] Obsłużyć zakończenie ISR/RETI w integracji z kontrolerem przerwań CPU.
- [x] Testować IM2, kilka aktywnych warunków, maskę i dwa połączone PIO.

## Linie zewnętrzne i timing

- [x] Zdefiniować aktywny poziom linii handshake w kontrakcie logicznym oraz
  jawne tłumaczenie aktywnego poziomu fizycznego przez `Z80PioSignalAdapter`.
- [x] Nie zakładać, że linia drukarki lub napędu ma semantykę PIO — adapter
  `KayproPioWiring` nadaje znaczenie wyjściu systemowemu i drukarkowemu.
- [x] Modelować przejścia krawędziowe; powtórzenie poziomu aktywnego strobe nie
  przyjmuje drugi raz tego samego transferu.
- [x] Zdefiniować zachowanie braku odpowiedzi: zapis pozostaje oczekujący do
  chwili `READY`, a pełny latch wejściowy zgłasza overrun.
- [x] Testować transfer CPU↔PIO i PIO↔peripheral z kontrolą kolejności sygnałów
  w `Z80PioBusAdapterTests`, `Z80PioSignalAdapterTests` i
  `KayproPioWiringTests`.

## Testy jednostkowe i integracyjne

- [x] `Z80PioGroup0Tests`: profil Z8420, domyślne porty, reset instancji i wartości fallback.
- [x] `Z80PioGroup1Tests`: adresowanie A/B data/control, reset, profil i parser słów.
- [x] `Z80PioMode01Tests`: output latch, ready/strobe i output interrupt dla Mode 0.
- [x] `Z80PioMode01Tests`: input latch, strobe, ready i input interrupt dla Mode 1.
- [x] `Z80PioMode23Tests`: bidirectional Port A i ograniczenie Port B.
- [x] `Z80PioMode23Tests`: kierunek bitów, odczyt mieszany, maska, AND/OR i level.
- [x] `Z80PioInterruptTests`: IM2, wektory A/B, priorytet A/B, IEI/IEO i RETI.
- [x] `Z80PioGroup1Tests`: reset, snapshot/restore i brak współdzielenia stanu instancji.
- [x] Test dwóch niezależnych instancji PIO w jednym łańcuchu przerwań.
- [x] Test adaptera Kaypro: PIO-G dla zewnętrznego portu drukarkowego oraz PIO-S
  dla systemu.
- [x] Test potwierdzający niezależność chipu od adaptera: `Z80PioBusAdapterTests`
  używa mapy bez adresów Kaypro.

## Przykłady użycia

- [x] Przykład Port A Mode 0 sterującego ośmioma wyjściami.
- [x] Przykład Port B Mode 1 przyjmującego bajty ze strobe.
- [x] Przykład Port A Mode 2 z pełnym handshake.
- [x] Przykład Mode 3 monitorującego wybrane bity w formule AND/OR.
- [x] Przykład IM2, w którym Port A i Port B mają różne priorytety.
- [x] Przykład dwóch instancji: `Z80PioDevice` + adapter Kaypro, bez kodu Kaypro w chipie.
- [x] Każdy przykład ma test wykonywalny i oczekiwane wartości linii/rejestrów.

## Definition of Done

- [x] Chip nie ma referencji do Kaypro, drukarki, napędu ani konkretnego adresu portu.
- [x] Wszystkie cztery tryby PIO mają działające ścieżki CPU i urządzenie zewnętrzne.
- [x] Słowa sterujące, maski, handshake i IM2 są pokryte testami.
- [x] Dwie instancje PIO mogą pracować niezależnie i w jednym daisy-chain.
- [x] Adapter Kaypro używa tylko publicznego kontraktu PIO.
- [x] Obecny `KayproPio` został zastąpiony przez dwie instancje `Z80PioDevice`
  spięte adapterem `KayproPioWiring`.

## Źródła

- [x] [Zilog Z8420/Z84C20 Product Specification](https://sapr.asvcorp.ru/datasheets/19/06/00000000619.pdf)
- [x] [Zilog Z80 PIO User Manual](https://www.z80ne.com/datasheets/z80pio.pdf)
- [x] [Z80 PIO documentation index](https://www.z80.de/z80/)
- [x] [MAME Kaypro II: dwa Z80 PIO i ich wiring](https://github.com/mamedev/mame/blob/master/src/mame/kaypro/kaypro.cpp#L1958-L1976)
- [x] Lokalna dokumentacja poprzedniej implementacji: `/home/prachwal/source/emulators/cpu-vibe-010/docs/machines/kaypro-ii/README.md`.
