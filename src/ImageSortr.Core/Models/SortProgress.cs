namespace ImageSortr.Core.Models;

/// <summary>
/// Represents a live progress update from an image-sorting batch.
/// </summary>
public sealed record SortProgress(
    int Current,
    int Total,
    string? CurrentFile,
    string Message);
