using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Views;
using Show;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Timers;
using System.Windows;

using SceneModel = InterdisciplinairProject.Core.Models.Scene;

namespace InterdisciplinairProject.ViewModels
{
    public partial class ShowbuilderViewModel : ObservableObject
    {
        private InterdisciplinairProject.Core.Models.Show _show = new InterdisciplinairProject.Core.Models.Show();
        private string? _currentShowPath;

        public ObservableCollection<SceneModel> Scenes { get; } = new();
        public ObservableCollection<TimelineShowScene> TimeLineScenes { get; } = new();

        private int id = 1;

        [ObservableProperty]
        private SceneModel? selectedScene;

        [ObservableProperty]
        private SceneModel? selectedTimelineScene;

        [ObservableProperty]
        private double progressPosition;

        [ObservableProperty]
        private System.TimeSpan currentTime;

        [ObservableProperty]
        private System.TimeSpan totalTime;

        [ObservableProperty]
        private string? currentShowId;

        [ObservableProperty]
        private string? currentShowName;

        [ObservableProperty]
        private string? message;

        [ObservableProperty]
        private bool hasUnsavedChanges = false;

        // new: per-scene fade cancellation tokens
        private readonly Dictionary<SceneModel, CancellationTokenSource> _fadeCts = new();

        // Playback timer and related fields
        private System.Timers.Timer? _playbackTimer;
        private double _elapsedMs;

        [ObservableProperty]
        private bool isPlaying;


        // ============================================================
        // CREATE SHOW
        // ============================================================
        [RelayCommand]
        private void CreateShow()
        {
            // Open the create show window
            var window = new CreateShowWindow();
            var vm = (CreateShowViewModel)window.DataContext;

            bool? result = window.ShowDialog();
            if (result == true && !string.IsNullOrWhiteSpace(vm.ShowName))
            {
                // Update current show
                CurrentShowName = vm.ShowName;
                Scenes.Clear();

                _show = new InterdisciplinairProject.Core.Models.Show
                {
                    Name = vm.ShowName,
                    Scenes = new List<SceneModel>()
                };

                _currentShowPath = null;

                Message = $"Nieuwe show '{vm.ShowName}' aangemaakt!";
            }
        }

        // ============================================================
        // IMPORT SCENES
        // ============================================================
        [RelayCommand]
        private void ImportScenes()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Scene",
                Filter = "JSON files (*.json)|*.json",
                Multiselect = false,
            };

            try
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedScenePath = openFileDialog.FileName;

