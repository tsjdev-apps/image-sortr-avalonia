namespace ImageSortr.Core.Models;

/// <summary>
/// Describes how an existing destination file is handled.
/// </summary>
public enum SortConflictMode
{
    /// <summary>Keep the existing destination file and skip the source file.</summary>
    Skip,

    /// <summary>Replace the existing destination file with a copied source file.</summary>
    Overwrite
}
