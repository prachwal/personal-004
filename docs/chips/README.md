# Układy emulowane

Ten katalog zawiera pełne checklisty i informacje per układ, wydzielone z dokumentacji PET i VIC-20. Checklisty opisują kontrakt urządzenia, testy jednostkowe, integrację z magistralą oraz znane ograniczenia.

## Układy

- [MOS2114](MOS2114.md) — 8.2 `MOS2114` — Color RAM.
- [MOS6522](MOS6522.md) — 8.3 `MOS6522` — VIA.
- [MT6520](MT6520.md) — 8.4 `MT6520` — PIA.
- [MOS6560](MOS6560.md) — 8.5 `MOS6560`/`MOS6561` — VIC-I.
- [MT6545](MT6545.md) — 8.6 `MT6545` — CRTC.
- [MC146818](MC146818.md) — 8.7 `MC146818` — RTC.
- [MOS6551](MOS6551.md) — 8.8 `MOS6551` — ACIA SuperPET.
- [MOS6702](MOS6702.md) — 8.9 `MOS6702` — dongle ochrony SuperPET.
- [FD1791](FD1791.md) — wspólny rdzeń kontrolera dyskietek WD/FD179x.
- [FD1793](FD1793.md) — wariant FD1791 z prawdziwą magistralą DAL.
- [Z80SIO](Z80SIO.md) — niezależny układ szeregowy Zilog Z8440, checklista implementacji.
- [Z80PIO](Z80PIO.md) — niezależny układ równoległy Zilog Z8420, checklista implementacji.
- [I8272](I8272.md) — kontroler dyskietek CPC6128 i kontrakt snapshotu.

## Wspólne kryteria

Ten etap dotyczy wyłącznie układów z `lib/PetEmulator.Chips`. Celem nie jest samo
osiągnięcie liczby procentowej, lecz jednoczesne pokrycie:

1. każdego wiersza i każdej gałęzi kodu produkcyjnego;
2. każdego rejestru, bitu sterującego, trybu pracy i przejścia stanu;
3. każdej ścieżki błędu oraz zachowania po `Reset`;
4. użycia układu przez PET/VIC-20 w teście integracyjnym z magistralą.

### Wspólny kontrakt jakości

- [x] `coverlet.collector` w `tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj`.
- [x] Deterministyczny `tests/coverage.runsettings` wykluczający kod testów,
      wygenerowany kod i infrastrukturę testową.
- [x] Uruchamianie raportu:
      `dotnet test tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj --no-restore --collect:"XPlat Code Coverage"`.
- [x] Raportowanie osobno line, branch i method coverage dla każdego pliku układu.
- [x] Wymaganie 100% line, 100% branch i 100% method coverage dla każdego układu.
- [x] `scripts/test-chips-coverage.sh` kończący się błędem przy niepełnym pokryciu.
- [ ] Dodać testy mutacyjne lub ręczne usunięcie każdej gałęzi jako kontrolę,
      że pokrycie nie jest tylko formalne.
- [ ] Nie oznaczać etapu jako ukończonego, jeśli kod jest pokryty, ale nie ma testu
      zachowania na poziomie magistrali lub maszyny.

### Kolejność realizacji

1. [x] Infrastruktura coverage i wspólne testy adresowania/resetu.
2. [ ] `MOS2114` — najprostszy model, zamknięcie kontraktu pamięci.
3. [ ] `MT6520` — PIA używane przez PET.
4. [ ] `MOS6522` — VIA używane przez PET i VIC-20.
5. [ ] `MT6545` — CRTC PET.
6. [ ] `MOS6560` — VIC-I, timing PAL/NTSC i audio.
7. [ ] `MC146818` — pełny kontrakt RTC albo formalnie ograniczony kontrakt kartridża.
8. [x] `MOS6551` — model ACIA, transport bajtowy, mapowanie SuperPET i test IRQ;
   rdzeń 6809 jest wydzielony i ma testowaną nakładkę SuperPET z firmware Waterloo,
   przełączaniem CPU, bankowanym RAM-em i modelem MOS 6702; warianty dongla
   wymagające innych sekwencji pozostają otwarte.
9. [ ] Testy integracyjne PET/VIC-20 i testy ROM/diagnostic.

Każdy dodatkowy układ powinien mieć własną mapę adresów oraz test boot/diagnostic.

## Uruchamianie testów

```text
dotnet test tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj --no-restore
dotnet test tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj --no-restore --collect:"XPlat Code Coverage"
```

`MOS6551` jest podłączony do mapy SuperPET `$EFF0-$EFF3` i transportu bajtowego.
MOS6702 jest podłączony do mapy 6809 `$EFE0-$EFE3`; przełączanie CPU, bankowany
RAM i test resetu firmware Waterloo są już dostępne.
