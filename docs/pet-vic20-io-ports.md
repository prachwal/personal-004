# Porty wejścia/wyjścia PET/CBM i VIC-20

Dokument porównuje porty fizyczne oraz ich mapowanie na rejestry układów I/O.
Status `repo` opisuje stan aktualnej implementacji emulatora, a nie tylko fakt,
że odpowiedni układ PIA/VIA/6560 istnieje w modelu.

## Oznaczenia

| Status | Znaczenie |
| --- | --- |
| ✅ | port lub funkcja jest podłączona i używana przez emulowane urządzenie |
| ◐ | układ/rejestry istnieją, ale część linii lub urządzeń zewnętrznych nie jest podłączona |
| ❌ | brak implementacji |

## Zestawienie modeli i rodzin

| Model / wariant | Rzeczywiste porty i urządzenia | Główne adresy I/O | Status w repozytorium | Czego brakuje |
| --- | --- | --- | --- | --- |
| PET 2001 / PET 2001-8 | klawiatura matrycowa, IEEE-488, kaseta, User Port, złącze rozszerzeń | `$E810-$E813`, `$E820-$E823`, `$E840-$E84F` | ◐ klawiatura, PIA, VIA, IEEE-488 i kaseta #1 | kaseta #2, semantyka User Port, magistrala rozszerzeń |
| PET 2001-32 | jak wyżej; 32 KiB RAM w późniejszym wariancie | j.w. | ◐ profil `pet-2001-32`, klawiatura, IEEE-488, kaseta #1 | jak wyżej; brak bankowania/rozszerzeń PET |
| PET/CBM 3000 i wczesne 4000 / 3032 | klawiatura, IEEE-488, dwie kasety, User Port, rozszerzenie | j.w. | ◐ najbliżej profilu 40-kolumnowego | druga kaseta, User Port i rozszerzenie |
| CBM 4032 / 40 kolumn | klawiatura, IEEE-488, kasety, User Port, rozszerzenie; wariant CRTC zależny od rewizji | j.w.; CRTC `$E880-$E881` w rewizjach z CRTC | ◐ profil `cbm-4032`, 40×25, CRTC, klawiatura, IEEE-488, kaseta #1 | druga kaseta, User Port, rozszerzenie; profil upraszcza różnice rewizji |
| CBM 8032 / seria 8000 | klawiatura, IEEE-488, kasety, User Port, rozszerzenie, CRTC; 80×25 | j.w. + CRTC `$E880-$E881` | ◐ profil `cbm-8032`, 80×25, CRTC, klawiatura, IEEE-488, kaseta #1 | druga kaseta, User Port, rozszerzenie 8096/8296 |
| 8096 / 8296 / SuperPET / SP9000 | porty PET/CBM oraz — zależnie od modelu — bankowane RAM, dodatkowy procesor, ACIA/RS-232 lub inne rozszerzenia | PET I/O j.w.; sterowanie pamięcią rozszerzoną m.in. `$FFF0` w 8096/8296 | ❌ brak osobnych profili | cały dodatkowy sprzęt i bankowanie |
| VIC-20 bez rozszerzenia | VIC-I, dwa VIA, joystick/paddle, User Port, kaseta, IEC serial, cartridge/expansion | `$9000-$900F`, `$9110-$911F`, `$9120-$912F`, `$9400-$97FF` | ◐ VIC-I, VIA1/VIA2, klawiatura, joystick, User Port, kaseta, IEC i Color RAM | fizyczne źródło paddle/light-pen, adapter RS-232, cartridge oraz I/O2/I/O3 |
| VIC-20 +3K / +8K / +16K / +24K / All | te same porty zewnętrzne; dodatkowo odpowiedni blok RAM | jak wyżej; pamięć bloków `$0400`, `$2000`, `$4000`, `$6000`, `$A000` | ◐ presety RAM są modelowane | brak urządzeń I/O cartridge i pełnej magistrali rozszerzeń |

## PET/CBM — szczegółowe mapowanie

Adresy są wspólne dla emulowanych profili PET. PIA/VIA są mapowane w
`PetMemoryBus`; CRTC jest tworzony tylko dla profili z `RequiresCrtc`.

