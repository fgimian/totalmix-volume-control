using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace TotalMixVC.Configuration;

/// <summary>Provides configuration related to the theme of the widget.</summary>
public record Theme
{
    public Theme()
    {
        HeadingTotalmixColor = (Color)ColorConverter.ConvertFromString(RawHeadingTotalmixColor);
        HeadingVolumeColor = (Color)ColorConverter.ConvertFromString(RawHeadingVolumeColor);
        VolumeReadoutColorNormal = (Color)
            ColorConverter.ConvertFromString(RawVolumeReadoutColorNormal);
        VolumeReadoutColorDimmed = (Color)
            ColorConverter.ConvertFromString(RawVolumeReadoutColorDimmed);
        VolumeBarBackgroundColor = (Color)
            ColorConverter.ConvertFromString(RawVolumeBarBackgroundColor);
        VolumeBarForegroundColorNormal = (Color)
            ColorConverter.ConvertFromString(RawVolumeBarForegroundColorNormal);
        VolumeBarForegroundColorDimmed = (Color)
            ColorConverter.ConvertFromString(RawVolumeBarForegroundColorDimmed);
        TrayTooltipMessageColor = (Color)
            ColorConverter.ConvertFromString(RawTrayTooltipMessageColor);
    }

    /// <summary>
    /// Gets or sets the background corner rounding of the widget and tray tooltip.
    /// </summary>
    public double BackgroundRounding { get; set; } = 1.0;

    /// <summary>Gets or sets the raw background color of the widget and tray tooltip.</summary>
    [JsonPropertyName("background_color")]
    public string RawBackgroundColor { get; set; } = "#e21e2328";

    /// <summary>
    /// Gets or sets the raw color of the "TotalMix" heading text on the widget and tray tooltip.
    /// </summary>
    [JsonPropertyName("heading_totalmix_color")]
    public string RawHeadingTotalmixColor { get; set; } = "#ffffff";

    /// <summary>
    /// Gets or sets the raw color of the "Volume" heading text on the widget and tray tooltip.
    /// </summary>
    [JsonPropertyName("heading_volume_color")]
    public string RawHeadingVolumeColor { get; set; } = "#e06464";

    /// <summary>Gets or sets the raw color of the decibel readout text on the widget.</summary>
    [JsonPropertyName("volume_readout_color_normal")]
    public string RawVolumeReadoutColorNormal { get; set; } = "#ffffff";

    /// <summary>
    /// Gets or sets the raw color of the decibel readout text on the widget when the volume is
    /// dimmed.
    /// </summary>
    [JsonPropertyName("volume_readout_color_dimmed")]
    public string RawVolumeReadoutColorDimmed { get; set; } = "#ffa500";

    /// <summary>Gets or sets the raw background color of volume bar on the widget.</summary>
    [JsonPropertyName("volume_bar_background_color")]
    public string RawVolumeBarBackgroundColor { get; set; } = "#333333";

    /// <summary>
    /// Gets or sets the raw current reading foreground color of volume bar on the widget.
    /// </summary>
    [JsonPropertyName("volume_bar_foreground_color_normal")]
    public string RawVolumeBarForegroundColorNormal { get; set; } = "#999999";

    /// <summary>
    /// Gets or sets the raw current reading foreground color of volume bar on the widget when the
    /// volume is dimmed.
    /// </summary>
    [JsonPropertyName("volume_bar_foreground_color_dimmed")]
    public string RawVolumeBarForegroundColorDimmed { get; set; } = "#996500";

    /// <summary>
    /// Gets or sets the raw foreground color of message text on the tray tooltip.
    /// </summary>
    [JsonPropertyName("tray_tooltip_message_color")]
    public string RawTrayTooltipMessageColor { get; set; } = "#ffffff";

    /// <summary>Gets the background color of the widget and tray tooltip.</summary>
    [JsonIgnore]
    public Color BackgroundColor { get; private set; } =
        (Color)ColorConverter.ConvertFromString("#e21e2328");

    /// <summary>
    /// Gets the color of the "TotalMix" heading text on the widget and tray tooltip.
    /// </summary>
    [JsonIgnore]
    public Color HeadingTotalmixColor { get; private set; }

    /// <summary>
    /// Gets the color of the "Volume" heading text on the widget and tray tooltip.
    /// </summary>
    [JsonIgnore]
    public Color HeadingVolumeColor { get; private set; }

    /// <summary>Gets the color of the decibel readout text on the widget.</summary>
    [JsonIgnore]
    public Color VolumeReadoutColorNormal { get; private set; }

    /// <summary>
    /// Gets the color of the decibel readout text on the widget when the volume is dimmed.
    /// </summary>
    [JsonIgnore]
    public Color VolumeReadoutColorDimmed { get; private set; }

