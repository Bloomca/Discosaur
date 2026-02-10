using System;
using Discosaur.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Discosaur.Views;

public sealed partial class Artwork : UserControl
{
    public Artwork()
    {
        InitializeComponent();
        App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        UpdateCoverArt();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.CurrentAlbumCoverArtPath):
                DispatcherQueue.TryEnqueue(UpdateCoverArt);
                break;
            case nameof(MainViewModel.IsAlwaysOnTop):
                DispatcherQueue.TryEnqueue(UpdatePinVisual);
                break;
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

    private void UpdatePinVisual()
    {
        PinIcon.Glyph = App.ViewModel.IsAlwaysOnTop ? "\uE841" : "\uE718";
    }

    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        App.ViewModel.ToggleAlwaysOnTopCommand.Execute(null);
    }
}
