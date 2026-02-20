# Contributing to MediaBrowser

Thank you for your interest in contributing! This document explains how to get
involved, report issues, and submit changes.

---

## Reporting Bugs

A good bug report makes it much easier to reproduce and fix the problem.
Please include:

1. **OS version** (e.g. Windows 11 22H2 x64)
2. **Application version** (first line of `log.txt`)
3. **Full `log.txt`** from the failing session
4. **Steps to reproduce** — the more specific, the better
5. **Expected behaviour** vs **actual behaviour**

Open a new issue: <https://github.com/etha-bob/MediaViewer/issues>

---

## Suggesting Features

Open an issue and prefix the title with `[Feature]`. Describe:

- What problem the feature solves
- How you imagine it working
- Any alternatives you considered

---

## Submitting Code Changes

### 1. Fork and branch

```bash
git clone https://github.com/etha-bob/MediaViewer.git
cd MediaViewer
git checkout -b feature/my-change
```

### 2. Build and test locally

See [BUILD.md](BUILD.md) for full instructions.

```powershell
dotnet build source/MediaBrowser/MediaBrowser.csproj --configuration Release -p:Platform=x64
```

### 3. Code style guidelines

- Follow the existing file formatting (spaces, not tabs; Allman-style braces).
- Keep methods focused and small.
- Add XML doc comments (`/// <summary>`) to public members.
- Avoid adding new NuGet packages unless strictly necessary.
- Write thread-safe code — several services are accessed from background tasks.

### 4. Commit messages

Use the imperative mood in the subject line:

```
Add keyboard shortcut to open selected file
Fix null reference in ConfigService when config directory is missing
Update README with system requirements
```

- Keep the subject line under 72 characters.
- Add a blank line, then a longer description if needed.

### 5. Open a Pull Request

- Target the `main` branch.
- Fill in the PR template — describe *what* changed and *why*.
- The CI pipeline will automatically build and validate the PR.
- Address any review comments promptly.

---

## Code of Conduct

Be respectful and constructive. Harassment, personal attacks, or discriminatory
language will not be tolerated and may result in removal from the project.

---

## Questions?

Open a discussion or issue on GitHub — we're happy to help.
