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
        private bool _waitForTab;
        private bool _isPlaying;

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
        /// Gets or sets a value indicating whether the timeline should pause and wait for a TAB key press before starting this scene.
        /// </summary>
        public bool WaitForTab
        {
            get => _waitForTab;
            set
            {
                if (_waitForTab != value)
                {
                    _waitForTab = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WaitForTab)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this scene is currently playing.
        /// </summary>
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
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
        /// Gets or sets the total duration in milliseconds (includes FadeIn + Hold + FadeOut).
        /// Setting this will automatically calculate HoldDurationMs.
        /// Minimum value is FadeInMs + FadeOutMs.
        /// </summary>
        public int TotalDurationMs
        {
            get => GetTotalDurationMs();
            set
            {
                int minDuration = (ShowScene?.FadeInMs ?? 0) + (ShowScene?.FadeOutMs ?? 0);
                int clampedValue = Math.Max(value, minDuration);
                int newHold = clampedValue - minDuration;
                if (_holdDurationMs != newHold)
                {
                    _holdDurationMs = newHold;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HoldDurationMs)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalDurationMs)));
                }
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

