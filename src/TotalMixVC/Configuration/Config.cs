using System.Collections.ObjectModel;
using System.Text.Json;
using Tomlyn;

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

    /// <summary>Gets configuration related to the behaviour of the widget user interface.</summary>
    public Interface Interface { get; init; } = new Interface();

    /// <summary>
    /// Parses TOML configuration text into a Config instance ensuring appropriate conversions and
    /// validation are performed.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="diagnostics">Any diagnostics recorded due to issues in the config.</param>
    /// <returns>The resulting config model.</returns>
    public static Config FromToml(string text, Collection<string> diagnostics)
    {
        try
        {
            var config = TomlSerializer.Deserialize<Config>(
                text,
                options: new TomlSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                }
            )!;

            config.ParseAndValidate(diagnostics);
            return config;
        }
        catch (TomlException ex)
        {
            foreach (var diagnostic in ex.Diagnostics)
            {
                diagnostics.Add(diagnostic.ToString());
            }

            return new Config();
        }
    }

    /// <summary>
    /// Parses raw properties to ensure they are valid and updates properties intended for use
    /// by the application.
    /// </summary>
    /// <param name="diagnostics">
    /// Error diagnostics recorded for properties which were not valid.
    /// </param>
    public void ParseAndValidate(Collection<string> diagnostics)
    {
        Osc.ParseAndValidate(diagnostics);
        Volume.Validate(diagnostics);
        Theme.ParseAndValidate(diagnostics);
        Interface.Validate(diagnostics);
    }
}
