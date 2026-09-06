using ImageSortr.App.Models;
using ImageSortr.App.Resources.Localization;
using ImageSortr.App.ViewModels;
using ImageSortr.Core.Models;
using ImageSortr.Core.Services;

namespace ImageSortr.Tests;

/// <summary>
/// Verifies the primary command state, progress, validation, and folder-picker behavior of the main view model.
/// </summary>
public sealed class MainWindowViewModelTests
{
    [Fact]
    public void StartCommandIsDisabledUntilBothRequiredFoldersHaveValues()
    {
        MainWindowViewModel viewModel = new(new StubImageSortService());

        Assert.False(viewModel.StartSortingCommand.CanExecute(null));

        viewModel.SourceFolder = "source";
        Assert.False(viewModel.StartSortingCommand.CanExecute(null));

        viewModel.TargetFolder = "target";
        Assert.True(viewModel.StartSortingCommand.CanExecute(null));
    }

    [Fact]
    public async Task BrowseCommandsStoreTheSelectedFolderPaths()
    {
        MainWindowViewModel viewModel = new(new StubImageSortService())
        {
            BrowseSourceFolderDelegate = () => Task.FromResult<string?>("C:\\Images\\Source"),
            BrowseTargetFolderDelegate = () => Task.FromResult<string?>("C:\\Images\\Target")
        };

        await viewModel.BrowseSourceFolderCommand.ExecuteAsync(null);
        await viewModel.BrowseTargetFolderCommand.ExecuteAsync(null);

        Assert.Equal("C:\\Images\\Source", viewModel.SourceFolder);
        Assert.Equal("C:\\Images\\Target", viewModel.TargetFolder);
    }

    [Fact]
    public void OverwriteCheckboxMapsToTheExpectedConflictMode()
    {
        MainWindowViewModel viewModel = new(new StubImageSortService());

        Assert.False(viewModel.IsOverwriteExisting);
        Assert.Equal(SortConflictMode.Skip, viewModel.SelectedConflictMode);

        viewModel.IsOverwriteExisting = true;
        Assert.Equal(SortConflictMode.Overwrite, viewModel.SelectedConflictMode);

        viewModel.IsOverwriteExisting = false;
        Assert.Equal(SortConflictMode.Skip, viewModel.SelectedConflictMode);
    }

    [Fact]
    public void IncludeYearCheckboxIsDisabledByDefault()
    {
        MainWindowViewModel viewModel = new(new StubImageSortService());

        Assert.False(viewModel.IncludeYearInFolderName);
        Assert.Equal(Strings.IncludeYear_DescriptionWithoutYear, viewModel.FolderNameHint);

        viewModel.IncludeYearInFolderName = true;

        Assert.Equal(Strings.IncludeYear_DescriptionWithYear, viewModel.FolderNameHint);
    }

    [Fact]
    public async Task StartCommandUpdatesProgressSummaryAndRequestAfterSuccessfulSort()
    {
        StubImageSortService service = new()
        {
            Handler = (options, progress, _) =>
            {
                SortedFileResult first = new(
                    "first.jpg",
                    Path.Combine("target", "2026-09-05", "first.jpg"),
                    new DateTime(2026, 9, 5),
                    SortFileStatus.Copied,
                    "Copied 'first.jpg'.");
                SortedFileResult second = new(
                    "second.jpg",
                    Path.Combine("target", "2026-09-05", "second.jpg"),
                    new DateTime(2026, 9, 5),
                    SortFileStatus.Skipped,
                    "Skipped 'second.jpg'.");

                progress?.Report(new SortProgress(1, 2, first.Message, first));
                progress?.Report(new SortProgress(2, 2, second.Message, second));

                return Task.FromResult(new SortResult(
                    2,
                    1,
                    1,
                    0,
                    1,
                    1,
                    [first, second]));
            }
        };
        MainWindowViewModel viewModel = new(service)
        {
            SourceFolder = "source",
            TargetFolder = "target",
            OptionalFolderName = "Hamburg",
            IncludeYearInFolderName = true,
            SelectedConflictMode = SortConflictMode.Overwrite
        };

        await viewModel.StartSortingCommand.ExecuteAsync(null);

        Assert.NotNull(service.LastOptions);
        Assert.Equal("source", service.LastOptions.SourceFolder);
        Assert.Equal("target", service.LastOptions.TargetFolder);
        Assert.Equal("Hamburg", service.LastOptions.OptionalFolderName);
        Assert.Equal(SortConflictMode.Overwrite, service.LastOptions.ConflictMode);
        Assert.True(service.LastOptions.IncludeYearInFolderName);
        Assert.False(viewModel.IsRunning);
        Assert.True(viewModel.CanEditInputs);
        Assert.Equal(2, viewModel.ProcessedFileCount);
        Assert.Equal(2, viewModel.TotalFileCount);
        Assert.Equal(100, viewModel.ProgressPercentage);
        Assert.Equal("100%", viewModel.ProgressPercentageText);
        Assert.Equal(3, viewModel.ProcessingHistory.Count);
        Assert.Equal(ProcessingStatus.Sorted, viewModel.ProcessingHistory[0].Status);
        Assert.Equal(ProcessingStatus.Skipped, viewModel.ProcessingHistory[1].Status);
        Assert.Equal(ProcessingStatus.Completed, viewModel.ProcessingHistory[2].Status);
        Assert.Equal(
            LocalizationFormatter.FormatProcessingSummary(1, 0, 1, 0),
            viewModel.ProcessingHistory[2].Details);
        Assert.True(viewModel.HasProcessingHistory);
        Assert.False(viewModel.HasNoProcessingHistory);
    }

