using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using OscCore;

namespace TotalMixVC.Communicator;

/// <summary>
/// Provides a UDP packet sender for Open Source Control (OSC).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Sender"/> class.
/// </remarks>
/// <param name="localEP">The endpoint to receive OSC data from.</param>
[ExcludeFromCodeCoverage]
public class Sender(IPEndPoint localEP) : ISender, IDisposable
{
    private readonly UdpClient _client = new();

    private bool _disposed;

    /// <summary>Gets or sets the outgoing OSC endpoint to send volume changes to.</summary>
    public IPEndPoint LocalEP { get; set; } = localEP;

    /// <summary>Disposes the current sender.</summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Sends an OSC packet to the configured endpoint.
    /// </summary>
    /// <param name="message">
    /// The <see cref="OscBundle"/> or <see cref="OscMessage"/> message to send.
    /// </param>
    /// <returns>The number of bytes sent to the endpoint.</returns>
    public Task<int> SendAsync(OscPacket message)
    {
        var datagram = message.ToByteArray();
        return _client.SendAsync(datagram, datagram.Length, LocalEP);
    }

    /// <summary>Disposes the current sender.</summary>
    /// <param name="disposing">Whether managed resources should be disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _client.Dispose();
        }

        _disposed = true;
    }
}
