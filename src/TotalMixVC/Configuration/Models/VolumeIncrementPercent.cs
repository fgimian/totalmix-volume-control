namespace TotalMixVC.Configuration.Models;

/// <summary>
/// Provides the increment to use when increasing or decreasing the volume in percent.
/// </summary>
public record VolumeIncrementPercent
{
    private float _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeIncrementPercent"/> class.
    /// </summary>
    /// <param name="value">The volume percentage value.</param>
    public VolumeIncrementPercent(float value)
    {
        Value = value;
    }

    /// <summary>Gets or sets the volume percentage value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The increment specified is not in the supported range.
    /// </exception>
    public float Value
    {
        get => _value;
        set
        {
            if (value is <= 0.0f or > 0.10f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Must be greater than 0 and less than or equal to 0.1."
                );
            }

            _value = value;
        }
    }
}