    /// <summary>Gets the background color of volume bar on the widget.</summary>
    [JsonIgnore]
    public Color VolumeBarBackgroundColor { get; private set; }

    /// <summary>Gets the current reading foreground color of volume bar on the widget.</summary>
    [JsonIgnore]
    public Color VolumeBarForegroundColorNormal { get; private set; }

    /// <summary>
    /// Gets the current reading foreground color of volume bar on the widget when the volume is
    /// dimmed.
    /// </summary>
    [JsonIgnore]
    public Color VolumeBarForegroundColorDimmed { get; private set; }

    /// <summary>Gets the foreground color of message text on the tray tooltip.</summary>
    [JsonIgnore]
    public Color TrayTooltipMessageColor { get; private set; }

    /// <summary>
    /// Parses raw properties to ensure they are valid and updates properties intended for use
    /// by the application.
    /// </summary>
    /// <param name="diagnostics">
    /// Error diagnostics recorded for properties which were not valid.
    /// </param>
    public void ParseAndValidate(Collection<string> diagnostics)
    {
        if (BackgroundRounding < 0.0)
        {
            BackgroundRounding = 1.0;
            diagnostics.Add(
                "(theme.background_rounding) : error : The value must be greater than or equal "
                    + "to 0."
            );
        }

        try
        {
            BackgroundColor = (Color)ColorConverter.ConvertFromString(RawBackgroundColor);
        }
        catch (FormatException)
        {
            RawBackgroundColor = "#e21e2328";
            diagnostics.Add("(theme.background_color) : error : The color specified was invalid.");
        }

        try
        {
            HeadingTotalmixColor = (Color)ColorConverter.ConvertFromString(RawHeadingTotalmixColor);
        }
        catch (FormatException)
        {
            RawHeadingTotalmixColor = "#ffffff";
            diagnostics.Add(
                "(theme.heading_totalmix_color) : error : The color specified was invalid."
            );
        }

        try
        {
            HeadingVolumeColor = (Color)ColorConverter.ConvertFromString(RawHeadingVolumeColor);
        }
        catch (FormatException)
        {
            RawHeadingVolumeColor = "#e06464";
            diagnostics.Add(
                "(theme.heading_volume_color) : error : The color specified was invalid."
            );
        }

        try
        {
            VolumeReadoutColorNormal = (Color)
                ColorConverter.ConvertFromString(RawVolumeReadoutColorNormal);
        }
        catch (FormatException)
        {
            RawVolumeReadoutColorNormal = "#ffffff";
            diagnostics.Add(
                "(theme.volume_readout_color_normal) : error : The color specified was invalid."
            );
        }

        try
        {
            VolumeReadoutColorDimmed = (Color)
                ColorConverter.ConvertFromString(RawVolumeReadoutColorDimmed);
        }
        catch (FormatException)
        {
            RawVolumeReadoutColorDimmed = "#ffa500";
            diagnostics.Add(
                "(theme.volume_readout_color_dimmed) : error : The color specified was invalid."
            );
        }

        try
        {
            VolumeBarBackgroundColor = (Color)
                ColorConverter.ConvertFromString(RawVolumeBarBackgroundColor);
        }
        catch (FormatException)
        {
            RawVolumeBarBackgroundColor = "#333333";
            diagnostics.Add(
                "(theme.volume_bar_background_color) : error : The color specified was invalid."
            );
        }

        try
        {
            VolumeBarForegroundColorNormal = (Color)
                ColorConverter.ConvertFromString(RawVolumeBarForegroundColorNormal);
        }
        catch (FormatException)
        {
            RawVolumeBarForegroundColorNormal = "#999999";
            diagnostics.Add(
                "(theme.volume_bar_foreground_color_normal) : error : The color specified was "
                    + "invalid."
            );
        }

        try
        {
            VolumeBarForegroundColorDimmed = (Color)
                ColorConverter.ConvertFromString(RawVolumeBarForegroundColorDimmed);
        }
        catch (FormatException)
        {
            RawVolumeBarForegroundColorDimmed = "#996500";
            diagnostics.Add(
                "(theme.volume_bar_foreground_color_dimmed) : error : The color specified was invalid."
            );
        }

        try
        {
            TrayTooltipMessageColor = (Color)
                ColorConverter.ConvertFromString(RawTrayTooltipMessageColor);
        }
        catch (FormatException)
        {
            RawTrayTooltipMessageColor = "#ffffff";
            diagnostics.Add(
                "(theme.tray_tooltip_message_color) : error : The color specified was invalid."
            );
        }
    }
}
