using System.Collections.ObjectModel;

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
    public float IncrementPercent { get; set; } = 0.02f;

    /// <summary>
    /// Gets or sets the fine increment that is to be used when adjusting the volume and holding
    /// the Shift key. The volume ranges from 0.0 and 1.0 and thus the max allowed fine increment
    /// is 0.05 to avoid major jumps in volume.
    /// </summary>
    public float FineIncrementPercent { get; set; } = 0.01f;

    /// <summary>
    /// Gets or sets the maximum volume that will be sent by the application where 1.0 is
    /// the loudest volume the device can receive.
    /// </summary>
    public float MaxPercent { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the increment that is to be used when adjusting the volume. The max allowed
    /// increment is 3.0 dB to avoid major jumps in volume.
    /// </summary>
    public float IncrementDecibels { get; set; } = 2.0f;

    /// <summary>
    /// Gets or sets the fine increment that is to be used when adjusting the volume and holding
    /// the Shift key. The max allowed fine increment is 1.5 dB to avoid major jumps in volume.
    /// </summary>
    public float FineIncrementDecibels { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the maximum volume that will be sent by the application where 6.0 dB is
    /// the loudest volume the device can receive.
    /// </summary>
    public float MaxDecibels { get; set; } = 6.0f;

    /// <summary>
    /// Validates that all properties are in the appropriate numeric range and resets their value
    /// if they don't meet range requirements.
    /// </summary>
    /// <param name="diagnostics">
    /// Error diagnostics recorded for properties which were not valid.
    /// </param>
    public void Validate(Collection<string> diagnostics)
    {
        if (IncrementPercent is <= 0.0f or > 0.10f)
        {
            IncrementPercent = 0.02f;
            diagnostics.Add(
                "(volume.increment_percent) : error : The value must be greater than 0 and less "
                    + "than or equal to 0.1."
            );
        }

        if (FineIncrementPercent is <= 0.0f or > 0.05f)
        {
            FineIncrementPercent = 0.01f;
            diagnostics.Add(
                "(volume.fine_increment_percent) : error : The value must be greater than 0 and "
                    + "less than or equal to 0.05."
            );
        }

        if (MaxPercent is <= 0.0f or > 1.0f)
        {
            MaxPercent = 1.0f;
            diagnostics.Add(
                "(volume.max_percent) : error : The value must be greater than 0 and less than or "
                    + "equal to 1.0."
            );
        }

        if (IncrementDecibels <= 0.0 || IncrementDecibels > 6.0 || IncrementDecibels % 0.5f != 0.0f)
        {
            IncrementDecibels = 2.0f;
            diagnostics.Add(
                "(volume.increment_decibels) : error : The value must be a multiple of 0.5 while "
                    + "being greater than 0 and less than or equal to 6.0."
            );
        }

        if (
            FineIncrementDecibels <= 0.0
            || FineIncrementDecibels > 3.0
            || FineIncrementDecibels % 0.25f != 0.0f
        )
        {
            FineIncrementDecibels = 1.0f;
            diagnostics.Add(
                "(volume.fine_increment_decibels) : error : The value must be a multiple of 0.25 "
                    + "while being greater than 0 and less than or equal to 3.0."
            );
        }

        if (MaxDecibels is > 6.0f)
        {
            MaxDecibels = 6.0f;
            diagnostics.Add(
                "(volume.max_decibels) : error : The value must be less than or equal to 6.0."
            );
        }
    }
}
