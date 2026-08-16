# Contributing to GoPro Importer

Thanks for helping improve GoPro Importer.

## Bug reports

Please include:

- GoPro model
- GoPro firmware version
- Windows version
- GoPro Importer version
- Steps to reproduce
- Relevant log lines

Before posting logs publicly, remove any local paths, filenames, or other information you do not want to share.

## Pull requests

Keep changes focused and describe what problem they solve. For changes involving verification or camera deletion, explain the safety behavior and failure handling clearly.

Please build the solution successfully before opening a pull request.

## Development

The project targets .NET 8 Windows Forms.

```powershell
dotnet restore GoProImporter.csproj
dotnet build GoProImporter.csproj -c Release
```

## Destructive-operation changes

Changes to the Delete from GoPro workflow should preserve these principles:

1. Deletion remains opt-in.
2. Verification happens before deletion.
3. Verification or delete failures retain the camera copy.
4. Logs clearly identify failures and retries.
