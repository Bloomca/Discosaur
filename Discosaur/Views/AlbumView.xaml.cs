using Discosaur.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
}
