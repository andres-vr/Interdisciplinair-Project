using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents a show containing multiple scenes.
/// </summary>
public class Show
{
    /// <summary>
    /// Gets or sets the unique identifier of the show.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the show.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the scenes in this show.
    /// </summary>
    [JsonPropertyName("scenes")]
    public List<Scene>? Scenes { get; set; }

    /// <summary>
    /// Gets or sets the ordered timeline scenes for this show.
    /// These represent the scenes in playback order with timing information.
    /// </summary>
    [JsonPropertyName("timelineScenes")]
    public List<TimelineShowScene>? TimelineScenes { get; set; }

    /// <summary>
    /// Gets the total duration of the show in milliseconds.
    /// Calculated from all timeline scenes' fade and hold durations.
    /// </summary>
    [JsonIgnore]
    public int TotalDurationMs => TimelineScenes?.Sum(t => t.GetTotalDurationMs()) ?? 0;

    /// <summary>
    /// Gets the display text for this show.
    /// </summary>
    [JsonIgnore]
    public string DisplayText => $"{Name} (ID: {Id}) - # of Scenes: {Scenes?.Count() ?? 0}";
}