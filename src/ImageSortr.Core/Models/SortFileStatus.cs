namespace ImageSortr.Core.Models;

/// <summary>
/// Describes the final outcome for one source file.
/// </summary>
public enum SortFileStatus
{
    /// <summary>The file was copied to its resolved date folder.</summary>
    Copied,

    /// <summary>The file was skipped because a destination file already existed.</summary>
    Skipped,

    /// <summary>The file could not be processed.</summary>
    Failed
}