    [Fact]
    public async Task InputsAreDisabledWhileASortIsRunning()
    {
        TaskCompletionSource<SortResult> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubImageSortService service = new()
        {
            Handler = (_, _, _) =>
            {
                started.SetResult();
                return completion.Task;
            }
        };
        MainWindowViewModel viewModel = new(service)
        {
            SourceFolder = "source",
            TargetFolder = "target"
        };

        Task sortingTask = viewModel.StartSortingCommand.ExecuteAsync(null);
        await started.Task;

        Assert.True(viewModel.IsRunning);
        Assert.False(viewModel.CanEditInputs);
        Assert.False(viewModel.StartSortingCommand.CanExecute(null));

        completion.SetResult(new SortResult(0, 0, 0, 0, 0, 0, []));
        await sortingTask;
    }

    [Fact]
    public async Task StartCommandShowsValidationDetailsForInvalidFolders()
    {
        MainWindowViewModel viewModel = new(new StubImageSortService
        {
            Handler = (_, _, _) => throw new DirectoryNotFoundException("The source folder 'missing' does not exist.")
        })
        {
            SourceFolder = "missing",
            TargetFolder = "target"
        };

        await viewModel.StartSortingCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasValidationMessage);
        Assert.Equal(Strings.Validation_SourceNotFound, viewModel.ValidationMessage);
        Assert.False(viewModel.IsRunning);
    }

    [Fact]
    public async Task StartingANewSortClearsHistoryAndResetsProgress()
    {
        int invocation = 0;
        TaskCompletionSource<SortResult> secondCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SortedFileResult previousFile = new(
            "previous.jpg",
            Path.Combine("target", "09-05", "previous.jpg"),
            new DateTime(2026, 9, 5),
            SortFileStatus.Copied,
            "Copied.");
        StubImageSortService service = new()
        {
            Handler = (_, _, _) =>
            {
                invocation++;
                if (invocation == 1)
                {
                    return Task.FromResult(new SortResult(1, 1, 0, 0, 1, 1, [previousFile]));
                }

                secondStarted.SetResult();
                return secondCompletion.Task;
            }
        };
        MainWindowViewModel viewModel = new(service)
        {
            SourceFolder = "source",
            TargetFolder = "target"
        };

        await viewModel.StartSortingCommand.ExecuteAsync(null);
        Assert.Equal(2, viewModel.ProcessingHistory.Count);

        Task sortingTask = viewModel.StartSortingCommand.ExecuteAsync(null);
        await secondStarted.Task;

        Assert.Empty(viewModel.ProcessingHistory);
        Assert.Equal(0, viewModel.ProcessedFileCount);
        Assert.Equal(0, viewModel.TotalFileCount);
        Assert.Equal(0, viewModel.ProgressPercentage);
        Assert.True(viewModel.HasNoProcessingHistory);

        secondCompletion.SetResult(new SortResult(0, 0, 0, 0, 0, 0, []));
        await sortingTask;
    }

    private sealed class StubImageSortService : IImageSortService
    {
        public SortOptions? LastOptions { get; private set; }

        public Func<SortOptions, IProgress<SortProgress>?, CancellationToken, Task<SortResult>> Handler { get; init; } =
            (_, _, _) => Task.FromResult(new SortResult(0, 0, 0, 0, 0, 0, []));

        public Task<SortResult> SortAsync(
            SortOptions options,
            IProgress<SortProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            LastOptions = options;
            return Handler(options, progress, cancellationToken);
        }
    }
}
