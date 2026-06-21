# Copilot Instructions

## Project

Cross-platform desktop application for sorting image files into date-based folders based on EXIF capture dates. Built with C# and Avalonia UI targeting .NET, runnable on Windows, macOS, and Linux.

The application is called **Image Sortr** and should follow the same general style and architecture as the existing image tool family:

* Image Renamr
* Image Convertr
* Image Resizr

The app lets the user select a source folder, a target folder, and optionally a custom folder name. It reads supported image files from the source folder, extracts the capture date from EXIF metadata where available, and copies the files into matching folders in the target folder.

## Tech Stack

* **Language:** C#
* **UI Framework:** [Avalonia](https://avaloniaui.net/) cross-platform desktop
* **UI Theme:** Semi.Avalonia
* **Pattern:** MVVM
* **MVVM Helpers:** CommunityToolkit.Mvvm
* **Dependency Injection:** Microsoft.Extensions.DependencyInjection
* **Package Manager:** NuGet
* **Build System:** MSBuild via .NET CLI
* **Testing:** xUnit

## Build & Run

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run the application
dotnet run --project src/ImageSortr.App/ImageSortr.App.csproj
```

If the solution file is available, prefer solution-based commands:

```bash
dotnet restore ImageSortr.slnx
dotnet build ImageSortr.slnx
dotnet run --project src/ImageSortr.App/ImageSortr.App.csproj
```

## Testing

```bash
# Run all tests
dotnet test

# Run all tests using the solution
dotnet test ImageSortr.slnx

# Run a single test
dotnet test --filter "FullyQualifiedName~MyTestClass.MyTestMethod"

# Run tests in a specific project
dotnet test tests/ImageSortr.Tests/ImageSortr.Tests.csproj
```

## Architecture

```text
src/
  ImageSortr.App/         # Main Avalonia application project
    ViewModels/           # MVVM ViewModels that bind to Views
    Views/                # Avalonia AXAML views
    App.axaml             # Application entry point
    App.axaml.cs          # Application bootstrap and service registration

  ImageSortr.Core/        # Non-UI application logic
    Models/               # Options, results, progress records, enums
    Services/             # Sorting, EXIF/date reading, folder resolving, file operations
    Validation/           # Input validation where useful

tests/
  ImageSortr.Tests/       # Unit/integration tests for Core and ViewModels
```

Keep UI and business logic clearly separated.

* `ImageSortr.App` contains Avalonia views, view models, app startup, styling, and dependency injection setup.
* `ImageSortr.Core` contains image discovery, EXIF date extraction, target folder resolution, file copy logic, conflict handling, validation, and progress reporting.
* `ImageSortr.Tests` verifies core behavior and view model behavior.

ViewModels may live in the app project, but they must remain testable and must not depend directly on Avalonia UI types.

Avalonia views use `.axaml`, not `.xaml`.

Code-behind should be minimal and should only contain view-specific wiring that cannot reasonably be expressed through bindings or commands.

## Key Conventions

* Follow standard .NET naming:

  * `PascalCase` for types, properties, methods, and public members
  * `camelCase` for local variables and parameters
  * `_camelCase` for private fields
* Enable nullable reference types.
* Prefer immutable models and records for options, results, and progress data.
* ViewModels implement `INotifyPropertyChanged`.
* Prefer `CommunityToolkit.Mvvm` source generators:

  * `[ObservableProperty]`
  * `[RelayCommand]`
* Use dependency injection through `Microsoft.Extensions.DependencyInjection`.
* Register services in `App.axaml.cs`.
* Keep long-running work asynchronous.
* Avoid blocking the UI thread.
* Use cancellation tokens where practical.
* Keep logic deterministic and easy to test.
* Do not place business logic in Avalonia code-behind.

## Branding

The app-specific accent color is:

```text
#A60321
```

Use this accent color consistently for:

* primary actions
* selected states
* highlights
* progress indicators
* important accent UI elements

Integrate the color through Avalonia/Semi.Avalonia theme resources where possible. Avoid hardcoding the color repeatedly throughout views.

Ensure the UI remains readable and has sufficient contrast in supported themes.

## Image Handling

Supported image extensions should include at least:

```text
.jpg
.jpeg
.png
.gif
.bmp
.tif
.tiff
.webp
.avif
```

The app should process files from the selected source folder only.

Do not recurse into nested subfolders unless this feature is explicitly added later.

Source files must never be modified or moved. The app copies files into the target folder.

## EXIF Date Handling

The app should determine the image date using the following priority:

1. EXIF `DateTimeOriginal`
2. EXIF `DateTimeDigitized`
3. EXIF `DateTime`
4. File creation time as fallback
5. File last write time as final fallback

The extracted date should be normalized to a local date and used only as a date component for folder naming.

Metadata reading must be isolated behind a testable abstraction, for example:

```csharp
public interface IImageDateReader
{
    DateTime GetImageDate(string filePath);
}
```

or an equivalent asynchronous version if that better fits the implementation.

If EXIF metadata is missing, invalid, or unreadable, the app must not crash. Use the fallback behavior.

## Folder Naming

By default, files are copied into folders named:

```text
YYYY-MM-DD
```

Example:

```text
2026-06-20
```

If the user provides an optional folder name, the generated folder name should be:

```text
YYYY-MM-DD - Custom Folder Name
```

Example:

```text
2026-06-20 - Hamburg
```

The optional folder name must be sanitized for Windows, macOS, and Linux file systems.

Trim whitespace.

Remove or replace invalid path characters.

If the sanitized custom folder name is empty, fall back to the date-only folder name.

## Existing Folder Matching

Before creating a new folder, the app must check whether a matching folder already exists in the selected target folder.

For a date such as:

```text
2026-06-20
```

the app should look for existing directories whose names start with either:

```text
2026-06-20
```

or:

```text
06-20
```

Examples of matching folders:

```text
2026-06-20
2026-06-20 - Hamburg
2026-06-20 Hamburg
06-20
06-20 - Hamburg
06-20 Hamburg
```

If a matching folder already exists, use it even if the remaining folder name is different from the generated name.

Examples:

* Date `2026-06-20`, optional folder name `Hamburg`, existing folder `2026-06-20 - Cruise` → use `2026-06-20 - Cruise`
* Date `2026-06-20`, existing folder `06-20 - Cruise` → use `06-20 - Cruise`
* Date `2026-06-20`, optional folder name `Hamburg`, no matching folder → create `2026-06-20 - Hamburg`
* Date `2026-06-20`, no optional folder name, no matching folder → create `2026-06-20`

When multiple matching folders exist, use a deterministic rule:

1. Prefer folders starting with `YYYY-MM-DD`.
2. Then prefer folders starting with `MM-DD`.
3. Within the same priority group, choose alphabetically using ordinal ignore-case comparison.

This logic should live in a dedicated, testable service or class, for example:

```csharp
DateFolderResolver
```

## File Conflict Handling

The app copies files while preserving the original file name and extension.

If a destination file already exists, support at least these modes:

```text
Skip
Overwrite
```

Default behavior should be:

```text
Skip
```

When skipping existing files, include them in the final summary.

When overwriting, perform file operations safely. Prefer writing to a temporary file first and replacing the destination file where practical.

## Core Services

Prefer a sorting service similar to:

```csharp
public interface IImageSortService
{
    Task<SortResult> SortAsync(
        SortOptions options,
        IProgress<SortProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
```

The service should:

1. Validate options.
2. Discover supported image files in deterministic order.
3. Extract or resolve the image date.
4. Resolve the destination folder.
5. Create the destination folder if necessary.
6. Copy the image file.
7. Respect conflict handling.
8. Report progress.
9. Continue processing when a single file fails.
10. Return a complete result summary.

Sort source files deterministically by file name using ordinal ignore-case comparison.

## Error Handling

The app should be robust and user-friendly.

* Do not crash because one image cannot be processed.
* If metadata extraction fails, use fallback timestamps.
* If copying one file fails, record the failure and continue.
* Show a clear final summary.
* Show friendly validation errors before starting if required inputs are invalid.

The final summary should include at least:

* total files found
* copied files
* skipped files
* failed files

Optionally include:

* number of folders used
* number of folders created

## Testing Guidelines

Add or update tests whenever behavior changes.

At minimum, cover:

* image extension filtering
* non-recursive source folder processing
* EXIF/date fallback behavior
* folder name generation
* existing folder matching by `YYYY-MM-DD`
* existing folder matching by `MM-DD`
* folder matching priority and deterministic ordering
* optional folder name sanitization
* skip existing conflict handling
* overwrite existing conflict handling
* progress reporting
* result summary counts
* validation failures
* ViewModel command enablement
* ViewModel progress and summary updates

Prefer testing core services directly without UI dependencies.

Use fake or stub implementations for metadata reading where that makes tests simpler and more deterministic.
