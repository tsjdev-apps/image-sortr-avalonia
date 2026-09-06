using ImageSortr.Core.Models;
using ImageSortr.Core.Services;

namespace ImageSortr.Tests;

/// <summary>
/// Verifies file discovery, copying, conflicts, resilience, and progress for the sorting pipeline.
/// </summary>
public sealed class ImageSortServiceTests
{
    private static readonly DateTime ImageDate = new(2026, 6, 20);

    [Fact]
    public async Task SortAsyncCopiesSupportedImagesToTheResolvedDateFolderAndPreservesNames()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateSourceFile("zebra.png", "zebra");
        _ = workspace.CreateSourceFile("Ant.JPG", "ant");
        _ = workspace.CreateSourceFile("notes.txt", "ignore");

        SortResult result = await CreateService().SortAsync(
            CreateOptions(workspace),
            cancellationToken: TestContext.Current.CancellationToken);

        string dateFolder = Path.Combine(workspace.TargetFolder, "2026-06-20");
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(2, result.CopiedFiles);
        Assert.Equal(0, result.SkippedFiles);
        Assert.Equal(0, result.FailedFiles);
        Assert.Equal("ant", await File.ReadAllTextAsync(Path.Combine(dateFolder, "Ant.JPG"), TestContext.Current.CancellationToken));
        Assert.Equal("zebra", await File.ReadAllTextAsync(Path.Combine(dateFolder, "zebra.png"), TestContext.Current.CancellationToken));
        Assert.All(result.Files, file => Assert.Equal(SortFileStatus.Copied, file.Status));
    }

    [Fact]
    public async Task SortAsyncCreatesOptionalNameFolderInsideTheDateFolderWhenNoMatchExists()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateSourceFile("camera.jpg", "content");

        SortResult result = await CreateService().SortAsync(
            CreateOptions(workspace, optionalFolderName: "Hamburg"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, result.FoldersCreated);
        Assert.True(File.Exists(Path.Combine(workspace.TargetFolder, "2026-06-20", "Hamburg", "camera.jpg")));
    }

    [Fact]
    public async Task SortAsyncUsesMonthDayFoldersByDefaultWhenTheYearIsNotIncluded()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateSourceFile("camera.jpg", "content");

        SortResult result = await CreateService().SortAsync(
            new SortOptions(workspace.SourceFolder, workspace.TargetFolder, null, SortConflictMode.Skip),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.CopiedFiles);
        Assert.True(File.Exists(Path.Combine(workspace.TargetFolder, "06-20", "camera.jpg")));
    }

    [Fact]
    public async Task SortAsyncCopiesImagesWithoutAUsableDateToUnknownDate()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateSourceFile("camera.avif", "content");
        ImageSortService service = new(new StubImageDateReader(_ => null), new DateFolderResolver());

        SortResult result = await service.SortAsync(
            CreateOptions(workspace),
            cancellationToken: TestContext.Current.CancellationToken);

        SortedFileResult file = Assert.Single(result.Files);
        Assert.Equal(1, result.CopiedFiles);
        Assert.Null(file.ImageDate);
        Assert.True(File.Exists(Path.Combine(workspace.TargetFolder, "Unknown Date", "camera.avif")));
    }

    [Fact]
    public async Task SortAsyncUsesMatchingExistingFolderBeforeCreatingRequestedFolder()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateSourceFile("camera.jpg", "content");
        _ = workspace.CreateTargetFolder("2026-06-20 - Cruise");

        SortResult result = await CreateService().SortAsync(
            CreateOptions(workspace, optionalFolderName: "Hamburg"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.CopiedFiles);
        Assert.Equal(1, result.FoldersCreated);
        Assert.True(File.Exists(Path.Combine(workspace.TargetFolder, "2026-06-20 - Cruise", "Hamburg", "camera.jpg")));
        Assert.False(Directory.Exists(Path.Combine(workspace.TargetFolder, "2026-06-20 - Hamburg")));
    }

    [Fact]
    public async Task SortAsyncSkipsExistingDestinationFilesByDefault()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateSourceFile("camera.jpg", "new content");
        _ = workspace.CreateTargetFile("2026-06-20/camera.jpg", "existing content");

        SortResult result = await CreateService().SortAsync(
            CreateOptions(workspace, SortConflictMode.Skip),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, result.CopiedFiles);
        Assert.Equal(1, result.SkippedFiles);
        Assert.Equal("existing content", await File.ReadAllTextAsync(
            Path.Combine(workspace.TargetFolder, "2026-06-20", "camera.jpg"),
            TestContext.Current.CancellationToken));
        Assert.Equal(SortFileStatus.Skipped, Assert.Single(result.Files).Status);
    }

    [Fact]
    public async Task SortAsyncSafelyOverwritesExistingDestinationFilesWhenRequested()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateSourceFile("camera.jpg", "fresh content");
        _ = workspace.CreateTargetFile("2026-06-20/camera.jpg", "existing content");

        SortResult result = await CreateService().SortAsync(
            CreateOptions(workspace, SortConflictMode.Overwrite),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.CopiedFiles);
        Assert.Equal(0, result.SkippedFiles);
        Assert.Equal(SortFileStatus.Overwritten, Assert.Single(result.Files).Status);
        Assert.Equal("fresh content", await File.ReadAllTextAsync(
            Path.Combine(workspace.TargetFolder, "2026-06-20", "camera.jpg"),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SortAsyncProcessesOnlyTheSelectedSourceFolderAndDoesNotRecurse()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateSourceFile("top-level.jpg", "top");
        _ = workspace.CreateSourceFile("nested/nested.jpg", "nested");

        SortResult result = await CreateService().SortAsync(
            CreateOptions(workspace),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.TotalFiles);
        Assert.True(File.Exists(Path.Combine(workspace.TargetFolder, "2026-06-20", "top-level.jpg")));
        Assert.False(File.Exists(Path.Combine(workspace.TargetFolder, "2026-06-20", "nested.jpg")));
    }

    [Fact]
    public async Task SortAsyncContinuesWhenOneFileFails()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateSourceFile("good.jpg", "good");
        _ = workspace.CreateSourceFile("bad.jpg", "bad");
        StubImageDateReader dateReader = new(filePath => Path.GetFileName(filePath) == "bad.jpg"
            ? throw new IOException("Metadata could not be read.")
            : ImageDate);

        ImageSortService service = new(dateReader, new DateFolderResolver());
        SortResult result = await service.SortAsync(
            CreateOptions(workspace),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(1, result.CopiedFiles);
        Assert.Equal(1, result.FailedFiles);
        Assert.True(File.Exists(Path.Combine(workspace.TargetFolder, "2026-06-20", "good.jpg")));
        Assert.Equal(SortFileStatus.Failed, Assert.Single(result.Files, file => file.SourceFile.EndsWith("bad.jpg", StringComparison.Ordinal)).Status);
    }

    [Fact]
    public async Task SortAsyncReportsProgressForScanningAndEveryDiscoveredFile()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateSourceFile("first.jpg", "one");
        _ = workspace.CreateSourceFile("second.jpg", "two");
        RecordingProgress progress = new();

        SortResult result = await CreateService().SortAsync(
            CreateOptions(workspace),
            progress,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.TotalFiles);
        Assert.Contains(progress.Updates, update => update.Message.Contains("Scanning", StringComparison.Ordinal));
        Assert.Equal(2, progress.Updates.Count(update => update.FileResult is not null));
        Assert.All(
            progress.Updates.Where(update => update.FileResult is not null),
            update => Assert.NotNull(update.FileResult));
        Assert.Equal(2, progress.Updates.Last().Current);
        Assert.Equal(2, progress.Updates.Last().Total);
    }

    [Fact]
    public async Task SortAsyncReturnsEmptyResultWhenNoSupportedImagesExist()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateSourceFile("notes.txt", "ignore");

        SortResult result = await CreateService().SortAsync(
            CreateOptions(workspace),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, result.TotalFiles);
        Assert.Empty(result.Files);
    }

    [Fact]
    public async Task SortAsyncRejectsTheSameSourceAndTargetFolder()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateSourceFile("camera.jpg", "content");

        ImageSortService service = CreateService();
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => service.SortAsync(
            new SortOptions(workspace.SourceFolder, workspace.SourceFolder, null, SortConflictMode.Skip),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("different source and target", exception.Message, StringComparison.Ordinal);
    }

    private static ImageSortService CreateService()
    {
        return new ImageSortService(new StubImageDateReader(_ => ImageDate), new DateFolderResolver());
    }

    private static SortOptions CreateOptions(
        TestWorkspace workspace,
        SortConflictMode conflictMode = SortConflictMode.Skip,
        string? optionalFolderName = null,
        bool includeYearInFolderName = true)
    {
        return new SortOptions(
            workspace.SourceFolder,
            workspace.TargetFolder,
            optionalFolderName,
            conflictMode,
            includeYearInFolderName);
    }

    private sealed class StubImageDateReader(Func<string, DateTime?> getImageDate) : IImageDateReader
    {
        public DateTime? GetImageDate(string filePath) => getImageDate(filePath);
    }

    private sealed class RecordingProgress : IProgress<SortProgress>
    {
        public List<SortProgress> Updates { get; } = [];

        public void Report(SortProgress value)
        {
            Updates.Add(value);
        }
    }
}
