using System.Text.Json.Serialization;
using TotalMixVC.Configuration.Converters;

namespace TotalMixVC.Configuration;

/// <summary>Provides configuration related to volume changes on the device.</summary>
public record Volume
{
    /// <summary>
    /// Gets the increment that is to be used when adjusting the volume. The volume ranges from
    /// 0.0 and 1.0 and thus the max allowed increment is 0.10 to avoid major jumps in volume.
    /// </summary>
    [JsonConverter(typeof(VolumeIncrementFloatConverter))]
    public float Increment { get; init; } = 0.02f;

    /// <summary>
    /// Gets the fineincrement that is to be used when adjusting the volume and holding the Shift
    /// key. The volume ranges from 0.0 and 1.0 and thus the max allowed fine increment is 0.05 to
    /// avoid major jumps in volume.
    /// </summary>
    [JsonConverter(typeof(VolumeFineIncrementFloatConverter))]
    public float FineIncrement { get; init; } = 0.01f;

    /// <summary>
    /// Gets the maximum volume that will be sent by the application where 1.0 is the loudest
    /// volume the device can receive.
    /// </summary>
    [JsonConverter(typeof(VolumeMaxFloatConverter))]
    public float Max { get; init; } = 1.0f;
}
