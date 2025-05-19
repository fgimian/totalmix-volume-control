namespace TotalMixVC.Communicator;

/// <summary>Provides a snapshot of the device volume.</summary>
public record DeviceSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceSnapshot"/> class.
    /// </summary>
    /// <param name="volume">The current device volume.</param>
    /// <param name="volumeDecibels">The current device volume in decibels.</param>
    /// <param name="isDimmed">Whether the device volume is dimmed.</param>
    public DeviceSnapshot(float volume, string volumeDecibels, bool isDimmed)
    {
        Volume = volume;
        VolumeDecibels = volumeDecibels;
        IsDimmed = isDimmed;
    }

    /// <summary>
    /// Gets the current device volume as a float (with a range of 0.0 to 1.0).
    /// </summary>
    public float Volume { get; init; }

    /// <summary>
    /// Gets the current device volume as a string in decibels.
    /// </summary>
    public string VolumeDecibels { get; init; }

    /// <summary>
    /// Gets a value indicating whether the device volume is dimmed.
    /// </summary>
    public bool IsDimmed { get; init; }
}
