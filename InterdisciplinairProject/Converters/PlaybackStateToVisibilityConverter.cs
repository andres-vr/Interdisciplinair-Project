using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Converters
{
    public class PlaybackStateToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ShowPlaybackState state && parameter is string targetStateStr)
            {
                if (Enum.TryParse<ShowPlaybackState>(targetStateStr, out var targetState))
                {
                    return state == targetState ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
