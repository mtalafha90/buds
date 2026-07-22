# Buds Control

A cross-platform desktop app for controlling Samsung Galaxy Buds and Apple AirPods from Ubuntu
(and Windows) - battery status, active noise cancellation, ambient/transparency mode, and so on -
outside of each earbuds' own phone app.

## Status: real hardware testing in progress

This project was originally built without access to real Galaxy Buds, real AirPods, or a Bluetooth
adapter at all - it's since been tried against a real, physically paired Samsung Galaxy Buds Core
on a real Ubuntu machine, which is how the issues below were actually found and fixed (rather than
guessed at). Apple/AirPods control is still completely unverified - no AirPods have been tested
against this yet.

- The architecture (connection management, UI, both transport layers) compiles clean on both
  target frameworks and has been confirmed to work end-to-end on real hardware for Samsung: device
  discovery via `bluetoothctl`, and a real RFCOMM socket connect over `AF_BLUETOOTH`, both
  succeeded against a real paired Galaxy Buds Core.
- The Samsung SPP frame format and message IDs were rewritten after fetching the actual current
  source of the reference client
  ([ThePBone/GalaxyBudsClient](https://github.com/ThePBone/GalaxyBudsClient),
  `GalaxyBudsClient/Message/SppMessageEnums.cs` and its `*Decoder.cs` classes) rather than guessed
  - this caught and fixed a real bug (the original frame format was missing a Request/Response
    type byte, and misread what the length field measured). Message IDs like `StatusUpdated`,
  `NoiseControls` and `SetAmbientMode` in `SamsungMessageId.cs` are now sourced from that real
  enum, not placeholders. What's still genuinely unverified: the exact numeric mapping for
  `NoiseControlModes` (Off/ANC/Ambient), and whether your specific model uses the 3-way
  `NoiseControls` selector or the simpler boolean `SetAmbientMode` toggle - see that file's doc
  comment.
- **Apple's AAP protocol for AirPods is still entirely unverified placeholder bytes**, sourced from
  general public write-ups rather than fetched reference source, clearly flagged in
  `AapConstants.cs` / `AapCommands.cs`. Nobody has tried this against real AirPods yet.

See "Verifying and fixing the protocol constants" below for how to close the remaining gaps.

## Architecture

```
src/
  BudsControl.Core/                Protocol-agnostic models and interfaces (no I/O)
  BudsControl.Protocols.Samsung/   Galaxy Buds SPP frame codec + device implementation
  BudsControl.Protocols.Apple/     AirPods AAP packet builder/parser + device implementation
  BudsControl.Transport.Linux/     BlueZ-based sockets (RFCOMM + L2CAP) and device discovery
  BudsControl.Transport.Windows/   WinRT Rfcomm + raw Winsock L2CAP, and device discovery
  BudsControl.App/                 Avalonia UI, multi-targeted net8.0 / net8.0-windows10.0.19041.0
```

The app project multi-targets `net8.0` (Linux) and `net8.0-windows10.0.19041.0` (Windows) from a
single codebase - that's what "converted to a Windows app" means here: there's no separate port to
do, the same UI and business logic already build for both, and `Composition/PlatformServices.cs`
just swaps in the right transport implementation per platform at compile time. Building the
Windows target does **not** require a Windows machine - the .NET SDK can cross-compile it (see
`EnableWindowsTargeting` in the `.csproj` files) - though you'll obviously need a real Windows
machine to *run* it.

### Why Samsung and Apple are implemented so differently

- **Galaxy Buds**: control runs over classic Bluetooth RFCOMM (Serial Port Profile), a proprietary
  binary message format layered on top. `BudsControl.Transport.Linux` connects this with a raw
  `AF_BLUETOOTH` socket, brute-force-scanning RFCOMM channels 1-30 rather than doing a proper SDP
  service search (a deliberate scope cut - see the doc comment on
  `LinuxRfcommTransportFactory` for why and how to replace it with real SDP if it's ever too slow).
- **AirPods**: control runs over L2CAP on a fixed PSM, no channel discovery needed - Apple's AAP
  protocol. WinRT's `StreamSocket` doesn't document arbitrary L2CAP PSM connections, so the Windows
  side goes through raw Winsock (`AF_BTH`) instead of WinRT for this one piece, matching how real
  shipping Windows Bluetooth accessory apps do it.

## Building on Ubuntu

```bash
sudo apt-get install dotnet-sdk-8.0   # or the dotnet-install.sh script from Microsoft
dotnet build
dotnet run --project src/BudsControl.App -f net8.0
```

Pairing itself is left entirely to Ubuntu's own Bluetooth settings (or `bluetoothctl pair
<address>`) - this app only lists already-paired devices and talks to them, it never performs
pairing. Once paired, click "Scan for paired earbuds" in the app.

Runtime prerequisites:
- `bluetoothd` (BlueZ) running, and your user able to run `bluetoothctl` (used for paired-device
  listing).
- Your user needs permission to open raw Bluetooth sockets - on most distributions this works
  out of the box for a normal desktop session; if you get permission errors, check you're in the
  `bluetooth` group (or you may need `sudo setcap cap_net_raw+eip` on the built binary, depending
  on distro policy).
- The earbuds' case must not be actively connected to another app (e.g. still open in the Samsung
  Wearable / an iPhone) - Bluetooth profiles like this are typically single-consumer.

## Building for Windows

From the same repository, on any machine with the .NET 8 SDK:

```bash
dotnet publish src/BudsControl.App -f net8.0-windows10.0.19041.0 -r win-x64 --self-contained
```

That produces a self-contained Windows executable under
`src/BudsControl.App/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/`. Copy it to a
Windows machine to run it - it hasn't been run on real Windows from this sandbox (there's no
Windows machine here), so treat first run as a test, particularly of the raw Winsock L2CAP path
(see `WinsockBluetoothNative.cs` for the specific risk called out there).

## Verifying and fixing the protocol constants

This is the one piece of real, hands-on work left before this reliably controls your earbuds. Two
options, in order of effort:

1. **Check against the reference open-source projects.** Samsung:
   [ThePBone/GalaxyBudsClient](https://github.com/ThePBone/GalaxyBudsClient) (see
   `GalaxyBudsRFCommProtocol.md` and the `MsgIds`/message classes under
   `GalaxyBudsClient.Platform`). Apple: [kavishdevar/librepods](https://github.com/kavishdevar/librepods)
   (see the `linux/` directory and its linked Wireshark dissector). Cross-check the byte values in
   `SamsungMessageId.cs`, `Crc16Ccitt.cs`'s checksum variant, `AapCommands.cs` and
   `AapNotificationParser.cs` against what those projects actually send/parse, and correct them -
   everything else in this repo (framing, transports, UI) should not need to change.
2. **Capture your own traffic.** Pair your earbuds with your phone, capture Bluetooth HCI traffic
   with Wireshark/btmon while toggling ANC in the official app, and read the real bytes off the
   wire. This is slower but gives you ground truth independent of any third-party write-up, and is
   what the reference projects above did originally.

## Known gaps / good next steps

- Samsung: only the "legacy" (0xFE/0xEE, 1-byte length field) SPP frame variant is implemented.
  The reference source shows newer buds use different framing (0xFD/0xDD, and a fragmented
  0xFC/0xCC variant for large transfers like firmware) - extend `SppFrameCodec` if you hit that.
- Samsung: the `NoiseControlModes` numeric mapping and the exact `ExtendedStatusUpdated` battery
  byte layout weren't confirmed against source (only `StatusUpdated` was) - if noise control or
  battery still looks wrong after the frame-format fix, these are the next things to check.
- Samsung RFCOMM channel discovery is a brute-force scan, not real SDP (see above).
- No packaging/installer work has been done (e.g. a `.deb`, a Windows MSIX/installer) - `dotnet
  publish` output is what you get today.
- No automated tests - correctness here fundamentally depends on real hardware, which wasn't
  available while building this.
