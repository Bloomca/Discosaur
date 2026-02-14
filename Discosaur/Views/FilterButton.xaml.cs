using System.ComponentModel;
using System.Linq;
using Discosaur.Models;
using Discosaur.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Discosaur.Views;

public sealed partial class FilterButton : UserControl
{
    public FilterButton()
    {
        InitializeComponent();

        UpdateVisual();

        App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        App.ThemeViewModel.PropertyChanged += ThemeViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsFilteringApplied))
            DispatcherQueue.TryEnqueue(UpdateVisual);
    }

    private void ThemeViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ThemeViewModel.PrimaryTextColor)
            or nameof(ThemeViewModel.IsDynamicThemeActive)
            or nameof(ThemeViewModel.FilterActiveColor))
        {
            DispatcherQueue.TryEnqueue(UpdateVisual);
        }
    }

    private void UpdateVisual()
    {
        var theme = App.ThemeViewModel;
        if (App.ViewModel.IsFilteringApplied)
        {
            if (theme.IsDynamicThemeActive)
                FilterBtn.Foreground = new SolidColorBrush(theme.FilterActiveColor);
            else
                FilterBtn.Foreground = new SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 0, 120, 212));
        }
        else
        {
            if (theme.IsDynamicThemeActive)
                FilterBtn.Foreground = new SolidColorBrush(theme.PrimaryTextColor);
            else
                FilterBtn.ClearValue(Control.ForegroundProperty);
        }
    }

    private void FilterBtn_Click(object sender, RoutedEventArgs e)
    {
        var vm = App.ViewModel;
        var existingConfig = vm.FilterConfiguration ?? new FilterConfiguration();

        // Album name search
        var albumSearchBox = new TextBox
        {
            Header = "Album name",
            PlaceholderText = "Search albums...",
            Text = existingConfig.AlbumNameSearch ?? "",
        };

        // Song name search
        var songSearchBox = new TextBox
        {
            Header = "Song name",
            PlaceholderText = "Search songs...",
            Text = existingConfig.SongNameSearch ?? "",
        };

        // Year range
        var yearFromBox = new NumberBox
        {
            Header = "Year from",
            PlaceholderText = "2000",
            Value = existingConfig.YearFrom.HasValue ? existingConfig.YearFrom.Value : double.NaN,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Width = 110,
        };
        var yearToBox = new NumberBox
        {
            Header = "Year to",
            PlaceholderText = "2010",
            Value = existingConfig.YearTo.HasValue ? existingConfig.YearTo.Value : double.NaN,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
            Width = 110,
        };
        var yearPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20 };
        yearPanel.Children.Add(yearFromBox);
        yearPanel.Children.Add(yearToBox);

        // Specific albums multi-select
        var allAlbumNames = vm.Library.Select(a => a.Name).Distinct().ToList();
        var albumListView = new ListView
        {
            SelectionMode = ListViewSelectionMode.Multiple,
            ItemsSource = allAlbumNames,
        };
        var hasAlbumSelections = existingConfig.SelectedAlbumNames is { Count: > 0 };
        if (hasAlbumSelections)
        {
            foreach (var name in allAlbumNames)
            {
                if (existingConfig.SelectedAlbumNames!.Contains(name))
                    albumListView.SelectedItems.Add(name);
            }
        }
        var albumExpander = new Expander
        {
            Header = "Albums",
            Content = albumListView,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            IsExpanded = hasAlbumSelections,
        };

        // Genres multi-select
        var allGenres = vm.Library
            .SelectMany(a => a.Tracks)
            .Where(t => !string.IsNullOrEmpty(t.Genre))
            .Select(t => t.Genre!)
            .Distinct()
            .OrderBy(g => g)
            .ToList();
        var genreListView = new ListView
        {
            SelectionMode = ListViewSelectionMode.Multiple,
            ItemsSource = allGenres,
        };
        var hasGenreSelections = existingConfig.SelectedGenres is { Count: > 0 };
        if (hasGenreSelections)
        {
            foreach (var genre in allGenres)
            {
                if (existingConfig.SelectedGenres!.Contains(genre))
                    genreListView.SelectedItems.Add(genre);
            }
        }
        var genreExpander = new Expander
        {
            Header = "Genres",
            Content = genreListView,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            IsExpanded = hasGenreSelections,
        };

        // Buttons
        var resetButton = new Button
        {
            Content = "Reset",
            Visibility = vm.IsFilteringApplied ? Visibility.Visible : Visibility.Collapsed,
        };
        var cancelButton = new Button { Content = "Cancel" };
        var applyButton = new Button
        {
            Content = "Apply",
            Style = (Style)Application.Current.Resources["AccentButtonStyle"],
        };

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 12, 0, 0)
        };
        buttonsPanel.Children.Add(resetButton);
        buttonsPanel.Children.Add(cancelButton);
        buttonsPanel.Children.Add(applyButton);

        // Assemble content
        var contentPanel = new StackPanel { Spacing = 12, Width = 300, Margin = new Thickness(0) };
        contentPanel.Children.Add(albumSearchBox);
        contentPanel.Children.Add(songSearchBox);
        contentPanel.Children.Add(yearPanel);
        if (allAlbumNames.Count > 0)
            contentPanel.Children.Add(albumExpander);
        if (allGenres.Count > 0)
            contentPanel.Children.Add(genreExpander);
        contentPanel.Children.Add(buttonsPanel);

        var border = new Border
        {
            Child = contentPanel,
            Padding = new Thickness(8),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 128, 128, 128)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Width = 280
        };

        var flyout = new Flyout
        {
            Content = border,
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom,
        };

        cancelButton.Click += (_, _) => flyout.Hide();

        resetButton.Click += (_, _) =>
        {
            vm.ClearFilter();
            flyout.Hide();
        };

        applyButton.Click += (_, _) =>
        {
            var config = new FilterConfiguration();

            if (!string.IsNullOrWhiteSpace(albumSearchBox.Text))
                config.AlbumNameSearch = albumSearchBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(songSearchBox.Text))
                config.SongNameSearch = songSearchBox.Text.Trim();

            if (!double.IsNaN(yearFromBox.Value))
                config.YearFrom = (uint)yearFromBox.Value;

            if (!double.IsNaN(yearToBox.Value))
                config.YearTo = (uint)yearToBox.Value;

            var selectedAlbums = albumListView.SelectedItems.Cast<string>().ToList();
            if (selectedAlbums.Count > 0)
                config.SelectedAlbumNames = selectedAlbums;

            var selectedGenres = genreListView.SelectedItems.Cast<string>().ToList();
            if (selectedGenres.Count > 0)
                config.SelectedGenres = selectedGenres;

            if (config.HasAnyCriteria)
                vm.ApplyFilter(config);
            else
                vm.ClearFilter();

            flyout.Hide();
        };

        flyout.ShowAt(FilterBtn);
    }
}
