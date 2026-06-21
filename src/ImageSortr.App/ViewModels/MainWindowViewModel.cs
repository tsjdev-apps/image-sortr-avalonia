using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageSortr.Core.Models;
using ImageSortr.Core.Services;

namespace ImageSortr.App.ViewModels;

/// <summary>
/// Coordinates the main Image Sortr setup, progress, and summary experience.
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

    /// <summary>Gets or sets the current number of files that have been processed.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressSummary))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    public partial int ProgressValue { get; set; }

    /// <summary>Gets or sets the total number of files expected in the current batch.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressSummary))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    public partial int ProgressMaximum { get; set; } = 1;

    /// <summary>Gets or sets the main status message shown to the user.</summary>
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Choose your folders and start sorting when you are ready.";

    /// <summary>Gets or sets the source file currently being handled.</summary>
    [ObservableProperty]
    public partial string CurrentFile { get; set; } = "No files have been processed yet.";

    /// <summary>Gets or sets the completion summary for the latest batch.</summary>
    [ObservableProperty]
    public partial string Summary { get; set; } = "No sorting batch has been completed yet.";

    /// <summary>Gets or sets a validation message for a batch that could not start.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    public partial string ValidationMessage { get; set; } = string.Empty;

    /// <summary>Gets whether setup inputs can be edited.</summary>
    public bool CanEditInputs => !IsRunning;

    /// <summary>Gets whether a validation message is available for display.</summary>
    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    /// <summary>Gets the folder-name example appropriate for the selected year option.</summary>
    public string FolderNameHint => IncludeYearInFolderName
        ? "New folders use YYYY-MM-DD/Your Name. Matching existing date folders are always reused."
        : "New folders use MM-DD/Your Name. Matching existing date folders are always reused.";

    /// <summary>Gets whether conflict mode is set to overwrite existing files.</summary>
    public bool IsOverwriteExisting
    {
        get => SelectedConflictMode == SortConflictMode.Overwrite;
        set => SelectedConflictMode = value ? SortConflictMode.Overwrite : SortConflictMode.Skip;
    }

    /// <summary>Gets the live progress as a friendly file count.</summary>
    public string ProgressSummary => $"{ProgressValue} / {ProgressMaximum} files processed";

    /// <summary>Gets the live progress as a percentage.</summary>
    public string ProgressPercentageText => ProgressMaximum <= 0
        ? "0%"
        : $"{Math.Round((double)ProgressValue / ProgressMaximum * 100, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture)}%";

    /// <summary>Gets text appropriate for the current start button state.</summary>
    public string StartButtonText => IsRunning ? "Sorting images..." : "Sort images";

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
        ValidationMessage = string.Empty;
        IsRunning = true;
        ProgressValue = 0;
        ProgressMaximum = 1;
        StatusMessage = "Preparing the sorting batch.";
        CurrentFile = "Scanning the selected source folder for supported images.";
        Summary = "Validating your folders and sorting options.";
        object progressLock = new();
        bool acceptsProgressUpdates = true;

        Progress<SortProgress> progress = new(update =>
        {
            lock (progressLock)
            {
                if (!acceptsProgressUpdates)
                {
                    return;
                }

                ProgressMaximum = Math.Max(1, update.Total);
                ProgressValue = Math.Min(update.Current, ProgressMaximum);
                CurrentFile = update.CurrentFile is null
                    ? update.Message
                    : $"{Path.GetFileName(update.CurrentFile)} — {update.Message}";
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
                ProgressMaximum = Math.Max(1, result.TotalFiles);
                ProgressValue = result.TotalFiles;
            }

            Summary = CreateSummary(result);
            StatusMessage = result.TotalFiles == 0
                ? "The source folder did not contain any supported image files."
                : "Sorting completed.";
            CurrentFile = Summary;
        }
        catch (ArgumentException exception)
        {
            HandleExpectedFailure("Please review the selected folders and try again.", exception.Message);
        }
        catch (DirectoryNotFoundException exception)
        {
            HandleExpectedFailure("The source folder could not be found.", exception.Message);
        }
        catch (IOException exception)
        {
            HandleExpectedFailure("The target folder could not be created or accessed.", exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            HandleExpectedFailure("The app does not have permission to access one of the selected folders.", exception.Message);
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

    private static string CreateSummary(SortResult result)
    {
        if (result.TotalFiles == 0)
        {
            return "Nothing was copied because no supported images were found.";
        }

        return $"{result.CopiedFiles.ToString(CultureInfo.CurrentCulture)} copied, " +
            $"{result.SkippedFiles.ToString(CultureInfo.CurrentCulture)} skipped, and " +
            $"{result.FailedFiles.ToString(CultureInfo.CurrentCulture)} failed across " +
            $"{result.FoldersUsed.ToString(CultureInfo.CurrentCulture)} folder(s).";
    }

    private void HandleExpectedFailure(string status, string details)
    {
        StatusMessage = status;
        Summary = "The sorting batch could not be started.";
        CurrentFile = details;
        ValidationMessage = details;
    }
}
