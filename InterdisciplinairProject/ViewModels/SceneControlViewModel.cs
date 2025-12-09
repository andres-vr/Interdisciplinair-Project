using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.ViewModels;
using InterdisciplinairProject.Core.Models;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using InterdisciplinairProject.Views;

public partial class SceneControlViewModel : ObservableObject
{
    private readonly Scene? _sceneModel;
    private readonly ShowbuilderViewModel? _parentShowVm;

    // cancellation for play animation
    private CancellationTokenSource? _playCts;

    // suppress pushing changes back to parent when they originate from the model (e.g. fades)
    private bool _suppressParentUpdate;

    public SceneControlViewModel(Scene? scene = null, ShowbuilderViewModel? parentShowVm = null)
    {
        _sceneModel = scene;
        _parentShowVm = parentShowVm;

        if (_sceneModel != null)
        {
            _dimmer = (_sceneModel.Dimmer / 255.0) * 100.0;
            _fadeInMs = _sceneModel.FadeInMs;
            _fadeOutMs = _sceneModel.FadeOutMs;
            _isActive = _dimmer > 0;

            _sceneModel.PropertyChanged += SceneModel_PropertyChanged;
        }
    }

    private void SceneModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_sceneModel == null) return;

        if (e.PropertyName == nameof(Scene.Dimmer))
        {
            // Mark suppression so OnDimmerChanged doesn't call UpdateSceneDimmer
            _suppressParentUpdate = true;
            try
            {
                double percent = (_sceneModel.Dimmer / 255.0) * 100.0;
                if (Math.Abs(Dimmer - percent) > 0.01)
                    Dimmer = percent;
            }
            finally
            {
                _suppressParentUpdate = false;
            }
        }
        else if (e.PropertyName == nameof(Scene.FadeInMs))
        {
            if (FadeInMs != _sceneModel.FadeInMs)
                FadeInMs = _sceneModel.FadeInMs;
        }
        else if (e.PropertyName == nameof(Scene.FadeOutMs))
        {
            if (FadeOutMs != _sceneModel.FadeOutMs)
                FadeOutMs = _sceneModel.FadeOutMs;
        }
    }

    public Scene? SceneModel => _sceneModel;
    public string? Id => _sceneModel?.Id;

    [ObservableProperty]
    private double _dimmer;

    partial void OnDimmerChanged(double value)
    {
        IsActive = value > 0.0;

        // If the change came from the model (fade), do not call UpdateSceneDimmer.
        if (_suppressParentUpdate)
            return;

        // Persist when slider moved manually (UpdateSceneDimmer handles zeroing other scenes and fixture updates).
        // Since 'value' is 0-100 slider value, convert to 0-255 for model.
        int dimmerByte = (int)Math.Round((value / 100.0) * 255.0);

        if (_sceneModel != null && _parentShowVm != null)
        {
            _parentShowVm.UpdateSceneDimmer(_sceneModel, dimmerByte);
        }
        else if (_sceneModel != null)
        {
            _sceneModel.Dimmer = dimmerByte;
        }
    }

    [ObservableProperty]
    private bool _isActive;

    partial void OnIsActiveChanged(bool value)
    {
        if (!value)
        {
            Dimmer = 0;
        }
    }

    [ObservableProperty]
    private int _fadeInMs;

    partial void OnFadeInMsChanged(int value)
    {
        if (_sceneModel != null)
            _sceneModel.FadeInMs = value;
    }

    [ObservableProperty]
    private int _fadeOutMs;

    partial void OnFadeOutMsChanged(int value)
    {
        if (_sceneModel != null)
            _sceneModel.FadeOutMs = value;
    }

    [RelayCommand]
    private void RequestToggle()
    {
        if (IsActive)
        {
            Dimmer = 0;
            IsActive = false;
        }
        else
        {
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (_sceneModel == null)
            return;

        var window = new SceneSettingsWindow(_sceneModel)
        {
            Owner = Application.Current?.MainWindow
        };

        window.ShowDialog();
    }

    // Play command: always fade to 100% over the configured FadeInMs.
    [RelayCommand]
    private async Task PlayAsync()
    {
        if (_sceneModel == null)
            return;

        // ensure fixtures list is visible immediately
        if (_parentShowVm != null && _sceneModel != null)
        {
            _parentShowVm.SelectedScene = _sceneModel;
        }

        // If we have a parent show VM, ask it to orchestrate fade-out of others
        if (_parentShowVm != null)
        {
            try
            {
                // Fade to 100% (255 byte value)
                await _parentShowVm.FadeToAndActivateAsync(_sceneModel, 255);
            }
            catch (OperationCanceledException) { }
            return;
        }

        // fallback: animate locally
        // cancel any running animation
        _playCts?.Cancel();
        _playCts?.Dispose();
        _playCts = new CancellationTokenSource();
        var ct = _playCts.Token;

        double targetPercent = 100.0;
        int targetByte = 255;
        int duration = Math.Max(0, _sceneModel.FadeInMs);

        try
        {
            await AnimateToAsync(targetByte, duration, ct);
        }
        catch (OperationCanceledException)
        {
            // canceled - no further action
        }
    }

    // animate Dimmer to target (0-255) over duration (ms). 
    private async Task AnimateToAsync(int targetByte, int durationMs, CancellationToken ct)
    {
        if (_sceneModel == null)
            return;

        // immediate set if duration is 0 or negative
        if (durationMs <= 0)
        {
            if (_parentShowVm != null)
                _parentShowVm.UpdateSceneDimmer(_sceneModel, targetByte);
            else
                _sceneModel.Dimmer = targetByte;

            return;
        }

        const int intervalMs = 20;
        int steps = Math.Max(1, durationMs / intervalMs);
        
        // Start from current model value (0-255)
        double start = _sceneModel.Dimmer; 
        double delta = (targetByte - start) / steps;

        for (int i = 1; i <= steps; i++)
        {
            ct.ThrowIfCancellationRequested();

            double next = start + delta * i;
            int nextVal = (int)Math.Round(Math.Max(0, Math.Min(255, next)));

            // Use parent VM method so fixtures are updated and other scenes are turned off.
            if (_parentShowVm != null)
            {
                _parentShowVm.UpdateSceneDimmer(_sceneModel, nextVal);
                // SceneModel_PropertyChanged will pick up the model change and update this VM's Dimmer property (percentage).
            }
            else
            {
                // fallback: update model (will trigger property change)
                _sceneModel.Dimmer = nextVal;
                // Manually update local Dimmer property to percentage for UI
                Dimmer = (nextVal / 255.0) * 100.0;
            }

            await Task.Delay(intervalMs, ct);
        }

        // ensure exact target at end
        if (_parentShowVm != null)
            _parentShowVm.UpdateSceneDimmer(_sceneModel, targetByte);
        else
            _sceneModel.Dimmer = targetByte;
    }
}
