namespace ImageSortr.Core.Models;

/// <summary>
/// Summarizes an image-sorting batch after every discovered file has been considered.
/// </summary>
public sealed record SortResult(
    int TotalFiles,
    int CopiedFiles,
    int SkippedFiles,
    int FailedFiles,
    int FoldersUsed,
    int FoldersCreated,
    IReadOnlyList<SortedFileResult> Files);
