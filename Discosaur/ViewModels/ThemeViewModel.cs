using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Discosaur.Helpers;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Discosaur.ViewModels;

public partial class ThemeViewModel : ObservableObject
{
    private readonly DispatcherQueue _dispatcherQueue;
    private string? _lastProcessedPath;

    [ObservableProperty]
    private Color _backgroundColor;

    [ObservableProperty]
    private Color _primaryTextColor;

    [ObservableProperty]
    private Color _secondaryTextColor;

    [ObservableProperty]
    private SolidColorBrush _selectionBrush = new(Colors.Transparent);

    [ObservableProperty]
    private SolidColorBrush _playingBrush = new(Colors.Transparent);

    [ObservableProperty]
    private SolidColorBrush _selectedAndPlayingBrush = new(Colors.Transparent);

    [ObservableProperty]
    private bool _isDynamicThemeActive;

    public ThemeViewModel(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    public async void ApplyDynamicThemeFromArtwork(string coverArtPath)
    {
        if (coverArtPath == _lastProcessedPath)
            return;

        _lastProcessedPath = coverArtPath;

        try
        {
            var dominant = await Task.Run(() =>
                ColorHelpers.GetDominantColorAsync(coverArtPath));

            if (coverArtPath != _lastProcessedPath)
                return;

            var primaryText = ColorHelpers.GetTextColor(dominant);
            var secondaryText = ColorHelpers.GetSecondaryTextColor(dominant);

            bool isDark = ColorHelpers.IsDark(dominant);
            float shift = isDark ? 0.15f : -0.15f;
            var highlightColor = ColorHelpers.ShiftLightness(dominant, shift);

            _dispatcherQueue.TryEnqueue(() =>
            {
                BackgroundColor = dominant;
                PrimaryTextColor = primaryText;
                SecondaryTextColor = secondaryText;
                SelectionBrush = new SolidColorBrush(highlightColor) { Opacity = 0.30 };
                PlayingBrush = new SolidColorBrush(highlightColor) { Opacity = 0.45 };
                SelectedAndPlayingBrush = new SolidColorBrush(highlightColor) { Opacity = 0.60 };
                IsDynamicThemeActive = true;
            });
        }
        catch
        {
            _dispatcherQueue.TryEnqueue(ResetToDefault);
        }
    }

    public void ResetToDefault()
    {
        _lastProcessedPath = null;
        IsDynamicThemeActive = false;
    }
}
