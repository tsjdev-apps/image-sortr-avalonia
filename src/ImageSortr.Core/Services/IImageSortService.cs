using ImageSortr.Core.Models;

namespace ImageSortr.Core.Services;

/// <summary>
/// Sorts top-level image files into date-based destination folders.
/// </summary>
public interface IImageSortService
{
    /// <summary>
    /// Sorts the supported images in the requested source directory.
    /// </summary>
    Task<SortResult> SortAsync(
        SortOptions options,
        IProgress<SortProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
