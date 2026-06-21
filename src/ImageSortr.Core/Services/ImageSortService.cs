using System.Collections.Frozen;
using ImageSortr.Core.Models;

namespace ImageSortr.Core.Services;

/// <summary>
/// Copies top-level supported image files into EXIF-date-based destination folders.
/// </summary>
public sealed class ImageSortService(
    IImageDateReader imageDateReader,
    DateFolderResolver dateFolderResolver) : IImageSortService
{
    private static readonly FrozenSet<string> SupportedExtensions = new[]
    {
        ".avif",
        ".bmp",
        ".gif",
        ".jpeg",
        ".jpg",
        ".png",
        ".tif",
        ".tiff",
        ".webp"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<SortResult> SortAsync(
        SortOptions options,
        IProgress<SortProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        SortOptions normalizedOptions = ValidateAndNormalizeOptions(options);
        _ = Directory.CreateDirectory(normalizedOptions.TargetFolder);

        progress?.Report(new SortProgress(0, 0, null, "Scanning the source folder for supported image files."));

        List<string> sourceFiles = [.. Directory
            .EnumerateFiles(normalizedOptions.SourceFolder, "*", SearchOption.TopDirectoryOnly)
            .Where(filePath => SupportedExtensions.Contains(Path.GetExtension(filePath)))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(Path.GetFileName, StringComparer.Ordinal)];

        if (sourceFiles.Count == 0)
        {
            progress?.Report(new SortProgress(0, 0, null, "No supported image files were found in the source folder."));
            return new SortResult(0, 0, 0, 0, 0, 0, []);
        }

        List<SortedFileResult> fileResults = [];
        HashSet<string> foldersUsed = new(PathComparer);
        HashSet<string> foldersCreated = new(PathComparer);
        int copiedFiles = 0;
        int skippedFiles = 0;
        int failedFiles = 0;

        for (int index = 0; index < sourceFiles.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sourceFile = sourceFiles[index];
            int current = index + 1;
            SortedFileResult fileResult;

            try
            {
                DateTime? imageDate = imageDateReader.GetImageDate(sourceFile);
                DateFolderResolution folderResolution = imageDate is DateTime resolvedImageDate
                    ? dateFolderResolver.Resolve(
                        normalizedOptions.TargetFolder,
                        resolvedImageDate,
                        normalizedOptions.OptionalFolderName,
                        normalizedOptions.IncludeYearInFolderName)
                    : dateFolderResolver.ResolveUnknownDateFolder(normalizedOptions.TargetFolder);

                bool dateFolderExisted = Directory.Exists(folderResolution.DateFolderPath);
                bool targetFolderExisted = Directory.Exists(folderResolution.FolderPath);
                _ = Directory.CreateDirectory(folderResolution.FolderPath);
                foldersUsed.Add(folderResolution.FolderPath);

                if (!dateFolderExisted)
                {
                    foldersCreated.Add(folderResolution.DateFolderPath);
                }

                if (!targetFolderExisted)
                {
                    foldersCreated.Add(folderResolution.FolderPath);
                }

                string destinationFile = Path.Combine(folderResolution.FolderPath, Path.GetFileName(sourceFile));
                if (File.Exists(destinationFile) && normalizedOptions.ConflictMode == SortConflictMode.Skip)
                {
                    skippedFiles++;
                    fileResult = new SortedFileResult(
                        sourceFile,
                        destinationFile,
                        imageDate,
                        SortFileStatus.Skipped,
                        $"Skipped '{Path.GetFileName(sourceFile)}' because it already exists in the destination folder.");
                }
                else
                {
                    await CopySafelyAsync(
                        sourceFile,
                        destinationFile,
                        normalizedOptions.ConflictMode == SortConflictMode.Overwrite,
                        cancellationToken);

                    copiedFiles++;
                    fileResult = new SortedFileResult(
                        sourceFile,
                        destinationFile,
                        imageDate,
                        SortFileStatus.Copied,
                        imageDate is null
                            ? $"Copied '{Path.GetFileName(sourceFile)}' to 'Unknown Date' because no usable date was found."
                            : $"Copied '{Path.GetFileName(sourceFile)}' to '{Path.GetFileName(folderResolution.FolderPath)}'.");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (IsPerFileFailure(exception))
            {
                failedFiles++;
                fileResult = new SortedFileResult(
                    sourceFile,
                    null,
                    null,
                    SortFileStatus.Failed,
                    $"Could not process '{Path.GetFileName(sourceFile)}': {exception.Message}");
            }

            fileResults.Add(fileResult);
            progress?.Report(new SortProgress(
                current,
                sourceFiles.Count,
                sourceFile,
                fileResult.Message));
        }

        return new SortResult(
            sourceFiles.Count,
            copiedFiles,
            skippedFiles,
            failedFiles,
            foldersUsed.Count,
            foldersCreated.Count,
            fileResults);
    }

    private static StringComparer PathComparer => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static bool IsPerFileFailure(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException;
    }

    private static SortOptions ValidateAndNormalizeOptions(SortOptions options)
    {
        string sourceFolder = ValidatePath(options.SourceFolder, nameof(options.SourceFolder));
        string targetFolder = ValidatePath(options.TargetFolder, nameof(options.TargetFolder));

        if (!Directory.Exists(sourceFolder))
        {
            throw new DirectoryNotFoundException($"The source folder '{sourceFolder}' does not exist.");
        }

        if (PathsMatch(sourceFolder, targetFolder))
        {
            throw new ArgumentException(
                "Choose different source and target folders to keep the original images safe.",
                nameof(options));
        }

        return options with
        {
            SourceFolder = sourceFolder,
            TargetFolder = targetFolder,
            OptionalFolderName = FolderNameSanitizer.Sanitize(options.OptionalFolderName)
        };
    }

    private static string ValidatePath(string folderPath, string argumentName)
    {
        return string.IsNullOrWhiteSpace(folderPath)
            ? throw new ArgumentException("Both source and target folders are required.", argumentName)
            : Path.GetFullPath(folderPath.Trim());
    }

    private static bool PathsMatch(string left, string right)
    {
        StringComparison comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(
            left.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            right.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            comparison);
    }

    private static async Task CopySafelyAsync(
        string sourceFile,
        string destinationFile,
        bool overwriteExisting,
        CancellationToken cancellationToken)
    {
        string temporaryFile = Path.Combine(
            Path.GetDirectoryName(destinationFile)!,
            $".{Path.GetFileName(destinationFile)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using FileStream sourceStream = new(
                sourceFile,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await using (FileStream temporaryStream = new(
                temporaryFile,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await sourceStream.CopyToAsync(temporaryStream, cancellationToken);
            }

            File.Move(temporaryFile, destinationFile, overwriteExisting);
        }
        finally
        {
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }
        }
    }
}
