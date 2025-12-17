using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace InterdisciplinairProject.Converters
{
    /// <summary>
    /// Converts IsPlaying boolean to background brush.
    /// True = white/light gradient (active), False = grey gradient (inactive)
    /// </summary>
    public class IsPlayingToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isPlaying = value is bool b && b;

            if (isPlaying)
            {
                // Active scene - white/light gradient
                var brush = new LinearGradientBrush();
                brush.StartPoint = new System.Windows.Point(0, 0);
                brush.EndPoint = new System.Windows.Point(0, 1);
                brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 255, 255), 0)); // #FFFFFF
                brush.GradientStops.Add(new GradientStop(Color.FromRgb(200, 250, 200), 1)); // rgba(205, 248, 203, 1)
                return brush;
            }
            else
            {
                // Inactive scene - very light grey gradient (subtle difference)
                var brush = new LinearGradientBrush();
                brush.StartPoint = new System.Windows.Point(0, 0);
                brush.EndPoint = new System.Windows.Point(0, 1);
                brush.GradientStops.Add(new GradientStop(Color.FromRgb(250, 250, 250), 0)); // #FAFAFA
                brush.GradientStops.Add(new GradientStop(Color.FromRgb(250, 250, 250), 0)); // rgba(225, 224, 224, 1)
                return brush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
