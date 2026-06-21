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
        Assert.StartsWith("New folders use MM-DD", viewModel.FolderNameHint, StringComparison.Ordinal);

        viewModel.IncludeYearInFolderName = true;

        Assert.StartsWith("New folders use YYYY-MM-DD", viewModel.FolderNameHint, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartCommandUpdatesProgressSummaryAndRequestAfterSuccessfulSort()
    {
        StubImageSortService service = new()
        {
            Handler = (options, progress, _) =>
            {
                progress?.Report(new SortProgress(1, 2, "first.jpg", "Copied 'first.jpg'."));
                progress?.Report(new SortProgress(2, 2, "second.jpg", "Skipped 'second.jpg'."));

                return Task.FromResult(new SortResult(
                    2,
                    1,
                    1,
                    0,
                    1,
                    1,
                    []));
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
        Assert.Equal(2, viewModel.ProgressValue);
        Assert.Equal(2, viewModel.ProgressMaximum);
        Assert.Equal("100%", viewModel.ProgressPercentageText);
        Assert.Equal("1 copied, 1 skipped, and 0 failed across 1 folder(s).", viewModel.Summary);
        Assert.Equal("Sorting completed.", viewModel.StatusMessage);
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
        Assert.Contains("missing", viewModel.ValidationMessage, StringComparison.Ordinal);
        Assert.Equal("The source folder could not be found.", viewModel.StatusMessage);
        Assert.False(viewModel.IsRunning);
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
