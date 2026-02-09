using System;
using Discosaur.Services;
using Discosaur.ViewModels;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Discosaur
{
    public partial class App : Application
    {
        // Singleton instances created once at startup
        public static Window MainWindow { get; private set; } = null!;
        public static IntPtr MainWindowHandle => WindowNative.GetWindowHandle(MainWindow);

        public static MainViewModel ViewModel { get; private set; } = null!;
        public static AudioPlayerService AudioPlayer { get; private set; } = null!;
        public static LibraryService LibraryService { get; private set; } = null!;

        private Window? _window;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Create singleton services and ViewModel once at startup
            AudioPlayer = new AudioPlayerService();
            LibraryService = new LibraryService();
            ViewModel = new MainViewModel(AudioPlayer, LibraryService);

            _window = new MainWindow();
            MainWindow = _window;
            _window.Activate();
        }
    }
}
