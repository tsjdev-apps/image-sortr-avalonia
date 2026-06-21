using ImageSortr.Core.Models;
using ImageSortr.Core.Services;

namespace ImageSortr.Tests;

/// <summary>
/// Verifies EXIF priority and timestamp fallback behavior without requiring binary image fixtures.
/// </summary>
public sealed class ImageDateReaderTests
{
    [Fact]
    public void GetImageDateUsesExifDateTimeOriginalBeforeOtherExifDates()
    {
        DateTime original = new(2023, 4, 5, 10, 30, 0);
        ImageDateReader reader = CreateReader(
            new ExifDateValues(original, new DateTime(2024, 1, 2), new DateTime(2025, 1, 2)));

        Assert.Equal(original.Date, reader.GetImageDate("image.jpg"));
    }

    [Fact]
    public void GetImageDateFallsBackThroughDigitizedAndDateTimeExifFields()
    {
        ImageDateReader digitizedReader = CreateReader(
            new ExifDateValues(null, new DateTime(2024, 2, 3), new DateTime(2025, 2, 3)));
        ImageDateReader dateTimeReader = CreateReader(
            new ExifDateValues(null, null, new DateTime(2025, 3, 4)));

        Assert.Equal(new DateTime(2024, 2, 3), digitizedReader.GetImageDate("image.jpg"));
        Assert.Equal(new DateTime(2025, 3, 4), dateTimeReader.GetImageDate("image.jpg"));
    }

    [Fact]
    public void GetImageDateUsesCreationTimeWhenExifMetadataIsMissing()
    {
        DateTime creationTime = new(2024, 5, 6, 12, 0, 0, DateTimeKind.Local);
        ImageDateReader reader = CreateReader(ExifDateValues.Empty, creationTime, new DateTime(2021, 1, 1));

        Assert.Equal(creationTime.Date, reader.GetImageDate("image.png"));
    }

    [Fact]
    public void GetImageDateUsesLastWriteTimeWhenCreationTimeIsUnavailable()
    {
        DateTime lastWriteTime = new(2024, 6, 7, 12, 0, 0, DateTimeKind.Utc);
        ImageDateReader reader = CreateReader(ExifDateValues.Empty, DateTime.MinValue, lastWriteTime);

        Assert.Equal(lastWriteTime.ToLocalTime().Date, reader.GetImageDate("image.webp"));
    }

    [Fact]
    public void GetImageDateDoesNotThrowWhenMetadataReaderFails()
    {
        ImageDateReader reader = new(
            new MetadataExtractorExifDateReader(),
            new StubTimestampProvider(new DateTime(2024, 7, 8), new DateTime(2023, 1, 1)));

        Assert.Equal(new DateTime(2024, 7, 8), reader.GetImageDate("unsupported.avif"));
    }

    [Fact]
    public void GetImageDateReturnsNullWhenNoUsableDateCanBeResolved()
    {
        ImageDateReader reader = CreateReader(ExifDateValues.Empty, DateTime.MinValue, DateTime.MinValue);

        Assert.Null(reader.GetImageDate("image-without-date.avif"));
    }

    private static ImageDateReader CreateReader(
        ExifDateValues values,
        DateTime? creationTime = null,
        DateTime? lastWriteTime = null)
    {
        return new ImageDateReader(
            new StubExifDateReader(values),
            new StubTimestampProvider(
                creationTime ?? new DateTime(2020, 1, 1),
                lastWriteTime ?? new DateTime(2019, 1, 1)));
    }

    private sealed class StubExifDateReader(ExifDateValues values) : IExifDateReader
    {
        public ExifDateValues ReadExifDates(string filePath) => values;
    }

    private sealed class StubTimestampProvider(
        DateTime creationTime,
        DateTime lastWriteTime) : IFileTimestampProvider
    {
        public DateTime GetCreationTime(string filePath) => creationTime;

        public DateTime GetLastWriteTime(string filePath) => lastWriteTime;
    }
}
