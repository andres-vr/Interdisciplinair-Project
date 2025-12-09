using System;
using System.Globalization;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters
{
    // Converts a progress value (0-100) and an available width (pixels)
    // to an X position in pixels.
    public class ProgressToPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return 0d;

            double progress = 0d;
            double width = 0d;

            if (values[0] is double d0) progress = d0;
            else if (values[0] is int i0) progress = i0;
            else if (values[0] != null && double.TryParse(values[0].ToString(), out var p)) progress = p;

            if (values[1] is double d1) width = d1;
            else if (values[1] is int i1) width = i1;
            else if (values[1] != null && double.TryParse(values[1].ToString(), out var w)) width = w;

            // clamp progress between 0 and 100
            progress = Math.Max(0, Math.Min(100, progress));

            // compute pixel position
            double x = (progress / 100.0) * width;

            // ensure not NaN
            if (double.IsNaN(x) || double.IsInfinity(x)) x = 0d;

            return x;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
