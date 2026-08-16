# GoPro Importer v1.6

Windows Forms utility for importing media from a GoPro over its local HTTP/LAN interface.

## V1.6 highlights

- Dark-mode interface
- Store by date (Last-Modified) enabled by default
- Remembers the five most recent save locations
- Remembers the GoPro URL
- Custom Windows application/taskbar icon
- Recursive GoPro media crawling
- Separate Photos, Videos, LRV, THM, and Other filters
- Duplicate skipping using reported camera file size
- Verify downloaded files by comparing the completed local file size with the camera-reported Content-Length
- Optional **Delete from GoPro after successful verification** workflow
- Camera deletion is opt-in, defaults off every launch, and requires confirmation before an import starts
- Verification failures never trigger camera deletion
- Delete failures are logged and leave the camera file untouched
- Per-session transaction log saved in the destination folder
- Completion summary for downloads, skips, verification, deletion, and errors
- Open Destination Folder button
- Download cancellation and progress logging
- `.part` temporary files for safer interrupted downloads

## Safe deletion behavior

When **Delete from GoPro after successful verification** is enabled, GoPro Importer uses the Open GoPro single-file media deletion endpoint only after the corresponding local file has passed verification. If the camera does not report a remote file size, if the local file size does not match, or if deletion returns an error, the camera copy is retained.

Deletion is intentionally not persisted as a user setting. It starts disabled each time the application launches.

## Build

Open `GoProImporter.sln` in Visual Studio on Windows, restore NuGet packages, and build the solution.

The project targets `.NET 8` Windows Forms and includes a GitHub Actions workflow that builds on `windows-latest`.
