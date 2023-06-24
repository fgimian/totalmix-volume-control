using OscCore;

namespace TotalMixVC.Communicator;

/// <summary>
/// Interface defining a UDP receiver for Open Source Control (OSC) traffic.
/// </summary>
public interface IListener
{
    /// <summary>
    /// Receives an OSC packet from the endpoint configured.
    /// </summary>
    /// <returns>
    /// An OSC packet which may be either a <see cref="OscBundle"/> or <see cref="OscMessage"/>.
    /// </returns>
    Task<OscPacket> ReceiveAsync();
}
