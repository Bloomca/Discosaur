using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using static Microsoft.UI.Win32Interop;

namespace Discosaur.Views;

public sealed partial class PlayList : UserControl
{
    public PlayList()
    {
        InitializeComponent();

        AlbumsControl.ItemsSource = App.ViewModel.Library;
    }

    private void PlayList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var vm = App.ViewModel;

        switch (e.Key)
        {
            case VirtualKey.Down:
                vm.SelectionViewModel.SelectNextTrack();
                e.Handled = true;
                break;

            case VirtualKey.Up:
                vm.SelectionViewModel.SelectPreviousTrack();
                e.Handled = true;
                break;

            case VirtualKey.End:
                vm.SelectionViewModel.SelectFirstTrackOfNextAlbum();
                e.Handled = true;
                break;

            case VirtualKey.Home:
                vm.SelectionViewModel.SelectFirstTrackOfPreviousAlbum();
                e.Handled = true;
                break;

            case VirtualKey.Enter:
                vm.PlaySelectedTrackCommand.Execute(null);
                e.Handled = true;
                break;

            case VirtualKey.Delete:
                vm.DeleteSelectedTracksCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var windowId = GetWindowIdFromWindow(App.MainWindowHandle);
        var folderPicker = new FolderPicker(windowId);

        var folder = await folderPicker.PickSingleFolderAsync();

        if (folder != null)
        {
            var storageFolder = await StorageFolder.GetFolderFromPathAsync(folder.Path);
            var token = StorageApplicationPermissions.FutureAccessList.Add(storageFolder);
            App.ViewModel.AddFolderToLibrary(folder.Path, token);
        }
    }
}
