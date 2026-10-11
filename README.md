# AtHome Camera Network Blocker

> **This software is permanently free and open source.** Licensed under the MIT License — use it, modify it, and redistribute it freely, forever. No paid version, no pro tier, no hidden cost.

A lightweight, single-file Windows utility that lets you **one-click block / unblock the network access** of the AtHome Camera (掌上看家) streamer (`AtHomeVideoStreamer.exe`) using the built-in Windows Firewall.

It runs silently at boot and blocks the camera program by default, with no tray icon and no taskbar presence. Run the `.exe` any time to open the control panel and toggle the connection.

---

## Features

- **One-click disconnect / reconnect** — adds or removes a Windows Firewall outbound-block rule for the target program.
- **Single `.exe` file** — no installer, no dependencies, no runtime to install (uses .NET Framework 4.x built into Windows 10 / 11).
- **Auto-start on boot** — registers a logon scheduled task that runs the tool in silent mode and applies the block, then exits. No UAC popup at login.
- **No tray icon, no taskbar** — completely invisible when running in the background. The control panel only appears when you launch the `.exe` manually.
- **Single instance** — a global mutex prevents duplicate copies from running.
- **English interface** — all UI text is in English.
- **Auto-detect** — can locate `AtHomeVideoStreamer.exe` automatically, or you can browse to select any target `.exe`.

## How it works

The tool does **not** kill the camera process. It calls `netsh advfirewall firewall` to create an outbound **block** rule for the target executable:

```
netsh advfirewall firewall add rule name="AtHomeNetBlocker_Rule"
    dir=out program="C:\path\to\AtHomeVideoStreamer.exe"
    action=block profile=any enable=yes
```

Because the rule is stored persistently in Windows Firewall, the program does **not** need to stay running. At boot, the silent (`/silent`) mode ensures the rule exists and then exits immediately.

Unblocking simply deletes the rule:

```
netsh advfirewall firewall delete rule name="AtHomeNetBlocker_Rule"
```

## Requirements

- Windows 10 / 11 (x86 or x64)
- Administrator privileges (required to modify Windows Firewall rules — the `.exe` requests elevation automatically)

## Usage

1. Double-click `AtHomeCameraBlocker.exe` (approve the administrator prompt).
2. Click **Detect** to auto-locate `AtHomeVideoStreamer.exe`, or click **Browse...** to select it manually.
3. Click **BLOCK NETWORK** to disconnect the camera program, or **UNBLOCK** to restore access.
4. Check **Auto-start on boot** if you want the camera blocked automatically every time you log in.
5. Close the window — the program exits and the firewall rule remains active.

### Command-line modes

| Argument | Behavior |
|---|---|
| *(none)* | Opens the English control panel. |
| `/silent` or `/autostart` | Applies the block rule silently and exits. Used by the boot scheduled task. |

## Build from source

Requires the .NET Framework C# compiler (`csc.exe`), which is included with Windows:

```bat
build.bat
```

Or manually:

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /target:winexe /platform:anycpu /optimize+ ^
  /out:AtHomeCameraBlocker.exe ^
  /win32icon:src\app.ico ^
  /win32manifest:src\app.manifest ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.dll ^
  src\Program.cs
```

The output is a single `AtHomeCameraBlocker.exe` (~15 KB).

## Project structure

```
AtHomeCameraBlocker/
├── README.md
├── LICENSE              (MIT)
├── .gitignore
├── build.bat            (one-click build)
└── src/
    ├── Program.cs       (full source, C# 5 / .NET Framework 4.x)
    ├── app.manifest     (requireAdministrator)
    └── generate-icon.ps1 (optional: regenerate the custom app.ico)
```

## License

[MIT License](LICENSE) — permanently free and open source.
