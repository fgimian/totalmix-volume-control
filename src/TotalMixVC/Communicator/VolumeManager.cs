using System.Diagnostics.CodeAnalysis;
using System.Net;
using OscCore;

namespace TotalMixVC.Communicator;

/// <summary>
/// Tracks the volume of the device and provides a way to send volume updates.
/// </summary>
public class VolumeManager : IDisposable
{
    /// <summary>
    /// The address to be used to sending and receiving volume as a float.
    /// </summary>
    private const string VolumeAddress = "/1/mastervolume";

    /// <summary>
    /// The address to be used for receiving volume as a string in decibels.
    /// </summary>
    private const string VolumeDecibelsAddress = "/1/mastervolumeVal";

    /// <summary>
    /// The address to be used for sending and receiving the dim mode.
    /// </summary>
    private const string DimAddress = "/1/mainDim";

    private readonly SemaphoreSlim _volumeMutex;

    private readonly ISender _sender;

    private readonly IListener _listener;

    private bool _useDecibels;

    private float _volumeRegularIncrement = 0.02f;

    private float _volumeFineIncrement = 0.01f;

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
    public string? VolumeDecibels { get; private set; }

    /// <summary>
    /// Gets whether dim is enabled on the device (where 0 is disabled and 1 is enabled).
    /// </summary>
    public float Dim { get; private set; } = -1.0f;

    /// <summary>
    /// Gets a value indicating whether the device volume is dimmed.
    /// </summary>
    public bool IsDimmed => Dim == 1.0f;

    /// <summary>
    /// Gets or sets a value indicating whether volume units are set in dB instead of percentages.
    /// </summary>
    public bool UseDecibels
    {
        get => _useDecibels;
        set
        {
            if (_useDecibels != value)
            {
                if (value)
                {
                    _volumeRegularIncrement = 1.0f;
                    _volumeFineIncrement = 0.5f;
                    _volumeMax = 6.0f;
                }
                else
                {
                    _volumeRegularIncrement = 0.02f;
                    _volumeFineIncrement = 0.01f;
                    _volumeMax = 1.0f;
                }
            }

            _useDecibels = value;
        }
    }

