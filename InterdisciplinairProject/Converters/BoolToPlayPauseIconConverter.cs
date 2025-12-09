using System;
using System.Globalization;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters
{
    /// <summary>
    /// Converts boolean IsPlaying state to appropriate play/pause icon.
    /// Returns "⏸" (pause) when playing, "▶" (play) when paused.
    /// </summary>
    public class BoolToPlayPauseIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPlaying)
            {
                return isPlaying ? "⏸" : "▶";
            }
            return "▶";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
