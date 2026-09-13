# Kaypro II

## Status

Pierwszy pakiet implementacji używa wspólnych kontraktów projektu:

- `src/PetEmulator.Kaypro/KayproMachine.cs` — kompozycja maszyny `IMachine`;
- `src/PetEmulator.Kaypro/KayproBus.cs` — pamięć 64 KiB, ROM monitora, VRAM i porty;
- `src/PetEmulator.Kaypro/KayproSio.cs` — pollingowy kanał konsoli SIO;
- `src/PetEmulator.Kaypro/KayproPioWiring.cs` — rejestry/latch PIO drukarki i sterowania;
- `src/PetEmulator.Kaypro/KayproVideo.cs` — ekran tekstowy 80×24;
- `src/PetEmulator.Kaypro/KayproDiskImage.cs` — surowy obraz 40 ścieżek × 10 sektorów × 512 bajtów;
- `src/PetEmulator.Kaypro/KayproFdcWiring.cs` — połączenie latcha systemowego z FD1793;
- `lib/PetEmulator.Chips/FD1791.cs` i `FD1793.cs` — wspólny model FDC, bez lokalnej kopii kontrolera.

Mapa I/O: `04–07` SIO, `08–0B` PIO/latch, `10–13` FD1793, `1C` port systemowy.
Bit 7 portu `1C` wybiera ROM, a bity `0–1` wybierają napęd.

## Zaimportowane assety

| Plik | Element | Rozmiar | SHA-256 | Pochodzenie |
| --- | --- | ---: | --- | --- |
| `roms/kaypro/kaypro-81-149c.bin` | monitor/system ROM U47, 81-149C | 2048 B | `5231e5dd0f6fb64a8ec50951269c248e00875906e391d044918e212d14b08f55` | poprzednia iteracja `cpu-vibe-011`, źródło opisane w `docs/kaypro-ii/02-roms-and-character-font.md` |
| `roms/kaypro/kaypro-81-146.bin` | generator znaków U43, 81-146 | 2048 B | `9df2034c2bf38ea218ae87c7c5669f689d3e70f86eaade8c51949d216936d777` | poprzednia iteracja `cpu-vibe-011`, źródło opisane w `docs/kaypro-ii/02-roms-and-character-font.md` |
| `roms/kaypro/kpii-149.td0` | systemowy dysk CP/M 2.2 dla ROM 81-149 | 112024 B | `6cba0285fd22126970ff46ee19a35ec20576980627496becc5f69aca5b7d926f` | poprzednia iteracja `cpu-vibe-011`, obraz TeleDisk `KPII-149` |

Są to binarne zrzuty ROM-ów z poprzedniej iteracji. Sam import nie oznacza
potwierdzenia praw do redystrybucji; przed publikacją repozytorium należy
zweryfikować licencję/proweniencję według dokumentacji źródłowej.

## Następne elementy

- pełny protokół Z80 SIO i przerwania IM2;
- jawny model Z80 PIO oraz sygnałów drukarki/napędów;
- loader TD0 dla obrazu `kpii-149.td0` — obraz jest już zaimportowany, ale loader nie jest jeszcze częścią projektu;
- loader ROM/font Kaypro z walidacją hashy;
- test bootu realnego ROM-u do promptu CP/M `A>`;
- integracja CLI/Desktop.

## Raport diagnostyczny bootu ROM → CP/M

### Zakres i para testowa

Zweryfikowana para użyta w teście binarnym:

- ROM `roms/kaypro/kaypro-81-149c.bin`, 2 KiB, SHA-256
  `5231e5dd0f6fb64a8ec50951269c248e00875906e391d044918e212d14b08f55`;
- obraz `roms/kaypro/cpm22-rom149.dsk`, 204800 B;
- test `tests/PetEmulator.Kaypro.Tests/KayproBootTests.cs`;
- narzędzie zewnętrzne `/usr/bin/z80dasm` 1.1.6.

Uwaga: hash ROM-u w pierwszej tabeli jest źródłem wiążącym; powyżej należy
utrzymać dokładnie ten sam hash po ewentualnej korekcie redakcyjnej.

`z80dasm` potwierdził mapę FDC w ROM-ie:

