using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Discosaur.Views;

public sealed partial class Player : UserControl
{
    private const string PlayGlyph = "\uE768";
    private const string PauseGlyph = "\uE769";

    public Player()
    {
        InitializeComponent();

        App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        UpdateUI();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(App.ViewModel.CurrentTrack) or nameof(App.ViewModel.IsPlaying))
        {
            DispatcherQueue.TryEnqueue(UpdateUI);
        }
    }

    private void UpdateUI()
    {
        var vm = App.ViewModel;

        TrackNameText.Text = vm.CurrentTrack?.FileName ?? "No track playing";
        PlayPauseIcon.Glyph = vm.IsPlaying ? PauseGlyph : PlayGlyph;
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        App.ViewModel.TogglePlayPauseCommand.Execute(null);
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        App.ViewModel.StopCommand.Execute(null);
    }
}
