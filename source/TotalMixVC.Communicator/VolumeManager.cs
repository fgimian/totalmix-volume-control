using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using OscCore;

namespace TotalMixVC.Communicator
{
    public class VolumeManager
    {
        public const string VolumeAddress = "/1/mastervolume";

        public float Volume
        {
            get
            {
                return _volume;
            }
        }

        public float VolumeIncrement
        {
            get
            {
                return _volumeIncrement;
            }

            set
            {
                if (value <= 0.0f || value > 0.10f)
                {
                    throw new ArgumentException(
                        "Volume increment must be greater than 0 and less than 0.1.");
                }

                _volumeIncrement = value;
            }
        }

        public float VolumeMax
        {
            get
            {
                return _volumeMax;
            }

            set
            {
                if (value > 1.0)
                {
                    throw new ArgumentException("Volume max can't be greater than 1.0.");
                }

                _volumeMax = value;
            }
        }

        private readonly SemaphoreSlim _volumeMutex;

        private readonly Listener _listener;

        private readonly Sender _sender;

        private float _volume = -1.0f;

        private float _volumeIncrement;

        private float _volumeMax = 1.0f;

        public VolumeManager(IPEndPoint incomingEP, IPEndPoint outgoingEP)
        {
            _volumeMutex = new SemaphoreSlim(1);
            _listener = new Listener(outgoingEP);
            _sender = new Sender(incomingEP);
        }

        public async Task GetDeviceVolume()
        {
            // Send an initial invalid value (-1.0) so that TotalMix can send us the current volume.
            await SendCurrentVolume().ConfigureAwait(false);

            // Wait until the current volume is updated by the listener.
            while (Volume == -1.0f)
            {
                await Task.Delay(25).ConfigureAwait(false);
            }
        }

        public async Task<bool> ReceiveVolume()
        {
            OscPacket packet = await _listener.Receive().ConfigureAwait(false);

            // Volume changes are presented in bundles, but we'll also check message just in case
            // this changes in the future.
            if (packet is OscBundle)
            {
                OscBundle bundle = packet as OscBundle;
                bool updated = false;

                // Iterate through all messages in the bundle.
                IEnumerator<OscMessage> messageEnumerator = bundle.Messages();
                while (messageEnumerator.MoveNext())
                {
                    OscMessage message = messageEnumerator.Current;
                    if (await UpdateVolumeFromMessage(message).ConfigureAwait(false))
                    {
                        updated = true;
                    }
                }

                return updated;
            }
            else
            {
                OscMessage message = packet as OscMessage;
                await UpdateVolumeFromMessage(message).ConfigureAwait(false);
                return true;
            }
        }

        public async Task<bool> IncreaseVolume()
        {
            await _volumeMutex.WaitAsync().ConfigureAwait(false);
            try
            {
                // Calculate the new volume.
                float newVolume = Volume + VolumeIncrement;

                // Ensure it doesn't exceed the max.
                if (newVolume >= VolumeMax)
                {
                    newVolume = VolumeMax;
                }

                // Only send an update via OSC if the value has changed.
                if (newVolume != Volume)
                {
                    _volume = newVolume;
                    await SendCurrentVolume().ConfigureAwait(false);
                    return true;
                }

                return false;
            }
            finally
            {
                _volumeMutex.Release();
            }
        }

        public async Task<bool> DecreaseVolume()
        {
            await _volumeMutex.WaitAsync().ConfigureAwait(false);
            try
            {
                // Calculate the new volume.
                float newVolume = Volume - VolumeIncrement;

                // Ensure it doesn't go below the minimum possible volume.
                if (newVolume < 0.0f)
                {
                    newVolume = 0.0f;
                }

                // Only send an update via OSC if the value has changed.
                if (newVolume != Volume)
                {
                    _volume = newVolume;
                    await SendCurrentVolume().ConfigureAwait(false);
                    return true;
                }

                return false;
            }
            finally
            {
                _volumeMutex.Release();
            }
        }

        private async Task SendCurrentVolume()
        {
            await _sender
                .Send(new OscMessage(VolumeAddress, Volume))
                .ConfigureAwait(false);
        }

        private async Task<bool> UpdateVolumeFromMessage(OscMessage message)
        {
            // Only process volume messages.
            if (message.Address != VolumeAddress)
            {
                return false;
            }

            // Volume messages should only contain one value.
            if (message.Count != 1)
            {
                return false;
            }

            // Obtain the value as a float.
            float newVolume;
            try
            {
                newVolume = (float)message[0];
            }
            catch (InvalidCastException)
            {
                return false;
            }

            await _volumeMutex.WaitAsync().ConfigureAwait(false);
            try
            {
                _volume = newVolume;
            }
            finally
            {
                _volumeMutex.Release();
            }

            return true;
        }
    }
}
