using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using OscCore;

namespace TotalMixVC.Communicator;

/// <summary>
/// Provides a UDP receiver for Open Source Control (OSC) traffic.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Listener"/> class.
/// </remarks>
/// <param name="localEP">The endpoint to receive OSC data from.</param>
[ExcludeFromCodeCoverage]
public class Listener(IPEndPoint localEP) : IListener, IDisposable
{
    private readonly UdpClient _client = new(localEP);

    private bool _disposed;

    /// <summary>
    /// Disposes the current listener.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Receives an OSC packet from the endpoint configured.
    /// </summary>
    /// <param name="cancellationTokenSource">
    /// An optional cancellation source that will cancel any receive requests which are in progress.
    /// </param>
    /// <returns>
    /// An OSC packet which may be either a <see cref="OscBundle"/> or <see cref="OscMessage"/>.
    /// </returns>
    public async Task<OscPacket> ReceiveAsync(
        CancellationTokenSource? cancellationTokenSource = null
    )
    {
        UdpReceiveResult result;
        if (cancellationTokenSource is not null)
        {
            result = await _client
                .ReceiveAsync(cancellationTokenSource.Token)
                .ConfigureAwait(false);
        }
        else
        {
            result = await _client.ReceiveAsync().ConfigureAwait(false);
        }

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
