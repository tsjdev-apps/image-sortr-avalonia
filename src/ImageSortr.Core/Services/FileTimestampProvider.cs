namespace ImageSortr.Core.Services;

/// <summary>
/// Gets local timestamps from the file system.
/// </summary>
public sealed class FileTimestampProvider : IFileTimestampProvider
{
    /// <inheritdoc />
    public DateTime GetCreationTime(string filePath) => File.GetCreationTime(filePath);

    /// <inheritdoc />
    public DateTime GetLastWriteTime(string filePath) => File.GetLastWriteTime(filePath);
}
