using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using static Microsoft.UI.Win32Interop;

namespace Discosaur.Views;

public sealed partial class PlayList : UserControl
{
    public PlayList()
    {
        InitializeComponent();

        AlbumsControl.ItemsSource = App.ViewModel.Library;
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var windowId = GetWindowIdFromWindow(App.MainWindowHandle);
        var folderPicker = new FolderPicker(windowId);

        var folder = await folderPicker.PickSingleFolderAsync();

        if (folder != null)
        {
            App.ViewModel.AddFolderToLibrary(folder.Path);
        }
    }
}
