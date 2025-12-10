using System.Text.Json.Serialization;
using System.Windows.Input;
using System.ComponentModel;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents a channel in a fixture.
/// </summary>
public class Channel : INotifyPropertyChanged
{
    private int _parameter;
    private string _value = string.Empty;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the name of the channel.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the channel.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the channel.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    /// <summary>
    /// Gets or sets the parameter of the channel.
    /// </summary>
    [JsonIgnore]
    public int Parameter
    {
        get => _parameter;
        set
        {
            if (_parameter != value)
            {
                _parameter = value;
                OnPropertyChanged(nameof(Parameter));
            }
        }
    }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    [JsonPropertyName("min")]
    public int Min { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    [JsonPropertyName("max")]
    public int Max { get; set; } = 255;

    /// <summary>
    /// Gets or sets the time.
    /// </summary>
    [JsonPropertyName("time")]
    public int Time { get; set; } = 0;

    /// <summary>
    /// Gets or sets the range.
    /// </summary>
    [JsonPropertyName("ranges")]
    public List<ChannelRange> Ranges { get; set; } = new();

    /// <summary>
    /// Gets or sets the effect type.
    /// </summary>
    [JsonPropertyName("channelEffect")]
    public ChannelEffect ChannelEffect { get; set; } = new ChannelEffect();

    [JsonPropertyName("channelEffects")]
    public List<ChannelEffect> ChannelEffects { get; set; } = new();

    /// <summary>
    /// Gets or sets the test command.
    /// </summary>
    [JsonIgnore]
    public ICommand? TestCommand { get; set; }

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">Name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
