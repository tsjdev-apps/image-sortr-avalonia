using ImageSortr.Core.Services;

namespace ImageSortr.Tests;

/// <summary>
/// Verifies deterministic folder reuse and generated-name behavior.
/// </summary>
public sealed class DateFolderResolverTests
{
    private static readonly DateTime ImageDate = new(2026, 6, 20);

    [Fact]
    public void ResolveGeneratesDateOnlyFolderWhenNoOptionalNameWasProvided()
    {
        using TestWorkspace workspace = new();

        DateFolderResolver resolver = new();
        var result = resolver.Resolve(workspace.TargetFolder, ImageDate, null, includeYearInFolderName: true);

        Assert.False(result.UsesExistingFolder);
        Assert.Equal("2026-06-20", Path.GetFileName(result.FolderPath));
    }

    [Fact]
    public void ResolveGeneratesMonthDayFolderWhenYearIsNotIncluded()
    {
        using TestWorkspace workspace = new();

        DateFolderResolver resolver = new();
        var result = resolver.Resolve(workspace.TargetFolder, ImageDate, null, includeYearInFolderName: false);

        Assert.False(result.UsesExistingFolder);
        Assert.Equal("06-20", Path.GetFileName(result.FolderPath));
    }

    [Fact]
    public void ResolveGeneratesCustomNameAsAChildOfTheDateFolderWhenNoMatchExists()
    {
        using TestWorkspace workspace = new();

        DateFolderResolver resolver = new();
        var result = resolver.Resolve(workspace.TargetFolder, ImageDate, "Hamburg", includeYearInFolderName: true);

        Assert.False(result.UsesExistingFolder);
        Assert.Equal(Path.Combine("2026-06-20", "Hamburg"), Path.GetRelativePath(workspace.TargetFolder, result.FolderPath));
        Assert.Equal("2026-06-20", Path.GetFileName(result.DateFolderPath));
    }

    [Fact]
    public void ResolveUsesExistingFullDateFolderRegardlessOfOptionalName()
    {
        using TestWorkspace workspace = new();
        string existingFolder = workspace.CreateTargetFolder("2026-06-20 - Cruise");

        DateFolderResolver resolver = new();
        var result = resolver.Resolve(workspace.TargetFolder, ImageDate, "Hamburg", includeYearInFolderName: true);

        Assert.False(result.UsesExistingFolder);
        Assert.Equal(existingFolder, result.DateFolderPath);
        Assert.Equal(Path.Combine(existingFolder, "Hamburg"), result.FolderPath);
    }

    [Fact]
    public void ResolveUsesExistingMonthDayFolderWhenNoFullDateFolderExists()
    {
        using TestWorkspace workspace = new();
        string existingFolder = workspace.CreateTargetFolder("06-20 Hamburg");

        DateFolderResolver resolver = new();
        var result = resolver.Resolve(workspace.TargetFolder, ImageDate, "Cruise", includeYearInFolderName: true);

        Assert.False(result.UsesExistingFolder);
        Assert.Equal(existingFolder, result.DateFolderPath);
        Assert.Equal(Path.Combine(existingFolder, "Cruise"), result.FolderPath);
    }

    [Fact]
    public void ResolvePrefersFullDateMatchOverMonthDayMatch()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateTargetFolder("06-20 - Summer");
        string fullDateFolder = workspace.CreateTargetFolder("2026-06-20 - Cruise");

        DateFolderResolver resolver = new();
        var result = resolver.Resolve(workspace.TargetFolder, ImageDate, null, includeYearInFolderName: true);

        Assert.Equal(fullDateFolder, result.FolderPath);
    }

    [Fact]
    public void ResolveUsesOrdinalIgnoreCaseAlphabeticalOrderForMultipleMatches()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateTargetFolder("2026-06-20 - Zoo");
        string alphabeticalFolder = workspace.CreateTargetFolder("2026-06-20 - Alps");

        DateFolderResolver resolver = new();
        var result = resolver.Resolve(workspace.TargetFolder, ImageDate, null, includeYearInFolderName: true);

        Assert.Equal(alphabeticalFolder, result.FolderPath);
    }

    [Fact]
    public void ResolveSanitizesCrossPlatformInvalidCharacters()
    {
        using TestWorkspace workspace = new();

        DateFolderResolver resolver = new();
        var result = resolver.Resolve(workspace.TargetFolder, ImageDate, "  Ham:burg?  ", includeYearInFolderName: true);

        Assert.Equal(Path.Combine("2026-06-20", "Hamburg"), Path.GetRelativePath(workspace.TargetFolder, result.FolderPath));
    }

    [Fact]
    public void ResolveFallsBackToDateOnlyFolderWhenSanitizationEmptiesTheName()
    {
        using TestWorkspace workspace = new();

        DateFolderResolver resolver = new();
        var result = resolver.Resolve(workspace.TargetFolder, ImageDate, " :?*| ", includeYearInFolderName: true);

        Assert.Equal("2026-06-20", Path.GetFileName(result.FolderPath));
    }

    [Fact]
    public void ResolveReusesAnExistingNamedFolderInsideTheMatchingDateFolder()
    {
        using TestWorkspace workspace = new();
        string existingFolder = workspace.CreateTargetFolder(Path.Combine("2026-06-20", "Hamburg"));

        DateFolderResolver resolver = new();
        var result = resolver.Resolve(workspace.TargetFolder, ImageDate, "Hamburg", includeYearInFolderName: true);

        Assert.True(result.UsesExistingFolder);
        Assert.Equal(existingFolder, result.FolderPath);
        Assert.Equal(Path.GetDirectoryName(existingFolder), result.DateFolderPath);
    }
}
