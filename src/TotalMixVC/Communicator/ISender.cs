using OscCore;

namespace TotalMixVC.Communicator;

/// <summary>
/// Interface defining a UDP packet sender for Open Source Control (OSC).
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends an OSC packet to the configured endpoint.
    /// </summary>
    /// <param name="message">
    /// The <see cref="OscBundle"/> or <see cref="OscMessage"/> message to send.
    /// </param>
    /// <returns>The number of bytes sent to the endpoint.</returns>
    Task<int> SendAsync(OscPacket message);
}
