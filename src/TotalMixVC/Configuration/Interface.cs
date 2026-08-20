using System.Collections.ObjectModel;

namespace TotalMixVC.Configuration;

/// <summary>Provides configuration related to the behaviour of the widget user interface.</summary>
public record Interface
{
    /// <summary>
    /// Gets or sets the UI scaling of the widget where 1.0 is a normal 100% scale.
    /// </summary>
    public double Scaling { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets both the horizontal and vertical offset in pixels from the top left of the
    /// screen where the widget will appear.
    /// </summary>
    public double PositionOffset { get; set; } = 40.0;

    /// <summary>
    /// Gets or sets the number of seconds before the widget begins to fade away after it has
    /// appeared.
    /// </summary>
    public double HideDelay { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the number of seconds which the widget will take to fade out after hide delay.
    /// </summary>
    public double FadeOutTime { get; set; } = 0.75;

    /// <summary>
    /// Gets or sets a value indicating whether the widget should be shown when remote volume
    /// changes are detected. Please note that the device seems to send volume changes at some
    /// random times which is why this setting is disabled by default.
    /// </summary>
    public bool ShowRemoteVolumeChanges { get; set; }

    /// <summary>
    /// Validates that all properties are in the appropriate numeric range and resets their value
    /// if they don't meet range requirements.
    /// </summary>
    /// <param name="diagnostics">
    /// Error diagnostics recorded for properties which were not valid.
    /// </param>
    public void Validate(Collection<string> diagnostics)
    {
        if (Scaling <= 0.0)
        {
            Scaling = 1.0;
            diagnostics.Add("(interface.scaling) : error : The value must be greater than 0.");
        }

        if (PositionOffset < 0.0)
        {
            PositionOffset = 40.0;
            diagnostics.Add(
                "(interface.position_offset) : error : The value must be greater than "
                    + "or equal to 0."
            );
        }

        if (HideDelay <= 0.0)
        {
            HideDelay = 2.0;
            diagnostics.Add("(interface.hide_delay) : error : The value must be greater than 0.");
        }

        if (FadeOutTime < 0.0)
        {
            FadeOutTime = 0.75;
            diagnostics.Add(
                "(interface.fade_out_time) : error : The value must be greater than or equal to 0."
            );
        }
    }
}
