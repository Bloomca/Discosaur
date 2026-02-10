using System;
using Microsoft.UI.Xaml.Data;

namespace Discosaur.Converters;

public class BoolToPlayPauseGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? "\uE769" : "\uE768"; // Pause : Play
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
