using System;
using Discosaur.Services;
using Discosaur.ViewModels;
using Microsoft.UI.Dispatching;
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
        public static PlayerViewModel PlayerViewModel { get; private set; } = null!;
        public static AudioPlayerService AudioPlayer { get; private set; } = null!;
        public static LibraryService LibraryService { get; private set; } = null!;
        public static StatePersisterService StatePersister { get; private set; } = null!;

        private Window? _window;

        public App()
        {
            InitializeComponent();
            UnhandledException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"UNHANDLED: {e.Exception}");
                e.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"APPDOMAIN: {e.ExceptionObject}");
            };
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"TASK: {e.Exception}");
            };
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Create singleton services and ViewModel once at startup
            AudioPlayer = new AudioPlayerService();
            LibraryService = new LibraryService();
            ViewModel = new MainViewModel(AudioPlayer, LibraryService);
            PlayerViewModel = new PlayerViewModel(AudioPlayer, DispatcherQueue.GetForCurrentThread());

            StatePersister = new StatePersisterService();
            RestoreState();

            _window = new MainWindow();
            MainWindow = _window;
            _window.ExtendsContentIntoTitleBar = true;
            _window.Activate();
        }

        private async void RestoreState()
        {
            await StatePersister.LoadAndRestoreAsync();
            StatePersister.SetReady();
        }
    }
}
