using System.Globalization;
using ImageSortr.App.Resources.Localization;

namespace ImageSortr.Tests;

/// <summary>
/// Verifies localized resources, culture fallback, and dynamic progress formatting.
/// </summary>
[Collection(CultureSensitiveTestGroup.Name)]
public sealed class LocalizationTests
{
    [Fact]
    public void EnglishResourcesAreUsedForEnglishCultures()
    {
        RunWithCulture("en-GB", () =>
        {
            Assert.Equal("Source folder", Strings.SourceFolder_Label);
            Assert.Equal("en-US", LocalizationSettings.CurrentLocaleName);
            Assert.Equal("No files have been processed yet.", Strings.Progress_NoFiles);
            Assert.Equal("1 / 10 file processed", LocalizationFormatter.FormatProgress(1, 10));
            Assert.Equal("2 / 10 files processed", LocalizationFormatter.FormatProgress(2, 10));
            Assert.Equal(
                "8 sorted · 1 overwritten · 2 skipped · 3 failed",
                LocalizationFormatter.FormatProcessingSummary(8, 1, 2, 3));
        });
    }

    [Fact]
    public void GermanResourcesAreUsedThroughNormalCultureFallback()
    {
        RunWithCulture("de-AT", () =>
        {
            Assert.Equal("Quellordner", Strings.SourceFolder_Label);
            Assert.Equal("de-DE", LocalizationSettings.CurrentLocaleName);
            Assert.Equal("Es wurden noch keine Dateien verarbeitet.", Strings.Progress_NoFiles);
            Assert.Equal("1 / 10 Datei verarbeitet", LocalizationFormatter.FormatProgress(1, 10));
            Assert.Equal("2 / 10 Dateien verarbeitet", LocalizationFormatter.FormatProgress(2, 10));
            Assert.Equal(
                "8 sortiert · 1 überschrieben · 2 übersprungen · 3 fehlgeschlagen",
                LocalizationFormatter.FormatProcessingSummary(8, 1, 2, 3));
        });
    }

    [Fact]
    public void UnsupportedCulturesFallBackToEnglish()
    {
        RunWithCulture("fr-FR", () =>
        {
            Assert.Equal("Target folder", Strings.TargetFolder_Label);
            Assert.Equal("Failed", Strings.ProcessingStatus_Failed);
            Assert.Equal("en-US", LocalizationSettings.CurrentLocaleName);
        });
    }

    private static void RunWithCulture(string cultureName, Action assertion)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            assertion();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}

/// <summary>
/// Prevents tests that temporarily change the current culture from running in parallel.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CultureSensitiveTestGroup
{
    /// <summary>Gets the shared collection name.</summary>
    public const string Name = "Culture-sensitive tests";
}
