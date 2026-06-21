namespace ImageSortr.Core.Services;

/// <summary>
/// Supplies file-system timestamps so date fallback behavior remains testable.
/// </summary>
public interface IFileTimestampProvider
{
    /// <summary>Gets the creation time for a file.</summary>
    DateTime GetCreationTime(string filePath);

    /// <summary>Gets the last-write time for a file.</summary>
    DateTime GetLastWriteTime(string filePath);
}
