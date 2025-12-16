using System;
using System.Globalization;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters
{
    public class PercentageWidthConverter : IMultiValueConverter
    {
        // Converts slider value, min, max, and container width to a proportional width
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 4 ||
                !(values[0] is double value) ||
                !(values[1] is double min) ||
                !(values[2] is double max) ||
                !(values[3] is double containerWidth))
                return 0.0;

            if (max <= min) return 0.0;

            double percent = (value - min) / (max - min);
            percent = Math.Max(0, Math.Min(1, percent));
            return percent * containerWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
