namespace TotalMixVC.Communicator;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using OscCore;

/// <summary>
/// Provides a UDP receiver for Open Source Control (OSC) traffic.
/// </summary>
[ExcludeFromCodeCoverage]
public class Listener : IListener, IDisposable
{
    private readonly UdpClient _client;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Listener"/> class.
    /// </summary>
    /// <param name="localEP">The endpoint to receive OSC data from.</param>
    public Listener(IPEndPoint localEP)
    {
        _client = new UdpClient(localEP);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="Listener"/> class.
    /// </summary>
    ~Listener()
    {
        Dispose(false);
    }

    /// <summary>
    /// Disposes the current listener.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Receives an OSC packet from the endpoint configured.
    /// </summary>
    /// <returns>
    /// An OSC packet which may be either a <see cref="OscBundle"/> or <see cref="OscMessage"/>.
    /// </returns>
    public async Task<OscPacket> ReceiveAsync()
    {
        UdpReceiveResult result = await _client.ReceiveAsync().ConfigureAwait(false);
        return OscPacket.Read(result.Buffer, 0, result.Buffer.Length);
    }

    /// <summary>
    /// Disposes the current listener.
    /// </summary>
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
