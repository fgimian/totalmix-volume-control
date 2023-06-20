namespace TotalMixVC;

/// <summary>Provides configuration related to OSC communication with the device.</summary>
public record Osc
{
    /// <summary>Gets the hostname to send volume changes to.</summary>
    public string OutgoingHostname { get; init; } = "127.0.0.1";

    /// <summary>
    /// Gets the port to use when sending volume changes. This should match the "Port incoming"
    /// setting in TotalMixFX.
    /// </summary>
    public ushort OutgoingPort { get; init; } = 7001;

    /// <summary>Gets the hostname to receive volume changes from. This should match the
    /// "Remote Controller Address" and should typically be "127.0.0.1".</summary>
    public string IncomingHostname { get; init; } = "127.0.0.1";

    /// <summary>
    /// Gets the port to use when receiving volume changes. This should match the "Port outgoing"
    /// setting in TotalMixFX.
    /// </summary>
    public ushort IncomingPort { get; init; } = 9001;
}

/// <summary>Provides configuration related to volume changes on the device.</summary>
public record Volume
{
    /// <summary>
    /// Gets the increment that is to be used when adjusting the volume. The volume ranges from
    /// 0.0 and 1.0 and thus the max allowed increment is 0.10 to avoid major jumps in volume.
    /// </summary>
    public float Increment { get; init; } = 0.02f;

    /// <summary>
    /// Gets the fineincrement that is to be used when adjusting the volume and holding the Shift
    /// key. The volume ranges from 0.0 and 1.0 and thus the max allowed fine increment is 0.05 to
    /// avoid major jumps in volume.
    /// </summary>
    public float FineIncrement { get; init; } = 0.01f;

    /// <summary>
    /// Gets the maximum volume that will be sent by the application where 1.0 is the loudest
    /// volume the device can receive.
    /// </summary>
    public float Max { get; init; } = 1.0f;
}

/// <summary>Provides configuration related to the theme of the widget.</summary>
public record Theme
{
    /// <summary>Gets the background corner rounding of the widget.</summary>
    public double BackgroundRounding { get; init; } = 1.0;

    /// <summary>Gets the background color of the widget.</summary>
    public string BackgroundColor { get; init; } = "#e21e2328";

    /// <summary>
    /// Gets the color of the "TotalMix" heading text on the widget.
    /// </summary>
    public string HeadingTotalmixColor { get; init; } = "#ffffff";

    /// <summary>Gets the color of the "Volume" heading text on the widget.</summary>
    public string HeadingVolumeColor { get; init; } = "#e06464";

    /// <summary>Gets the color of the decibel readout text on the widget.</summary>
    public string VolumeReadoutColorNormal { get; init; } = "#ffffff";

    /// <summary>
    /// Gets the color of the decibel readout text on the widget when the volume is dimmed.
    /// </summary>
    public string VolumeReadoutColorDimmed { get; init; } = "#ffa500";

    /// <summary>Gets the background color of volume bar on the widget.</summary>
    public string VolumeBarBackgroundColor { get; init; } = "#333333";

    /// <summary>Gets the foreground color of volume bar on the widget.</summary>
    public string VolumeBarForegroundColorNormal { get; init; } = "#999999";

    /// <summary>
    /// Gets the foreground color of volume bar on the widget when the volume is dimmed.
    /// </summary>
    public string VolumeBarForegroundColorDimmed { get; init; } = "#996500";
}

/// <summary>Provides configuration related the behaviour of the widget user interface.</summary>
public record Interface
{
    /// <summary>Gets the UI scaling of the widget where 1.0 is a normal 100% scale.</summary>
    public double Scaling { get; init; } = 1.0;

    /// <summary>
    /// Gets both the horizontal and vertical offset in pixels from the top left of the screen
    /// where the widget will appear.
    /// </summary>
    public double PositionOffset { get; init; } = 40.0;

    /// <summary>
    /// Gets the number of seconds before the widget begins to fade away after it has appeared.
    /// </summary>
    public double HideDelay { get; init; } = 2.0;

    /// <summary>
    /// Gets the number of second which the widget will take to fade out after hide delay.
    /// </summary>
    public double FadeOutTime { get; init; } = 1.0;

    /// <summary>
    /// Gets a value indicating whether the widget should be shown when remote volume changes are
    /// detected. Please note that the device seems to send volume changes at some random times
    /// which is why this setting is disabled by default.
    /// </summary>
    public bool ShowRemoteVolumeChanges { get; init; }
}

/// <summary>
/// Provides all configurable settings for the application along with suitable defaults.
/// </summary>
public record Config
{
    /// <summary>Gets configuration related to OSC communication with the device.</summary>
    public Osc Osc { get; init; } = new Osc();

    /// <summary>Gets configuration related to volume changes on the device.</summary>
    public Volume Volume { get; init; } = new Volume();

    /// <summary>Gets configuration related to the theme of the widget.</summary>
    public Theme Theme { get; init; } = new Theme();

    /// <summary>Gets configuration related the behaviour of the widget user interface.</summary>
    public Interface Interface { get; init; } = new Interface();
}
