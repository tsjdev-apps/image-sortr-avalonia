using ImageSortr.Core.Models;

namespace ImageSortr.Core.Services;

/// <summary>
/// Resolves an image date using the preferred EXIF fields before falling back to file timestamps.
/// </summary>
public sealed class ImageDateReader(
    IExifDateReader exifDateReader,
    IFileTimestampProvider fileTimestampProvider) : IImageDateReader
{
    /// <inheritdoc />
    public DateTime? GetImageDate(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        ExifDateValues exifDates = exifDateReader.ReadExifDates(filePath);

        DateTime? captureDate = exifDates.DateTimeOriginal
            ?? exifDates.DateTimeDigitized
            ?? exifDates.DateTime;

        if (captureDate is not null)
        {
            return NormalizeToLocalDate(captureDate.Value);
        }

        DateTime creationTime = fileTimestampProvider.GetCreationTime(filePath);
        if (IsUsableTimestamp(creationTime))
        {
            return NormalizeToLocalDate(creationTime);
        }

        DateTime lastWriteTime = fileTimestampProvider.GetLastWriteTime(filePath);
        return IsUsableTimestamp(lastWriteTime)
            ? NormalizeToLocalDate(lastWriteTime)
            : null;
    }

    private static bool IsUsableTimestamp(DateTime value)
    {
        return value != DateTime.MinValue && value != DateTime.MaxValue;
    }

    private static DateTime NormalizeToLocalDate(DateTime value)
    {
        DateTime localDateTime = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        return localDateTime.Date;
    }
}
