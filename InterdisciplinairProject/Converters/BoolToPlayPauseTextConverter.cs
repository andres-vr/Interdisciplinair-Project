using System;
using System.Globalization;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters
{
    /// <summary>
    /// Converts boolean IsPlaying state to appropriate play/pause text label.
    /// Returns "Pause" when playing, "Play" when paused.
    /// </summary>
    public class BoolToPlayPauseTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPlaying)
            {
                return isPlaying ? "Pause" : "Play";
            }
            return "Play";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
