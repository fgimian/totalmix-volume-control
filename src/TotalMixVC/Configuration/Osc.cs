﻿namespace TotalMixVC.Configuration;

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
