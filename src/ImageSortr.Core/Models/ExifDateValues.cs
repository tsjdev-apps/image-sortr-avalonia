namespace ImageSortr.Core.Models;

/// <summary>
/// Contains the EXIF date fields that can describe when an image was captured.
/// </summary>
public sealed record ExifDateValues(
    DateTime? DateTimeOriginal,
    DateTime? DateTimeDigitized,
    DateTime? DateTime)
{
    /// <summary>Gets an empty set of EXIF date values.</summary>
    public static ExifDateValues Empty { get; } = new(null, null, null);
}