| Port | Znaczenie |
| --- | --- |
| `10h` | status/command |
| `11h` | track |
| `12h` | sector |
| `13h` | data |

### Rzeczywista ścieżka wykonania

1. ROM startuje pod `0000h`, inicjalizuje stos i sprzęt, a następnie wybiera
   napęd przez port systemowy `1Ch`.
2. Procedura statusu pod `0431h` wykonuje `HALT`, `IN A,(10h)`, `BIT 0,A` i
   `JR NZ,0432h`. Bit 0 oznacza `BUSY`; ta pętla nie oczekuje na klawiaturę.
3. Procedura transferu jest kopiowana/wykonywana z RAM-u pod `FF00h`. W śladzie
   występują m.in. `FF26/FF27/FF29` oraz `FF2B/FF2C/FF2E`: `HALT`, `INI`,
   `JR NZ`. Każdy bajt powinien być pobrany po osobnym DRQ/NMI.
4. Wektor NMI pod `0066h` zawiera `RET`. NMI budzi CPU z `HALT` i wraca do
   instrukcji po `HALT`; właściwy transfer wykonuje `INI`.
5. ROM wysyła komendę odczytu `88h` do portu `10h`, a FDC zgłasza DRQ przez
   ścieżkę `FD1791/FD1793 → KayproFdcWiring → KayproBus → NMI Z80`.
6. Dane są zapisywane przez `INI` do pamięci systemowej, m.in. w obszarze
   `FA00h`/`FC00h`, który później ma zawierać załadowany kod CP/M.

### Objaw przed poprawką

Przed poprawką test kończył się bez `A>`:

- `PC=0132h`, gdzie ROM wykonywał nieskończoną pętlę awaryjną;
- `FdcStatus=04h`, czyli `LostData`;
- `DRQ=true`, `TransferIndex=511`, a następnie timeout;
- RAM zawierał tylko częściowo załadowane dane.

ROM docierał do statusowej pętli `0432h`, ponieważ FDC pozostawał zajęty
oczekiwaniem na niepobrany bajt. Nie było to zawieszenie na wejściu z klawiatury.

### Co zostało potwierdzone

- Obraz ma poprawny rozmiar dla Kaypro II: `40 × 10 × 512 B = 204800 B`.
- Dokumentacja i niezależne definicje formatu Kaypro II potwierdzają dysk
  jednostronny, 40 ścieżek, 10 sektorów po 512 B.
- ROM i obraz są zgodne hashem/proweniencją z poprzednią implementacją.
- Adresy portów FDC są zgodne z disassemblacją ROM-u.
- `FD1791` ma zaimplementowane per-bajtowe DRQ, deadline, INTRQ i diagnostyczne
  zdarzenia `DataRequested`, `DataRead`, `LostData` oraz `CommandCompleted`.
- Mapowanie pamięci, ROM-u, VRAM i portów działa na tyle, że ROM wykonuje kod,
  wydaje komendy FDC i kopiuje część danych do RAM-u.
- Testy wiringowe FDC przechodzą; problem występuje dopiero w pełnym przebiegu
  ROM + CPU + FDC + NMI.

### Główne odkrycie z diagnostyki NMI/DRQ

Ślad pokazuje, że transfer ROM-u używa dwóch bloków pętli `HALT/INI/DJNZ`.
`FD1791DiagnosticEvent.TransferIndex` jest emitowany po zwiększeniu licznika
w `ReadData()`. Zatem `DataRead` z `TransferIndex=511` oznacza odczyt bajtu
o indeksie 510. Pełny sektor kończy się zdarzeniem `DataRead` z indeksem 512,
a następnie `CommandCompleted`.

Przy błędzie ostatni blok kończył się z `B=00`, podczas gdy FDC miał nadal
`DRQ=1` i indeks `511`; brakowało odczytu finalnego bajtu. Następnie ROM
przechodził do:

```text
FF2E  JR NZ,FF2Bh
FF30  JR FF41h
FF4C  CALL 0431h
0431  HALT
0432  IN A,(10h)
```

FDC czeka wtedy na ostatni bajt, a ROM czeka na zakończenie komendy — powstaje
zamknięta pętla kończąca się deadline i `LostData`.

Ważne szczegóły śladu:

