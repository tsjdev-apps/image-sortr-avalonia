namespace ImageSortr.Core.Models;

/// <summary>
/// Captures the user's settings for one image-sorting batch.
/// </summary>
public sealed record SortOptions(
    string SourceFolder,
    string TargetFolder,
    string? OptionalFolderName,
    SortConflictMode ConflictMode,
    bool IncludeYearInFolderName = false);
