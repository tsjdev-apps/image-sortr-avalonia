using System.Collections.Frozen;
using System.Text;

namespace ImageSortr.Core.Services;

/// <summary>
/// Produces folder-name fragments that are safe across Windows, macOS, and Linux.
/// </summary>
public static class FolderNameSanitizer
{
    private static readonly FrozenSet<char> CrossPlatformInvalidCharacters =
        new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' }
            .Concat(Path.GetInvalidFileNameChars())
            .ToFrozenSet();

    /// <summary>
    /// Removes invalid filename characters and Windows-incompatible trailing characters from a custom folder name.
    /// </summary>
    public static string Sanitize(string? folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return string.Empty;
        }

        StringBuilder builder = new(folderName.Length);
        foreach (char character in folderName.Trim())
        {
            if (!char.IsControl(character) && !CrossPlatformInvalidCharacters.Contains(character))
            {
                _ = builder.Append(character);
            }
        }

        return builder.ToString().Trim().TrimEnd('.', ' ').Trim();
    }
}
