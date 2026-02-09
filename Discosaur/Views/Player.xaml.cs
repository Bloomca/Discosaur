using Discosaur.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Discosaur.Views;

public sealed partial class Player : UserControl
{
    public MainViewModel ViewModel => App.ViewModel;

    private bool _isUpdatingSlider;

    public Player()
    {
        InitializeComponent();

        App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        App.PlayerViewModel.PropertyChanged += PlayerViewModel_PropertyChanged;
        ProgressSlider.ValueChanged += ProgressSlider_ValueChanged;

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

        _isUpdatingSlider = true;
        ProgressSlider.Value = pvm.ProgressPercent;
        _isUpdatingSlider = false;

        CurrentTimeText.Text = pvm.CurrentTimeText;
        TotalTimeText.Text = pvm.TotalTimeText;
    }

    private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_isUpdatingSlider)
        {
            App.PlayerViewModel.SeekToPercent(e.NewValue);
        }
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
