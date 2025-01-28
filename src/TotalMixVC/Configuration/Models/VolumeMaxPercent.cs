namespace TotalMixVC.Configuration.Models;

/// <summary>
/// Provides the maximum volume that should be allowed when increasing the volume in percent.
/// </summary>
public record VolumeMaxPercent
{
    private float _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeMaxPercent"/> class.
    /// </summary>
    /// <param name="value">The volume percentage value.</param>
    public VolumeMaxPercent(float value)
    {
        Value = value;
    }

    /// <summary>Gets or sets the volume percentage value.</summary>
    /// <exception cref="InvalidOperationException">
    /// The max volume specified is not in the supported range.
    /// </exception>
    public float Value
    {
        get => _value;
        set
        {
            if (value is <= 0.0f or > 1.0f)
            {
                throw new InvalidOperationException(
                    "The value must be greater than 0 and less than or equal to 1.0."
                );
            }

            _value = value;
        }
    }
}
