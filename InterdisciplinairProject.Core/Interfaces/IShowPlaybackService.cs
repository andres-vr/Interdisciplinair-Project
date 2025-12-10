using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Core.Interfaces;

/// <summary>
/// Interface for show playback orchestration services.
/// Manages the timing and sequencing of scenes during show playback.
/// This service coordinates with IHardwareConnection to send scenes to DMX hardware.
/// </summary>
public interface IShowPlaybackService
{
    /// <summary>
    /// Gets a value indicating whether a show is currently playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Gets a value indicating whether playback is currently paused.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    ShowPlaybackState CurrentState { get; }

    /// <summary>
    /// Starts playback of a show.
    /// </summary>
    /// <param name="show">The show to play.</param>
    /// <param name="progress">Optional progress reporter for playback status.</param>
    /// <param name="cancellationToken">Token to cancel playback.</param>
    /// <returns>True if the show completed successfully, otherwise false.</returns>
    Task<bool> StartAsync(Show show, IProgress<ShowPlaybackProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the current show playback.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes paused playback.
    /// </summary>
    void Resume();

    /// <summary>
    /// Stops the current show playback and resets to the beginning.
    /// </summary>
    void Stop();
}
