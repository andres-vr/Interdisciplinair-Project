using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Core.Interfaces;

/// <summary>
/// Interface for hardware connection operations.
/// This interface defines the high-level contract for DMX hardware communication.
/// Implementation flow:
/// - SetChannelValueAsync: Updates a single channel and sends only that fixture to DMX.
/// - SendFixtureAsync: Sends a complete fixture's channels to DMX (preserves other fixtures).
/// - SendSceneAsync: Clears and sends all fixtures in a scene to DMX (full scene preview).
/// - SendShowAsync: Plays a complete show with scenes in sequence with proper timing.
/// The implementing class (HardwareConnection) coordinates between JSON persistence and
/// the DmxService which manages the 512-channel DMX universe state.
/// </summary>
public interface IHardwareConnection
{
    /// <summary>
    /// Sends a value to a specific channel of a fixture asynchronously.
    /// </summary>
    /// <param name="fixtureInstanceId">The instance ID of the fixture.</param>
    /// <param name="channelName">The name of the channel (e.g. "dimmer").</param>
    /// <param name="value">The value between 0 and 255.</param>
    /// <returns>True if successful, otherwise false.</returns>
    Task<bool> SetChannelValueAsync(string fixtureInstanceId, string channelName, byte value);

    /// <summary>
    /// Sends the entire scene to the DMX controller asynchronously.
    /// </summary>
    /// <param name="scene">The scene to send.</param>
    /// <returns>True if successful, otherwise false.</returns>
    Task<bool> SendSceneAsync(Scene scene);

    /// <summary>
    /// Sends a single fixture's channel values to the DMX controller asynchronously.
    /// </summary>
    /// <param name="fixture">The fixture to send.</param>
    /// <returns>True if successful, otherwise false.</returns>
    Task<bool> SendFixtureAsync(Fixture fixture);

    /// <summary>
    /// Sends a complete show to the DMX controller asynchronously.
    /// This initiates playback of all scenes in order with proper timing.
    /// </summary>
    /// <param name="show">The show to send.</param>
    /// <param name="progress">Optional progress reporter for playback status.</param>
    /// <param name="cancellationToken">Token to cancel playback.</param>
    /// <returns>True if the show completed successfully, otherwise false.</returns>
    Task<bool> SendShowAsync(Show show, IProgress<ShowPlaybackProgress>? progress = null, CancellationToken cancellationToken = default);
}

