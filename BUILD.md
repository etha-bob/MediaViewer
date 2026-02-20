# Building MediaBrowser from Source

## Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 8.0 or later | https://dotnet.microsoft.com/download/dotnet/8.0 |
| Windows | 10 / 11 (64-bit) | — |

> **Note:** The application targets `net8.0-windows` and uses WPF, so it can only be
> built on Windows (or in a Windows container).

---

## Quick Build

```powershell
# 1. Clone the repository
git clone https://github.com/etha-bob/MediaViewer.git
cd MediaViewer

# 2. Restore NuGet packages
dotnet restore source/MediaBrowser/MediaBrowser.csproj

# 3. Build (Debug)
dotnet build source/MediaBrowser/MediaBrowser.csproj -p:Platform=x64

# 4. Build (Release)
dotnet build source/MediaBrowser/MediaBrowser.csproj --configuration Release -p:Platform=x64
```

The compiled output lands in `source/MediaBrowser/bin/x64/<Configuration>/net8.0-windows/`.

---

## Publish Self-Contained Portable Executable

```powershell
dotnet publish source/MediaBrowser/MediaBrowser.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:Platform=x64 `
    --output publish/
```

The single `MediaBrowser.exe` in `publish/` contains the .NET runtime and all
dependencies — no installation required on the target machine.

---

## Packaging Script

A PowerShell helper script creates the release ZIP used by CI/CD:

```powershell
.\scripts\package-portable.ps1 -Version "v1.0.0-local"
```

This produces `MediaBrowser-Portable-v1.0.0-local.zip` with the executable,
a default `config/config.json`, and documentation.

---

## Project Layout

```
source/
└── MediaBrowser/
    ├── Models/          # Data model (MediaFile, SubDirectoryNode)
    ├── ViewModels/      # MVVM ViewModel (filtering, sorting, state)
    ├── Services/        # FileScanner, ConfigService, Logger, ShellHelper
    ├── Converters/      # WPF value converters
    ├── App.xaml(.cs)    # Application entry point, global exception handling
    ├── MainWindow.xaml(.cs)
    └── MediaBrowser.csproj
```

---

## Common Build Errors

| Error | Cause | Fix |
|-------|-------|-----|
| `NETSDK1020` – platform not supported | Building on Linux/macOS | Build on Windows |
| `CS0234` – type not found | Missing restore | Run `dotnet restore` first |
| `Platform=x64` warning | Incorrect SDK bitness | Install the x64 .NET SDK |

---

## Debug vs Release

| Configuration | Optimisations | PDB | Use for |
|---------------|--------------|-----|---------|
| Debug | Off | Full | Day-to-day development |
| Release | On | `pdbonly` (separate file) | Releases / performance testing |

PDB files are kept separate from the executable so crash dumps can still be
symbolicated without increasing the size of the distributed binary.
