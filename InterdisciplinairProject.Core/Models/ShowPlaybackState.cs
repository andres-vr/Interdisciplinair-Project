using System.Text.Json.Serialization;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents the state of show playback.
/// </summary>
public enum ShowPlaybackState
{
    /// <summary>
    /// No show is playing.
    /// </summary>
    Idle,

    /// <summary>
    /// Show is currently playing.
    /// </summary>
    Playing,

    /// <summary>
    /// Show is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Currently fading in a scene.
    /// </summary>
    FadingIn,

    /// <summary>
    /// Scene is at full intensity (holding).
    /// </summary>
    Holding,

    /// <summary>
    /// Currently fading out a scene.
    /// </summary>
    FadingOut,

    /// <summary>
    /// Transitioning between scenes.
    /// </summary>
    TransitioningToNext,

    /// <summary>
    /// Show playback completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Show playback was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// An error occurred during playback.
    /// </summary>
    Error
}
