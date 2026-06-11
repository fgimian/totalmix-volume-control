using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json.Serialization;

namespace TotalMixVC.Configuration;

/// <summary>Provides configuration related to OSC communication with the device.</summary>
public record Osc
{
    public Osc()
    {
        OutgoingEndPoint = IPEndPoint.Parse(RawOutgoingEndPoint);
        IncomingEndPoint = IPEndPoint.Parse(RawIncomingEndPoint);
    }

    /// <summary>
    /// Gets or sets the raw endpoint to send volume changes to. The port should match the
    /// "Port incoming" setting in TotalMixFX.
    /// </summary>
    [JsonPropertyName("outgoing_endpoint")]
    public string RawOutgoingEndPoint { get; set; } = "127.0.0.1:7001";

    /// <summary>
    /// Gets or sets the raw endpoint to receive volume changes from. This address should match the
    /// "Remote Controller Address" and should typically be "127.0.0.1". The port should match the
    /// "Port outgoing" setting in TotalMixFX.
    /// </summary>
    [JsonPropertyName("incoming_endpoint")]
    public string RawIncomingEndPoint { get; set; } = "127.0.0.1:9001";

    /// <summary>Gets the endpoint to send volume changes to.</summary>
    [JsonIgnore]
    public IPEndPoint OutgoingEndPoint { get; private set; }

    /// <summary>Gets the endpoint to receive volume changes from.</summary>
    [JsonIgnore]
    public IPEndPoint IncomingEndPoint { get; private set; }

    /// <summary>
    /// Parses raw properties to ensure they are valid and updates properties intended for use
    /// by the application.
    /// </summary>
    /// <param name="diagnostics">
    /// Error diagnostics recorded for properties which were not valid.
    /// </param>
    public void ParseAndValidate(Collection<string> diagnostics)
    {
        if (!IPEndPoint.TryParse(RawOutgoingEndPoint, out var outgoingEndPoint))
        {
            RawOutgoingEndPoint = "127.0.0.1:7001";
            diagnostics.Add(
                "(osc.outgoing_endpoint) : error : An invalid endpoint address was specified."
            );
        }
        else
        {
            OutgoingEndPoint = outgoingEndPoint;
        }

        if (!IPEndPoint.TryParse(RawIncomingEndPoint, out var incomingEndPoint))
        {
            RawIncomingEndPoint = "127.0.0.1:9001";
            diagnostics.Add(
                "(osc.incoming_endpoint) : error : An invalid endpoint address was specified."
            );
        }
        else
        {
            IncomingEndPoint = incomingEndPoint;
        }
    }
}