| Adres | Układ / linie | Funkcja sprzętowa | Stan w repo |
| --- | --- | --- | --- |
| `$E810` | PIA1 Port A | wybór wiersza klawiatury; sense kasety #1/#2; IEEE EOI; wejście diagnostyczne | ◐ wybór klawiatury, sense kasety #1 i EOI; kaseta #2 pozostaje nieaktywna |
| `$E811` | PIA1 CA1/CA2 | odczyt kasety #1; w starszych rewizjach blanking ekranu lub EOI | ◐ odczyt kasety / sygnał synchronizacji modelu |
| `$E812` | PIA1 Port B | kolumny klawiatury | ✅ |
| `$E813` | PIA1 CB1/CB2 | retrace/blanking; silnik kasety #1 | ◐ kaseta #1 |
| `$E820` | PIA2 Port A | wejście danych IEEE-488 | ✅ |
| `$E821` | PIA2 CA1/CA2 | IEEE NDAC / pozostałe handshake | ✅ w `PetIeeeBusBinding` |
| `$E822` | PIA2 Port B | wyjście danych IEEE-488 | ✅ |
| `$E823` | PIA2 CB1/CB2 | IEEE SRQ/DAV | ✅ w zakresie używanym przez magistralę |
| `$E840` | VIA Port B | IEEE handshake i ATN; linie kasety #2; zapis kasety | ◐ IEEE jest podłączone; User Port i kaseta #2 nie |
| `$E841` / `$E84F` | VIA Port A | User Port i handshake CA2 | ◐ rejestry VIA istnieją, brak zewnętrznego urządzenia User Port |
| `$E842-$E84E` | VIA DDR/timery/shift/PCR/IFR/IER | timery, IRQ i sterowanie linii VIA | ✅ układ VIA; nie wszystkie linie mają model zewnętrzny |
| `$E880-$E881` | CRTC 6545 | indeks i dane kontrolera obrazu | ✅ tylko profile `cbm-4032` i `cbm-8032` |

Emulator ma więc działającą ścieżkę `PIA → PetIeeeBus → PetIeeeDiskDrive`
dla obrazów D64 oraz jedną emulowaną kasetę. Nie ma osobnego modelu drugiego
gniazda kasety, User Portu ani rozszerzeń pamięciowych 8096/8296.

## VIC-20 — szczegółowe mapowanie

W VIC-20 nie ma równoległej magistrali IEEE-488. Napęd dyskowy komunikuje się
szeregową magistralą IEC, której linie są rozdzielone pomiędzy oba VIA.

| Adres | Układ / linie | Funkcja sprzętowa | Stan w repo |
| --- | --- | --- | --- |
| `$9000-$900F` | VIC-I 6560/6561 | obraz, kolory, raster, dźwięk, wejścia paddle/light-pen | ◐ rejestry wejść paddle/light-pen są podłączone do API maszyny; brak fizycznego źródła analogowego w Desktop |
| `$9110` | VIA1 Port B | User Port / linie pomocnicze interfejsów szeregowych | ✅ osiem linii User Portu z wejściem, wyjściem i DDRB; brak adaptera RS-232 |
| `$9111` | VIA1 Port A | IEC CLK IN bit 0, DATA IN bit 1, joystick bits 2–5, cassette sense bit 6, IEC ATN OUT bit 7 | ✅ IEC, joystick i cassette sense są scalane na właściwych bitach |
| `$911C-$911F` | VIA1 PCR/IFR/IER/ORA | sterowanie CA2 kasety oraz obsługa VIA | ◐ CA2 kasety jest podłączone; pozostałe linie bez urządzeń zewnętrznych |
| `$9120` | VIA2 Port B | skanowanie klawiatury; cassette WRITE bit 3; prawa joysticka PB7 | ✅ klawiatura, cassette WRITE i joystick PB7 |
| `$9121` | VIA2 Port A | wybór kolumn/wierszy klawiatury | ✅ |
| `$912C` | VIA2 PCR/CA1/CA2/CB1/CB2 | cassette READ; IEC CLK OUT, DATA OUT i SRQ | ◐ cassette READ i IEC CLK/DATA; SRQ bez urządzenia |
| `$9400-$97FF` | Color RAM | pamięć atrybutów koloru | ✅ `MOS2114` |
| `$9800-$9FFF` | I/O2/I/O3 | obszar urządzeń cartridge/rozszerzeń I/O | ❌ odczyt otwartej magistrali, zapis ignorowany |
| `$A000-$BFFF` | cartridge / expansion | ROM/RAM cartridge i urządzenia rozszerzeń | ◐ presety RAM udostępniają pamięć, ale brak ROM cartridge i urządzeń I/O |

W repozytorium podłączenie dysku VIC-20 jest zatem funkcjonalne dla IEC:

| Linia IEC | VIA / bit | Implementacja |
| --- | --- | --- |
| ATN OUT | VIA1 PA7, `$9111` / `$911F` | `Vic20SerialBusBinding` |
| CLK IN | VIA1 PA0, `$9111` / `$911F` | `Vic20SerialBusBinding` |
| DATA IN | VIA1 PA1, `$9111` / `$911F` | `Vic20SerialBusBinding` |
| CLK OUT | VIA2 CA2, `$912C` | `Vic20SerialBusBinding` |
| DATA OUT | VIA2 CB2, `$912C` | `Vic20SerialBusBinding` |

