# AudioSwap

AudioSwap is a small Windows tray utility for quickly switching between two saved audio output devices.

It is built as a WinUI 3 desktop app with a lightweight settings window and a tray-first workflow, so you can swap outputs faster than going through Windows quick settings each time.

## Features

- Tray icon with quick toggle, settings, and exit actions
- Save two preferred playback devices and switch between them instantly
- Persist preferences in `%LOCALAPPDATA%\AudioSwap\settings.json`
- Change the Windows default output across console, multimedia, and communications roles
- Dynamic tray icon updates based on the active saved output
- Fixed-size settings window with custom title bar and dark mode support

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK
- Inno Setup 6 if you want to build the installer

## Run Locally

To build and install a local test copy:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-AudioSwap.ps1
```

That script publishes the app and installs it to `%LOCALAPPDATA%\Programs\AudioSwap`.

## Build Release Installer

To build an Inno Setup installer:

```powershell
powershell -ExecutionPolicy Bypass -File .\Build-InnoInstaller.ps1 -AppVersion 0.1.0
```

The compiled installer is written to `dist\inno`.

## Project Layout

- `Models/` application data models
- `Services/` audio switching, settings persistence, logging, native helpers, and tray integration
- `installer/` Inno Setup packaging script
- `Install-AudioSwap.ps1` local publish and install script
- `Build-InnoInstaller.ps1` release installer build script

## Notes

AudioSwap is currently focused on the working app shell: background tray behavior, saved device preferences, and fast switching. The UI can be iterated further from this stable base.
