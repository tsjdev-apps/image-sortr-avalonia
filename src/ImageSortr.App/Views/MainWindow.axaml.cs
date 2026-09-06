using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ImageSortr.App.Resources.Localization;
using ImageSortr.App.ViewModels;

namespace ImageSortr.App.Views;

/// <summary>
/// Hosts the main view and supplies Avalonia folder-picker delegates to its view model.
/// </summary>
public partial class MainWindow : Window
{
    private MainWindowViewModel? subscribedViewModel;

    /// <summary>Initializes the Avalonia view.</summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += HandleDataContextChanged;
    }

    /// <summary>Initializes the view with its application view model.</summary>
    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        DataContext = viewModel;
        viewModel.BrowseSourceFolderDelegate = () => PickFolderAsync(Strings.SourceFolder_PickerTitle);
        viewModel.BrowseTargetFolderDelegate = () => PickFolderAsync(Strings.TargetFolder_PickerTitle);
    }

    private void HandleDataContextChanged(object? sender, EventArgs e)
    {
        if (subscribedViewModel is not null)
        {
            subscribedViewModel.ProcessingHistory.CollectionChanged -= HandleProcessingHistoryChanged;
        }

        subscribedViewModel = DataContext as MainWindowViewModel;
        if (subscribedViewModel is not null)
        {
            subscribedViewModel.ProcessingHistory.CollectionChanged += HandleProcessingHistoryChanged;
        }
    }

    private void HandleProcessingHistoryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add
            || subscribedViewModel is not { IsRunning: true }
            || subscribedViewModel.ProcessingHistory.Count == 0)
        {
            return;
        }

        object newestEntry = subscribedViewModel.ProcessingHistory[^1];
        Dispatcher.UIThread.Post(
            () => ProcessingHistoryList.ScrollIntoView(newestEntry),
            DispatcherPriority.Background);
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