Kaseta VIC-20 korzysta z VIA2 CA1 jako `cassette READ`, VIA1 PA6 jako sense,
VIA1 CA2 jako sterowania silnikiem oraz VIA2 PB3 jako `cassette WRITE`.

## Co jest obecnie dostępne, a czego nie ma

| Obszar | Dostępne w repo | Brak / ograniczenie |
| --- | --- | --- |
| PET klawiatura | matryca PIA1 i profile 2001/4032/8032 | brak pełnej zmienności rewizji sprzętowych |
| PET dyski | IEEE-488, urządzenia D64, montowanie i transfer DOS | brak drugiego niezależnego modelu kontrolera/portu; to ograniczenie modelu, nie standardu IEEE |
| PET kasety | kaseta #1 i jej status | kaseta #2, pełny User Port i zewnętrzne linie VIA |
| PET obraz | profile 40/80 kolumn, CRTC dla profili CRTC | brak 8096/8296 i ich bankowania |
| VIC-20 klawiatura | VIA2 skanowanie matrycy | brak konfliktu z joystickiem; joystick Desktop używa numpada |
| VIC-20 dyski | IEC przez VIA1/VIA2, napędy D64 i status w modelu | brak dodatkowych urządzeń pod I/O2/I/O3 i cartridge bus |
| VIC-20 kaseta | odczyt, zapis logiczny/SAVE-LOAD, motor/sense | brak osobnego zewnętrznego modelu analogowego portu |
| VIC-20 User Port / RS-232 | User Port VIA1 PB0-PB7 z DDRB; odczyt/zapis dostępny przez `Vic20Machine.UserPort` | brak adaptera RS-232 i obsługi protokołu szeregowego |
| VIC-20 pamięć | presety 3K/8K/16K/24K/All | nie jest to pełna emulacja cartridge ROM ani urządzeń rozszerzeń |

## Źródła i kod repozytorium

