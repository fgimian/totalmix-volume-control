using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using OscCore;
using TotalMixVC.Helpers;

namespace TotalMixVC.Communicator
{
    /// <summary>
    /// Tracks the volume of the device and provides a way to send volume updates.
    /// </summary>
    public class VolumeManager
    {
        /// <summary>
        /// The address to be used to sending and receiving volume as a float.
        /// </summary>
        public const string VolumeAddress = "/1/mastervolume";

        /// <summary>
        /// The address to be used for receiving volume as a string in decibels.
        /// </summary>
        public const string VolumeDecibelsAddress = "/1/mastervolumeVal";

        private readonly SemaphoreSlim _volumeMutex;

        private readonly ISender _sender;

        private readonly IListener _listener;

        private float _volumeRegularIncrement;

        private float _volumeFineIncrement;

        private float _volumeMax = 1.0f;

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeManager"/> class.
        /// </summary>
        /// <param name="outgoingEP">
        /// The outgoing OSC endpoint to send volume changes to.  This should be set to the
        /// incoming port in TotalMix settings.
        /// </param>
        /// <param name="incomingEP">
        /// The incoming OSC endpoint to receive volume changes from.  This should be set to the
        /// outgoing port in TotalMix settings.
        /// </param>
        [ExcludeFromCodeCoverage]
        public VolumeManager(IPEndPoint outgoingEP, IPEndPoint incomingEP)
        {
            _volumeMutex = new SemaphoreSlim(1);
            _sender = new Sender(outgoingEP);
            _listener = new Listener(incomingEP);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeManager"/> class.
        /// </summary>
        /// <param name="sender">A custom sender that implements the ISender interface.</param>
        /// <param name="listener">A custom listener that implements the ISender interface.</param>
        public VolumeManager(ISender sender, IListener listener)
        {
            _volumeMutex = new SemaphoreSlim(1);
            _sender = sender;
            _listener = listener;
        }

        /// <summary>
        /// Gets the current device volume as a float (with a range of 0.0 to 1.0).
        /// </summary>
        public float Volume { get; private set; } = -1.0f;

        /// <summary>
        /// Gets the current device volume as a string in decibels.
        /// </summary>
        public string VolumeDecibels { get; private set; }

        /// <summary>
        /// Gets or sets the increment to use when regularly increasing or decreasing the volume.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The regular volume increment is not within the required range.
        /// </exception>
        public float VolumeRegularIncrement
        {
            get => _volumeRegularIncrement;

            set
            {
                if (value is <= 0.0f or > 0.10f)
                {
                    throw new ArgumentException(
                        "Regular volume increment must be greater than 0 and less than 0.1.");
                }

                _volumeRegularIncrement = value;
            }
        }

        /// <summary>
        /// Gets or sets the increment to use when finely increasing or decreasing the volume.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The fine volume increment is not within the required range.
        /// </exception>
        public float VolumeFineIncrement
        {
            get => _volumeFineIncrement;

            set
            {
                if (value is <= 0.0f or > 0.05f)
                {
                    throw new ArgumentException(
                        "Fine volume increment must be greater than 0 and less than 0.05.");
                }

                _volumeFineIncrement = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum volume that should be allowed when increasing the volume.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The max volume increment not within the required range.
        /// </exception>
        public float VolumeMax
        {
            get => _volumeMax;

            set
            {
                if (value is <= 0.0f or > 1.0f)
                {
                    throw new ArgumentException("Volume max can't be greater than 1.0.");
                }

                _volumeMax = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the volume has been obtained from the device at least
        /// once.
        /// </summary>
        public bool IsVolumeInitialized
        {
            get => Volume != -1.0f && VolumeDecibels is not null;
        }

        /// <summary>
        /// Requests the current device volume by sending an invalid value (-1.0) so that TotalMix
        /// can send us the current volume.  This method assumes you are running the
        /// <see cref="ReceiveVolumeAsync"/> method in an async thread.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task RequestVolumeAsync()
        {
            return SendVolumeAsync(-1.0f);
        }

        /// <summary>
        /// Attempts to receive the device volume for the given timeout.
        /// </summary>
        /// <param name="timeout">
        /// The amount of time wo wait for a volume message before giving up.
        /// </param>
        /// <param name="cancellationTokenSource">
        /// An optional cancellation token source so the task may be cancelled by the caller while
        /// attempting to receive data.
        /// </param>
        /// <returns>
        /// The task object representing the asynchronous operation which will contain a boolean
        /// indicating whether or not the volume was obtained from the device.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the task is cancelled using the provided cancellation token source.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown if the task times out.</exception>
        public async Task<bool> ReceiveVolumeAsync(
            int timeout = 5000, CancellationTokenSource cancellationTokenSource = null)
        {
            // Ping events are sent from the device every around every 1 second, so we only
            // wait until a given timeout of 5 seconds before giving up and forcing a fresh
            // receive request.  This ensures that the receiver can detect a device which was
            // previous offline.
            OscPacket packet;
            try
            {
                packet = await _listener
                    .ReceiveAsync()
                    .TimeoutAfter<OscPacket>(timeout, cancellationTokenSource)
                    .ConfigureAwait(false);
            }
            catch (OscException)
            {
                // An incomplete packet may be received if the device goes offline during
                // transmission of the message.
                return false;
            }
            catch (TimeoutException)
            {
                // Reset the volume back to an initial state so that the caller is forced to
                // request device volume before continuing as this may have changed while the
                // device was offline.
                await _volumeMutex.WaitAsync().ConfigureAwait(false);
                try
                {
                    Volume = -1.0f;
                    VolumeDecibels = null;
                }
                finally
                {
                    _volumeMutex.Release();
                }

                throw;
            }

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
            return await UpdateVolumeFromMessagesAsync(messages).ConfigureAwait(false);
        }

        /// <summary>
        /// Increase the volume of the device.
        /// </summary>
        /// <param name="fine">Whether or not to use a fine increment.</param>
        /// <returns>
        /// The task object representing the asynchronous operation which will contain a boolean
        /// indicating whether or not the volume needed to be updated for the device.
        /// </returns>
        public async Task<bool> IncreaseVolumeAsync(bool fine = false)
        {
            if (!IsVolumeInitialized)
            {
                return false;
            }

            await _volumeMutex.WaitAsync().ConfigureAwait(false);
            try
            {
                // Calculate the new volume.
                float increment = fine ? _volumeFineIncrement : _volumeRegularIncrement;
                float newVolume = Volume + increment;

                // Ensure it doesn't exceed the max.
                if (newVolume >= VolumeMax)
                {
                    newVolume = VolumeMax;
                }

                // Only send an update via OSC if the value has changed.
                if (newVolume != Volume)
                {
                    await SendVolumeAsync(newVolume).ConfigureAwait(false);
                    Volume = newVolume;
                    return true;
                }

                return false;
            }
            finally
            {
                _volumeMutex.Release();
            }
        }

        /// <summary>
        /// Decreases the volume of the device.
        /// </summary>
        /// <param name="fine">Whether or not to use a fine increment.</param>
        /// <returns>
        /// The task object representing the asynchronous operation which will contain a boolean
        /// indicating whether or not the volume needed to be updated for the device.
        /// </returns>
        public async Task<bool> DecreaseVolumeAsync(bool fine = false)
        {
            if (!IsVolumeInitialized)
            {
                return false;
            }

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
                    await SendVolumeAsync(newVolume).ConfigureAwait(false);
                    Volume = newVolume;
                    return true;
                }

                return false;
            }
            finally
            {
                _volumeMutex.Release();
            }
        }

        private Task SendVolumeAsync(float volume)
        {
            return _sender.SendAsync(new OscMessage(VolumeAddress, volume));
        }

        private async Task<bool> UpdateVolumeFromMessagesAsync(List<OscMessage> messages)
        {
            // After a volume change, the volume in decibels is sent immediately.  This comes
            // through much faster than the bundle containing both values, so it makes the
            // indicator more responsive if we capture this explicitly.
            if (
                messages.Count == 1
                && messages[0].Address == VolumeDecibelsAddress
                && messages[0].Count == 1)
            {
                await _volumeMutex.WaitAsync().ConfigureAwait(false);
                try
                {
                    VolumeDecibels = (string)messages[0][0];
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
                messages.Count == 2
                && messages[0].Address == VolumeAddress
                && messages[1].Address == VolumeDecibelsAddress
                && messages[0].Count == 1
                && messages[1].Count == 1)
            {
                await _volumeMutex.WaitAsync().ConfigureAwait(false);
                try
                {
                    Volume = (float)messages[0][0];
                    VolumeDecibels = (string)messages[1][0];
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
