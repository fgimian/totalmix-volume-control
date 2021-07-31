using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using OscCore;

namespace TotalMixVC.Communicator
{
    public class Listener : IDisposable
    {
        private readonly UdpClient _client;

        private bool _disposed = false;

        public Listener(IPEndPoint localEP)
        {
            _client = new UdpClient(localEP);
        }

        ~Listener()
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

        public async Task<OscPacket> Receive()
        {
            var result = await _client.ReceiveAsync().ConfigureAwait(false);
            return OscPacket.Read(result.Buffer, 0, result.Buffer.Length);
        }
    }
}
