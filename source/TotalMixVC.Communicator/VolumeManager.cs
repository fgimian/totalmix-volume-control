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

        public const string VolumeDecibelsAddress = "/1/mastervolumeVal";

        public float Volume
        {
            get
            {
                return _volume;
            }
        }

        public string VolumeDecibels
        {
            get
            {
                return _volumeDecibels;
            }
        }

        public float VolumeRegularIncrement
        {
            get
            {
                return _volumeRegularIncrement;
            }

            set
            {
                if (value <= 0.0f || value > 0.10f)
                {
                    throw new ArgumentException(
                        "Regular volume increment must be greater than 0 and less than 0.1.");
                }

                _volumeRegularIncrement = value;
            }
        }

        public float VolumeFineIncrement
        {
            get
            {
                return _volumeFineIncrement;
            }

            set
            {
                if (value <= 0.0f || value > 0.05f)
                {
                    throw new ArgumentException(
                        "Fine volume increment must be greater than 0 and less than 0.05.");
                }

                _volumeFineIncrement = value;
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

        private readonly SemaphoreSlim _volumeDecibelsMutex;

        private readonly Sender _sender;

        private readonly Listener _listener;

        private float _volume = -1.0f;

        private string _volumeDecibels;

        private float _volumeRegularIncrement;

        private float _volumeFineIncrement;

        private float _volumeMax = 1.0f;

        public VolumeManager(IPEndPoint outgoingEP, IPEndPoint incomingEP)
        {
            _volumeMutex = new SemaphoreSlim(1);
            _volumeDecibelsMutex = new SemaphoreSlim(1);
            _sender = new Sender(outgoingEP);
            _listener = new Listener(incomingEP);
        }

        public async Task GetDeviceVolume()
        {
            while (Volume == -1.0f)
            {
                // Send an initial invalid value (-1.0) so that TotalMix can send us the current
                // volume.
                await SendCurrentVolume().ConfigureAwait(false);

                // Wait up until one second for the current volume to updated by the listener.
                // If no update is received, the initial value will be resent.
                for (uint iterations = 0; Volume == -1.0f && iterations < 40; iterations++)
                {
                    await Task.Delay(25).ConfigureAwait(false);
                }
            }
        }

        public async Task<bool> ReceiveVolume()
        {
            // Ping events are sent from the device every around every 1 second, so we only
            // wait until a given timeout of 5 seconds before giving up and forcing a fresh
            // receive request.  This ensures that the receiver can detect a device which was
            // previous offline.
            var task = _listener.Receive();
            if (await Task.WhenAny(task, Task.Delay(5000)).ConfigureAwait(false) != task)
            {
                return false;
            }

            OscPacket packet = task.Result;

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
                    await UpdateVolumeDecibelsFromMessage(message).ConfigureAwait(false);

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
                await UpdateVolumeDecibelsFromMessage(message).ConfigureAwait(false);
                return await UpdateVolumeFromMessage(message).ConfigureAwait(false);
            }
        }

        public async Task<bool> IncreaseVolume(bool fine = false)
        {
            await _volumeMutex.WaitAsync().ConfigureAwait(false);
            try
            {
                // Calculate the new volume.
                float increment = fine ? VolumeFineIncrement : VolumeRegularIncrement;
                float newVolume = Volume + increment;

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

        public async Task<bool> DecreaseVolume(bool fine = false)
        {
            await _volumeMutex.WaitAsync().ConfigureAwait(false);
            try
            {
                // Calculate the new volume.
                float increment = fine ? VolumeFineIncrement : VolumeRegularIncrement;
                float newVolume = Volume - increment;

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

        private async Task<bool> UpdateVolumeDecibelsFromMessage(OscMessage message)
        {
            // Only process volume messages.
            if (message.Address != VolumeDecibelsAddress)
            {
                return false;
            }

            // Volume messages should only contain one value.
            if (message.Count != 1)
            {
                return false;
            }

            // Obtain the decibel value as a string.
            string newVolumeDecibels;
            try
            {
                newVolumeDecibels = (string)message[0];
            }
            catch (InvalidCastException)
            {
                return false;
            }

            await _volumeDecibelsMutex.WaitAsync().ConfigureAwait(false);
            try
            {
                _volumeDecibels = newVolumeDecibels;
            }
            finally
            {
                _volumeDecibelsMutex.Release();
            }

            return true;
        }
    }
}
