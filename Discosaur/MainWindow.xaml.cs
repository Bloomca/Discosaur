using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Discosaur;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AppWindow.Resize(new SizeInt32(600, 900));
    }
}
