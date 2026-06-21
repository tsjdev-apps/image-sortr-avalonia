using ImageSortr.Core.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace ImageSortr.Core.Services;

/// <summary>
/// Reads EXIF capture-date fields using the cross-platform MetadataExtractor library.
/// </summary>
public sealed class MetadataExtractorExifDateReader : IExifDateReader
{
    /// <inheritdoc />
    public ExifDateValues ReadExifDates(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            IEnumerable<MetadataExtractor.Directory> metadata = ImageMetadataReader.ReadMetadata(filePath);
            ExifSubIfdDirectory? subIfdDirectory = metadata.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            ExifIfd0Directory? ifd0Directory = metadata.OfType<ExifIfd0Directory>().FirstOrDefault();

            return new ExifDateValues(
                TryGetDateTime(subIfdDirectory, ExifDirectoryBase.TagDateTimeOriginal),
                TryGetDateTime(subIfdDirectory, ExifDirectoryBase.TagDateTimeDigitized),
                TryGetDateTime(ifd0Directory, ExifDirectoryBase.TagDateTime));
        }
        catch (ImageProcessingException)
        {
            return ExifDateValues.Empty;
        }
        catch (IOException)
        {
            return ExifDateValues.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            return ExifDateValues.Empty;
        }
    }

    private static DateTime? TryGetDateTime(
        MetadataExtractor.Directory? directory,
        int tagType)
    {
        return directory is not null && directory.TryGetDateTime(tagType, out DateTime dateTime)
            ? dateTime
            : null;
    }
}
