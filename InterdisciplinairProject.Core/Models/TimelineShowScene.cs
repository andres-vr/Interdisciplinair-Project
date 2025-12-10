using System.ComponentModel;

namespace InterdisciplinairProject.Core.Models
{
    public class TimelineShowScene : INotifyPropertyChanged
    {
        private int _zIndex;
        private int _duration;

        public int Id { get; set; }
        public Scene ShowScene { get; set; }

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

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
