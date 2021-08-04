using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using OscCore;

namespace TotalMixVC.Communicator
{
    /// <summary>
    /// Provides a UDP packet sender for Open Source Control (OSC).
    /// </summary>
    public class Sender : IDisposable
    {
        private readonly UdpClient _client;

        private readonly IPEndPoint _localEP;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sender"/> class.
        /// </summary>
        /// <param name="localEP">The endpoint to receive OSC data from.</param>
        public Sender(IPEndPoint localEP)
        {
            _localEP = localEP;
            _client = new UdpClient();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Sender"/> class.
        /// </summary>
        ~Sender()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the current sender.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the current sender.
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

        /// <summary>
        /// Sends an OSC packet to the configured endpoint.
        /// </summary>
        /// <param name="message">
        /// The <see cref="OscBundle"/> or <see cref="OscMessage"/> message to send.
        /// </param>
        /// <returns>The number of bytes sent to the endpoint.</returns>
        public async Task<int> Send(OscPacket message)
        {
            byte[] datagram = message.ToByteArray();
            return await _client
                .SendAsync(datagram, datagram.Length, _localEP)
                .ConfigureAwait(false);
        }
    }
}
