using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static Microsoft.UI.Win32Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Discosaur.Views
{
    public sealed partial class Player : UserControl
    {
        public Player()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var windowId = Win32Interop.GetWindowIdFromWindow(App.MainWindowHandle);
            var filePicker = new FileOpenPicker(windowId);

            var file = await filePicker.PickSingleFileAsync();

            if (file != null)
            {
                try
                {
                    using var reader = new MediaFoundationReader(file.Path);
                    using var waveOut = new WaveOutEvent();

                    waveOut.Init(reader);
                    waveOut.Play();

                    while (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(200);
                    }
                } catch {
                    Console.WriteLine("oh no");
                }
                
            }
            
            
        }
    }
}
