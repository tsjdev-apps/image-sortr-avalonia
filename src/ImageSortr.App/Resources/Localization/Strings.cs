using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace ImageSortr.App.Resources.Localization;

#pragma warning disable CA1707 // Resource keys intentionally use semantic underscore-separated names.

/// <summary>
/// Provides strongly typed access to the application's localized resources.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager ResourceManager = new(
        "ImageSortr.App.Resources.Localization.Strings",
        typeof(Strings).Assembly);

    public static string Main_WindowTitle => GetString();
    public static string Main_Title => GetString();
    public static string Main_Description => GetString();
    public static string Theme_Locale => GetString();
    public static string SourceFolder_Label => GetString();
    public static string SourceFolder_Watermark => GetString();
    public static string SourceFolder_BrowseTooltip => GetString();
    public static string SourceFolder_PickerTitle => GetString();
    public static string TargetFolder_Label => GetString();
    public static string TargetFolder_Watermark => GetString();
    public static string TargetFolder_BrowseTooltip => GetString();
    public static string TargetFolder_PickerTitle => GetString();
    public static string OptionalFolderName_Label => GetString();
    public static string OptionalFolderName_Watermark => GetString();
    public static string IncludeYear_Label => GetString();
    public static string IncludeYear_DescriptionWithYear => GetString();
    public static string IncludeYear_DescriptionWithoutYear => GetString();
    public static string OverwriteFiles_Label => GetString();
    public static string OverwriteFiles_Description => GetString();
    public static string SortImages_Button => GetString();
    public static string SortingImages_Button => GetString();
    public static string Progress_Title => GetString();
    public static string Progress_ProcessedFile => GetString();
    public static string Progress_ProcessedFiles => GetString();
    public static string Progress_NoFiles => GetString();
    public static string ProcessingHistory_Title => GetString();
    public static string ProcessingStatus_Sorted => GetString();
    public static string ProcessingStatus_Skipped => GetString();
    public static string ProcessingStatus_Overwritten => GetString();
    public static string ProcessingStatus_Failed => GetString();
    public static string ProcessingStatus_Completed => GetString();
    public static string ProcessingSummary_Title => GetString();
    public static string ProcessingSummary_Counts => GetString();
    public static string ProcessingDetail_FileExists => GetString();
    public static string ProcessingDetail_CouldNotProcess => GetString();
    public static string Validation_InvalidFolders => GetString();
    public static string Validation_SourceNotFound => GetString();
    public static string Validation_TargetUnavailable => GetString();
    public static string Validation_AccessDenied => GetString();

    private static string GetString([CallerMemberName] string resourceName = "")
    {
        return ResourceManager.GetString(resourceName, CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException($"Missing localization resource '{resourceName}'.");
    }
}

#pragma warning restore CA1707
