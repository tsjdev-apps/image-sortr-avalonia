using ImageSortr.App.Resources.Localization;

namespace ImageSortr.App.Models;

/// <summary>
/// Represents one compact, localized entry in the processing history.
/// </summary>
public sealed class ProcessingEntry
{
    /// <summary>Initializes a processing-history entry.</summary>
    public ProcessingEntry(
        string fileName,
        ProcessingStatus status,
        string? destination = null,
        string? details = null)
    {
        FileName = fileName;
        Status = status;
        Destination = destination;
        Details = details;
    }

    /// <summary>Gets the source image's file name.</summary>
    public string FileName { get; }

    /// <summary>Gets the result status.</summary>
    public ProcessingStatus Status { get; }

    /// <summary>Gets the destination folder relative to the selected target folder.</summary>
    public string? Destination { get; }

    /// <summary>Gets localized status details or summary counts.</summary>
    public string? Details { get; }

    /// <summary>Gets whether a destination can be displayed.</summary>
    public bool HasDestination => !string.IsNullOrWhiteSpace(Destination);

    /// <summary>Gets whether an explanatory message can be displayed.</summary>
    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    /// <summary>Gets a non-color status marker.</summary>
    public string StatusIcon => Status switch
    {
        ProcessingStatus.Sorted => "✓",
        ProcessingStatus.Skipped => "–",
        ProcessingStatus.Overwritten => "↻",
        ProcessingStatus.Failed => "!",
        ProcessingStatus.Completed => "Σ",
        _ => "•"
    };

    /// <summary>Gets the localized status label.</summary>
    public string StatusText => Status switch
    {
        ProcessingStatus.Sorted => Strings.ProcessingStatus_Sorted,
        ProcessingStatus.Skipped => Strings.ProcessingStatus_Skipped,
        ProcessingStatus.Overwritten => Strings.ProcessingStatus_Overwritten,
        ProcessingStatus.Failed => Strings.ProcessingStatus_Failed,
        ProcessingStatus.Completed => Strings.ProcessingStatus_Completed,
        _ => string.Empty
    };
}
