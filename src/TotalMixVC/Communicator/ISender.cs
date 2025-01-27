using System.Net;
using OscCore;

namespace TotalMixVC.Communicator;

/// <summary>
/// Interface defining a UDP packet sender for Open Source Control (OSC).
/// </summary>
public interface ISender
{
    /// <summary>Gets or sets the outgoing OSC endpoint to send volume changes to.</summary>
    public IPEndPoint LocalEP { get; set; }

    /// <summary>
    /// Sends an OSC packet to the configured endpoint.
    /// </summary>
    /// <param name="message">
    /// The <see cref="OscBundle"/> or <see cref="OscMessage"/> message to send.
    /// </param>
    /// <returns>The number of bytes sent to the endpoint.</returns>
    Task<int> SendAsync(OscPacket message);
}
