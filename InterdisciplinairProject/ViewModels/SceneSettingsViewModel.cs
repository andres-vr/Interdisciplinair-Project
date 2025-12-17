using CommunityToolkit.Mvvm.Input;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SceneModel = InterdisciplinairProject.Core.Models.Scene;

namespace InterdisciplinairProject.ViewModels
{
    /// <summary>
    /// ViewModel for Scene Settings Window with validation.
    /// </summary>
    public class SceneSettingsViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private static readonly CultureInfo CommaCulture = new CultureInfo("nl-NL"); // Dutch culture uses comma
        
        private readonly Window _window;
        private readonly SceneModel _sceneModel;
        private readonly Dictionary<string, List<string>> _errors = new();

        private string _name;
        private string _fadeInSeconds;
        private string _durationMs;
        private string _fadeOutSeconds;
        private string _dimmer;
        private double _dimmerSlider;
        private bool _isUpdatingDimmer; // Flag to prevent circular updates

        public SceneSettingsViewModel(Window window, SceneModel sceneModel)
        {
            _window = window;
            _sceneModel = sceneModel;

            // Initialize with current scene values (convert ms to seconds)
            _name = sceneModel.Name ?? string.Empty;
            _fadeInSeconds = ConvertMillisecondsToSeconds(sceneModel.FadeInMs);
            _durationMs = sceneModel.DurationMs.ToString();
            _fadeOutSeconds = ConvertMillisecondsToSeconds(sceneModel.FadeOutMs);
            _dimmer = sceneModel.Dimmer.ToString();
            _dimmerSlider = ConvertDimmerToSlider(sceneModel.Dimmer);

            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public SceneModel SceneModel => _sceneModel;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                    ValidateName();
                    NotifyCanExecuteChanged();
                }
            }
        }

        public string FadeInSeconds
        {
            get => _fadeInSeconds;
            set
            {
                if (_fadeInSeconds != value)
                {
                    _fadeInSeconds = value;
                    OnPropertyChanged();
                    ValidateFadeInSeconds();
                    NotifyCanExecuteChanged();
                }
            }
        }

        public string DurationMs
        {
            get => _durationMs;
            set
            {
                if (_durationMs != value)
                {
                    _durationMs = value;
                    OnPropertyChanged();
                    ValidateDurationMs();
                    NotifyCanExecuteChanged();
                }
            }
        }

        public string FadeOutSeconds
        {
            get => _fadeOutSeconds;
            set
            {
                if (_fadeOutSeconds != value)
                {
                    _fadeOutSeconds = value;
                    OnPropertyChanged();
                    ValidateFadeOutSeconds();
                    NotifyCanExecuteChanged();
                }
            }
        }

        public string Dimmer
        {
            get => _dimmer;
            set
            {
                if (_dimmer != value)
                {
                    _dimmer = value;
                    OnPropertyChanged();
                    ValidateDimmer();
                    NotifyCanExecuteChanged();
                    
                    // Update slider when textbox changes (0-255 -> 0-100)
                    if (!_isUpdatingDimmer && int.TryParse(_dimmer, out int dimmerValue))
                    {
                        _isUpdatingDimmer = true;
                        DimmerSlider = ConvertDimmerToSlider(dimmerValue);
                        _isUpdatingDimmer = false;
                    }
                }
            }
        }

        public double DimmerSlider
        {
            get => _dimmerSlider;
            set
            {
                if (Math.Abs(_dimmerSlider - value) > 0.01)
                {
                    _dimmerSlider = value;
                    OnPropertyChanged();
                    
                    // Update textbox when slider changes (0-100 -> 0-255)
                    if (!_isUpdatingDimmer)
                    {
                        _isUpdatingDimmer = true;
                        int dimmerValue = ConvertSliderToDimmer(_dimmerSlider);
                        Dimmer = dimmerValue.ToString();
                        _isUpdatingDimmer = false;
                    }
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public bool HasErrors => _errors.Count > 0;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return _errors.Values.SelectMany(e => e);
            }

            return _errors.ContainsKey(propertyName) ? _errors[propertyName] : Enumerable.Empty<string>();
        }

        private void ValidateName()
        {
            ClearErrors(nameof(Name));
            if (string.IsNullOrWhiteSpace(_name))
            {
                AddError(nameof(Name), "Name is required.");
            }
        }

        private void ValidateFadeInSeconds()
        {
            ClearErrors(nameof(FadeInSeconds));
            if (!decimal.TryParse(_fadeInSeconds, NumberStyles.Any, CommaCulture, out decimal value) || value < 0)
            {
                AddError(nameof(FadeInSeconds), "Must be a positive number.");
            }
        }

        private void ValidateDurationMs()
        {
            ClearErrors(nameof(DurationMs));
            if (!int.TryParse(_durationMs, out int value) || value < 0)
            {
                AddError(nameof(DurationMs), "Must be a positive integer.");
            }
        }

        private void ValidateFadeOutSeconds()
        {
            ClearErrors(nameof(FadeOutSeconds));
            if (!decimal.TryParse(_fadeOutSeconds, NumberStyles.Any, CommaCulture, out decimal value) || value < 0)
            {
                AddError(nameof(FadeOutSeconds), "Must be a positive number.");
            }
        }

        private void ValidateDimmer()
        {
            ClearErrors(nameof(Dimmer));
            if (!int.TryParse(_dimmer, out int value))
            {
                AddError(nameof(Dimmer), "Must be a valid integer.");
            }
            else if (value < 0 || value > 255)
            {
                AddError(nameof(Dimmer), "Must be between 0 and 255.");
            }
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
            {
                _errors[propertyName] = new List<string>();
            }

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }

        private bool CanExecuteSave()
        {
            // Validate all fields
            ValidateName();
            ValidateFadeInSeconds();
            ValidateDurationMs();
            ValidateFadeOutSeconds();
            ValidateDimmer();

            return !HasErrors;
        }

        private void ExecuteSave()
        {
            if (!CanExecuteSave())
            {
                return;
            }

            // Update the scene model (convert seconds to milliseconds)
            _sceneModel.Name = _name;
            _sceneModel.FadeInMs = ConvertSecondsToMilliseconds(_fadeInSeconds);
            _sceneModel.DurationMs = int.Parse(_durationMs);
            _sceneModel.FadeOutMs = ConvertSecondsToMilliseconds(_fadeOutSeconds);
            _sceneModel.Dimmer = int.Parse(_dimmer);

            _window.DialogResult = true;
            _window.Close();
        }

        private void ExecuteCancel()
        {
            _window.DialogResult = false;
            _window.Close();
        }

        private void NotifyCanExecuteChanged()
        {
            (SaveCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Converts dimmer value (0-255) to slider value (0-100).
        /// </summary>
        private double ConvertDimmerToSlider(int dimmerValue)
        {
            return (dimmerValue / 255.0) * 100.0;
        }

        /// <summary>
        /// Converts slider value (0-100) to dimmer value (0-255).
        /// </summary>
        private int ConvertSliderToDimmer(double sliderValue)
        {
            return (int)Math.Round((sliderValue / 100.0) * 255.0);
        }

        /// <summary>
        /// Converts milliseconds to seconds as a string with decimal precision.
        /// Uses comma as decimal separator.
        /// </summary>
        private string ConvertMillisecondsToSeconds(int milliseconds)
        {
            decimal seconds = milliseconds / 1000m;
            // Remove trailing zeros and decimal point if whole number
            string format = seconds % 1 == 0 ? "0" : "0.##";
            return seconds.ToString(format, CommaCulture);
        }

        /// <summary>
        /// Converts seconds (as string) to milliseconds.
        /// Parses using comma as decimal separator.
        /// </summary>
        private int ConvertSecondsToMilliseconds(string secondsStr)
        {
            if (decimal.TryParse(secondsStr, NumberStyles.Any, CommaCulture, out decimal seconds))
            {
                return (int)Math.Round(seconds * 1000m);
            }
            return 0;
        }
    }
}
