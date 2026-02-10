using System;
using Microsoft.UI.Xaml.Data;

namespace Discosaur.Converters;

public class SliderPercentToTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double percent)
        {
            var total = App.AudioPlayer.TotalDuration;
            var current = TimeSpan.FromSeconds(total.TotalSeconds * percent / 100);

            return current.TotalHours >= 1
                ? current.ToString(@"h\:mm\:ss")
                : current.ToString(@"m\:ss");
        }

        return "0:00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
