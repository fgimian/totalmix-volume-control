using System.Windows.Media;

namespace TotalMixVC.Configuration;

/// <summary>Provides configuration related to the theme of the widget.</summary>
public record Theme
{
    /// <summary>
    /// Gets or sets the background corner rounding of the widget and tray tooltip.
    /// </summary>
    public double BackgroundRounding { get; set; } = 1.0;

    /// <summary>Gets or sets the background color of the widget and tray tooltip.</summary>
    public Color BackgroundColor { get; set; } =
        (Color)ColorConverter.ConvertFromString("#e21e2328")!;

    /// <summary>
    /// Gets or sets the color of the "TotalMix" heading text on the widget and tray tooltip.
    /// </summary>
    public Color HeadingTotalmixColor { get; set; } =
        (Color)ColorConverter.ConvertFromString("#ffffff")!;

    /// <summary>
    /// Gets or sets the color of the "Volume" heading text on the widget and tray tooltip.
    /// </summary>
    public Color HeadingVolumeColor { get; set; } =
        (Color)ColorConverter.ConvertFromString("#e06464")!;

    /// <summary>Gets or sets the color of the decibel readout text on the widget.</summary>
    public Color VolumeReadoutColorNormal { get; set; } =
        (Color)ColorConverter.ConvertFromString("#ffffff")!;

    /// <summary>
    /// Gets or sets the color of the decibel readout text on the widget when the volume is dimmed.
    /// </summary>
    public Color VolumeReadoutColorDimmed { get; set; } =
        (Color)ColorConverter.ConvertFromString("#ffa500")!;

    /// <summary>Gets or sets the background color of volume bar on the widget.</summary>
    public Color VolumeBarBackgroundColor { get; set; } =
        (Color)ColorConverter.ConvertFromString("#333333")!;

    /// <summary>
    /// Gets or sets the current reading foreground color of volume bar on the widget.
    /// </summary>
    public Color VolumeBarForegroundColorNormal { get; set; } =
        (Color)ColorConverter.ConvertFromString("#999999")!;

    /// <summary>
    /// Gets or sets the current reading foreground color of volume bar on the widget when the
    /// volume is dimmed.
    /// </summary>
    public Color VolumeBarForegroundColorDimmed { get; set; } =
        (Color)ColorConverter.ConvertFromString("#996500")!;

    /// <summary>
    /// Gets or sets the foreground color of message text on the tray tooltip.
    /// </summary>
    public Color TrayTooltipMessageColor { get; set; } =
        (Color)ColorConverter.ConvertFromString("#ffffff")!;
}
