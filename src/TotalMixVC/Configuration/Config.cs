﻿using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Tomlyn;
using Tomlyn.Syntax;
using TotalMixVC.Configuration.Models;

namespace TotalMixVC.Configuration;

/// <summary>
/// Provides all configurable settings for the application along with suitable defaults.
/// </summary>
public partial record Config
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
                        double increment when type == typeof(VolumeIncrementPercent) =>
                            new VolumeIncrementPercent((float)increment),
                        double increment when type == typeof(VolumeFineIncrementPercent) =>
                            new VolumeFineIncrementPercent((float)increment),
                        double max when type == typeof(VolumeMaxPercent) => new VolumeMaxPercent(
                            (float)max
                        ),
                        double increment when type == typeof(VolumeIncrementDecibels) =>
                            new VolumeIncrementDecibels((float)increment),
                        double increment when type == typeof(VolumeFineIncrementDecibels) =>
                            new VolumeFineIncrementDecibels((float)increment),
                        double max when type == typeof(VolumeMaxDecibels) => new VolumeMaxDecibels(
                            (float)max
                        ),
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

    /// <summary>
    /// Consolidates diagnostics returned by Tomlyn in an attempt to have one clean message
    /// for each property issue discovered.
    /// </summary>
    /// <param name="diagnostics">
    /// The diagnostics bag returned by Tomlyn upon parsing a TOML file with errors or warnings.
    /// </param>
    /// <returns>A consolidated iterable of formatted diagnostic messages.</returns>
    public static IEnumerable<string> CleanDiagnostics(DiagnosticsBag diagnostics)
    {
        var diagnosticsExceptionRegex = DiagnosticsExceptionRegex();

        foreach (var group in diagnostics.GroupBy(diagnostic => diagnostic.Span))
        {
            var reason = (string?)null;
            var span = group.Key;

            foreach (var diagnostic in group)
            {
                if (
                    diagnostic.Message.StartsWith(
                        "Unsupported type to convert ",
                        StringComparison.Ordinal
                    )
                )
                {
                    continue;
                }

                var match = diagnosticsExceptionRegex.Match(diagnostic.Message);
                if (match.Success)
                {
                    reason = match.Groups[1].Value;
                    continue;
                }

                var message = new StringBuilder();

                message
                    .Append(span.ToStringSimple())
                    .Append(" : ")
                    .Append(diagnostic.Kind == DiagnosticMessageKind.Warning ? "warning" : "error")
                    .Append(" : ")
                    .Append(diagnostic.Message);

                if (!diagnostic.Message.EndsWith('.'))
                {
                    message.Append('.');
                }

                if (reason is not null)
                {
                    message.Append(' ').Append(reason);
                }

                yield return message.ToString();
            }
        }
    }

    [GeneratedRegex(@"Exception while trying to convert \S+ to type \S+. Reason: (.*)")]
    private static partial Regex DiagnosticsExceptionRegex();
}
