using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Discosaur;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // this will need to be revised a bit; at first the library
        // should be visible and later collapsed by pressing a button
        AppWindow.Resize(new SizeInt32(540, 800));
    }
}
