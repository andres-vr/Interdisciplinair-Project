using System;
using System.Globalization;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters
{
    /// <summary>
    /// Converts scene duration (fadeIn + duration + fadeOut) to width in pixels.
    /// Uses a scale factor where 1 millisecond = configurable pixels (default 0.05px).
    /// </summary>
    public class SceneDurationToWidthConverter : IMultiValueConverter
    {
        // Scale factor: pixels per millisecond (default: 50px per second = 0.05px/ms)
        private const double PixelsPerMillisecond = 0.05;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Expected values: [0] = fadeInMs, [1] = durationMs, [2] = fadeOutMs, [3] = availableWidth (optional)
            if (values == null || values.Length < 3)
                return 100.0; // default width

            int fadeInMs = 0;
            int durationMs = 0;
            int fadeOutMs = 0;

            // Parse fadeIn
            if (values[0] is int fi)
                fadeInMs = fi;
            else if (values[0] != null && int.TryParse(values[0].ToString(), out var fip))
                fadeInMs = fip;

            // Parse duration
            if (values[1] is int dur)
                durationMs = dur;
            else if (values[1] != null && int.TryParse(values[1].ToString(), out var durp))
                durationMs = durp;

            // Parse fadeOut
            if (values[2] is int fo)
                fadeOutMs = fo;
            else if (values[2] != null && int.TryParse(values[2].ToString(), out var fop))
                fadeOutMs = fop;

            // Calculate total duration in milliseconds
            int totalDurationMs = fadeInMs + durationMs + fadeOutMs;

            // Convert to width (minimum 50px for visibility)
            double width = Math.Max(50.0, totalDurationMs * PixelsPerMillisecond);

            return width;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}