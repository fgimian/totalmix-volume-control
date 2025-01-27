namespace TotalMixVC.Configuration;

/// <summary>Provides configuration related the behaviour of the widget user interface.</summary>
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
    /// Gets or sets the number of second which the widget will take to fade out after hide delay.
    /// </summary>
    public double FadeOutTime { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets a value indicating whether the widget should be shown when remote volume
    /// changes are detected. Please note that the device seems to send volume changes at some
    /// random times which is why this setting is disabled by default.
    /// </summary>
    public bool ShowRemoteVolumeChanges { get; set; }
}
