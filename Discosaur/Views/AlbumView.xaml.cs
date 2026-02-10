using Discosaur.Models;
using Discosaur.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Discosaur.Views;

public sealed partial class AlbumView : UserControl
{
    private static readonly Brush SelectedBrush = new SolidColorBrush(Colors.CornflowerBlue) { Opacity = 0.3 };
    private static readonly Brush TransparentBrush = new SolidColorBrush(Colors.Transparent);
    private static Brush? _playingBrush;
    private static Brush? _selectedAndPlayingBrush;

    public static readonly DependencyProperty AlbumProperty =
        DependencyProperty.Register(nameof(Album), typeof(Album), typeof(AlbumView), new PropertyMetadata(null));

    public Album Album
    {
        get => (Album)GetValue(AlbumProperty);
        set => SetValue(AlbumProperty, value);
    }

    public AlbumView()
    {
        InitializeComponent();
        InitAccentBrushes();
        App.ViewModel.SelectedTracks.CollectionChanged += (_, _) => UpdateSelectionVisuals();
        App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        UpdateSelectionVisuals();
    }

    private static void InitAccentBrushes()
    {
        if (_playingBrush != null) return;

        Color accent;
        if (Application.Current.Resources.TryGetValue("SystemAccentColorLight2", out var res) && res is Color c)
        {
            accent = c;
        }
        else
        {
            accent = Color.FromArgb(255, 0, 120, 212);
        }

        _playingBrush = new SolidColorBrush(accent) { Opacity = 0.25 };
        _selectedAndPlayingBrush = new SolidColorBrush(accent) { Opacity = 0.45 };
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentTrack))
        {
            DispatcherQueue.TryEnqueue(UpdateSelectionVisuals);
        }
    }

    private void UpdateSelectionVisuals()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var selectedTracks = App.ViewModel.SelectedTracks;
            var currentTrack = App.ViewModel.CurrentTrack;

            for (int i = 0; i < TracksControl.Items.Count; i++)
            {
                var container = TracksControl.ContainerFromIndex(i) as ContentPresenter;
                if (container == null) continue;

                var border = FindChild<Border>(container);
                if (border == null) continue;

                if (TracksControl.Items[i] is not Track track) continue;

                bool isSelected = selectedTracks.Contains(track);
                bool isPlaying = track == currentTrack;

                if (isSelected && isPlaying)
                    border.Background = _selectedAndPlayingBrush;
                else if (isPlaying)
                    border.Background = _playingBrush;
                else if (isSelected)
                    border.Background = SelectedBrush;
                else
                    border.Background = TransparentBrush;
            }
        });
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;

            var descendant = FindChild<T>(child);
            if (descendant != null)
                return descendant;
        }
        return null;
    }

    private void Track_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: Track track })
        {
            App.ViewModel.SelectTrack(track);
        }
    }

    private void Track_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: Track track })
        {
            App.ViewModel.PlayTrackCommand.Execute(track);
        }
    }
}
