using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using OscCore;

namespace TotalMixVC.Communicator
{
    public class Sender : IDisposable
    {
        private readonly UdpClient _client;

        private readonly IPEndPoint _localEP;

        private bool _disposed = false;

        public Sender(IPEndPoint localEP)
        {
            _localEP = localEP;
            _client = new UdpClient();
        }

        ~Sender()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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

        public async Task<int> Send(OscPacket message)
        {
            byte[] datagram = message.ToByteArray();
            return await _client
                .SendAsync(datagram, datagram.Length, _localEP)
                .ConfigureAwait(false);
        }
    }
}
