using System.Text.Json.Serialization;
using System.Windows.Media;
using TotalMixVC.Configuration.Converters;

namespace TotalMixVC.Configuration;

/// <summary>Provides configuration related to the theme of the widget.</summary>
public record Theme
{
    private static readonly BrushConverter s_converter = new();

    /// <summary>Gets the background corner rounding of the widget and tray tooltip.</summary>
    [JsonConverter(typeof(NonNegativeDoubleConverter))]
    public double BackgroundRounding { get; init; } = 1.0;

    /// <summary>Gets the background color of the widget and tray tooltip.</summary>
    [JsonConverter(typeof(SolidColorBrushConverter))]
    public SolidColorBrush BackgroundColor { get; init; } =
        (SolidColorBrush)s_converter.ConvertFromString("#e21e2328")!;

    /// <summary>
    /// Gets the color of the "TotalMix" heading text on the widget and tray tooltip.
    /// </summary>
    [JsonConverter(typeof(SolidColorBrushConverter))]
    public SolidColorBrush HeadingTotalmixColor { get; init; } =
        (SolidColorBrush)s_converter.ConvertFromString("#ffffff")!;

    /// <summary>
    /// Gets the color of the "Volume" heading text on the widget and tray tooltip.
    /// </summary>
    [JsonConverter(typeof(SolidColorBrushConverter))]
    public SolidColorBrush HeadingVolumeColor { get; init; } =
        (SolidColorBrush)s_converter.ConvertFromString("#e06464")!;

    /// <summary>Gets the color of the decibel readout text on the widget.</summary>
    [JsonConverter(typeof(SolidColorBrushConverter))]
    public SolidColorBrush VolumeReadoutColorNormal { get; init; } =
        (SolidColorBrush)s_converter.ConvertFromString("#ffffff")!;

    /// <summary>
    /// Gets the color of the decibel readout text on the widget when the volume is dimmed.
    /// </summary>
    [JsonConverter(typeof(SolidColorBrushConverter))]
    public SolidColorBrush VolumeReadoutColorDimmed { get; init; } =
        (SolidColorBrush)s_converter.ConvertFromString("#ffa500")!;

    /// <summary>Gets the background color of volume bar on the widget.</summary>
    [JsonConverter(typeof(SolidColorBrushConverter))]
    public SolidColorBrush VolumeBarBackgroundColor { get; init; } =
        (SolidColorBrush)s_converter.ConvertFromString("#333333")!;

    /// <summary>Gets the current reading foreground color of volume bar on the widget.</summary>
    [JsonConverter(typeof(SolidColorBrushConverter))]
    public SolidColorBrush VolumeBarForegroundColorNormal { get; init; } =
        (SolidColorBrush)s_converter.ConvertFromString("#999999")!;

    /// <summary>
    /// Gets the current reading foreground color of volume bar on the widget when the volume is
    /// dimmed.
    /// </summary>
    [JsonConverter(typeof(SolidColorBrushConverter))]
    public SolidColorBrush VolumeBarForegroundColorDimmed { get; init; } =
        (SolidColorBrush)s_converter.ConvertFromString("#996500")!;

    /// <summary>
    /// Gets the foreground color of message text on the tray tooltip.
    /// </summary>
    [JsonConverter(typeof(SolidColorBrushConverter))]
    public SolidColorBrush TrayTooltipMessageColor { get; init; } =
        (SolidColorBrush)s_converter.ConvertFromString("#ffffff")!;
}