    /// <summary>
    /// Gets or sets the increment to use when regularly increasing or decreasing the volume.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The regular volume increment is not within the required range.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// When the regular increment specified is not in the supported range.
    /// </exception>
    public float VolumeRegularIncrement
    {
        get => _volumeRegularIncrement;
        set
        {
            if (_useDecibels && !(value is 0.5f or 1.0f or 1.5f or 2.0f or 2.5f or 3.0f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Specified dB volume increment must be a multiple of 0.5 while being greater "
                        + "than 0 and less than or equal to 3.0."
                );
            }
            else if (!_useDecibels && value is <= 0.0f or > 0.10f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Specified volume increment must be greater than 0 and less than or equal "
                        + "to 0.1."
                );
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// When the fine increment specified is not in the supported range.
    /// </exception>
    public float VolumeFineIncrement
    {
        get => _volumeFineIncrement;
        set
        {
            if (_useDecibels && !(value is 0.5f or 1.0f or 1.5f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Specified fine dB volume increment must be a multiple of 0.5 while being "
                        + "greater than 0 and less than or equal to 1.5."
                );
            }
            else if (!_useDecibels && value is <= 0.0f or > 0.05f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Specified fine volume increment must be greater than 0 and less than or "
                        + "equal to 0.05."
                );
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// When the max volume specified is not in the supported range.
    /// </exception>
    public float VolumeMax
    {
        get => _volumeMax;
        set
        {
            if (_useDecibels && value is > 6.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Specified max dB volume must be less than or equal to 6.0."
                );
            }
            else if (!_useDecibels && value is <= 0.0f or > 1.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Specified max volume must be greater than 0 and less than or equal to 1.0."
                );
            }

            _volumeMax = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the volume has been obtained from the device at least
    /// once.
    /// </summary>
    public bool IsVolumeInitialized =>
        Volume != -1.0f && VolumeDecibels is not null && Dim != -1.0f;

    /// <summary>
    /// Requests the current device volume by sending an invalid value (-1.0) for volume and
    /// invalid value (-1.0) for dim so that TotalMix can send us the current volume and dim.
    /// This method assumes you are running the <see cref="ReceiveVolumeAsync"/> method in an
    /// async thread.
    /// </summary>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public async Task RequestVolumeAsync()
    {
        await SendVolumeAsync(-1.0f).ConfigureAwait(false);
        await SendDimAsync(-1.0f).ConfigureAwait(false);
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
        int timeout = 5000,
        CancellationTokenSource? cancellationTokenSource = null
    )
    {
        // Ping events are sent from the device every around every 1 second, so we only
        // wait until a given timeout of 5 seconds before giving up and forcing a fresh
        // receive request.  This ensures that the receiver can detect a device which was
        // previous offline.
        OscPacket packet;
        using var receiveCancellationTokenSource = new CancellationTokenSource();
        try
        {
            packet = await _listener
                .ReceiveAsync(receiveCancellationTokenSource)
                .TimeoutAfter(timeout, cancellationTokenSource)
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
            // Cancel the receive task since it timed out.
            await receiveCancellationTokenSource.CancelAsync().ConfigureAwait(false);

            // Reset the volume back to an initial state so that the caller is forced to
            // request device volume before continuing as this may have changed while the
            // device was offline.
            await _volumeMutex
                .WaitAsync(cancellationTokenSource?.Token ?? default)
                .ConfigureAwait(false);
            try
            {
                Volume = -1.0f;
                VolumeDecibels = null;
                Dim = -1.0f;
            }
            finally
            {
                _volumeMutex.Release();
            }

            throw;
        }

        // Volume changes are only presented in bundles.
        if (packet is OscBundle bundle)
        {
            // Build a list of messages from the bundle.
            List<OscMessage> messages = [];
            var messageEnumerator = bundle.Messages();
            while (messageEnumerator.MoveNext())
            {
                messages.Add(messageEnumerator.Current);
            }

            // Attempt to update the volume reading from the bundle of messages.
            return await UpdateVolumeFromMessagesAsync(messages).ConfigureAwait(false);
        }

        return false;
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
            var increment = fine ? _volumeFineIncrement : _volumeRegularIncrement;

            float newVolume;
            if (UseDecibels)
            {
                var volumeDB =
                    MathF.Floor(MathF.Round(ValueToDecibels(Volume) / increment, 1)) * increment;
                volumeDB += increment;

                // Ensure it doesn't exceed the max dB.
                if (volumeDB >= VolumeMax)
                {
                    volumeDB = VolumeMax;
                }

                newVolume = DecibelsToValue(volumeDB);
            }
            else
            {
                newVolume = Volume + increment;

                // Ensure it doesn't exceed the max.
                if (newVolume >= VolumeMax)
                {
                    newVolume = VolumeMax;
                }
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
            var increment = fine ? VolumeFineIncrement : VolumeRegularIncrement;
            float newVolume;
            if (UseDecibels)
            {
                var volumeDB =
                    MathF.Ceiling(MathF.Round(ValueToDecibels(Volume) / increment, 1)) * increment;
                volumeDB -= increment;
                newVolume = DecibelsToValue(volumeDB);
            }
            else
            {
                newVolume = Volume - increment;
            }

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

    /// <summary>
    /// Toggles the dim mode of the device.
    /// </summary>
    /// <returns>
    /// The task object representing the asynchronous operation which will contain a boolean
    /// indicating whether or not the dim mode needed to be updated for the device.
    /// </returns>
    public async Task<bool> ToggloDimAsync()
    {
        if (!IsVolumeInitialized)
        {
            return false;
        }

        await _volumeMutex.WaitAsync().ConfigureAwait(false);
        try
        {
            // To toggle the dim function, we must simply send 1, not the actual 0 or 1 value.
            await SendDimAsync(1.0f).ConfigureAwait(false);
            Dim = Dim == 1.0f ? 0.0f : 1.0f;
            return true;
        }
        finally
        {
            _volumeMutex.Release();
        }
    }

    /// <summary>Disposes the current volume manager.</summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Disposes the current volume manager.</summary>
    /// <param name="disposing">Whether managed resources should be disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _volumeMutex.Dispose();
        }
    }

    private static float DecibelsToValue(float dBValue)
    {
        var sendValue =
            dBValue >= -6.0f
                ? (dBValue + 26.8235294118f) * (1.0f / 0.0320855615f) / 1023.0f
                : (826.0f - MathF.Sqrt(-34869.0f - (11033.0f * dBValue))) / 1023.0f;

        return Math.Clamp(sendValue, 0.0f, 1.0f);
    }

    private static float ValueToDecibels(float receivedValue)
    {
        var faderPos = receivedValue * 1023.0f;
        if (faderPos >= 649.0f)
        {
            return (faderPos * 0.0320855615f) - 26.8235294118f;
        }

        return (faderPos * faderPos * (-1.0f / 11033.0f)) + (faderPos * 0.1497326203f) - 65.0f;
    }

    private Task<int> SendVolumeAsync(float volume)
    {
        return _sender.SendAsync(new OscMessage(VolumeAddress, volume));
    }

    private Task<int> SendDimAsync(float dim)
    {
        return _sender.SendAsync(new OscMessage(DimAddress, dim));
    }

    private async Task<bool> UpdateVolumeFromMessagesAsync(List<OscMessage> messages)
    {
        var received = false;

        foreach (
            var message in messages.Where(m =>
                m.Address is VolumeDecibelsAddress or VolumeAddress or DimAddress && m.Count is 1
            )
        )
        {
            await _volumeMutex.WaitAsync().ConfigureAwait(false);
            try
            {
                if (message.Address == VolumeDecibelsAddress)
                {
                    VolumeDecibels = (string)message[0];
                }
                else if (message.Address == VolumeAddress)
                {
                    Volume = (float)message[0];
                }
                else
                {
                    Dim = (float)message[0];
                }

                received = true;
            }
            catch (InvalidCastException)
            {
                // Ignore errors in the impossible case that the device sends us the wrong
                // data type.
            }
            finally
            {
                _volumeMutex.Release();
            }
        }

        return received;
    }
}
