using System.Net;
using System.Runtime.Serialization;

namespace TotalMixVC.Configuration;

/// <summary>Provides configuration related to OSC communication with the device.</summary>
public record Osc
{
    /// <summary>
    /// Gets or sets the endpoint to send volume changes to. The port should match the
    /// "Port incoming" setting in TotalMixFX.
    /// </summary>
    [DataMember(Name = "outgoing_endpoint")]
    public IPEndPoint OutgoingEndPoint { get; set; } = new IPEndPoint(IPAddress.Loopback, 7001);

    /// <summary>
    /// Gets or sets the endpoint to receive volume changes from. This address should match the
    /// "Remote Controller Address" and should typically be "127.0.0.1". The port should match the
    /// "Port outgoing" setting in TotalMixFX.
    /// </summary>
    [DataMember(Name = "incoming_endpoint")]
    public IPEndPoint IncomingEndPoint { get; set; } = new IPEndPoint(IPAddress.Loopback, 9001);
}
