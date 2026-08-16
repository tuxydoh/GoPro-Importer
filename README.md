# GoPro Importer

[![Build](https://github.com/tuxydoh/GoPro-Importer/actions/workflows/build.yml/badge.svg)](https://github.com/tuxydoh/GoPro-Importer/actions/workflows/build.yml)
[![Latest Release](https://img.shields.io/github/v/release/tuxydoh/GoPro-Importer)](https://github.com/tuxydoh/GoPro-Importer/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/platform-Windows-0078D4)](https://github.com/tuxydoh/GoPro-Importer/releases/latest)

**Fast Windows media import for GoPro cameras, with verification, safe delete-after-verify, retry handling, and session logging.**

GoPro Importer is an unofficial open-source Windows utility for importing media from a GoPro over the camera's local HTTP/Wi-Fi interface. It is designed around a simple idea: importing footage should be quick, predictable, and cautious about deleting anything from the camera.

> **Unofficial project:** GoPro Importer is an independent open-source project and is not affiliated with, endorsed by, sponsored by, or maintained by GoPro, Inc. GoPro and related marks are trademarks of their respective owners.

## Highlights

- Dark-mode Windows interface
- Store imported media by camera `Last-Modified` date
- Remembers the five most recent save locations
- Remembers the GoPro media URL
- Recursive GoPro media crawling
- Separate Photos, Videos, LRV, THM, and Other filters
- Duplicate detection using camera-reported file size
- Verification of newly completed local files against camera-reported `Content-Length`
- SHA-256 comparison of pre-existing local files against camera contents before destructive deletion
- Optional **Delete from GoPro after successful verification** workflow
- Automatic retry/backoff for transient camera delete failures
- Per-session transaction log saved in the destination folder
- Completion summary for downloads, skips, verification, deletion, and errors
- Open Destination Folder button
- Download cancellation and progress logging
- `.part` temporary files for safer interrupted downloads

## Download

For most users, download the latest self-contained Windows x64 package from **[GitHub Releases](https://github.com/tuxydoh/GoPro-Importer/releases/latest)**. Visual Studio and a separate .NET installation are not required.

1. Download the latest `GoProImporter-v*-win-x64.zip` release.
2. Extract the ZIP to a folder of your choice.
3. Run `GoProImporter.exe`.
4. Connect your computer to the GoPro's Wi-Fi network before importing.

Windows may display a SmartScreen warning because the executable is not currently code-signed.

## Safe deletion behavior

**Delete from GoPro is destructive and should be used carefully.**

Deletion is opt-in, starts disabled every time the application launches, and requires confirmation before an import begins. A camera file is only submitted for deletion after its corresponding local copy passes verification.

For a file downloaded during the current import, verification requires the completed local file size to match the camera-reported `Content-Length`. The download is first completed to a `.part` file and closed before it is moved into place.

For a file that already exists locally, matching filename and size are **not enough to authorize camera deletion**. When Delete from GoPro is enabled, GoPro Importer reads both the existing local file and the camera file and requires their SHA-256 hashes to match before deletion is attempted. This additional comparison can take roughly as long as reading the file from the camera again, but it prevents deletion based only on an accidental same-size match.

If the camera does not report a size, local and remote sizes differ, SHA-256 comparison fails, or all delete attempts fail, the camera copy is retained.

Transient camera delete errors are retried with increasing delays. A failed delete never removes the verified local copy.

Before enabling deletion for important footage, test the workflow with disposable media on your own camera and firmware version.

## Privacy

GoPro Importer does not require an account, cloud service, API key, analytics service, or telemetry service. Application settings are stored locally under the current Windows user's Local Application Data folder. The app stores the GoPro URL and up to five recent destination folders.

Session logs are written to the selected destination folder and may contain local folder paths and GoPro media filenames. Review logs before sharing them publicly if those details are sensitive.

## Stream Deck

An optional Stream Deck launcher icon is included in `Assets/` for users who launch GoPro Importer from an Elgato Stream Deck. It is not required to build or run the application.

## Roadmap

The roadmap captures ideas for future versions. These are planned directions, not promises or committed release dates.

### v1.7 — Import experience

- Dry Run / Preview mode before downloading or deleting
- Automatic GoPro discovery on the local network
- Camera information/status panel
- Battery and storage information where available
- Improved live progress details including file counts and transferred size
- Colorized/scannable session log output
- UI polish for filters, integrity controls, and destructive-action warnings

### v1.8 — Workflow improvements

- Multiple GoPro support
- Import presets
- Optional automatic import when a supported camera becomes available
- Better destination/profile management
- Additional import summary and history tools

### v2.0 — Distribution and richer media workflow

- Automatic update support
- Installer/package option alongside the portable ZIP
- Media thumbnail/preview support
- Broader workflow automation and export options

Have an idea that belongs here? Open a feature request rather than assuming it is already scheduled.

## Build from source

Requirements:

- Windows
- .NET 8 SDK
- Visual Studio with .NET desktop development support, or the `dotnet` CLI

Open `GoProImporter.sln` in Visual Studio, restore NuGet packages, and build the solution.

To publish a self-contained Windows x64 build from the command line:

```powershell
dotnet publish GoProImporter.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The repository includes GitHub Actions automation for Windows builds, dependency vulnerability checks, and versioned release packages. Pull-request builds run with read-only repository permissions; release write permissions are isolated to trusted pushes to `main`.

## Contributing

Issues and pull requests are welcome. For bug reports, include the GoPro model, firmware version, GoPro Importer version, Windows version, relevant log lines, expected behavior, actual behavior, and steps to reproduce. Please remove personal paths or filenames from logs if you do not want them public.

For security vulnerabilities, follow [SECURITY.md](SECURITY.md) and use GitHub's private vulnerability reporting instead of posting exploit details in a public issue.

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidance.

## License

Licensed under the [MIT License](LICENSE).
