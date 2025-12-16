using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Services;

/// <summary>
/// Orchestrates show playback with proper timing and scene transitions.
/// Implements fade-in, hold, and fade-out for each scene in sequence.
/// This service coordinates with IHardwareConnection to send scenes to DMX hardware.
/// </summary>
public class ShowPlaybackService : IShowPlaybackService
{
    private const int TimerIntervalMs = 50;

    private readonly IHardwareConnection _hardwareConnection;
    private readonly IDmxService _dmxService;

    private CancellationTokenSource? _internalCts;
    private bool _isPaused;
    private ShowPlaybackState _currentState;
    private readonly object _stateLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowPlaybackService"/> class.
    /// </summary>
    /// <param name="hardwareConnection">The hardware connection for sending scenes.</param>
    /// <param name="dmxService">The DMX service for channel control.</param>
    public ShowPlaybackService(IHardwareConnection hardwareConnection, IDmxService dmxService)
    {
        _hardwareConnection = hardwareConnection ?? throw new ArgumentNullException(nameof(hardwareConnection));
        _dmxService = dmxService ?? throw new ArgumentNullException(nameof(dmxService));
        _currentState = ShowPlaybackState.Idle;
    }

    /// <summary>
    /// Gets a value indicating whether a show is currently playing.
    /// </summary>
    public bool IsPlaying
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState == ShowPlaybackState.Playing ||
                       _currentState == ShowPlaybackState.FadingIn ||
                       _currentState == ShowPlaybackState.Holding ||
                       _currentState == ShowPlaybackState.FadingOut ||
                       _currentState == ShowPlaybackState.TransitioningToNext;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether playback is currently paused.
    /// </summary>
    public bool IsPaused
    {
        get
        {
            lock (_stateLock)
            {
                return _isPaused;
            }
        }
    }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    public ShowPlaybackState CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
    }

    /// <summary>
    /// Starts playback of a show.
    /// </summary>
    /// <param name="show">The show to play.</param>
    /// <param name="progress">Optional progress reporter for playback status.</param>
    /// <param name="cancellationToken">Token to cancel playback.</param>
    /// <returns>True if the show completed successfully, otherwise false.</returns>
    public async Task<bool> StartAsync(InterdisciplinairProject.Core.Models.Show show, IProgress<ShowPlaybackProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (show?.TimelineScenes == null || show.TimelineScenes.Count == 0)
        {
            Debug.WriteLine("[PLAYBACK] StartAsync: show or timeline is empty");
            return false;
        }

        // Stop any existing playback
        Stop();

        // Create internal cancellation token source
        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _internalCts.Token;

        try
        {
            SetState(ShowPlaybackState.Playing);
            Debug.WriteLine($"[PLAYBACK] Starting show '{show.Name}' with {show.TimelineScenes.Count} scenes");

            double totalDurationMs = show.TotalDurationMs;
            double totalElapsedMs = 0;
            int sceneIndex = 0;

            foreach (var timelineScene in show.TimelineScenes)
            {
                token.ThrowIfCancellationRequested();

                var scene = timelineScene.ShowScene;
                if (scene == null)
                {
                    sceneIndex++;
                    continue;
                }

                Debug.WriteLine($"[PLAYBACK] Playing scene {sceneIndex + 1}/{show.TimelineScenes.Count}: '{scene.Name}'");

                // Calculate scene timing
                int fadeInMs = scene.FadeInMs;
                int holdMs = timelineScene.HoldDurationMs;
                int fadeOutMs = scene.FadeOutMs;

                // === FADE IN ===
                SetState(ShowPlaybackState.FadingIn);
                await FadeSceneAsync(
                    scene,
                    0,
                    100,
                    fadeInMs,
                    sceneIndex,
                    show.TimelineScenes.Count,
                    totalElapsedMs,
                    totalDurationMs,
                    progress,
                    token);

                totalElapsedMs += fadeInMs;

                // === HOLD ===
                SetState(ShowPlaybackState.Holding);
                await HoldSceneAsync(
                    scene,
                    holdMs,
                    sceneIndex,
                    show.TimelineScenes.Count,
                    totalElapsedMs,
                    totalDurationMs,
                    progress,
                    token);

                totalElapsedMs += holdMs;

                // === FADE OUT ===
                SetState(ShowPlaybackState.FadingOut);
                await FadeSceneAsync(
                    scene,
                    100,
                    0,
                    fadeOutMs,
                    sceneIndex,
                    show.TimelineScenes.Count,
                    totalElapsedMs,
                    totalDurationMs,
                    progress,
                    token);

                totalElapsedMs += fadeOutMs;

                // Transition to next scene
                SetState(ShowPlaybackState.TransitioningToNext);
                sceneIndex++;
            }

            SetState(ShowPlaybackState.Completed);
            Debug.WriteLine("[PLAYBACK] Show completed successfully");

            // Report final progress
            ReportProgress(progress, null, show.TimelineScenes.Count - 1, show.TimelineScenes.Count, 0, totalDurationMs, totalDurationMs, ShowPlaybackState.Completed);

            return true;
        }
        catch (OperationCanceledException)
        {
            SetState(ShowPlaybackState.Cancelled);
            Debug.WriteLine("[PLAYBACK] Show playback cancelled");
            return false;
        }
        catch (Exception ex)
        {
            SetState(ShowPlaybackState.Error);
            Debug.WriteLine($"[PLAYBACK] Error during playback: {ex.Message}");
            return false;
        }
        finally
        {
            _internalCts?.Dispose();
            _internalCts = null;
        }
    }

    /// <summary>
    /// Pauses the current show playback.
    /// </summary>
    public void Pause()
    {
        lock (_stateLock)
        {
            if (IsPlaying)
            {
                _isPaused = true;
                Debug.WriteLine("[PLAYBACK] Playback paused");
            }
        }
    }

    /// <summary>
    /// Resumes paused playback.
    /// </summary>
    public void Resume()
    {
        lock (_stateLock)
        {
            if (_isPaused)
            {
                _isPaused = false;
                Debug.WriteLine("[PLAYBACK] Playback resumed");
            }
        }
    }

    /// <summary>
    /// Stops the current show playback and resets to the beginning.
    /// </summary>
    public void Stop()
    {
        lock (_stateLock)
        {
            _internalCts?.Cancel();
            _isPaused = false;
            _currentState = ShowPlaybackState.Idle;
            Debug.WriteLine("[PLAYBACK] Playback stopped");
        }

        // Clear all DMX channels
        _dmxService.ClearAllChannels();
        _dmxService.SendFrame();
    }

    /// <summary>
    /// Fades a scene from startDimmer to endDimmer over durationMs.
    /// </summary>
    private async Task FadeSceneAsync(
        Scene scene,
        int startDimmer,
        int endDimmer,
        int durationMs,
        int sceneIndex,
        int totalScenes,
        double baseElapsedMs,
        double totalDurationMs,
        IProgress<ShowPlaybackProgress>? progress,
        CancellationToken token)
    {
        if (durationMs <= 0)
        {
            // Instant transition
            await ApplyDimmerToSceneAsync(scene, endDimmer);
            return;
        }

        int steps = Math.Max(1, durationMs / TimerIntervalMs);
        double dimmerDelta = (endDimmer - startDimmer) / (double)steps;

        for (int i = 0; i <= steps; i++)
        {
            token.ThrowIfCancellationRequested();

            // Handle pause
            while (_isPaused && !token.IsCancellationRequested)
            {
                await Task.Delay(TimerIntervalMs, token);
            }

            double currentDimmer = startDimmer + (dimmerDelta * i);
            currentDimmer = Math.Max(0, Math.Min(100, currentDimmer));

            await ApplyDimmerToSceneAsync(scene, (int)currentDimmer);

            // Report progress
            double sceneElapsedMs = i * TimerIntervalMs;
            double totalElapsed = baseElapsedMs + sceneElapsedMs;
            ReportProgress(progress, scene, sceneIndex, totalScenes, sceneElapsedMs, totalElapsed, totalDurationMs, _currentState);

            if (i < steps)
            {
                await Task.Delay(TimerIntervalMs, token);
            }
        }
    }

    /// <summary>
    /// Holds a scene at full intensity for the specified duration.
    /// </summary>
    private async Task HoldSceneAsync(
        Scene scene,
        int durationMs,
        int sceneIndex,
        int totalScenes,
        double baseElapsedMs,
        double totalDurationMs,
        IProgress<ShowPlaybackProgress>? progress,
        CancellationToken token)
    {
        if (durationMs <= 0)
        {
            return;
        }

        int steps = Math.Max(1, durationMs / TimerIntervalMs);

        for (int i = 0; i < steps; i++)
        {
            token.ThrowIfCancellationRequested();

            // Handle pause
            while (_isPaused && !token.IsCancellationRequested)
            {
                await Task.Delay(TimerIntervalMs, token);
            }

            // Report progress
            double sceneElapsedMs = i * TimerIntervalMs;
            double totalElapsed = baseElapsedMs + sceneElapsedMs;
            ReportProgress(progress, scene, sceneIndex, totalScenes, sceneElapsedMs, totalElapsed, totalDurationMs, _currentState);

            // IMPORTANT: Ensure we send DMX frames during the hold phase
            // Many DMX fixtures/controllers require continuous signal stream
            await ApplyDimmerToSceneAsync(scene, 100);

            await Task.Delay(TimerIntervalMs, token);
        }
    }

    /// <summary>
    /// Applies a dimmer value to all fixtures in a scene.
    /// </summary>
    private async Task ApplyDimmerToSceneAsync(Scene scene, int dimmerPercent)
    {
        Debug.WriteLine($"[PLAYBACK] ApplyDimmerToSceneAsync: scene={scene?.Name ?? "NULL"}, dimmerPercent={dimmerPercent}");

        if (scene?.Fixtures == null)
        {
            Debug.WriteLine("[PLAYBACK] ApplyDimmerToSceneAsync: scene or fixtures is null!");
            return;
        }

        Debug.WriteLine($"[PLAYBACK] Scene has {scene.Fixtures.Count} fixtures");

        // Update scene dimmer
        scene.Dimmer = dimmerPercent;

        // Calculate byte value (0-255)
        byte dimmerValue = (byte)Math.Round(dimmerPercent * 255.0 / 100.0);
        Debug.WriteLine($"[PLAYBACK] Dimmer byte value: {dimmerValue}");

        // Apply to all fixtures
        foreach (var fixture in scene.Fixtures)
        {
            if (fixture.Channels == null)
            {
                Debug.WriteLine($"[PLAYBACK] Fixture {fixture.Name} has no channels, skipping");
                continue;
            }

            Debug.WriteLine($"[PLAYBACK] Processing fixture: {fixture.Name}, StartAddress={fixture.StartAddress}, Channels={fixture.Channels.Count}");

            // Update fixture dimmer
            fixture.Dimmer = dimmerValue;

            // Set all channels in DMX universe
            for (int i = 0; i < fixture.Channels.Count; i++)
            {
                var channel = fixture.Channels[i];
                int dmxAddress = fixture.StartAddress + i;

                // Get the original value - try Parameter first, then parse Value string
                byte originalValue = (byte)channel.Parameter;
                if (originalValue == 0 && !string.IsNullOrEmpty(channel.Value))
                {
                    if (int.TryParse(channel.Value, out int parsedValue))
                    {
                        originalValue = (byte)Math.Min(255, Math.Max(0, parsedValue));
                    }
                }

                // Scale channel value by dimmer
                byte scaledValue = (byte)Math.Round(originalValue * (dimmerPercent / 100.0));

                Debug.WriteLine($"[PLAYBACK]   DMX[{dmxAddress}] = {scaledValue} (original={originalValue}, channel={channel.Name}, Value={channel.Value})");
                _dmxService.SetChannel(dmxAddress, scaledValue);
            }
        }

        // Send the frame
        bool result = _dmxService.SendFrame();
        Debug.WriteLine($"[PLAYBACK] SendFrame result: {result}");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Reports playback progress.
    /// </summary>
    private static void ReportProgress(
        IProgress<ShowPlaybackProgress>? progress,
        Scene? currentScene,
        int sceneIndex,
        int totalScenes,
        double sceneElapsedMs,
        double totalElapsedMs,
        double totalDurationMs,
        ShowPlaybackState state)
    {
        progress?.Report(new ShowPlaybackProgress
        {
            CurrentScene = currentScene,
            CurrentSceneIndex = sceneIndex,
            TotalScenes = totalScenes,
            CurrentSceneElapsedMs = sceneElapsedMs,
            TotalElapsedMs = totalElapsedMs,
            TotalDurationMs = totalDurationMs,
            State = state
        });
    }

    /// <summary>
    /// Sets the current playback state.
    /// </summary>
    private void SetState(ShowPlaybackState state)
    {
        lock (_stateLock)
        {
            _currentState = state;
        }
    }
}
