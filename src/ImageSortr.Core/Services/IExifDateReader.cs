using ImageSortr.Core.Models;

namespace ImageSortr.Core.Services;

/// <summary>
/// Reads the relevant EXIF date fields from an image without applying fallback rules.
/// </summary>
public interface IExifDateReader
{
    /// <summary>
    /// Gets any EXIF capture-date fields available for a file.
    /// </summary>
    ExifDateValues ReadExifDates(string filePath);
}
