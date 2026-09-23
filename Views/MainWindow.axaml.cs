using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Ps4PkgSender.ViewModels;

namespace Ps4PkgSender.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnAddPKGClicked(object? sender, RoutedEventArgs args)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null || topLevel.StorageProvider == null)
            return;

        var pkgPaths = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select PKGs",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("PKG Files")
                {
                    Patterns = ["*.pkg"]
                }
            ]
        });

        if (this.DataContext is MainViewModel viewModel)
            foreach (var pkgPath in pkgPaths)
            {
                if (pkgPath.TryGetLocalPath() is { } path)
                    viewModel.AddPKG(path);
            }
    }
}