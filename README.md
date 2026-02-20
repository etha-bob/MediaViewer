# MediaBrowser

A **portable Windows application** for browsing and viewing media files (images, videos, GIFs,
and animated files) from a selected directory, with filtering, sorting, and zoom capabilities.

[![Build and Release](https://github.com/etha-bob/MediaViewer/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/etha-bob/MediaViewer/actions/workflows/build-and-release.yml)

---

## Features

| Feature | Details |
|---|---|
| **Directory Browser** | Recursively scans any folder for all supported media |
| **Thumbnail Grid** | Scrollable wrap-panel with dynamically sized thumbnails |
| **Filter by Type** | All · Images · Videos · Animated (GIF/APNG) |
| **Filter by Filename** | Real-time search as you type |
| **Filter by Date** | Modified-date range via date-pickers |
| **Filter by Size** | Min / max file size in KB |
| **Sorting** | Name · Date Created · Date Modified · Size · Type — ascending or descending |
| **Zoom Slider** | 100 px → 800 px; Ctrl+Plus / Ctrl+Minus keyboard shortcuts |
| **Portable** | No installation · No registry · Settings in `config/config.json` |
| **Auto-resume** | Reopens the last-used directory on next launch |

## Supported Formats

| Category | Extensions |
|---|---|
| Images | `.jpg` `.jpeg` `.png` `.bmp` `.tiff` `.tif` `.webp` `.psp` `.tga` `.pcx` `.xyz` |
| Videos | `.mp4` `.avi` `.mov` `.mkv` `.webm` |
| Animated | `.gif` `.apng` |

## System Requirements

- Windows 10 / 11 (64-bit)
- 4 GB RAM recommended for large collections

## Installation

1. Download the latest `MediaBrowser-Portable-*.zip` from the [Releases](../../releases) page.
2. Extract to **any location** (USB drive, local folder, network share, etc.).
3. Run **`MediaBrowser.exe`** — no installation required.

## Building from Source

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```powershell
# Build
dotnet build source/MediaBrowser/MediaBrowser.csproj --configuration Release -p:Platform=x64

# Publish self-contained single-file executable
dotnet publish source/MediaBrowser/MediaBrowser.csproj `
    --configuration Release --runtime win-x64 --self-contained true `
    -p:PublishSingleFile=true --output publish/

# Or use the packaging script
.\scripts\package-portable.ps1 -Version "v1.0.0-local"
```

## Project Structure

```
MediaBrowser/
├── .github/
│   └── workflows/
│       ├── build-and-release.yml   # CI/CD: build + publish releases on main
│       └── pr-validation.yml       # PR validation build
├── source/
│   └── MediaBrowser/
│       ├── Models/
│       │   └── MediaFile.cs        # Media file data model
│       ├── ViewModels/
│       │   └── MainViewModel.cs    # MVVM view model (filtering, sorting, state)
│       ├── Services/
│       │   ├── FileScanner.cs      # Recursive media file discovery
│       │   └── ConfigService.cs    # Portable JSON configuration
│       ├── Converters/
│       │   └── Converters.cs       # WPF value converters
│       ├── App.xaml / App.xaml.cs
│       ├── MainWindow.xaml         # Main UI (toolbar, grid, zoom bar)
│       ├── MainWindow.xaml.cs
│       └── MediaBrowser.csproj
├── scripts/
│   ├── package-portable.ps1        # Local packaging helper
│   └── generate-checksums.ps1      # SHA-256 checksum generator
└── README.md
```

## CI/CD

Every push to `main` automatically:
1. Builds the project with .NET 8
2. Publishes a self-contained single-file Windows executable
3. Packages it into a portable ZIP with config template and README
4. Generates a SHA-256 checksum
5. Creates a versioned GitHub Release with the ZIP attached

PRs trigger a validation build that comments the result directly on the PR.

## Keyboard Shortcuts

| Key | Action |
|---|---|
| `Ctrl` + `+` | Increase thumbnail size |
| `Ctrl` + `-` | Decrease thumbnail size |

## Portable Mode

All application state is persisted to `config/config.json` in the same folder as the
executable — no registry keys, no `%APPDATA%` writes. Safe to run from a USB drive.

## License

[MIT License](LICENSE)