using System.Net;
using System.Windows.Media;
using Tomlyn;
using Tomlyn.Syntax;

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

    /// <summary>
    /// Parses TOML configuration text into a Config instance ensuring appropriate conversions and
    /// validation are performed.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="config">The output config model.</param>
    /// <param name="diagnostics">The diagnostics if this method returns false.</param>
    /// <returns>Whether or not the config was parsed successfully.</returns>
    public static bool TryFromToml(string text, out Config? config, out DiagnosticsBag? diagnostics)
    {
        var isValid = Toml.TryToModel(
            text,
            out config,
            out diagnostics,
            options: new TomlModelOptions()
            {
                ConvertToModel = (value, type) =>
                    value switch
                    {
                        string color when type == typeof(Color) => (Color)
                            ColorConverter.ConvertFromString(color)!,
                        string address when type == typeof(IPEndPoint) => IPEndPoint.Parse(address),
                        _ => null,
                    },
            }
        );

        if (config is not null)
        {
            config.Theme.BackgroundRounding = Math.Max(config.Theme.BackgroundRounding, 0.0);
            config.Interface.Scaling = Math.Max(config.Interface.Scaling, double.Epsilon);
            config.Interface.PositionOffset = Math.Max(config.Interface.PositionOffset, 0.0);
            config.Interface.HideDelay = Math.Max(config.Interface.HideDelay, double.Epsilon);
            config.Interface.FadeOutTime = Math.Max(config.Interface.FadeOutTime, 0.0);
        }

        return isValid;
    }
}
