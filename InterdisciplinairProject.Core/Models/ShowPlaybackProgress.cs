namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents the current progress state during show playback.
/// Used to report playback status to the UI layer.
/// </summary>
public class ShowPlaybackProgress
{
    /// <summary>
    /// Gets or sets the currently playing scene.
    /// </summary>
    public Scene? CurrentScene { get; set; }

    /// <summary>
    /// Gets or sets the index of the current scene (0-based).
    /// </summary>
    public int CurrentSceneIndex { get; set; }

    /// <summary>
    /// Gets or sets the total number of scenes in the show.
    /// </summary>
    public int TotalScenes { get; set; }

    /// <summary>
    /// Gets or sets the elapsed time in milliseconds for the current scene.
    /// </summary>
    public double CurrentSceneElapsedMs { get; set; }

    /// <summary>
    /// Gets or sets the total elapsed time in milliseconds for the show.
    /// </summary>
    public double TotalElapsedMs { get; set; }

    /// <summary>
    /// Gets or sets the total duration of the show in milliseconds.
    /// </summary>
    public double TotalDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the current playback state.
    /// </summary>
    public ShowPlaybackState State { get; set; }

    /// <summary>
    /// Gets or sets the current time as a TimeSpan for display purposes.
    /// </summary>
    public TimeSpan CurrentTime => TimeSpan.FromMilliseconds(TotalElapsedMs);

    /// <summary>
    /// Gets or sets the total time as a TimeSpan for display purposes.
    /// </summary>
    public TimeSpan TotalTime => TimeSpan.FromMilliseconds(TotalDurationMs);

    /// <summary>
    /// Gets the overall progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage => TotalDurationMs > 0
        ? (TotalElapsedMs / TotalDurationMs) * 100
        : 0;

    /// <summary>
    /// Formats a TimeSpan as "HH:MM:SS".
    /// </summary>
    /// <param name="ts">The TimeSpan to format.</param>
    /// <returns>Formatted time string.</returns>
    public static string FormatTime(TimeSpan ts)
    {
        return ts.ToString(@"hh\:mm\:ss");
    }

    /// <summary>
    /// Gets the current time formatted as "HH:MM:SS".
    /// </summary>
    public string CurrentTimeFormatted => FormatTime(CurrentTime);

    /// <summary>
    /// Gets the total time formatted as "HH:MM:SS".
    /// </summary>
    public string TotalTimeFormatted => FormatTime(TotalTime);
}
