using System;
using Discosaur.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Discosaur.Views;

public sealed partial class Artwork : UserControl
{
    public Artwork()
    {
        InitializeComponent();
        App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentAlbumCoverArtPath))
        {
            DispatcherQueue.TryEnqueue(UpdateCoverArt);
        }
    }

    private void UpdateCoverArt()
    {
        var path = App.ViewModel.CurrentAlbumCoverArtPath;

        if (!string.IsNullOrEmpty(path))
            CoverArtImage.Source = new BitmapImage(new Uri(path));
        else
            CoverArtImage.Source = null;
    }
}
