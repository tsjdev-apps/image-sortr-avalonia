namespace ImageSortr.Core.Models;

/// <summary>
/// Describes the directory selected for an image date.
/// </summary>
public sealed record DateFolderResolution(
    string FolderPath,
    string DateFolderPath,
    bool UsesExistingFolder);