- przed błędem odczyty dochodziły do indeksów `509`, `510`, `511`, ale brakowało
  finalnego zdarzenia `DataRead` z indeksem 512;
- `NMI` jest dostarczane po `HALT`, nie w dowolnym miejscu wykonywania CPU;
- wymuszenie NMI bez warunku `HALT` powoduje zagnieżdżone NMI w procedurze
  `INI` i jest błędne;
- próba kolejkowania NMI bez linii poziomowej nie zmieniła objawu, więc nie jest
  obecnie uznana za rozwiązanie.

### Hipotezy odrzucone

- **Oczekiwanie na klawiaturę:** odrzucone. ROM czyta port `10h` FDC, nie porty
  SIO; klawiatura może być obsługiwana dopiero po udanym bootowaniu CP/M.
- **Zły rozmiar sektora 256 B:** odrzucone. Kaypro II i obraz używają 512 B;
  zmiana modelu na 256 B byłaby maskowaniem problemu i zniszczyłaby zgodność
  z geometrią obrazu.
- **Za krótki deadline:** nie jest główną przyczyną. Zwiększenie deadline do
  `1_000_000_000` nie naprawiło bootu i zatrzymało transfer już przy pierwszym
  DRQ. Wartość robocza została przywrócona do `10000` T-states.
- **Błędna mapa portów lub brak dysku:** odrzucone. ROM wydaje właściwe
  komendy, FDC czyta dane, a diagnostyka wiringowa przechodzi.
- **Brak SIO/IM2:** odrzucone dla samego bootu. Awaria występuje wcześniej,
  w transferze system track przez FDC/NMI.

### Zastosowana poprawka

Źródłem błędu było traktowanie `InterruptSequence` jako kolejki przyczyn
przerwań. Wspólna flaga `_fdcNmiPending` nie rozróżniała DRQ od INTRQ, nie
przechowywała poziomu sygnału i mogła przeżyć moment, w którym źródłowe
zdarzenie już zniknęło. Spóźnione NMI budziło `HALT`, a `INI` zmniejszało `B`
bez pobrania nowego bajtu.

`KayproBus.Tick()` generuje teraz impuls NMI wyłącznie wtedy, gdy CPU jest
w `HALT` i aktualnie aktywny jest `FD1791.DrqAsserted` albo
`FD1791.IntrqAsserted`. Nie replayuje już bezkontekstowych zdarzeń
`InterruptSequence`; `FdcNmiPending` pokazuje bieżący poziom DRQ/INTRQ.
Nie zmieniano `INI`, geometrii obrazu ani rdzenia Z80.

### Weryfikacja po poprawce

- `KayproFdcWiringTests` + `KayproMachineTests`: **16/16 PASS**;
- binarny test `KayproBootTests`: **1/1 PASS**;
- ROM `81-149C` i obraz `cpm22-rom149.dsk` przechodzą transfer system track
  bez `LostData` i dochodzą do promptu CP/M `A>`.

Pełna regresja `PetEmulator.Kaypro.Tests` po tej zmianie ma **21/22 PASS**.
Jedyna pozostała awaria dotyczy niezależnego testu
`KayproPioWiringTests.SystemPioOutputUpdatesMachineBankAndDriveSignalsInOrder`
(`DriveSelect=0`, oczekiwano `1`); nie dotyczy ścieżki bootu FDC/NMI.

## Profil Avalonia i font terminala

Profil `Kaypro II` jest dostępny w menu `Machine`. Widok terminala używa:

- monitora `roms/kaypro/kaypro-81-149c.bin`;
- fontu `roms/kaypro/kaypro-81-146.bin` (2048 B, 256 glifów 8×8);
- obrazu domyślnego `roms/kaypro/cpm22-rom149.dsk`, jeśli jest dostępny.

Mapowanie fontu jest zgodne z poprzednią implementacją `CpuVibe`: dla każdego
kodu VRAM używany jest glif `0x80 | (code & 0x7f)`, ponieważ właściwe znaki
znajdują się w górnej połowie ROM-u. Bity rastra są odwracane (`0` oznacza
zaświecenie wiązki CRT). Widoczny ekran ma 80×24 znaki, stride VRAM wynosi 128,
a komórka terminala 8×10 — osiem wierszy glifu i dwa wiersze odstępu CRT.
