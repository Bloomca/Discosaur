using Discosaur.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Discosaur.Views;

public sealed partial class AlbumView : UserControl
{
    private static readonly Brush SelectedBrush = new SolidColorBrush(Colors.CornflowerBlue) { Opacity = 0.3 };
    private static readonly Brush TransparentBrush = new SolidColorBrush(Colors.Transparent);

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
        App.ViewModel.SelectedTracks.CollectionChanged += (_, _) => UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var selectedTracks = App.ViewModel.SelectedTracks;

            for (int i = 0; i < TracksControl.Items.Count; i++)
            {
                var container = TracksControl.ContainerFromIndex(i) as ContentPresenter;
                if (container == null) continue;

                var border = FindChild<Border>(container);
                if (border == null) continue;

                if (TracksControl.Items[i] is not Track track) continue;
                border.Background = selectedTracks.Contains(track) ? SelectedBrush : TransparentBrush;
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
