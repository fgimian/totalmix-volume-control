namespace TotalMixVC.Configuration.Models;

/// <summary>
/// Provides the increment to use when increasing or decreasing the volume in decibels.
/// </summary>
public record VolumeIncrementDecibels
{
    private float _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeIncrementDecibels"/> class.
    /// </summary>
    /// <param name="value">The volume decibel value.</param>
    public VolumeIncrementDecibels(float value)
    {
        Value = value;
    }

    /// <summary>Gets or sets the volume decibel value.</summary>
    /// <exception cref="InvalidOperationException">
    /// The increment specified is not in the supported range.
    /// </exception>
    public float Value
    {
        get => _value;
        set
        {
            if (value <= 0.0 || value > 6.0 || value % 0.5f != 0.0f)
            {
                throw new InvalidOperationException(
                    "The value must be a multiple of 0.5 while being greater than 0 and less "
                        + "than or equal to 6.0."
                );
            }

            _value = value;
        }
    }
}
