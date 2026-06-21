using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ImageSortr.App.ViewModels;

namespace ImageSortr.App.Views;

/// <summary>
/// Hosts the main view and supplies Avalonia folder-picker delegates to its view model.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Initializes the Avalonia view.</summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Initializes the view with its application view model.</summary>
    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        DataContext = viewModel;
        viewModel.BrowseSourceFolderDelegate = () => PickFolderAsync("Choose the source folder");
        viewModel.BrowseTargetFolderDelegate = () => PickFolderAsync("Choose the target folder");
    }

    private async Task<string?> PickFolderAsync(string title)
    {
        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                Title = title
            });

        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }
}
