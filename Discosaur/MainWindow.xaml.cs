using Discosaur.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Discosaur;

public sealed partial class MainWindow : Window
{
    private const int DefaultWidth = 580;
    private const int ExpandedHeight = 1200;
    private const int CollapsedHeight = 770;

    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new SizeInt32(DefaultWidth, ExpandedHeight));

        App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsLibraryExpanded):
                DispatcherQueue.TryEnqueue(UpdateLibraryVisibility);
                break;
            case nameof(MainViewModel.IsAlwaysOnTop):
                DispatcherQueue.TryEnqueue(UpdateAlwaysOnTop);
                break;
        }
    }

    private void UpdateLibraryVisibility()
    {
        if (App.ViewModel.IsLibraryExpanded)
        {
            PlayListRow.Height = new GridLength(1, GridUnitType.Star);
            PlayListView.Visibility = Visibility.Visible;
            AppWindow.Resize(new SizeInt32(DefaultWidth, ExpandedHeight));
        }
        else
        {
            PlayListRow.Height = new GridLength(0);
            PlayListView.Visibility = Visibility.Collapsed;
            AppWindow.Resize(new SizeInt32(DefaultWidth, CollapsedHeight));
        }
    }

    private void UpdateAlwaysOnTop()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = App.ViewModel.IsAlwaysOnTop;
        }
    }
}
