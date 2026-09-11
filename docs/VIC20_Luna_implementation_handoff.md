# VIC-20 Emulator – implementation handoff for Luna

## Cel

Rozbudować istniejący modularny emulator 8-bit (.NET 10) o obsługę VIC-20:

1. rozszerzeń jako pluginów DLL,
2. ROM/RAM/urządzeń mapowanych w przestrzeni adresowej,
3. portów/magistral VIC-20,
4. joysticka z Windows przy emulatorze uruchomionym w WSL,
5. audio generowanego przez VIC-I.

> Uwaga: nazwy projektów poniżej są proponowane. Należy dopasować je do faktycznej struktury solution. Nie umieszczać logiki VIC-20 w projekcie Terminal/UI.

---

## Proponowana struktura

```text
src/
  CmosCpu.Abstractions/
    IExpansionPlugin.cs
    IBusDevice.cs
    IJoystickSource.cs
    IAudioOutput.cs
    AddressRange.cs

  CmosCpu.Vic20/
    Expansion/
      ExpansionBus.cs
      ExpansionLoader.cs
      RomDevice.cs
      RamDevice.cs
    Input/
      Vic20JoystickPort.cs
    Audio/
      VicIAudio.cs
    VicI/
      VicI.cs

plugins/
  Vic20.BasicCart.dll
  Vic20.RamExpansion.dll
  Vic20.MegaCart.dll
  Vic20.Joystick.Remote.dll
```

---

## 1. Expansion Bus i pluginy DLL

Każda DLL może dostarczać ROM, RAM, urządzenie I/O albo ich kombinację.

```csharp
public interface IExpansionPlugin
{
    string Name { get; }
    IEnumerable<IBusDevice> Create(IExpansionContext context);
}

public interface IBusDevice
{
    IEnumerable<AddressRange> Ranges { get; }

    byte Read(ushort address);
    void Write(ushort address, byte value);

    void Reset();
    void Tick();
}

public readonly record struct AddressRange(
    ushort Start,
    ushort End,
    BusAccess Access);

[Flags]
public enum BusAccess
{
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write
}
```

ROM:

```csharp
public sealed class RomDevice : IBusDevice
{
    private readonly byte[] _rom;
    private readonly ushort _start;

    public RomDevice(ushort start, byte[] rom)
    {
        _start = start;
        _rom = rom;
    }

    public IEnumerable<AddressRange> Ranges =>
        [new(_start, (ushort)(_start + _rom.Length - 1), BusAccess.Read)];

    public byte Read(ushort address) => _rom[address - _start];

    public void Write(ushort address, byte value) { }

    public void Reset() { }
    public void Tick() { }
}
```

RAM analogicznie, ale `ReadWrite`.

Plugin:

```csharp
public sealed class MegaCartPlugin : IExpansionPlugin
{
    public string Name => "MegaCart";

    public IEnumerable<IBusDevice> Create(IExpansionContext context)
    {
        yield return new BankedRom(...);
        yield return new RamDevice(0x2000, 8192);
        yield return new MegaCartRegisters(0x9800);
    }
}
```

`ExpansionLoader`:

- użyć `AssemblyLoadContext`,
- wyszukać typy implementujące `IExpansionPlugin`,
- instancjonować plugin,
- rejestrować urządzenia w `ExpansionBus`,
- zgłaszać konflikt zakresów,
- opcjonalnie umożliwić embedded ROM jako `EmbeddedResource`.

Nie wprowadzać sztucznego limitu liczby DLL. Ograniczeniem mają być konflikty mapowania.

---

## 2. Mapa rozszerzeń VIC-20

Obsłużyć co najmniej:

```text
BLK1  $2000-$3FFF
BLK2  $4000-$5FFF
BLK3  $6000-$7FFF
IO2   $9800-$9BFF
IO3   $9C00-$9FFF
BLK5  $A000-$BFFF
```

Typowe konfiguracje:

- ROM 8 KB,
- ROM 16 KB,
- ROM 32 KB,
- RAM 3/8/16/24/32 KB,
- bank switching,
- urządzenia korzystające z IO2/IO3.

Dwa urządzenia zajmujące ten sam zakres powinny powodować jawny konflikt, chyba że plugin sam implementuje przełączanie/bankowanie.

---

## 3. Inne porty VIC-20

Docelowo rozdzielić logicznie:

- `ExpansionBus`
- `UserPort`
- `IecBus`
- `CassettePort`
- `JoystickPort`
- audio/video jako backendy wyjściowe.

