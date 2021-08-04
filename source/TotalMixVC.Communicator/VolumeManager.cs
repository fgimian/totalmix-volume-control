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
            _sender = new Sender(outgoingEP);
            _listener = new Listener(incomingEP);
        }

        public async Task GetDeviceVolume()
        {
            while (_volume == -1.0f)
            {
                // Send an initial invalid value (-1.0) so that TotalMix can send us the current
                // volume.
                await SendCurrentVolume().ConfigureAwait(false);

                // Wait up until one second for the current volume to updated by the listener.
                // If no update is received, the initial value will be resent.
                for (uint iterations = 0; _volume == -1.0f && iterations < 40; iterations++)
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

            // Volume changes are only presented in bundles.
            if (packet is not OscBundle)
            {
                return false;
            }

            OscBundle bundle = packet as OscBundle;

            // Build a list of messages from the bundle.
            List<OscMessage> messages = new();
            IEnumerator<OscMessage> messageEnumerator = bundle.Messages();
            while (messageEnumerator.MoveNext())
            {
                messages.Add(messageEnumerator.Current);
            }

            // Attempt to update the volume reading from the bundle of messages.
            return await UpdateVolumeFromMessages(messages).ConfigureAwait(false);
        }

        public async Task<bool> IncreaseVolume(bool fine = false)
        {
            await _volumeMutex.WaitAsync().ConfigureAwait(false);
            try
            {
                // Calculate the new volume.
                float increment = fine ? _volumeFineIncrement : _volumeRegularIncrement;
                float newVolume = _volume + increment;

                // Ensure it doesn't exceed the max.
                if (newVolume >= VolumeMax)
                {
                    newVolume = VolumeMax;
                }

                // Only send an update via OSC if the value has changed.
                if (newVolume != _volume)
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
                float newVolume = _volume - increment;

                // Ensure it doesn't go below the minimum possible volume.
                if (newVolume < 0.0f)
                {
                    newVolume = 0.0f;
                }

                // Only send an update via OSC if the value has changed.
                if (newVolume != _volume)
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
                .Send(new OscMessage(VolumeAddress, _volume))
                .ConfigureAwait(false);
        }

        private async Task<bool> UpdateVolumeFromMessages(List<OscMessage> messages)
        {
            // After a volume change, the volume in decibels is sent immediately.  This comes
            // through much faster than the bundle containing both values, so it makes the
            // indicator more responsive if we capture this explicitly.
            if (
                messages.Count == 1 &&
                messages[0].Address == VolumeDecibelsAddress &&
                messages[0].Count == 1)
            {
                await _volumeMutex.WaitAsync().ConfigureAwait(false);
                try
                {
                    _volumeDecibels = (string)messages[0][0];
                    return true;
                }
                catch (InvalidCastException)
                {
                    return false;
                }
                finally
                {
                    _volumeMutex.Release();
                }
            }

            // A bundle containing two messages will come through next which contains two items
            // for the volume as a decimal and also a string containing the decibel reading.
            if (
                messages.Count == 2 &&
                messages[0].Address == VolumeAddress &&
                messages[1].Address == VolumeDecibelsAddress &&
                messages[0].Count == 1 &&
                messages[1].Count == 1)
            {
                try
                {
                    _volume = (float)messages[0][0];
                    _volumeDecibels = (string)messages[1][0];
                    return true;
                }
                catch (InvalidCastException)
                {
                    return false;
                }
                finally
                {
                    _volumeMutex.Release();
                }
            }

            return false;
        }
    }
}
