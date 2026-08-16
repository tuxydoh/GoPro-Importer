# GoPro Importer v1.6.3

GoPro Importer is an unofficial Windows utility for importing media from a GoPro over the camera's local HTTP/Wi-Fi interface.

> **Unofficial project:** GoPro Importer is an independent open-source project and is not affiliated with, endorsed by, sponsored by, or maintained by GoPro, Inc. GoPro and related marks are trademarks of their respective owners.

## Features

- Dark-mode Windows interface
- Store by date using camera `Last-Modified` metadata
- Remembers the five most recent save locations
- Remembers the GoPro media URL
- Recursive GoPro media crawling
- Separate Photos, Videos, LRV, THM, and Other filters
- Duplicate detection using camera-reported file size
- Verification of completed local files against camera-reported `Content-Length`
- Optional **Delete from GoPro after successful verification** workflow
- Automatic retry/backoff for transient camera delete failures
- Per-session transaction log saved in the destination folder
- Completion summary for downloads, skips, verification, deletion, and errors
- Open Destination Folder button
- Download cancellation and progress logging
- `.part` temporary files for safer interrupted downloads

## Download

For most users, use the latest package from **GitHub Releases**. The Windows x64 release is self-contained, so Visual Studio and a separate .NET installation are not required.

1. Download `GoProImporter-v1.6.3-win-x64.zip` from Releases.
2. Extract the ZIP to a folder of your choice.
3. Run `GoProImporter.exe`.
4. Connect your computer to the GoPro's Wi-Fi network before importing.

Windows may display a SmartScreen warning because the executable is not currently code-signed.

## Safe deletion behavior

**Delete from GoPro is destructive and should be used carefully.**

Deletion is opt-in, starts disabled every time the application launches, and requires confirmation before an import begins. A camera file is only submitted for deletion after its corresponding local copy passes verification.

The current verification step compares the completed local file size with the camera-reported `Content-Length`. If the camera does not report a size, the sizes do not match, or all delete attempts fail, the camera copy is retained.

Transient camera delete errors are retried with increasing delays. A failed delete never removes the verified local copy.

Before enabling deletion for important footage, test the workflow with disposable media on your own camera and firmware version.

## Privacy

GoPro Importer does not require an account, cloud service, API key, analytics service, or telemetry service. Application settings are stored locally under the current Windows user's Local Application Data folder. The app stores the GoPro URL and up to five recent destination folders.

Session logs are written to the selected destination folder and may contain local folder paths and GoPro media filenames. Review logs before sharing them publicly if those details are sensitive.

## Stream Deck

An optional Stream Deck launcher icon is included as a convenience for users who launch GoPro Importer from an Elgato Stream Deck. It is not required to build or run the application.

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

The repository also includes GitHub Actions automation for Windows builds and versioned release packages.

## Contributing

Issues and pull requests are welcome. For bug reports, include the GoPro model, firmware version, relevant log lines, and steps to reproduce. Please remove personal paths or filenames from logs if you do not want them public.

## License

Licensed under the [MIT License](LICENSE).
