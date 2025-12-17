using System;
using System.Globalization;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters
{
    /// <summary>
    /// Converts a value by applying a scale factor.
    /// The ConverterParameter should be a double representing the scale factor (e.g., 0.9 for 90%).
    /// </summary>
    public class ScaleFactorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double actualValue && parameter is string scaleString)
            {
                if (double.TryParse(scaleString, NumberStyles.Any, CultureInfo.InvariantCulture, out double scale))
                {
                    // Apply scale factor and ensure minimum value
                    double result = actualValue * scale;
                    return Math.Max(result, 100); // Minimum height of 100
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
