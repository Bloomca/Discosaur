using Discosaur.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Discosaur.Models;
using Windows.UI;

namespace Discosaur.Views;

public sealed partial class Player : UserControl
{
    public MainViewModel ViewModel => App.ViewModel;
    public PlaybackViewModel Playback => App.ViewModel.PlaybackViewModel;

    private bool _isUserInteracting;

    public Player()
    {
        InitializeComponent();

        Playback.PropertyChanged += PlaybackViewModel_PropertyChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        App.PlayerViewModel.PropertyChanged += PlayerViewModel_PropertyChanged;
        App.ThemeViewModel.PropertyChanged += ThemeViewModel_PropertyChanged;

        ProgressSlider.AddHandler(PointerPressedEvent,
            new PointerEventHandler(ProgressSlider_PointerPressed), true);
        ProgressSlider.AddHandler(PointerReleasedEvent,
            new PointerEventHandler(ProgressSlider_PointerReleased), true);
        ProgressSlider.AddHandler(PointerCanceledEvent,
            new PointerEventHandler(ProgressSlider_PointerCanceled), true);

        UpdateTrackName();
        UpdateTooltips();
        UpdateVolumeVisual();
        UpdateVolumeTooltip();
        UpdateThemeColors();
    }

    private void PlaybackViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PlaybackViewModel.CurrentTrack):
                DispatcherQueue.TryEnqueue(UpdateTrackName);
                DispatcherQueue.TryEnqueue(UpdateTooltips);
                break;
            case nameof(PlaybackViewModel.RepeatMode):
                DispatcherQueue.TryEnqueue(UpdateRepeatVisual);
                break;
            case nameof(PlaybackViewModel.IsShuffleEnabled):
                DispatcherQueue.TryEnqueue(UpdateShuffleVisual);
                DispatcherQueue.TryEnqueue(UpdateTooltips);
                break;
            case nameof(PlaybackViewModel.VolumeLevel):
            case nameof(PlaybackViewModel.VolumeColorMode):
                DispatcherQueue.TryEnqueue(UpdateVolumeVisual);
                break;
            case nameof(PlaybackViewModel.ReducedVolumeLevel):
                DispatcherQueue.TryEnqueue(UpdateVolumeTooltip);
                break;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsLibraryExpanded):
                DispatcherQueue.TryEnqueue(UpdateCollapseVisual);
                break;
        }
    }

    private void PlayerViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateProgress);
    }

    private void ThemeViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ThemeViewModel.PrimaryTextColor):
            case nameof(ThemeViewModel.SecondaryTextColor):
            case nameof(ThemeViewModel.IsDynamicThemeActive):
                DispatcherQueue.TryEnqueue(UpdateThemeColors);
                break;
        }
    }

    private void UpdateThemeColors()
    {
        var theme = App.ThemeViewModel;
        if (theme.IsDynamicThemeActive)
        {
            TrackNameText.Foreground = new SolidColorBrush(theme.PrimaryTextColor);
            BandNameText.Foreground = new SolidColorBrush(theme.SecondaryTextColor);
            TimeText.Foreground = new SolidColorBrush(theme.PrimaryTextColor);
        }
        else
        {
            TrackNameText.Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseHighBrush"];
            BandNameText.Foreground = new SolidColorBrush(Color.FromArgb(255, 0x99, 0x99, 0x99));
            TimeText.Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseHighBrush"];
        }
    }

    private void UpdateTrackName()
    {
        TrackNameText.Text = Playback.CurrentTrack?.Title ?? "No track playing";
        BandNameText.Text = GenerateBandAlbumName();
    }

    private string GenerateBandAlbumName()
    {
        if (Playback.CurrentTrack == null) return string.Empty;

        var bandName = Playback.CurrentTrack.Artist;
        var albumName = Playback.CurrentTrack.AlbumTitle;

        if (bandName != null && albumName != null)
        {
            return $"{bandName}: {albumName}";
        }
        else if (bandName != null)
        {
            return bandName;
        }
        else if (albumName != null)
        {
            return albumName;
        } else
        {
            return string.Empty;
        }
    }

    private void UpdateTooltips()
    {
        if (Playback.IsShuffleEnabled)
        {
            ToolTipService.SetToolTip(PreviousButton, "Random track");
            ToolTipService.SetToolTip(NextButton, "Random track");
        }
        else
        {
            ToolTipService.SetToolTip(PreviousButton, Playback.PreviousTrackTitle ?? "Previous track");
            ToolTipService.SetToolTip(NextButton, Playback.NextTrackTitle ?? "Next track");
        }
    }

    private void UpdateRepeatVisual()
    {
        switch (Playback.RepeatMode)
        {
            case RepeatMode.Off:
                RepeatIcon.Glyph = "\uE8EE";
                RepeatIcon.Opacity = 0.4;
                break;
            case RepeatMode.Album:
                RepeatIcon.Glyph = "\uE8EE";
                RepeatIcon.Opacity = 1.0;
                break;
            case RepeatMode.Track:
                RepeatIcon.Glyph = "\uE8ED";
                RepeatIcon.Opacity = 1.0;
                break;
        }
    }

    private void UpdateShuffleVisual()
    {
        ShuffleIcon.Opacity = Playback.IsShuffleEnabled ? 1.0 : 0.4;
    }

    private void UpdateCollapseVisual()
    {
        CollapseIcon.Glyph = ViewModel.IsLibraryExpanded ? "\uE70D" : "\uE70E";
    }

    private void UpdateProgress()
    {
        var pvm = App.PlayerViewModel;

        if (!_isUserInteracting)
        {
            ProgressSlider.Value = pvm.ProgressPercent;
        }

        TimeText.Text = $"{pvm.CurrentTimeText}/{pvm.TotalTimeText}";
    }

    // --- Volume visual ---

    private void UpdateVolumeVisual()
    {
        var level = Playback.VolumeLevel;

        VolumeIcon.Glyph = level switch
        {
            >= 90 => "\uE995",
            >= 60 => "\uE994",
            >= 20 => "\uE993",
            >= 1  => "\uE992",
            _     => "\uE74F"
        };

        VolumeIcon.Foreground = Playback.VolumeColorMode switch
        {
            VolumeColorMode.Reduced => new SolidColorBrush(ColorHelper.FromArgb(255, 91, 155, 213)),
            VolumeColorMode.Manual  => new SolidColorBrush(ColorHelper.FromArgb(255, 237, 125, 49)),
            _                       => (Brush)Application.Current.Resources["SystemControlForegroundBaseHighBrush"]
        };

        UpdateVolumeTooltip();
    }

    private void UpdateVolumeTooltip()
    {
        ToolTipService.SetToolTip(VolumeButton,
            $"Left-click: change volume to {Playback.ReducedVolumeLevel}%\nRight-click: open volume menu");
    }

    // --- Slider events ---

    private void ProgressSlider_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isUserInteracting = true;
    }

    private void ProgressSlider_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isUserInteracting)
        {
            _isUserInteracting = false;
            App.PlayerViewModel.SeekToPercent(ProgressSlider.Value);
        }
    }

    private void ProgressSlider_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        _isUserInteracting = false;
    }

    // --- Transport button clicks ---

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        Playback.TogglePlayPauseCommand.Execute(null);
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        Playback.StopCommand.Execute(null);
    }

    private void Previous_Click(object sender, RoutedEventArgs e)
    {
        Playback.PlayPreviousCommand.Execute(null);
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        Playback.PlayNextCommand.Execute(null);
    }

    private void Repeat_Click(object sender, RoutedEventArgs e)
    {
        Playback.CycleRepeatModeCommand.Execute(null);
    }

    private void Shuffle_Click(object sender, RoutedEventArgs e)
    {
        Playback.ToggleShuffleCommand.Execute(null);
    }

    private void Collapse_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleLibraryExpandedCommand.Execute(null);
    }

    // --- Volume ---

    private void Volume_Click(object sender, RoutedEventArgs e)
    {
        Playback.ToggleVolume();
    }

    private void Volume_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        e.Handled = true;

        var volumeSlider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = Playback.VolumeLevel,
            Width = 200,
            Header = "Volume"
        };

        var reducedSlider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = Playback.ReducedVolumeLevel,
            Width = 200,
            Header = "Reduced level"
        };

        volumeSlider.ValueChanged += (_, args) =>
        {
            Playback.SetVolume((int)args.NewValue);
        };

        reducedSlider.ValueChanged += (_, args) =>
        {
            Playback.SetReducedVolumeLevel((int)args.NewValue);
        };

        var panel = new StackPanel { Spacing = 12, Padding = new Thickness(4) };
        panel.Children.Add(volumeSlider);
        panel.Children.Add(reducedSlider);

        var flyout = new Flyout
        {
            Content = panel,
            Placement = FlyoutPlacementMode.Bottom
        };

        flyout.ShowAt(VolumeButton);
    }
}
