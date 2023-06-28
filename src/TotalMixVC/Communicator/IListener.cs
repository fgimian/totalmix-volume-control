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
    /// <param name="cancellationTokenSource">
    /// An optional cancellation source that will cancel any receive requests which are in progress.
    /// </param>
    /// <returns>
    /// An OSC packet which may be either a <see cref="OscBundle"/> or <see cref="OscMessage"/>.
    /// </returns>
    Task<OscPacket> ReceiveAsync(CancellationTokenSource? cancellationTokenSource = null);
}
