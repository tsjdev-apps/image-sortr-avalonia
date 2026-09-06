using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace ImageSortr.App.Resources.Localization;

/// <summary>
/// Formats localized strings that contain runtime values.
/// </summary>
public static class LocalizationFormatter
{
    private static readonly ConcurrentDictionary<string, CompositeFormat> ProcessingSummaryFormats = new();

    /// <summary>Formats the processed-file count with basic singular/plural handling.</summary>
    public static string FormatProgress(int processedFiles, int totalFiles)
    {
        string format = processedFiles == 1
            ? Strings.Progress_ProcessedFile
            : Strings.Progress_ProcessedFiles;

        return string.Format(
            CultureInfo.CurrentCulture,
            format,
            processedFiles,
            totalFiles);
    }

    /// <summary>Formats the final counts written to the processing history.</summary>
    public static string FormatProcessingSummary(
        int sortedFiles,
        int overwrittenFiles,
        int skippedFiles,
        int failedFiles)
    {
        CompositeFormat format = ProcessingSummaryFormats.GetOrAdd(
            LocalizationSettings.CurrentLocaleName,
            static _ => CompositeFormat.Parse(Strings.ProcessingSummary_Counts));

        return string.Format(
            CultureInfo.CurrentCulture,
            format,
            sortedFiles,
            overwrittenFiles,
            skippedFiles,
            failedFiles);
    }
}
