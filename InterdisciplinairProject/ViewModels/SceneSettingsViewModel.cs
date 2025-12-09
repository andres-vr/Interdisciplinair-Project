using CommunityToolkit.Mvvm.Input;
using System.Collections;
using System.ComponentModel;
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
        private readonly Window _window;
        private readonly SceneModel _sceneModel;
        private readonly Dictionary<string, List<string>> _errors = new();

        private string _name;
        private string _fadeInMs;
        private string _durationMs;
        private string _fadeOutMs;
        private string _dimmer;
        private double _dimmerSlider;
        private bool _isUpdatingDimmer; // Flag to prevent circular updates

        public SceneSettingsViewModel(Window window, SceneModel sceneModel)
        {
            _window = window;
            _sceneModel = sceneModel;

            // Initialize with current scene values
            _name = sceneModel.Name ?? string.Empty;
            _fadeInMs = sceneModel.FadeInMs.ToString();
            _durationMs = sceneModel.DurationMs.ToString();
            _fadeOutMs = sceneModel.FadeOutMs.ToString();
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

        public string FadeInMs
        {
            get => _fadeInMs;
            set
            {
                if (_fadeInMs != value)
                {
                    _fadeInMs = value;
                    OnPropertyChanged();
                    ValidateFadeInMs();
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

        public string FadeOutMs
        {
            get => _fadeOutMs;
            set
            {
                if (_fadeOutMs != value)
                {
                    _fadeOutMs = value;
                    OnPropertyChanged();
                    ValidateFadeOutMs();
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

        private void ValidateFadeInMs()
        {
            ClearErrors(nameof(FadeInMs));
            if (!int.TryParse(_fadeInMs, out int value) || value < 0)
            {
                AddError(nameof(FadeInMs), "Must be a positive integer.");
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

        private void ValidateFadeOutMs()
        {
            ClearErrors(nameof(FadeOutMs));
            if (!int.TryParse(_fadeOutMs, out int value) || value < 0)
            {
                AddError(nameof(FadeOutMs), "Must be a positive integer.");
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
            ValidateFadeInMs();
            ValidateDurationMs();
            ValidateFadeOutMs();
            ValidateDimmer();

            return !HasErrors;
        }

        private void ExecuteSave()
        {
            if (!CanExecuteSave())
            {
                return;
            }

            // Update the scene model
            _sceneModel.Name = _name;
            _sceneModel.FadeInMs = int.Parse(_fadeInMs);
            _sceneModel.DurationMs = int.Parse(_durationMs);
            _sceneModel.FadeOutMs = int.Parse(_fadeOutMs);
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
    }
}
