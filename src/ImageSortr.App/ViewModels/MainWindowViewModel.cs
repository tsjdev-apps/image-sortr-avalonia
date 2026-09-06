using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageSortr.App.Models;
using ImageSortr.App.Resources.Localization;
using ImageSortr.Core.Models;
using ImageSortr.Core.Services;

namespace ImageSortr.App.ViewModels;

/// <summary>
/// Coordinates the main Image Sortr setup and processing-history experience.
/// </summary>
public sealed partial class MainWindowViewModel(
    IImageSortService imageSortService) : ObservableObject
{
    /// <summary>Gets or sets whether a sort batch is running.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSortingCommand))]
    [NotifyPropertyChangedFor(nameof(CanEditInputs))]
    [NotifyPropertyChangedFor(nameof(StartButtonText))]
    public partial bool IsRunning { get; set; }

    /// <summary>Gets or sets the source folder selected by the user.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSortingCommand))]
    public partial string SourceFolder { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination folder selected by the user.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSortingCommand))]
    public partial string TargetFolder { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional suffix for newly created date folders.</summary>
    [ObservableProperty]
    public partial string OptionalFolderName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether newly created date folder names include their year.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FolderNameHint))]
    public partial bool IncludeYearInFolderName { get; set; }

    /// <summary>Gets or sets the behavior used when a destination file already exists.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverwriteExisting))]
    public partial SortConflictMode SelectedConflictMode { get; set; } = SortConflictMode.Skip;

    /// <summary>Gets or sets the number of files processed in the current batch.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressSummary))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    public partial int ProcessedFileCount { get; set; }

    /// <summary>Gets or sets the total number of files in the current batch.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressSummary))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    [NotifyPropertyChangedFor(nameof(ProgressBarMaximum))]
    public partial int TotalFileCount { get; set; }

    /// <summary>Gets or sets a validation message for a batch that could not start.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    public partial string ValidationMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets whether processing-history entries are available.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoProcessingHistory))]
    public partial bool HasProcessingHistory { get; private set; }

    /// <summary>Gets the entries produced by the current sorting operation.</summary>
    public ObservableCollection<ProcessingEntry> ProcessingHistory { get; } = [];

    /// <summary>Gets whether setup inputs can be edited.</summary>
    public bool CanEditInputs => !IsRunning;

    /// <summary>Gets whether a validation message is available for display.</summary>
    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    /// <summary>Gets whether the processing history is empty.</summary>
    public bool HasNoProcessingHistory => !HasProcessingHistory;

    /// <summary>Gets the folder-name example appropriate for the selected year option.</summary>
    public string FolderNameHint => IncludeYearInFolderName
        ? Strings.IncludeYear_DescriptionWithYear
        : Strings.IncludeYear_DescriptionWithoutYear;

    /// <summary>Gets whether conflict mode is set to overwrite existing files.</summary>
    public bool IsOverwriteExisting
    {
        get => SelectedConflictMode == SortConflictMode.Overwrite;
        set => SelectedConflictMode = value ? SortConflictMode.Overwrite : SortConflictMode.Skip;
    }

    /// <summary>Gets the localized live progress count.</summary>
    public string ProgressSummary => LocalizationFormatter.FormatProgress(
        ProcessedFileCount,
        TotalFileCount);

    /// <summary>Gets the live progress percentage.</summary>
    public double ProgressPercentage => TotalFileCount <= 0
        ? 0
        : Math.Round(
            (double)ProcessedFileCount / TotalFileCount * 100,
            MidpointRounding.AwayFromZero);

    /// <summary>Gets a non-zero maximum suitable for the progress bar.</summary>
    public int ProgressBarMaximum => Math.Max(1, TotalFileCount);

    /// <summary>Gets the localized live progress as a percentage.</summary>
    public string ProgressPercentageText => string.Create(
        CultureInfo.CurrentCulture,
        $"{ProgressPercentage:0}%");

    /// <summary>Gets text appropriate for the current start button state.</summary>
    public string StartButtonText => IsRunning
        ? Strings.SortingImages_Button
        : Strings.SortImages_Button;

    /// <summary>Gets or sets the UI-supplied source-folder picker.</summary>
    public Func<Task<string?>>? BrowseSourceFolderDelegate { get; set; }

    /// <summary>Gets or sets the UI-supplied target-folder picker.</summary>
    public Func<Task<string?>>? BrowseTargetFolderDelegate { get; set; }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task BrowseSourceFolderAsync()
    {
        if (BrowseSourceFolderDelegate is null)
        {
            throw new InvalidOperationException("No source-folder picker delegate has been assigned.");
        }

        SourceFolder = await BrowseSourceFolderDelegate() ?? string.Empty;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task BrowseTargetFolderAsync()
    {
        if (BrowseTargetFolderDelegate is null)
        {
            throw new InvalidOperationException("No target-folder picker delegate has been assigned.");
        }

        TargetFolder = await BrowseTargetFolderDelegate() ?? string.Empty;
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanStart))]
    private async Task StartSortingAsync()
    {
        ResetProgress();
        ValidationMessage = string.Empty;
        IsRunning = true;

        object progressLock = new();
        HashSet<string> displayedFiles = new(StringComparer.Ordinal);
        bool acceptsProgressUpdates = true;

        Progress<SortProgress> progress = new(update =>
        {
            lock (progressLock)
            {
                if (!acceptsProgressUpdates)
                {
                    return;
                }

                TotalFileCount = Math.Max(0, update.Total);
                ProcessedFileCount = Math.Clamp(update.Current, 0, TotalFileCount);

                if (update.FileResult is not null
                    && displayedFiles.Add(update.FileResult.SourceFile))
                {
                    AddProcessingEntry(update.FileResult);
                }
            }
        });

        try
        {
            SortOptions options = new(
                SourceFolder,
                TargetFolder,
                OptionalFolderName,
                SelectedConflictMode,
                IncludeYearInFolderName);

            SortResult result = await Task.Run(
                () => imageSortService.SortAsync(options, progress));

            lock (progressLock)
            {
                acceptsProgressUpdates = false;
                TotalFileCount = result.TotalFiles;
                ProcessedFileCount = result.TotalFiles;

                foreach (SortedFileResult fileResult in result.Files)
                {
                    if (displayedFiles.Add(fileResult.SourceFile))
                    {
                        AddProcessingEntry(fileResult);
                    }
                }

                AddProcessingSummary(result);
            }
        }
        catch (ArgumentException)
        {
            HandleExpectedFailure(Strings.Validation_InvalidFolders);
        }
        catch (DirectoryNotFoundException)
        {
            HandleExpectedFailure(Strings.Validation_SourceNotFound);
        }
        catch (IOException)
        {
            HandleExpectedFailure(Strings.Validation_TargetUnavailable);
        }
        catch (UnauthorizedAccessException)
        {
            HandleExpectedFailure(Strings.Validation_AccessDenied);
        }
        finally
        {
            lock (progressLock)
            {
                acceptsProgressUpdates = false;
            }

            IsRunning = false;
        }
    }

    private bool CanStart()
    {
        return !IsRunning
            && !string.IsNullOrWhiteSpace(SourceFolder)
            && !string.IsNullOrWhiteSpace(TargetFolder);
    }

    private void ResetProgress()
    {
        ProcessingHistory.Clear();
        HasProcessingHistory = false;
        ProcessedFileCount = 0;
        TotalFileCount = 0;
    }

    private void AddProcessingEntry(SortedFileResult fileResult)
    {
        ProcessingStatus status = fileResult.Status switch
        {
            SortFileStatus.Copied => ProcessingStatus.Sorted,
            SortFileStatus.Skipped => ProcessingStatus.Skipped,
            SortFileStatus.Overwritten => ProcessingStatus.Overwritten,
            SortFileStatus.Failed => ProcessingStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(
                nameof(fileResult),
                fileResult.Status,
                "Unsupported processing status.")
        };

        string? destination = GetRelativeDestination(fileResult.DestinationFile);
        string? details = status switch
        {
            ProcessingStatus.Skipped => Strings.ProcessingDetail_FileExists,
            ProcessingStatus.Failed => Strings.ProcessingDetail_CouldNotProcess,
            _ => null
        };

        ProcessingHistory.Add(new ProcessingEntry(
            Path.GetFileName(fileResult.SourceFile),
            status,
            destination,
            details));
        HasProcessingHistory = true;
    }

    private string? GetRelativeDestination(string? destinationFile)
    {
        string? destinationFolder = Path.GetDirectoryName(destinationFile);
        if (string.IsNullOrWhiteSpace(destinationFolder))
        {
            return null;
        }

        string relativePath = Path.GetRelativePath(TargetFolder, destinationFolder);
        return relativePath == "."
            ? Path.GetFileName(destinationFolder)
            : relativePath;
    }

    private void AddProcessingSummary(SortResult result)
    {
        int overwrittenFiles = result.Files.Count(
            file => file.Status == SortFileStatus.Overwritten);
        int sortedFiles = Math.Max(0, result.CopiedFiles - overwrittenFiles);
        string summary = LocalizationFormatter.FormatProcessingSummary(
            sortedFiles,
            overwrittenFiles,
            result.SkippedFiles,
            result.FailedFiles);

        ProcessingHistory.Add(new ProcessingEntry(
            Strings.ProcessingSummary_Title,
            ProcessingStatus.Completed,
            details: summary));
        HasProcessingHistory = true;
    }

    private void HandleExpectedFailure(string message)
    {
        ValidationMessage = message;
    }
}
