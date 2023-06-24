namespace TotalMixVC.Configuration;

/// <summary>Provides configuration related to the theme of the widget.</summary>
public record Theme
{
    /// <summary>Gets the background corner rounding of the widget and tray tooltip.</summary>
    public double BackgroundRounding { get; init; } = 1.0;

    /// <summary>Gets the background color of the widget and tray tooltip.</summary>
    public string BackgroundColor { get; init; } = "#e21e2328";

    /// <summary>
    /// Gets the color of the "TotalMix" heading text on the widget and tray tooltip.
    /// </summary>
    public string HeadingTotalmixColor { get; init; } = "#ffffff";

    /// <summary>
    /// Gets the color of the "Volume" heading text on the widget and tray tooltip.
    /// </summary>
    public string HeadingVolumeColor { get; init; } = "#e06464";

    /// <summary>Gets the color of the decibel readout text on the widget.</summary>
    public string VolumeReadoutColorNormal { get; init; } = "#ffffff";

    /// <summary>
    /// Gets the color of the decibel readout text on the widget when the volume is dimmed.
    /// </summary>
    public string VolumeReadoutColorDimmed { get; init; } = "#ffa500";

    /// <summary>Gets the background color of volume bar on the widget.</summary>
    public string VolumeBarBackgroundColor { get; init; } = "#333333";

    /// <summary>Gets the current reading foreground color of volume bar on the widget.</summary>
    public string VolumeBarForegroundColorNormal { get; init; } = "#999999";

    /// <summary>
    /// Gets the current reading foreground color of volume bar on the widget when the volume is
    /// dimmed.
    /// </summary>
    public string VolumeBarForegroundColorDimmed { get; init; } = "#996500";

    /// <summary>
    /// Gets the foreground color of message text on the tray tooltip.
    /// </summary>
    public string TrayTooltipMessageColor { get; init; } = "#ffffff";
}
