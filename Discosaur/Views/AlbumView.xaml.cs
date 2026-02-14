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
        App.ViewModel.SelectionViewModel.SelectedTracks.CollectionChanged += (_, _) => UpdateSelectionVisuals();
        App.ViewModel.PlaybackViewModel.PropertyChanged += PlaybackViewModel_PropertyChanged;
        App.ThemeViewModel.PropertyChanged += ThemeViewModel_PropertyChanged;

        UpdateSelectionVisuals();
        UpdateTextColors();
    }

    private void PlaybackViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlaybackViewModel.CurrentTrack))
        {
            DispatcherQueue.TryEnqueue(UpdateSelectionVisuals);
        }
    }

    private void ThemeViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ThemeViewModel.SelectionBrush):
            case nameof(ThemeViewModel.PlayingBrush):
            case nameof(ThemeViewModel.SelectedAndPlayingBrush):
            case nameof(ThemeViewModel.IsDynamicThemeActive):
                DispatcherQueue.TryEnqueue(UpdateSelectionVisuals);
                DispatcherQueue.TryEnqueue(UpdateTextColors);
                break;
            case nameof(ThemeViewModel.PrimaryTextColor):
            case nameof(ThemeViewModel.SecondaryTextColor):
                DispatcherQueue.TryEnqueue(UpdateSelectionVisuals);
                DispatcherQueue.TryEnqueue(UpdateTextColors);
                break;
        }
    }

    private void UpdateSelectionVisuals()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var theme = App.ThemeViewModel;
            var selectedTracks = App.ViewModel.SelectionViewModel.SelectedTracks;
            var currentTrack = App.ViewModel.PlaybackViewModel.CurrentTrack;

            Brush selectionBrush, playingBrush, selectedAndPlayingBrush;
            if (theme.IsDynamicThemeActive)
            {
                selectionBrush = theme.SelectionBrush;
                playingBrush = theme.PlayingBrush;
                selectedAndPlayingBrush = theme.SelectedAndPlayingBrush;
            }
            else
            {
                Color accent;
                if (Application.Current.Resources.TryGetValue("SystemAccentColorLight2", out var res) && res is Color c)
                    accent = c;
                else
                    accent = Color.FromArgb(255, 0, 120, 212);

                selectionBrush = new SolidColorBrush(Colors.CornflowerBlue) { Opacity = 0.3 };
                playingBrush = new SolidColorBrush(accent) { Opacity = 0.25 };
                selectedAndPlayingBrush = new SolidColorBrush(accent) { Opacity = 0.45 };
            }

            var transparentBrush = new SolidColorBrush(Colors.Transparent);

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
                    border.Background = selectedAndPlayingBrush;
                else if (isPlaying)
                    border.Background = playingBrush;
                else if (isSelected)
                    border.Background = selectionBrush;
                else
                    border.Background = transparentBrush;

                UpdateTrackTextColors(border, theme);
            }
        });
    }

    private static void UpdateTrackTextColors(Border border, ThemeViewModel theme)
    {
        var grid = FindChild<Grid>(border);
        if (grid == null) return;

        for (int j = 0; j < VisualTreeHelper.GetChildrenCount(grid); j++)
        {
            if (VisualTreeHelper.GetChild(grid, j) is not TextBlock tb) continue;

            if (theme.IsDynamicThemeActive)
            {
                tb.Foreground = Grid.GetColumn(tb) == 0
                    ? new SolidColorBrush(theme.SecondaryTextColor)
                    : new SolidColorBrush(theme.PrimaryTextColor);
            }
            else
            {
                tb.Foreground = Grid.GetColumn(tb) == 0
                    ? new SolidColorBrush(Color.FromArgb(255, 0x99, 0x99, 0x99))
                    : (Brush)Application.Current.Resources["SystemControlForegroundBaseHighBrush"];
            }
        }
    }

    private void UpdateTextColors()
    {
        var theme = App.ThemeViewModel;
        if (theme.IsDynamicThemeActive)
            AlbumHeaderText.Foreground = new SolidColorBrush(theme.SecondaryTextColor);
        else
            AlbumHeaderText.Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"];
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
            var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
    Windows.System.VirtualKey.Control);

            bool ctrlPressed = (ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;

            if (ctrlPressed)
            {
                App.ViewModel.SelectionViewModel.SelectExtraTrack(track);
            } else
            {
                App.ViewModel.SelectionViewModel.SelectTrack(track);
            }
        }
    }

    private void Track_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: Track track })
        {
            App.ViewModel.PlaybackViewModel.PlayTrackCommand.Execute(track);
        }
    }

    private void AlbumNameTapped(object sender, TappedRoutedEventArgs e)
    {
        App.ViewModel.SelectionViewModel.SelectAlbum(Album);
    }
}
