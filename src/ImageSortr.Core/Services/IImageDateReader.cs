namespace ImageSortr.Core.Services;

/// <summary>
/// Resolves a usable local image date from EXIF data or file-system timestamps.
/// </summary>
public interface IImageDateReader
{
    /// <summary>
    /// Gets the best available local date for an image file.
    /// </summary>
    DateTime? GetImageDate(string filePath);
}
