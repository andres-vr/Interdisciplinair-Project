using System.ComponentModel;

namespace InterdisciplinairProject.Core.Models
{
    /// <summary>
    /// Represents a scene placed on the show timeline with timing information.
    /// </summary>
    public class TimelineShowScene : INotifyPropertyChanged
    {
        private int _zIndex;
        private int _duration;
        private int _holdDurationMs;

        /// <summary>
        /// Gets or sets the unique identifier for this timeline entry.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the scene associated with this timeline entry.
        /// </summary>
        public Scene ShowScene { get; set; } = null!;

        /// <summary>
        /// Gets or sets the z-index for layering.
        /// </summary>
        public int ZIndex
        {
            get => _zIndex;
            set
            {
                if (_zIndex != value)
                {
                    _zIndex = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZIndex)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the duration in milliseconds.
        /// </summary>
        public int Duration
        {
            get => _duration;
            set
            {
                if (_duration != value)
                {
                    _duration = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Duration)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the hold duration in milliseconds (time scene stays at full before fading out).
        /// </summary>
        public int HoldDurationMs
        {
            get => _holdDurationMs;
            set
            {
                if (_holdDurationMs != value)
                {
                    _holdDurationMs = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HoldDurationMs)));
                }
            }
        }

        /// <summary>
        /// Gets the total duration of this timeline scene (FadeIn + Hold + FadeOut).
        /// </summary>
        /// <returns>Total duration in milliseconds.</returns>
        public int GetTotalDurationMs()
        {
            return (ShowScene?.FadeInMs ?? 0) + HoldDurationMs + (ShowScene?.FadeOutMs ?? 0);
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

