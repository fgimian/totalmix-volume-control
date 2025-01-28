namespace TotalMixVC.Configuration.Models;

/// <summary>
/// Provides the increment to use when finely increasing or decreasing the volume in percent.
/// </summary>
public record VolumeFineIncrementPercent
{
    private float _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeFineIncrementPercent"/> class.
    /// </summary>
    /// <param name="value">The volume percentage value.</param>
    public VolumeFineIncrementPercent(float value)
    {
        Value = value;
    }

    /// <summary>Gets or sets the volume percentage value.</summary>
    /// <exception cref="InvalidOperationException">
    /// The fine increment specified is not in the supported range.
    /// </exception>
    public float Value
    {
        get => _value;
        set
        {
            if (value is <= 0.0f or > 0.05f)
            {
                throw new InvalidOperationException(
                    "The value must be greater than 0 and less than or equal to 0.05."
                );
            }

            _value = value;
        }
    }
}
