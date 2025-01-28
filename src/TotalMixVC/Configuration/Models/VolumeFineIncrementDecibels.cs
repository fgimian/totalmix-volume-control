namespace TotalMixVC.Configuration.Models;

/// <summary>
/// Provides the increment to use when finely increasing or decreasing the volume in decibels.
/// </summary>
public record VolumeFineIncrementDecibels
{
    private float _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeFineIncrementDecibels"/> class.
    /// </summary>
    /// <param name="value">The volume decibel value.</param>
    public VolumeFineIncrementDecibels(float value)
    {
        Value = value;
    }

    /// <summary>Gets or sets the volume decibel value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The fine increment specified is not in the supported range.
    /// </exception>
    public float Value
    {
        get => _value;
        set
        {
            if (value <= 0.0 || value > 3.0 || value % 0.25f != 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Must be a multiple of 0.25 while being greater than 0 and less than or equal "
                        + "to 3.0."
                );
            }

            _value = value;
        }
    }
}