                    InterdisciplinairProject.Core.Models.Scene scene = SceneExtractor.ExtractScene(selectedScenePath);
                    if (!Scenes.Any(s => s.Id == scene.Id))
                    {
                        // ensure imported scene slider starts at 0
                        var showScene = new SceneModel
                        {
                            Id = scene.Id,
                            Name = scene.Name,
                            Dimmer = 0,
                            FadeInMs = scene.FadeInMs,
                            FadeOutMs = scene.FadeOutMs,
                            Fixtures = scene.Fixtures?.Select(f => new Fixture
                            {
                                InstanceId = f.InstanceId,
                                FixtureId = f.FixtureId,
                                Name = f.Name,
                                Manufacturer = f.Manufacturer,
                                Dimmer = 0,
                                // Copy the Channels collection
                                Channels = new System.Collections.ObjectModel.ObservableCollection<Channel>(
                                    f.Channels?.Select(c => new Channel
                                    {
                                        Name = c.Name,
                                        Type = c.Type,
                                        Value = c.Value,
                                        Parameter = c.Parameter,
                                        Min = c.Min,
                                        Max = c.Max,
                                        Time = c.Time,
                                        ChannelEffect = new ChannelEffect
                                        {
                                            Enabled = c.ChannelEffect?.Enabled ?? false,
                                            EffectType = c.ChannelEffect?.EffectType ?? Core.Models.Enums.EffectType.FadeIn,
                                            Time = c.ChannelEffect?.Time ?? 0,
                                            Min = c.ChannelEffect?.Min ?? 0,
                                            Max = c.ChannelEffect?.Max ?? 255,
                                            Parameters = c.ChannelEffect?.Parameters != null
                                                ? new System.Collections.Generic.Dictionary<string, object>(c.ChannelEffect.Parameters)
                                                : new System.Collections.Generic.Dictionary<string, object>()
                                        }
                                    }) ?? Enumerable.Empty<Channel>()
                                )
                            }).ToList()
                        };
                        Scenes.Add(showScene);
                        Message = $"Scene '{scene.Name}' imported successfully!";
                        hasUnsavedChanges = true;
                    }
                    else
                    {
                        Message = "This scene has already been imported.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // SCENE SELECTION
        // ============================================================
        [RelayCommand]
        private void SceneSelectionChanged(SceneModel selectedScene)
        {
            selectedScene = selectedScene;
        }

        // ============================================================
        // SAVE AS
        // ============================================================
        [RelayCommand]
        private void SaveAs()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Show As",
                    Filter = "JSON files (*.json)|*.json",
                    DefaultExt = ".json",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    FileName = string.IsNullOrWhiteSpace(CurrentShowName)
                        ? "NewShow.json"
                        : $"{CurrentShowName}.json",
                    AddExtension = true,
                    OverwritePrompt = true
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string path = saveFileDialog.FileName;
                    SaveShowToPath(path);
                    _currentShowPath = path;
                    MessageBox.Show($"Show saved to '{path}'",
                        "Save As", MessageBoxButton.OK, MessageBoxImage.Information);
                    hasUnsavedChanges = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // SAVE
        // ============================================================
        [RelayCommand]
        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentShowPath))
                {
                    SaveAs();
                    return;
                }

                SaveShowToPath(_currentShowPath);
                MessageBox.Show($"Show saved to '{_currentShowPath}'",
                    "Save", MessageBoxButton.OK, MessageBoxImage.Information);
                hasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OpenShow()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Open Existing Show",
                    Filter = "JSON files (*.json)|*.json",
                    Multiselect = false,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedPath = openFileDialog.FileName;
                    string jsonString = File.ReadAllText(selectedPath);

                    var doc = JsonDocument.Parse(jsonString);
                    if (!doc.RootElement.TryGetProperty("show", out var showElement))
                    {
                        Message = "Het geselecteerde bestand bevat geen geldige 'show'-structuur.";
                        return;
                    }

                    var loadedShow = JsonSerializer.Deserialize<InterdisciplinairProject.Core.Models.Show>(showElement.GetRawText());
                    if (loadedShow == null)
                    {
                        Message = "Kon show niet deserialiseren. Bestand mogelijk corrupt.";
                        return;
                    }

                    _show = loadedShow;

                    CurrentShowId = _show.Id;
                    CurrentShowName = _show.Name;
                    _currentShowPath = selectedPath;

                    Scenes.Clear();
                    if (_show.Scenes != null)
                    {
                        foreach (var scene in _show.Scenes)
                        {
                            // when opening/importing a show, reset dimmer to 0 so sliders start off
                            scene.Dimmer = 0;

                            // Calculate channel ratios for all fixtures in the scene
                            if (scene.Fixtures != null)
                            {
                                foreach (var fixture in scene.Fixtures)
                                {
                                    //fixture.CalculateChannelRatios();
                                }
                            }

                            Scenes.Add(scene);
                        }
                    }


                    // Load timeline scenes if present
                    TimeLineScenes.Clear();
                    if (doc.RootElement.TryGetProperty("timeline", out var timelineElement))
                    {
                        var timelineScenes = JsonSerializer.Deserialize<List<TimelineShowScene>>(timelineElement.GetRawText());
                        if (timelineScenes != null)
                        {
                            foreach (var timelineScene in timelineScenes)
                            {
                                TimeLineScenes.Add(timelineScene);
                            }
                        }
                    }

                    Message = $"Show '{_show.Name}' succesvol geopend!";
                    hasUnsavedChanges = false;
                }
            }
            catch (JsonException)
            {
                Message = "Het geselecteerde bestand bevat ongeldige JSON.";
            }
            catch (Exception ex)
            {
                Message = $"Er is een fout opgetreden bij het openen van de show:\n{ex.Message}";
            }
        }

        private void SaveShowToPath(string path)
        {
            // Zorg dat _show up-to-date is
            _show.Id = CurrentShowId ?? GenerateRandomId();
            _show.Name = CurrentShowName ?? "Unnamed Show";
            _show.Scenes = Scenes.ToList();

            // Wrap in "show" object for compatible JSON
            var wrapper = new
            {
                show = _show,
                timeline = TimeLineScenes.ToList()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(wrapper, options);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private string GenerateRandomId()
        {
            Random rnd = new Random();
            int number = rnd.Next(1, 999);
            string id = number.ToString();
            return id;
        }

        // ============================================================
        // DELETE SCENE
        // ============================================================
        [RelayCommand]
        private void DeleteScene(SceneModel? scene)
        {
            if (scene == null)
                return;

            // Ask for confirmation before deleting
            var result = MessageBox.Show(
                $"Weet je zeker dat je de scene '{scene.Name}' wilt verwijderen?",
                "Bevestig verwijderen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // remove from the UI collection
            if (Scenes.Contains(scene))
                Scenes.Remove(scene);

            // keep underlying show in sync if needed
            if (_show?.Scenes != null && _show.Scenes.Contains(scene))
                _show.Scenes.Remove(scene);

            Message = $"Scene '{scene.Name}' verwijderd.";
            hasUnsavedChanges = true;
        }

        public void UpdateSceneDimmer(SceneModel scene, int dimmer)
        {
            if (scene == null)
                return;

            // Cancel any fade in progress for this scene because user is manually changing it
            CancelFadeForScene(scene);

            dimmer = Math.Max(0, Math.Min(255, dimmer));

            // if we're turning this scene on (dimmer > 0), immediately turn all other scenes off.
            if (dimmer > 0)
            {
                foreach (var other in Scenes.ToList())
                {
                    if (!ReferenceEquals(other, scene) && other.Dimmer > 0)
                    {
                        other.Dimmer = 0;

                        // update other scene fixtures to 0
                        if (other.Fixtures != null)
                        {
                            foreach (var fixture in other.Fixtures)
                            {
                                try
                                {
                                    // set observable property if available
                                    fixture.Dimmer = 0;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[ERROR] Error zeroing fixture dimmer: {ex.Message}");
                                }
                            }
                        }

                        // refresh the Scenes collection item so UI updates if needed
                        var idx = Scenes.IndexOf(other);
                        if (idx >= 0) Scenes[idx] = other;
                    }
                }
            }

            // update model for the requested scene
            scene.Dimmer = dimmer;

            // update fixture channels for the requested scene
            if (scene.Fixtures != null)
            {
                // Calculate the scene dimmer percentage (0.0 to 1.0)
                double dimmerPercentage = dimmer / 255.0;

                foreach (var fixture in scene.Fixtures)
                {
                    try
                    {
                        // Update fixture dimmer for overall control
                        byte channelValue = (byte)dimmer;
                        fixture.Dimmer = channelValue;

                        // Update individual channels based on their effects
                        foreach (var channel in fixture.Channels)
                        {
                            if (channel.ChannelEffect?.Enabled == true &&
                                (channel.ChannelEffect.EffectType == Core.Models.Enums.EffectType.FadeIn ||
                                 channel.ChannelEffect.EffectType == Core.Models.Enums.EffectType.FadeOut))
                            {
                                // Get the channel's min and max values
                                byte channelMin = channel.ChannelEffect.Min;
                                byte channelMax = channel.ChannelEffect.Max;

                                // Calculate the value based on effect type
                                int calculatedValue;
                                if (channel.ChannelEffect.EffectType == Core.Models.Enums.EffectType.FadeIn)
                                {
                                    // FadeIn: slider at 0% = channelMin, slider at 100% = channelMax
                                    calculatedValue = (int)Math.Round(channelMin + (channelMax - channelMin) * dimmerPercentage);
                                }
                                else // FadeOut
                                {
                                    // FadeOut: slider at 0% = channelMax, slider at 100% = channelMin
                                    calculatedValue = (int)Math.Round(channelMax - (channelMax - channelMin) * dimmerPercentage);
                                }

                                // Clamp the value to byte range
                                calculatedValue = Math.Max(0, Math.Min(255, calculatedValue));

                                // Update the channel value
                                channel.Parameter = calculatedValue;
                                channel.Value = calculatedValue.ToString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Error updating fixture channels/dimmer: {ex.Message}");
                    }
                }
            }

            // Ensure UI reflects changes
            if (SelectedScene == scene)
            {
                OnPropertyChanged(nameof(SelectedScene));
            }
            else
            {
                var idx = Scenes.IndexOf(scene);
                if (idx >= 0) Scenes[idx] = scene;
            }
        }

        // Cancels any running fade for the provided scene
        private void CancelFadeForScene(SceneModel scene)
        {
            if (scene == null) return;

            if (_fadeCts.TryGetValue(scene, out var cts))
            {
                try
                {
                    cts.Cancel();
                }
                catch { }
                cts.Dispose();
                _fadeCts.Remove(scene);
            }
        }

        // Fade a single scene to target over durationMs, updating fixtures and Scenes on the UI thread.
        private async Task FadeSceneAsync(SceneModel scene, int target, int durationMs, CancellationToken token)
        {
            if (scene == null) return;

            if (durationMs <= 0)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    scene.Dimmer = target;
                    UpdateFixturesForScene(scene, target);
                    var idx = Scenes.IndexOf(scene);
                    if (idx >= 0) Scenes[idx] = scene;
                    if (SelectedScene == scene) OnPropertyChanged(nameof(SelectedScene));
                });
                return;
            }

            const int intervalMs = 20;
            int steps = Math.Max(1, durationMs / intervalMs);

            // read authoritative start on UI thread (await returns the value)
            int start = await Application.Current.Dispatcher.InvokeAsync(() => scene.Dimmer);

            double delta = (target - start) / (double)steps;

            for (int i = 1; i <= steps; i++)
            {
                token.ThrowIfCancellationRequested();
                double next = start + delta * i;
                int nextInt = (int)Math.Round(Math.Max(0, Math.Min(255, next)));

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    scene.Dimmer = nextInt;
                    UpdateFixturesForScene(scene, nextInt);
                    var idx = Scenes.IndexOf(scene);
                    if (idx >= 0) Scenes[idx] = scene;
                    if (SelectedScene == scene) OnPropertyChanged(nameof(SelectedScene));
                });

                await Task.Delay(intervalMs, token);
            }

            // ensure exact target at end
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                scene.Dimmer = target;
                UpdateFixturesForScene(scene, target);
                var idx = Scenes.IndexOf(scene);
                if (idx >= 0) Scenes[idx] = scene;
                if (SelectedScene == scene) OnPropertyChanged(nameof(SelectedScene));
            });
        }

        // Helper to update fixture dimmer channels for a scene on the caller thread (call from UI dispatcher)
        private void UpdateFixturesForScene(SceneModel scene, int dimmer)
        {
            if (scene?.Fixtures == null) return;

            // Calculate the scene dimmer percentage (0.0 to 1.0)
            double dimmerPercentage = dimmer / 255.0;
            byte channelValue = (byte)dimmer;

            foreach (var fixture in scene.Fixtures)
            {
                try
                {
                    fixture.Dimmer = channelValue;

                    // Update individual channels based on their effects
                    foreach (var channel in fixture.Channels)
                    {
                        if (channel.ChannelEffect?.Enabled == true &&
                            (channel.ChannelEffect.EffectType == Core.Models.Enums.EffectType.FadeIn ||
                             channel.ChannelEffect.EffectType == Core.Models.Enums.EffectType.FadeOut))
                        {
                            // Get the channel's min and max values
                            byte channelMin = channel.ChannelEffect.Min;
                            byte channelMax = channel.ChannelEffect.Max;

                            // Calculate the value based on effect type
                            int calculatedValue;
                            if (channel.ChannelEffect.EffectType == Core.Models.Enums.EffectType.FadeIn)
                            {
                                // FadeIn: slider at 0% = channelMin, slider at 100% = channelMax
                                calculatedValue = (int)Math.Round(channelMin + (channelMax - channelMin) * dimmerPercentage);
                            }
                            else // FadeOut
                            {
                                // FadeOut: slider at 0% = channelMax, slider at 100% = channelMin
                                calculatedValue = (int)Math.Round(channelMax - (channelMax - channelMin) * dimmerPercentage);
                            }

                            // Clamp the value to byte range
                            calculatedValue = Math.Max(0, Math.Min(255, calculatedValue));

                            // Update the channel value
                            channel.Parameter = calculatedValue;
                            channel.Value = calculatedValue.ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Error updating fixture channels/dimmer: {ex.Message}");
                }
            }
        }

        // Public method used by SceneControlViewModel.PlayAsync to activate scene with fade orchestration.
        public async Task FadeToAndActivateAsync(SceneModel targetScene, int targetDimmer)
        {
            if (targetScene == null) return;

            // Cancel any fade for the target (we'll run a new one)
            CancelFadeForScene(targetScene);

            // collect currently active other scenes
            var activeOthers = Scenes.Where(s => !ReferenceEquals(s, targetScene) && s.Dimmer > 0).ToList();

            // fade out others in parallel
            var fadeOutTasks = new List<Task>();
            foreach (var other in activeOthers)
            {
                // cancel existing token for other and create a new one
                CancelFadeForScene(other);
                var cts = new CancellationTokenSource();
                _fadeCts[other] = cts;
                fadeOutTasks.Add(Task.Run(() => FadeSceneAsync(other, 0, Math.Max(0, other.FadeOutMs), cts.Token)));
            }

            try
            {
                // wait for all fade-outs to complete
                await Task.WhenAll(fadeOutTasks);
            }
            catch (OperationCanceledException) { /* one of fades cancelled; continue */ }

            // ensure other scenes are set to 0 (defensive)
            foreach (var other in activeOthers)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    other.Dimmer = 0;
                    UpdateFixturesForScene(other, 0);
                    var idx = Scenes.IndexOf(other);
                    if (idx >= 0) Scenes[idx] = other;
                });
                CancelFadeForScene(other);
            }

            // now fade target scene to requested dimmer using its FadeInMs
            CancelFadeForScene(targetScene);
            var ctsTarget = new CancellationTokenSource();
            _fadeCts[targetScene] = ctsTarget;

            try
            {
                await FadeSceneAsync(targetScene, targetDimmer, Math.Max(0, targetScene.FadeInMs), ctsTarget.Token);
            }
            finally
            {
                CancelFadeForScene(targetScene);
            }
        }
        [RelayCommand]
        private void AddSceneToTimeline()
        {
            if (SelectedScene == null)
            {
                return;
            }
            try
            {
                TimelineShowScene scene = new TimelineShowScene();
                scene.ShowScene = SelectedScene;
                scene.Id = id;
                if (scene.ShowScene != null)
                {
                    TimeLineScenes.Add(scene);
                    UpdateTimelineZIndices();
                }
                id++;
                hasUnsavedChanges = true;
            }
            catch (OperationCanceledException) { }
        }

        private void UpdateTimelineZIndices()
        {
            // Reverse Z-Index: First item has highest ZIndex
            int count = TimeLineScenes.Count;
            for (int i = 0; i < count; i++)
            {
                TimeLineScenes[i].ZIndex = count - i;
            }
        }

        public void MoveTimelineScene(TimelineShowScene scene, string direction)
        {
            if (scene == null) return;
            int index = TimeLineScenes.IndexOf(scene);
            if (index < 0) return; // scene not found in timeline

            if (direction == "left")
            {
                if (index == 0) return; // already first
                TimeLineScenes.Move(index, index - 1);
            }
            else if (direction == "right")
            {
                if (index >= TimeLineScenes.Count - 1) return; // already last
                if (index >= TimeLineScenes.Count - 1) return; // already last
                TimeLineScenes.Move(index, index + 1);
            }
            UpdateTimelineZIndices();
            hasUnsavedChanges = true;
        }

        [RelayCommand]
        private void DeleteTimelineScene(TimelineShowScene? scene)
        {
            if (scene == null)
                return;

            // Ask for confirmation before deleting
            var result = MessageBox.Show(
                $"Weet je zeker dat je de scene '{scene.ShowScene.Name}' wilt verwijderen?",
                "Bevestig verwijderen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // remove from the UI collection
            if (TimeLineScenes.Contains(scene))
            {
                TimeLineScenes.Remove(scene);
                UpdateTimelineZIndices();
            }

            Message = $"Scene '{scene.ShowScene.Name}' verwijderd.";
            hasUnsavedChanges = true;
        }

        /// <summary>
        /// Opens the scene settings window for the selected scene.
        /// </summary>
        [RelayCommand]
        private void OpenSceneSettings(SceneModel? scene)
        {
            if (scene == null)
                return;

            var window = new InterdisciplinairProject.Views.SceneSettingsWindow(scene)
            {
                Owner = Application.Current?.MainWindow
            };

            window.ShowDialog();
        }

        // Formatted time properties for binding in the view
        public string CurrentTimeFormatted => FormatTime(CurrentTime);
        public string TotalTimeFormatted => FormatTime(TotalTime);

        partial void OnCurrentTimeChanged(System.TimeSpan value)
        {
            OnPropertyChanged(nameof(CurrentTimeFormatted));
        }

        partial void OnTotalTimeChanged(System.TimeSpan value)
        {
            OnPropertyChanged(nameof(TotalTimeFormatted));
        }

        public static string FormatTime(System.TimeSpan ts)
        {
            return ts.ToString(@"hh\:mm\:ss");
        }

        // ============================================================
        // PLAYBACK TIMER LOGIC
        // ============================================================
        
        /// <summary>
        /// Gets the total duration of all timeline scenes in milliseconds.
        /// </summary>
        private double TotalDurationMs
        {
            get
            {
                if (TimeLineScenes == null || !TimeLineScenes.Any())
                    return 0;

                double total = 0;
                foreach (var scene in TimeLineScenes)
                {
                    if (scene?.ShowScene != null)
                    {
                        // Add fade in, hold time (if any), and fade out
                        total += scene.ShowScene.FadeInMs;
                        total += scene.ShowScene.FadeOutMs;
                        // You can add scene duration here if your scenes have a hold/duration property
                    }
                }
                return total;
            }
        }

        /// <summary>
        /// Initializes the playback timer with 50ms interval.
        /// </summary>
        private void InitializePlaybackTimer()
        {
            if (_playbackTimer != null)
                return;

            _playbackTimer = new System.Timers.Timer(50); // 50ms interval
            _playbackTimer.Elapsed += PlaybackTimer_Elapsed;
            _playbackTimer.AutoReset = true;
        }

        /// <summary>
        /// Timer elapsed handler that updates elapsed time and progress position.
        /// </summary>
        private void PlaybackTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            _elapsedMs += 50; // Increment by timer interval

            double totalMs = TotalDurationMs;
            if (totalMs > 0)
            {
                // Calculate progress position (0-100)
                double newProgress = (_elapsedMs / totalMs) * 100;
                
                // Update on UI thread
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ProgressPosition = Math.Min(newProgress, 100);
                    CurrentTime = TimeSpan.FromMilliseconds(_elapsedMs);
                    TotalTime = TimeSpan.FromMilliseconds(totalMs);
                });

                // Stop when we reach the end
                if (_elapsedMs >= totalMs)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        StopPlayback();
                    });
                }
            }
        }

        /// <summary>
        /// Toggles between play and pause states.
        /// </summary>
        [RelayCommand]
        private void TogglePlayPause()
        {
            if (_playbackTimer == null)
            {
                InitializePlaybackTimer();
            }

            if (IsPlaying)
            {
                // Pause
                _playbackTimer?.Stop();
                IsPlaying = false;
                Message = "Playback gepauzeerd";
            }
            else
            {
                // Play
                _playbackTimer?.Start();
                IsPlaying = true;
                Message = "Playback gestart";
            }
        }

        /// <summary>
        /// Stops playback and resets to the beginning.
        /// </summary>
        [RelayCommand]
        private void StopPlayback()
        {
            _playbackTimer?.Stop();
            IsPlaying = false;
            _elapsedMs = 0;
            ProgressPosition = 0;
            CurrentTime = TimeSpan.Zero;
            
            // Keep TotalTime displayed
            double totalMs = TotalDurationMs;
            TotalTime = TimeSpan.FromMilliseconds(totalMs);
            
            Message = "Playback gestopt";
        }

        /// <summary>
        /// Cleanup method to dispose of the timer.
        /// </summary>
        public void Dispose()
        {
            if (_playbackTimer != null)
            {
                _playbackTimer.Stop();
                _playbackTimer.Elapsed -= PlaybackTimer_Elapsed;
                _playbackTimer.Dispose();
                _playbackTimer = null;
            }

            // Dispose all fade cancellation tokens
            foreach (var cts in _fadeCts.Values)
            {
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch { }
            }
            _fadeCts.Clear();
        }

        // Destructor to ensure cleanup
        ~ShowbuilderViewModel()
        {
            Dispose();
        }

    }
}