namespace ImageSortr.Core.Models;

/// <summary>
/// Represents the outcome of sorting one source image.
/// </summary>
public sealed record SortedFileResult(
    string SourceFile,
    string? DestinationFile,
    DateTime? ImageDate,
    SortFileStatus Status,
    string Message);
