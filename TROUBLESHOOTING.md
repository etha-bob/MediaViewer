# Troubleshooting MediaBrowser

## The application won't start

### Step 1 – Check `log.txt`

MediaBrowser writes a log file to the **same folder as the executable**.
Open `log.txt` and look for lines marked `[ERROR]` near the bottom.

### Step 2 – Common causes

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| Nothing happens on double-click | Missing Visual C++ runtime or .NET dependency | Re-download the portable ZIP; ensure you run the `MediaBrowser.exe` inside the extracted folder |
| "Application failed to start" dialog | Corrupted single-file bundle | Re-extract the ZIP to a local drive (not a network share or OneDrive-synced path) |
| Window flashes then disappears | Unhandled startup exception | Open `log.txt` for the stack trace |
| Blank/black thumbnails | WPF MediaElement codec missing | Install the latest Windows Media Player / codec pack; try the K-Lite Codec Pack |
| Config values ignored | Corrupt `config/config.json` | Delete `config/config.json` — the app will recreate it with defaults on next launch |
| "Access denied" writing config | Running from read-only media | Move the application folder to a writable location |

---

## Where is `log.txt`?

```
<folder containing MediaBrowser.exe>
├── MediaBrowser.exe
├── log.txt          ← here
└── config/
    └── config.json
```

Each run **appends** to the existing file so history is preserved.
The file is plain text and can be opened with Notepad or any text editor.

---

## Reading the log

```
[2026-01-15 09:31:04] [INFO ] === MediaBrowser Starting ===
[2026-01-15 09:31:04] [INFO ] Version   : 1.0.0.0
[2026-01-15 09:31:04] [INFO ] Executable: C:\Tools\MediaBrowser\
[2026-01-15 09:31:04] [INFO ] OS        : Microsoft Windows 11.0.22631
[2026-01-15 09:31:04] [INFO ] .NET      : .NET 8.0.x
[2026-01-15 09:31:04] [INFO ] Startup: application initialised successfully
```

If you see an `[ERROR]` line, the full exception message and stack trace follow
immediately after — copy that section when reporting a bug.

---

## Thumbnails not loading

1. Confirm the files are in a [supported format](README.md#supported-formats).
2. Check that the path contains only standard characters (no emoji or unusual Unicode).
3. Verify the application has read access to the media folder.
4. For videos, ensure Windows Media Foundation codecs are installed.

---

## Performance issues with large libraries

- The application lazy-loads thumbnails as you scroll — only visible items are decoded.
- For libraries over ~5 000 files, reduce the thumbnail size (zoom slider) to improve
  rendering speed.
- Avoid scanning network shares or slow USB drives; copy media locally first.

---

## Reporting a bug

Please include:

1. The **OS version** and **architecture** (e.g. Windows 11 22H2, x64).
2. The **application version** (shown in `log.txt` on the `Version` line).
3. The **full contents of `log.txt`** from the failing session.
4. Steps to reproduce the problem.

Open an issue at: <https://github.com/etha-bob/MediaViewer/issues>
