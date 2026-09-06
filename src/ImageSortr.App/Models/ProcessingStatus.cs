namespace ImageSortr.App.Models;

/// <summary>
/// Describes the user-facing outcome of processing one image.
/// </summary>
public enum ProcessingStatus
{
    /// <summary>The image was copied to its destination.</summary>
    Sorted,

    /// <summary>The image was skipped without changing the destination.</summary>
    Skipped,

    /// <summary>The image replaced an existing destination file.</summary>
    Overwritten,

    /// <summary>The image could not be processed.</summary>
    Failed,

    /// <summary>The entry summarizes a completed sorting operation.</summary>
    Completed
}
