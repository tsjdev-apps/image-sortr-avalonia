namespace ImageSortr.Core.Models;

/// <summary>
/// Represents a live progress update from an image-sorting batch.
/// </summary>
public sealed record SortProgress(
    int Current,
    int Total,
    string Message,
    SortedFileResult? FileResult = null);
