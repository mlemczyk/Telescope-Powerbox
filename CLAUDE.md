# PowerBox: rules for AI agents

Firmware (Arduino UNO R4 WiFi, PlatformIO) and an ASCOM Switch driver (C#,
.NET Framework 4.7.2) for a telescope power box and dew-heater controller.
`README.md` describes the hardware and behaviour; read it first.

## Hard rules

- **Never commit Wi-Fi credentials.** They live in
  `Arduino/PowerBox/src/arduino_secrets.h`, which is git-ignored; the committed
  template is `arduino_secrets.example.h`. An earlier history had them in
  `WebServer.cpp` and had to be rewritten before the repo could be public.
  Before any commit, check that no SSID or password literal is staged.
- **The data line is a contract between the two halves.** The driver
  (`SwitchHardware.cs`, `updateValues()`) splits the page body on `\n`, takes
  line 2 (the third line, right after `<!DOCTYPE HTML>` and `<html>`) and
  parses 16 space-separated values by position. Change the firmware's
  `dataLine` and the driver's parser together, in the same order, or the driver
  breaks silently. Nothing may be printed before that line, and nothing may
  wrap it.
- **The box's IP address is in two places**: `IPAddress ip(...)` in
  `WebServer.cpp` and the URL in `updateValues()`. Keep them equal.
- **Heater safety.** A heater is driven only while its DS18B20 probe is
  connected; keep that behaviour in `MeterReader.cpp` / `DewController.cpp`.

## Verifying a change

- Firmware: `pio run -e uno_r4_wifi` in `Arduino/PowerBox/` must build. That
  is all an agent can check; behaviour needs the hardware, so say what was and
  was not tested.
- Driver: builds in Visual Studio with the ASCOM Platform installed. The files
  `LocalServer.cs`, `ClassFactory.cs`, `GarbageCollection.cs`,
  `ReferenceCountedObject.cs`, `HardwareClassAttribute.cs` and `ReadMe.htm`
  come from the ASCOM template: leave them alone unless the template is being
  updated. The driver's own logic is in `SwitchDriver/`.

## Conventions

- Build output and IDE state (`.pio/`, `bin/`, `obj/`, `.vs/`) are ignored;
  don't commit them.
- Commit messages say what changed and why.