- [PET I/O map — Zimmers](https://www.zimmers.net/anonftp/pub/cbm/maps/PETio.txt) — sygnały PIA/VIA, IEEE, kaset i User Portu.
- [VIC-20 memory map — CBM Italia](https://www.cbmitapages.it/vic20/vic20map.html) — rejestry VIC/VIA oraz funkcje linii joysticka, kasety i IEC.
- [VIC-20 Programmer's Reference Manual](https://www.manualslib.com/manual/950679/Commodore-Vic-20.html?page=134) — dokumentacja programistyczna mapy I/O.
- [VIC-20 IEC w tym repozytorium](vic20-disk.md) — potwierdzone połączenia linii IEC i ich rejestrów.
- [VIC-20 kaseta w tym repozytorium](vic20-tape.md) — mapowanie linii kasety i ograniczenia modelu.
- Implementacja PET: `src/PetEmulator.Pet/PetMemoryBus.cs`, `src/PetEmulator.Pet/PetMachine.cs`, `src/PetEmulator.Pet/PetProfileCatalog.cs`.
- Implementacja VIC-20: `src/PetEmulator.Vic20/Vic20MemoryMap.cs`, `src/PetEmulator.Vic20/Vic20MemoryBus.cs`, `src/PetEmulator.Vic20/Serial/Vic20SerialBusBinding.cs`, `src/PetEmulator.Vic20/Tape/Vic20Datasette.cs`.

## Checklist realizacji brakujących elementów

Kolejność uwzględnia zależności: najpierw kontrakty i testy linii I/O, potem
urządzenia korzystające z VIA/PIA, a na końcu warianty sprzętowe i rozszerzenia
pamięci. Każdy etap powinien kończyć się testami jednostkowymi oraz testem
integracyjnym właściwego modelu, jeśli infrastruktura na to pozwala.

### 1. Wspólne przygotowanie

- [x] Spisać testami aktualne zachowanie wszystkich używanych bitów PIA/VIA.
  - [x] Rozdzielić testy rejestru układu od testów urządzenia podłączonego do linii.
  - [x] Dodać asercje, że niepodłączone linie zachowują stan otwartej magistrali lub stan pull-up.
- [x] Ustalić minimalny kontrakt dla zewnętrznych linii I/O.
  - [x] Wykorzystać istniejące callbacki `PortAInput`/`PortBInput` i callbacki zapisu; testy potwierdzają wejście, wyjście oraz maskowanie przez DDR.
  - [x] Zachować active-low/open-collector w istniejących wiązaniach IEC; bez zmian adresów i kontraktów klawiatury, IEC oraz kasety.

### 2. VIC-20 — wejścia i User Port

- [x] Dodać model joysticka VIC-20.
  - [x] Podłączyć kierunki i fire do właściwych bitów VIA1/VIA2.
  - [x] Dodać obsługę joysticka w Desktop przez numpad `8/2/4/6/0` oraz test odczytu linii.
- [x] Dodać wejścia paddle/light-pen.
  - [x] Rozszerzyć model VIC-I o wejścia zewnętrzne `SetPaddlePosition` i `StrobeLightPen`.
  - [x] Zweryfikować odczyt przez rejestry `$9006-$9009`.
- [x] Dodać urządzenie User Portu VIC-20.
  - [x] Udostępnić VIA1 Port B jako osiem linii wejścia/wyjścia z kierunkiem DDRB.
  - [ ] Dodać opcjonalny adapter RS-232 bez wiązania go bezpośrednio z ViewModelem.
  - [x] Dodać test kierunku linii, odczytu wejść i zapisu wyjść.

### 3. VIC-20 — cartridge i I/O2/I/O3

- [ ] Wprowadzić model urządzenia cartridge.
  - [ ] Rozdzielić cartridge ROM, cartridge RAM i urządzenie I/O.
  - [ ] Zachować obecne presety RAM jako osobny, kompatybilny przypadek.
- [ ] Podłączyć `$9800-$9FFF` jako konfigurowalny obszar I/O2/I/O3.
  - [ ] Zdefiniować odczyt, zapis i zachowanie niezmapowanych adresów.
  - [ ] Dodać test ładowania obrazu cartridge oraz test urządzenia rejestrowego.
- [ ] Dodać montowanie cartridge w CLI/Desktop dopiero po ustabilizowaniu API magistrali.

### 4. VIC-20 — pełniejszy model kasety

- [ ] Oddzielić model linii fizycznych kasety od obecnego dekodowania impulsów.
  - [ ] Zachować istniejące, działające SAVE/LOAD logiczne jako ścieżkę zgodności.
  - [ ] Dodać osobny zapis impulsów z VIA2 PB3 tylko wtedy, gdy testy potwierdzą stabilność.
- [ ] Dodać testy przejść motor, sense, READ i WRITE oraz test round-trip z prawdziwym ROM-em.

### 5. PET — druga kaseta i pełne linie VIA

- [ ] Dodać niezależny model kasety #2.
  - [ ] Podłączyć sense PIA1 PA5 i sterowanie silnikiem VIA Port B bit 4.
  - [ ] Rozdzielić status, transport i obraz taśmy od kasety #1.
  - [ ] Dodać test równoległego podłączenia kasety #1 i #2.
- [ ] Uzupełnić brakujące linie IEEE/User Portu w wiązaniu PET VIA.
  - [ ] Zweryfikować bity DAV, NRFD, NDAC, ATN i zapisu kasety względem mapy PET.
  - [ ] Nie zmieniać działającego transferu D64 bez testu regresji IEEE-488.
- [ ] Dodać model PET User Portu.
  - [ ] Udostępnić Port A VIA oraz handshake CA2 jako zewnętrzny interfejs.
  - [ ] Dodać test kierunku linii i aktywnego handshake.

### 6. PET — rewizje modeli i obraz

- [ ] Rozdzielić profile według rzeczywistych rewizji, jeśli różnice wpływają na I/O.
  - [ ] Określić, które warianty mają CRTC, a które dyskretny układ obrazu.
  - [ ] Dodać osobne manifesty ROM i test startu dla każdego profilu.
- [ ] Zweryfikować różnice klawiatury, kaset i złączy między PET 2001, 3000,
      4000/4032 i 8000/8032.
  - [ ] Nie tworzyć profilu tylko dla różnicy obudowy lub nazwy handlowej.

### 7. PET 8096/8296 i SuperPET

- [ ] Najpierw dodać osobne profile i manifesty ROM, bez zmiany istniejących profili.
- [ ] Zaimplementować bankowanie pamięci oraz rejestr sterujący rozszerzeniem.
  - [ ] Dodać test przełączania banku i ochrony obszarów ROM/I/O.
- [ ] Dopiero potem dodać dodatkowy procesor, ACIA/RS-232 i pozostałe urządzenia
      SuperPET/SP9000.
  - [ ] Każdy dodatkowy układ powinien mieć własną mapę adresów i test boot/diagnostic.
- [ ] Rozszerzyć Desktop/CLI o wybór tych profili po przejściu testów ROM i magistrali.
