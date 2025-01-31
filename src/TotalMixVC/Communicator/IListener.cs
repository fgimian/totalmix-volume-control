using System.Net;
using System.Net.Sockets;
using OscCore;

namespace TotalMixVC.Communicator;

/// <summary>
/// Interface defining a UDP receiver for Open Source Control (OSC) traffic.
/// </summary>
public interface IListener
{
    /// <summary>Gets the incoming OSC endpoint to receive volume changes from.</summary>
    IPEndPoint EP { get; }

    /// <summary>
    /// Receives an OSC packet from the endpoint configured.
    /// </summary>
    /// <param name="cancellationTokenSource">
    /// An optional cancellation source that will cancel any receive requests which are in progress.
    /// </param>
    /// <returns>
    /// An OSC packet which may be either a <see cref="OscBundle"/> or <see cref="OscMessage"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// The underlying <see cref="Socket"/> has been closed.
    /// </exception>
    /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
    Task<OscPacket> ReceiveAsync(CancellationTokenSource? cancellationTokenSource = null);
}
