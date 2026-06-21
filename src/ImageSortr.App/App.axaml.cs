using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ImageSortr.App.ViewModels;
using ImageSortr.App.Views;
using ImageSortr.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImageSortr.App;

/// <summary>
/// Configures Image Sortr's application services and desktop window.
/// </summary>
internal sealed partial class App : Application
{
    private ServiceProvider? serviceProvider;

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            serviceProvider = ConfigureServices();
            desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
            desktop.Exit += DesktopOnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        ServiceCollection services = new();

        _ = services.AddSingleton<IExifDateReader, MetadataExtractorExifDateReader>();
        _ = services.AddSingleton<IFileTimestampProvider, FileTimestampProvider>();
        _ = services.AddSingleton<IImageDateReader, ImageDateReader>();
        _ = services.AddSingleton<DateFolderResolver>();
        _ = services.AddSingleton<IImageSortService, ImageSortService>();

        _ = services.AddSingleton<MainWindowViewModel>();
        _ = services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider(validateScopes: true);
    }

    private void DesktopOnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        serviceProvider?.Dispose();
        serviceProvider = null;
    }
}
