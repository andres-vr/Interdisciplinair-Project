using System;
using System.Globalization;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters
{
    /// <summary>
    /// Converts milliseconds to seconds for display in the UI.
    /// Uses comma as decimal separator.
    /// </summary>
    public class MillisecondsToSecondsConverter : IValueConverter
    {
        private static readonly CultureInfo CommaCulture = new CultureInfo("nl-NL"); // Dutch culture uses comma

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int ms)
            {
                double seconds = ms / 1000.0;
                // Display without decimals if it's a whole number, otherwise show decimals
                if (seconds % 1 == 0)
                    return seconds.ToString("F0", CommaCulture);
                else
                    return seconds.ToString("0.##", CommaCulture);
            }

            if (value is long msLong)
            {
                double seconds = msLong / 1000.0;
                // Display without decimals if it's a whole number, otherwise show decimals
                if (seconds % 1 == 0)
                    return seconds.ToString("F0", CommaCulture);
                else
                    return seconds.ToString("0.##", CommaCulture);
            }

            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                // Try to parse with comma culture first
                if (double.TryParse(str, NumberStyles.Any, CommaCulture, out double seconds))
                {
                    return (int)Math.Round(seconds * 1000);
                }
                
                // Fallback to invariant culture (accepts period)
                if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out seconds))
                {
                    return (int)Math.Round(seconds * 1000);
                }
            }

            if (value is double secondsDouble)
            {
                return (int)Math.Round(secondsDouble * 1000);
            }

            return 0;
        }
    }
}
