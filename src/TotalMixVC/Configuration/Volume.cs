namespace TotalMixVC.Configuration;

/// <summary>Provides configuration related to volume changes on the device.</summary>
public record Volume
{
    /// <summary>
    /// Gets or sets a value indicating whether volume units are set in dB instead of percentages.
    /// </summary>
    public bool UseDecibels { get; set; }

    /// <summary>
    /// Gets or sets the increment that is to be used when adjusting the volume. The volume ranges
    /// from 0.0 and 1.0 and thus the max allowed increment is 0.10 for percentages or 3.0 in
    /// decibels to avoid major jumps in volume.
    /// </summary>
    public float? Increment { get; set; }

    /// <summary>
    /// Gets or sets the fine increment that is to be used when adjusting the volume and holding
    /// the Shift key. The volume ranges from 0.0 and 1.0 and thus the max allowed fine increment
    /// is 0.05 for percentages and 1.5 in decibels to avoid major jumps in volume. When using
    /// decibels, it is generally a good idea to ensure the fine increment is a multiple of
    /// increment.
    /// </summary>
    public float? FineIncrement { get; set; }

    /// <summary>
    /// Gets or sets the maximum volume that will be sent by the application where 1.0 or 6.0 dB is
    /// the loudest volume the device can receive.
    /// </summary>
    public float? Max { get; set; }
}
