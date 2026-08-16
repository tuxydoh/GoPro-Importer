# GoPro Importer v1.5

Windows Forms utility for importing media from a GoPro over its local HTTP/LAN interface.

## V1.5 highlights

- Dark-mode interface
- Store by date (Last-Modified) enabled by default
- Remembers the five most recent save locations
- Remembers the GoPro URL
- Custom Windows application/taskbar icon
- Recursive GoPro media crawling
- Photo/video/other file filters
- Duplicate skipping
- Download cancellation and progress logging
- `.part` temporary files for safer interrupted downloads

## Build

Open `GoProImporter.sln` in Visual Studio on Windows, restore NuGet packages, and build the solution.

The project includes a GitHub Actions workflow that builds on `windows-latest`.
