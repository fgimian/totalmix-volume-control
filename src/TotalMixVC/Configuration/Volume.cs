using TotalMixVC.Configuration.Models;

namespace TotalMixVC.Configuration;

/// <summary>Provides configuration related to volume changes on the device.</summary>
public record Volume
{
    /// <summary>
    /// Gets or sets a value indicating whether volume units are set in dB instead of percentages.
    /// </summary>
    public bool UseDecibels { get; set; }

    /// <summary>
    /// Gets or sets the increment that is to be used when adjusting the volume in percent. The
    /// volume ranges from 0.0 and 1.0 and thus the max allowed increment is 0.10 to avoid major
    /// jumps in volume.
    /// </summary>
    public VolumeIncrementPercent IncrementPercent { get; set; } = new(0.02f);

    /// <summary>
    /// Gets or sets the fine increment that is to be used when adjusting the volume and holding
    /// the Shift key. The volume ranges from 0.0 and 1.0 and thus the max allowed fine increment
    /// is 0.05 to avoid major jumps in volume.
    /// </summary>
    public VolumeFineIncrementPercent FineIncrementPercent { get; set; } = new(0.01f);

    /// <summary>
    /// Gets or sets the maximum volume that will be sent by the application where 1.0 is
    /// the loudest volume the device can receive.
    /// </summary>
    public VolumeMaxPercent MaxPercent { get; set; } = new(1.0f);

    /// <summary>
    /// Gets or sets the increment that is to be used when adjusting the volume. The max allowed
    /// increment is 3.0 dB to avoid major jumps in volume.
    /// </summary>
    public VolumeIncrementDecibels IncrementDecibels { get; set; } = new(2.0f);

    /// <summary>
    /// Gets or sets the fine increment that is to be used when adjusting the volume and holding
    /// the Shift key. The max allowed fine increment is 1.5 dB to avoid major jumps in volume.
    /// </summary>
    public VolumeFineIncrementDecibels FineIncrementDecibels { get; set; } = new(1.0f);

    /// <summary>
    /// Gets or sets the maximum volume that will be sent by the application where 6.0 dB is
    /// the loudest volume the device can receive.
    /// </summary>
    public VolumeMaxDecibels MaxDecibels { get; set; } = new(6.0f);
}