Najważniejsze do późniejszej implementacji:

- IEC: 1540/1541 i inne urządzenia,
- User Port: GPIO/RS-232 itp.,
- Cassette: Datasette / TAP,
- Control Port: joystick/paddle.

---

## 4. Joystick Windows -> WSL

Preferowana architektura: NIE przekazywać fizycznego USB bezpośrednio do WSL.

```text
Gamepad
  -> Windows
  -> XInput/SDL
  -> Vic20.InputHost.exe
  -> TCP
  -> emulator w WSL
  -> IJoystickSource
  -> Vic20JoystickPort
```

API:

```csharp
public interface IJoystickSource
{
    JoystickState GetState();
}

public readonly record struct JoystickState(
    bool Up,
    bool Down,
    bool Left,
    bool Right,
    bool Fire);
```

Prosty protokół może używać jednego bajtu:

```text
bit0 Up
bit1 Down
bit2 Left
bit3 Right
bit4 Fire
```

`Vic20.Joystick.Remote.dll` może implementować klienta TCP.

Dzięki temu emulator nie zależy od źródła:

- Xbox Pad,
- DualSense,
- klawiatura,
- fizyczny joystick,
- testowy input.

`usbipd-win` traktować jako opcję alternatywną, nie podstawową.

---

## 5. VIC-I Audio

VIC-I (MOS 6560/6561) generuje również audio.

Rejestry:

```text
$900A tone 1
$900B tone 2
$900C tone 3
$900D noise
$900E volume (0-15)
```

Model:

```text
VicI
 ├─ Video
 └─ Audio
     ├─ Tone1
     ├─ Tone2
     ├─ Tone3
     └─ Noise
          ↓
       Mixer
          ↓
     IAudioOutput
```

Interfejs backendu:

```csharp
public interface IAudioOutput
{
    int SampleRate { get; }

    void WriteSamples(ReadOnlySpan<float> samples);
}
```

Backend powinien być wymienny:

- Windows,
- Linux/WSL,
- SDL,
- WAV,
- NullAudioOutput do testów.

Audio ma należeć do emulacji VIC-I, NIE do cartridge/pluginu.

---

## 6. Wymagania implementacyjne

1. Nie wiązać core emulatora z Avalonia/Terminal/WPF.
2. Nie ładować pluginów DLL bezpośrednio w klasach CPU.
3. CPU komunikuje się tylko z mapą pamięci/busem.
4. Konflikt zakresów ma być wykrywany przy rejestracji.
5. ROM ma ignorować Write.
6. RAM ma obsługiwać Read/Write.
7. Urządzenia I/O mogą implementować efekty uboczne w `Read/Write`.
8. Plugin może dostarczać wiele urządzeń.
9. Pluginy mają być opcjonalne.
10. Wszystkie interfejsy sprzętowe trzymać w warstwie abstractions/core.
11. Dodać testy jednostkowe dla:

- mapowania ROM,
- mapowania RAM,
- konfliktów,
- plugin loading,
- joystick bit mapping,
- podstawowych rejestrów audio VIC-I.

---

## 7. Kryteria ukończenia pierwszego etapu

Pierwszy etap uznać za gotowy, gdy:

- emulator potrafi załadować DLL z katalogu `plugins`,
- DLL może zarejestrować ROM/RAM/I/O,
- działa wykrywanie konfliktów adresowych,
- można załadować prosty ROM 8 KB do BLK5,
- można dodać RAM do BLK1,
- joystick może być podany przez `IJoystickSource`,
- kod VIC-I ma przygotowaną warstwę audio z 3 tone + noise,
- UI pozostaje tylko klientem konfigurującym emulator.

---

## Zadanie dla Luny

Zaimplementuj powyższą architekturę w istniejącym repozytorium. Najpierw przeanalizuj strukturę solution i dopasuj proponowane klasy do istniejących projektów zamiast tworzyć duplikaty warstw. Zachowaj obecne konwencje DI, namespace, testów i konfiguracji.

Najpierw wykonaj:

1. interfejsy abstractions,
2. `ExpansionBus`,
3. `RomDevice` i `RamDevice`,
4. `ExpansionLoader`,
5. przykładowy plugin DLL,
6. testy,
7. joystick abstraction,
8. szkielet audio VIC-I.

Nie przebudowuj całego emulatora, jeśli nie jest to konieczne.
