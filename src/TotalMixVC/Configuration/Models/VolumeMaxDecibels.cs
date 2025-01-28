namespace TotalMixVC.Configuration.Models;

/// <summary>
/// Provides the maximum volume that should be allowed when increasing the volume in decibels.
/// </summary>
public record VolumeMaxDecibels
{
    private float _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeMaxDecibels"/> class.
    /// </summary>
    /// <param name="value">The volume decibel value.</param>
    public VolumeMaxDecibels(float value)
    {
        Value = value;
    }

    /// <summary>Gets or sets the volume decibel value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The max volume specified is not in the supported range.
    /// </exception>
    public float Value
    {
        get => _value;
        set
        {
            if (value is > 6.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Must be less than or equal to 6.0."
                );
            }

            _value = value;
        }
    }
}
