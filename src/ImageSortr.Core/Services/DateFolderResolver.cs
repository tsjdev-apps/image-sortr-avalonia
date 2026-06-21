using System.Globalization;
using ImageSortr.Core.Models;

namespace ImageSortr.Core.Services;

/// <summary>
/// Resolves a date folder by reusing an existing matching folder before generating a new one.
/// </summary>
public sealed class DateFolderResolver
{
    /// <summary>
    /// Resolves the shared destination folder for images without a usable date.
    /// </summary>
    public DateFolderResolution ResolveUnknownDateFolder(string targetFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFolder);

        string folderPath = Path.Combine(Path.GetFullPath(targetFolder), "Unknown Date");
        return new DateFolderResolution(
            folderPath,
            folderPath,
            UsesExistingFolder: Directory.Exists(folderPath));
    }

    /// <summary>
    /// Finds a matching target directory or generates the requested date-based directory path.
    /// </summary>
    public DateFolderResolution Resolve(
        string targetFolder,
        DateTime imageDate,
        string? optionalFolderName,
        bool includeYearInFolderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFolder);

        string normalizedTargetFolder = Path.GetFullPath(targetFolder);
        string fullDatePrefix = imageDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string monthDayPrefix = imageDate.ToString("MM-dd", CultureInfo.InvariantCulture);

        string? existingDateFolder = Directory.Exists(normalizedTargetFolder)
            ? FindExistingFolder(normalizedTargetFolder, fullDatePrefix, monthDayPrefix)
            : null;

        string generatedDateFolderName = includeYearInFolderName ? fullDatePrefix : monthDayPrefix;
        string dateFolderPath = existingDateFolder ?? Path.Combine(normalizedTargetFolder, generatedDateFolderName);
        string sanitizedFolderName = FolderNameSanitizer.Sanitize(optionalFolderName);

        if (string.IsNullOrEmpty(sanitizedFolderName))
        {
            return new DateFolderResolution(
                dateFolderPath,
                dateFolderPath,
                UsesExistingFolder: existingDateFolder is not null);
        }

        string namedFolderPath = Path.Combine(dateFolderPath, sanitizedFolderName);

        return new DateFolderResolution(
            namedFolderPath,
            dateFolderPath,
            UsesExistingFolder: Directory.Exists(namedFolderPath));
    }

    private static string? FindExistingFolder(
        string targetFolder,
        string fullDatePrefix,
        string monthDayPrefix)
    {
        IEnumerable<string> existingFolders = Directory.EnumerateDirectories(targetFolder);

        return existingFolders
            .Select(path => new FolderCandidate(path, Path.GetFileName(path)))
            .Where(candidate => candidate.Name.StartsWith(fullDatePrefix, StringComparison.OrdinalIgnoreCase)
                || candidate.Name.StartsWith(monthDayPrefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.Name.StartsWith(fullDatePrefix, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Name, StringComparer.Ordinal)
            .Select(candidate => candidate.Path)
            .FirstOrDefault();
    }

    private sealed record FolderCandidate(string Path, string Name);
}
