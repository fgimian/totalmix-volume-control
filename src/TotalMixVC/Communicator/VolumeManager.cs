using System.Net.Sockets;
using OscCore;

namespace TotalMixVC.Communicator;

/// <summary>
/// Tracks the volume of the device and provides a way to send volume updates.
/// </summary>
/// <param name="sender">A custom sender that implements the ISender interface.</param>
public class VolumeManager(ISender sender) : IDisposable
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

    /// <summary>
    /// A write semaphore (mutex) for the volume value properties Volume, VolumeDecibels and Dim.
    /// </summary>
    private readonly SemaphoreSlim _volumeSemaphore = new(1);

    /// <summary>
    /// Gets or sets the implementation of ISender which is used to send messages to the device.
    /// </summary>
    public ISender Sender { get; set; } = sender;

    /// <summary>
    /// Gets or sets the implementation of IListener which is used to receive messages from the
    /// device.
    /// </summary>
    public IListener? Listener { get; set; }

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
    public bool UseDecibels { get; set; }

    /// <summary>
    /// Gets or sets the increment to use when increasing or decreasing the volume
    /// in percent.
    /// </summary>
    public float VolumeIncrementPercent { get; set; }

    /// <summary>
    /// Gets or sets the increment to use when finely increasing or decreasing the volume
    /// in percent.
    /// </summary>
    public float VolumeFineIncrementPercent { get; set; }

    /// <summary>
    /// Gets or sets the maximum volume that should be allowed when increasing the volume
    /// in percent.
    /// </summary>
    public float VolumeMaxPercent { get; set; }

    /// <summary>
    /// Gets or sets the increment to use when increasing or decreasing the volume
    /// in decibels.
    /// </summary>
    public float VolumeIncrementDecibels { get; set; }

    /// <summary>
    /// Gets or sets the increment to use when finely increasing or decreasing the volume
    /// in decibels.
    /// </summary>
    public float VolumeFineIncrementDecibels { get; set; }

    /// <summary>
    /// Gets or sets the maximum volume that should be allowed when increasing the volume
    /// in decibels.
    /// </summary>
    public float VolumeMaxDecibels { get; set; }

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
    /// The task was cancelled using the provided cancellation token source.
    /// </exception>
    /// <exception cref="InvalidOperationException">The listener is null.</exception>
    /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
    /// <exception cref="TimeoutException">The task timed out.</exception>
    public async Task<bool> ReceiveVolumeAsync(
        int timeout = 5000,
        CancellationTokenSource? cancellationTokenSource = null
    )
    {
        if (Listener is null)
        {
            throw new InvalidOperationException();
        }

        // Ping events are sent from the device every around every 1 second, so we only
        // wait until a given timeout of 5 seconds before giving up and forcing a fresh
        // receive request. This ensures that the receiver can detect a device which was
        // previous offline.
        OscPacket packet;
        using var receiveCancellationTokenSource = new CancellationTokenSource();
        try
        {
            packet = await Listener
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
        catch (Exception ex) when (ex is TimeoutException or SocketException)
        {
            // Cancel the receive task since it timed out.
            await receiveCancellationTokenSource.CancelAsync().ConfigureAwait(false);

            // Reset the volume back to an initial state so that the caller is forced to
            // request device volume before continuing as this may have changed while the
            // device was offline.
            await _volumeSemaphore
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
                _volumeSemaphore.Release();
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

        await _volumeSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            // Calculate the new volume.
            float newVolume;
            if (UseDecibels)
            {
                var increment = fine ? VolumeFineIncrementDecibels : VolumeIncrementDecibels;

                var volumeDB =
                    MathF.Floor(MathF.Round(ValueToDecibels(Volume) / increment, 1)) * increment;
                volumeDB += increment;

                // Ensure it doesn't exceed the max dB.
                if (volumeDB >= VolumeMaxDecibels)
                {
                    volumeDB = VolumeMaxDecibels;
                }

                newVolume = DecibelsToValue(volumeDB);
            }
            else
            {
                var increment = fine ? VolumeFineIncrementPercent : VolumeIncrementPercent;

                newVolume = Volume + increment;

                // Ensure it doesn't exceed the max.
                if (newVolume >= VolumeMaxPercent)
                {
                    newVolume = VolumeMaxPercent;
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
            _volumeSemaphore.Release();
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

        await _volumeSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            // Calculate the new volume.
            float newVolume;
            if (UseDecibels)
            {
                var increment = fine ? VolumeFineIncrementDecibels : VolumeIncrementDecibels;

                var volumeDB =
                    MathF.Ceiling(MathF.Round(ValueToDecibels(Volume) / increment, 1)) * increment;
                volumeDB -= increment;

                newVolume = DecibelsToValue(volumeDB);
            }
            else
            {
                var increment = fine ? VolumeFineIncrementPercent : VolumeIncrementPercent;
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
            _volumeSemaphore.Release();
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

        await _volumeSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            // To toggle the dim function, we must simply send 1, not the actual 0 or 1 value.
            await SendDimAsync(1.0f).ConfigureAwait(false);
            Dim = Dim == 1.0f ? 0.0f : 1.0f;
            return true;
        }
        finally
        {
            _volumeSemaphore.Release();
        }
    }

    /// <summary>Disposes the current volume manager.</summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Converts a volume value in dB to a float value in the range 0.0 to 1.0 that may then be
    /// sent to the device.
    /// </summary>
    /// <param name="dBValue">The volume in decibels.</param>
    /// <returns>The converted volume represented as a float between 0.0 and 1.0.</returns>
    internal static float DecibelsToValue(float dBValue)
    {
        var sendValue =
            dBValue >= -6.0f
                ? (dBValue + 26.8235294118f) * (1.0f / 0.0320855615f) / 1023.0f
                : (826.0f - MathF.Sqrt(-34869.0f - (11033.0f * dBValue))) / 1023.0f;

        return Math.Clamp(sendValue, 0.0f, 1.0f);
    }

    /// <summary>
    /// Converts a float volume value in the range 0.0 to 1.0 to a value in dB.
    /// </summary>
    /// <param name="receivedValue">
    /// The received volume represented as a float between 0.0 and 1.0.
    /// </param>
    /// <returns>The converted volume represented in decibels.</returns>
    internal static float ValueToDecibels(float receivedValue)
    {
        var faderPos = receivedValue * 1023.0f;
        if (faderPos >= 649.0f)
        {
            return (faderPos * 0.0320855615f) - 26.8235294118f;
        }

        return (faderPos * faderPos * (-1.0f / 11033.0f)) + (faderPos * 0.1497326203f) - 65.0f;
    }

    /// <summary>Disposes the current volume manager.</summary>
    /// <param name="disposing">Whether managed resources should be disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _volumeSemaphore.Dispose();
        }
    }

    private async Task SendVolumeAsync(float volume)
    {
        try
        {
            await Sender.SendAsync(new OscMessage(VolumeAddress, volume)).ConfigureAwait(false);
        }
        catch (SocketException)
        {
            // This exception is raised during a reconnect which can be ignored.
        }
    }

    private async Task SendDimAsync(float dim)
    {
        try
        {
            await Sender.SendAsync(new OscMessage(DimAddress, dim)).ConfigureAwait(false);
        }
        catch (SocketException)
        {
            // This exception is raised during a reconnect which can be ignored.
        }
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
            await _volumeSemaphore.WaitAsync().ConfigureAwait(false);
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
                _volumeSemaphore.Release();
            }
        }

        return received;
    }
}
