namespace TotalMixVC.Configuration;

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
