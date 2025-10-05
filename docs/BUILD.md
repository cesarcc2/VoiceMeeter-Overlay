# Build and Run

VMHud is a .NET 8 WPF application targeting Windows 10/11.

## Prerequisites

- Windows 10 or 11
- .NET 8 SDK (https://dotnet.microsoft.com/download)
- Visual Studio 2022 (optional) or `dotnet` CLI
- Voicemeeter Potato installed for runtime validation
- Ensure `VoicemeeterRemote.dll` is available on PATH or next to the executable

## Clone and Restore

```powershell
git clone <your-repo-url>
cd VoiceMeter Overlay
dotnet --info
dotnet restore
```

## Build

```powershell
dotnet build -c Debug
```

## Run (App project)

```powershell
dotnet run -c Debug --project src/VMHud.App
```

If Voicemeeter is not running, the overlay stays idle. When Voicemeeter starts, the matrix updates automatically.

## Tests

```powershell
dotnet test -c Debug
```

## Publish (single-file)

To create a a installer, use Inno Setup Compiler

or to compile just the app

```powershell
dotnet publish src/VMHud.App -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false
```

Artifacts will appear under `src/VMHud.App/bin/Release/net8.0-windows/win-x64/publish/`.

Copy `VoicemeeterRemote.dll` next to the published EXE or ensure the directory containing it is on PATH.

## Notes

- We do not redistribute Voicemeeter libraries; install Voicemeeter separately
- If hotkey fails to register, change it in the config file and retry