using Discosaur.Models;
using Discosaur.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Discosaur.Views;

public sealed partial class Player : UserControl
{
    public MainViewModel ViewModel => App.ViewModel;

    private bool _isUserInteracting;

    public Player()
    {
        InitializeComponent();

        App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        App.PlayerViewModel.PropertyChanged += PlayerViewModel_PropertyChanged;

        ProgressSlider.AddHandler(PointerPressedEvent,
            new PointerEventHandler(ProgressSlider_PointerPressed), true);
        ProgressSlider.AddHandler(PointerReleasedEvent,
            new PointerEventHandler(ProgressSlider_PointerReleased), true);
        ProgressSlider.AddHandler(PointerCanceledEvent,
            new PointerEventHandler(ProgressSlider_PointerCanceled), true);

        UpdateTrackName();
        UpdateTooltips();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.CurrentTrack):
                DispatcherQueue.TryEnqueue(UpdateTrackName);
                DispatcherQueue.TryEnqueue(UpdateTooltips);
                break;
            case nameof(MainViewModel.RepeatMode):
                DispatcherQueue.TryEnqueue(UpdateRepeatVisual);
                break;
            case nameof(MainViewModel.IsShuffleEnabled):
                DispatcherQueue.TryEnqueue(UpdateShuffleVisual);
                DispatcherQueue.TryEnqueue(UpdateTooltips);
                break;
            case nameof(MainViewModel.IsLibraryExpanded):
                DispatcherQueue.TryEnqueue(UpdateCollapseVisual);
                break;
        }
    }

    private void PlayerViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateProgress);
    }

    private void UpdateTrackName()
    {
        TrackNameText.Text = ViewModel.CurrentTrack?.Title ?? "No track playing";
    }

    private void UpdateTooltips()
    {
        if (ViewModel.IsShuffleEnabled)
        {
            ToolTipService.SetToolTip(PreviousButton, "Random track");
            ToolTipService.SetToolTip(NextButton, "Random track");
        }
        else
        {
            ToolTipService.SetToolTip(PreviousButton, ViewModel.PreviousTrackTitle ?? "Previous track");
            ToolTipService.SetToolTip(NextButton, ViewModel.NextTrackTitle ?? "Next track");
        }
    }

    private void UpdateRepeatVisual()
    {
        switch (ViewModel.RepeatMode)
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
        ShuffleIcon.Opacity = ViewModel.IsShuffleEnabled ? 1.0 : 0.4;
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

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.TogglePlayPauseCommand.Execute(null);
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.StopCommand.Execute(null);
    }

    private void Previous_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.PlayPreviousCommand.Execute(null);
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.PlayNextCommand.Execute(null);
    }

    private void Repeat_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CycleRepeatModeCommand.Execute(null);
    }

    private void Shuffle_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleShuffleCommand.Execute(null);
    }

    private void Collapse_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleLibraryExpandedCommand.Execute(null);
    }
}
