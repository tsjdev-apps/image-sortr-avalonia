namespace ImageSortr.Tests;

/// <summary>
/// Creates an isolated source and target directory pair for file-system tests.
/// </summary>
internal sealed class TestWorkspace : IDisposable
{
    /// <summary>Initializes a fresh temporary workspace.</summary>
    public TestWorkspace()
    {
        RootFolder = Path.Combine(Path.GetTempPath(), "ImageSortr.Tests", Guid.NewGuid().ToString("N"));
        SourceFolder = Path.Combine(RootFolder, "source");
        TargetFolder = Path.Combine(RootFolder, "target");

        _ = Directory.CreateDirectory(SourceFolder);
        _ = Directory.CreateDirectory(TargetFolder);
    }

    /// <summary>Gets the workspace root directory.</summary>
    public string RootFolder { get; }

    /// <summary>Gets the source directory used by sorting tests.</summary>
    public string SourceFolder { get; }

    /// <summary>Gets the target directory used by sorting tests.</summary>
    public string TargetFolder { get; }

    /// <summary>Creates a text-backed source file with a selected extension.</summary>
    public string CreateSourceFile(string relativePath, string content)
    {
        string filePath = Path.Combine(SourceFolder, relativePath);
        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, content);
        return filePath;
    }

    /// <summary>Creates a target file, including its containing directories.</summary>
    public string CreateTargetFile(string relativePath, string content)
    {
        string filePath = Path.Combine(TargetFolder, relativePath);
        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, content);
        return filePath;
    }

    /// <summary>Creates a target directory and returns its path.</summary>
    public string CreateTargetFolder(string name)
    {
        return Directory.CreateDirectory(Path.Combine(TargetFolder, name)).FullName;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(RootFolder))
        {
            Directory.Delete(RootFolder, recursive: true);
        }
    }
}
