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
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentTrack))
        {
            DispatcherQueue.TryEnqueue(UpdateTrackName);
        }
    }

    private void PlayerViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateProgress);
    }

    private void UpdateTrackName()
    {
        TrackNameText.Text = ViewModel.CurrentTrack?.FileName ?? "No track playing";
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
}
