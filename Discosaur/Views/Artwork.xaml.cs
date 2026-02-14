using System;
using Discosaur.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Discosaur.Views;

public sealed partial class Artwork : UserControl
{
    public Artwork()
    {
        InitializeComponent();
        App.ViewModel.PlaybackViewModel.PropertyChanged += PlaybackViewModel_PropertyChanged;
        App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        App.ThemeViewModel.PropertyChanged += ThemeViewModel_PropertyChanged;
        UpdateCoverArt();
    }

    private void PlaybackViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlaybackViewModel.CurrentAlbumCoverArtPath))
        {
            DispatcherQueue.TryEnqueue(UpdateCoverArt);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsAlwaysOnTop))
        {
            DispatcherQueue.TryEnqueue(UpdatePinVisual);
        }
    }

    private void UpdateCoverArt()
    {
        var path = App.ViewModel.PlaybackViewModel.CurrentAlbumCoverArtPath;

        if (!string.IsNullOrEmpty(path))
            CoverArtImage.Source = new BitmapImage(new Uri(path));
        else
            CoverArtImage.Source = null;
    }

    private void UpdatePinVisual()
    {
        PinIcon.Glyph = App.ViewModel.IsAlwaysOnTop ? "\uE841" : "\uE718";
    }

    private void ThemeViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ThemeViewModel.PrimaryTextColor) or nameof(ThemeViewModel.IsDynamicThemeActive))
        {
            DispatcherQueue.TryEnqueue(UpdatePinColor);
        }
    }

    private void UpdatePinColor()
    {
        var theme = App.ThemeViewModel;
        if (theme.IsDynamicThemeActive)
            PinIcon.Foreground = new SolidColorBrush(theme.PrimaryTextColor);
        else
            PinIcon.Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseHighBrush"];
    }

    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        App.ViewModel.ToggleAlwaysOnTopCommand.Execute(null);
    }
}
