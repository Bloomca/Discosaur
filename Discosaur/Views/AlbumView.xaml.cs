using Discosaur.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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
    }

    private void Track_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: Track track })
        {
            App.ViewModel.PlayTrackCommand.Execute(track);
        }
    }
}
