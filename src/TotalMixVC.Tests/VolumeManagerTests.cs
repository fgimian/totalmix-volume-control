using NSubstitute;
using OscCore;
using TotalMixVC.Communicator;
using Xunit;

namespace TotalMixVC.Tests;

public sealed class VolumeManagerTests : IDisposable
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private readonly IListener _listener = Substitute.For<IListener>();

    private readonly VolumeManager _volumeManager;

    public VolumeManagerTests()
    {
        _sender = Substitute.For<ISender>();
        _listener = Substitute.For<IListener>();
        _volumeManager = new(_sender, _listener);
    }

    public void Dispose()
    {
        _volumeManager.Dispose();
    }

    [Fact]
    public void Constructor_EnablingUseDecibels_UpdatesDefaults()
    {
        _volumeManager.UseDecibels = true;
        Assert.Equal(2.0f, _volumeManager.VolumeRegularIncrement);
        Assert.Equal(1.0f, _volumeManager.VolumeFineIncrement);
        Assert.Equal(6.0f, _volumeManager.VolumeMax);
    }

    [Fact]
    public void Constructor_DisablingUseDecibels_UpdatesDefaults()
    {
        _volumeManager.UseDecibels = true;
        _volumeManager.UseDecibels = false;
        Assert.Equal(0.02f, _volumeManager.VolumeRegularIncrement);
        Assert.Equal(0.01f, _volumeManager.VolumeFineIncrement);
        Assert.Equal(1.0f, _volumeManager.VolumeMax);
    }

    [Fact]
    public void Constructor_EnablingUseDecibelsMultipleTimes_RetainsValues()
    {
        _volumeManager.UseDecibels = true;
        _volumeManager.VolumeRegularIncrement = 2.0f;
        _volumeManager.VolumeFineIncrement = 1.0f;
        _volumeManager.VolumeMax = 0.0f;
        _volumeManager.UseDecibels = true;
        Assert.Equal(2.0f, _volumeManager.VolumeRegularIncrement);
        Assert.Equal(1.0f, _volumeManager.VolumeFineIncrement);
        Assert.Equal(0.0f, _volumeManager.VolumeMax);
    }

    [Fact]
    public void Constructor_DisablingUseDecibelsMultipleTimes_RetainsValues()
    {
        _volumeManager.VolumeRegularIncrement = 0.04f;
        _volumeManager.VolumeFineIncrement = 0.02f;
        _volumeManager.VolumeMax = 0.8f;
        _volumeManager.UseDecibels = false;
        Assert.Equal(0.04f, _volumeManager.VolumeRegularIncrement);
        Assert.Equal(0.02f, _volumeManager.VolumeFineIncrement);
        Assert.Equal(0.8f, _volumeManager.VolumeMax);
    }

    [Fact]
    public void Constructor_ValidVolumeRegularIncrementPercentage_SetsProperty()
    {
        _volumeManager.VolumeRegularIncrement = 0.03f;
        Assert.Equal(0.03f, _volumeManager.VolumeRegularIncrement);
    }

    [Theory]
    [InlineData(0.25f)]
    [InlineData(0.5f)]
    [InlineData(0.75f)]
    [InlineData(1.0f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(1.75f)]
    [InlineData(2.0f)]
    [InlineData(2.5f)]
    [InlineData(4.0f)]
    [InlineData(5.0f)]
    [InlineData(5.5f)]
    [InlineData(5.75f)]
    public void Constructor_ValidVolumeRegularIncrementDecibels_SetsProperty(
        float volumeRegularIncrement
    )
    {
        _volumeManager.UseDecibels = true;
        _volumeManager.VolumeRegularIncrement = volumeRegularIncrement;
        Assert.Equal(volumeRegularIncrement, _volumeManager.VolumeRegularIncrement);
    }

    [Theory]
    [InlineData(0.30f)]
    [InlineData(-0.01f)]
    public void Constructor_InvalidVolumeRegularIncrementPercentage_ThrowsException(
        float volumeRegularIncrement
    )
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _volumeManager.VolumeRegularIncrement = volumeRegularIncrement
        );
    }

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(0.0f)]
    [InlineData(1.1f)]
    [InlineData(2.7f)]
    [InlineData(3.1f)]
    [InlineData(6.25f)]
    [InlineData(6.5f)]
    public void Constructor_InvalidVolumeRegularIncrementDecibels_ThrowsException(
        float volumeRegularIncrement
    )
    {
        _volumeManager.UseDecibels = true;
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _volumeManager.VolumeRegularIncrement = volumeRegularIncrement
        );
    }

    [Fact]
    public void Constructor_ValidVolumeFineIncrementPercentage_SetsProperty()
    {
        _volumeManager.VolumeFineIncrement = 0.01f;
        Assert.Equal(0.01f, _volumeManager.VolumeFineIncrement);
    }

    [Theory]
    [InlineData(0.25f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2.0f)]
    [InlineData(2.75f)]
    public void Constructor_ValidVolumeFineIncrementDecibels_SetsProperty(float volumeFineIncrement)
    {
        _volumeManager.UseDecibels = true;
        _volumeManager.VolumeFineIncrement = volumeFineIncrement;
        Assert.Equal(volumeFineIncrement, _volumeManager.VolumeFineIncrement);
    }

    [Theory]
    [InlineData(0.10f)]
    [InlineData(-0.03f)]
    public void Constructor_InvalidVolumeFineIncrementPercentage_ThrowsException(
        float volumeFineIncrement
    )
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _volumeManager.VolumeFineIncrement = volumeFineIncrement
        );
    }

    [Theory]
    [InlineData(-0.03f)]
    [InlineData(0.3f)]
    [InlineData(1.1f)]
    [InlineData(1.9f)]
    [InlineData(3.25f)]
    [InlineData(3.5f)]
    public void Constructor_InvalidVolumeFineIncrementDecibels_ThrowsException(
        float volumeFineIncrement
    )
    {
        _volumeManager.UseDecibels = true;
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _volumeManager.VolumeFineIncrement = volumeFineIncrement
        );
    }

    [Fact]
    public void Constructor_ValidVolumeMaxPercentage_SetsProperty()
    {
        _volumeManager.VolumeMax = 0.90f;
        Assert.Equal(0.90f, _volumeManager.VolumeMax);
    }

    [Theory]
    [InlineData(-61.2f)]
    [InlineData(-32.0f)]
    [InlineData(0.0f)]
    [InlineData(3.5f)]
    [InlineData(6.0f)]
    public void Constructor_ValidVolumeMaxDecibels_SetsProperty(float volumeMax)
    {
        _volumeManager.UseDecibels = true;
        _volumeManager.VolumeMax = volumeMax;
        Assert.Equal(volumeMax, _volumeManager.VolumeMax);
    }

    [Theory]
    [InlineData(1.10f)]
    [InlineData(-0.15f)]
    public void Constructor_InvalidVolumeMaxPercentage_ThrowsException(float volumeMax)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _volumeManager.VolumeMax = volumeMax);
    }

    [Theory]
    [InlineData(6.1f)]
    [InlineData(10.0f)]
    public void Constructor_InvalidVolumeMaxDecibels_ThrowsException(float volumeMax)
    {
        _volumeManager.UseDecibels = true;
        Assert.Throws<ArgumentOutOfRangeException>(() => _volumeManager.VolumeMax = volumeMax);
    }

    [Fact]
    public async Task RequestVolumeAsync_RegularRequest_RequestsVolume_Async()
    {
        await _volumeManager.RequestVolumeAsync();
        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { -1.0f })
                )
            );
    }

    [Fact]
    public async Task ReceiveVolumeAsync_ReceivesAllNormal_UpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB")
                    )
                )
            );

        var received = await _volumeManager.ReceiveVolumeAsync();

        Assert.True(received);
        Assert.Equal(0.20f, _volumeManager.Volume);
        Assert.Equal("-38.2 dB", _volumeManager.VolumeDecibels);
        Assert.False(_volumeManager.IsDimmed);
        Assert.True(_volumeManager.IsVolumeInitialized);
    }

    [Fact]
    public async Task ReceiveVolumeAsync_ReceivesAllDimmed_UpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 1f),
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB")
                    )
                )
            );

        var received = await _volumeManager.ReceiveVolumeAsync();

        Assert.True(received);
        Assert.Equal(0.20f, _volumeManager.Volume);
        Assert.Equal("-38.2 dB", _volumeManager.VolumeDecibels);
        Assert.True(_volumeManager.IsDimmed);
        Assert.True(_volumeManager.IsVolumeInitialized);
    }

    [Fact]
    public async Task ReceiveVolumeAsync_ReceivesInvalidTypes_DoesNotReceiveResult_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", "On"),
                        new OscMessage("/1/mastervolume", "-38.2 dB"),
                        new OscMessage("/1/mastervolumeVal", 0.20f)
                    )
                )
            );

        var received = await _volumeManager.ReceiveVolumeAsync();

        Assert.False(received);
        Assert.False(_volumeManager.IsVolumeInitialized);
    }

    [Fact]
    public async Task ReceiveVolumeAsync_ReceivesDecibelsOnly_UpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB")
                    )
                ),
                Task.FromResult<OscPacket>(
                    new OscBundle(OscTimeTag.Now, new OscMessage("/1/mastervolumeVal", "-40.5 dB"))
                )
            );

        var receivedAll = await _volumeManager.ReceiveVolumeAsync();
        var receivedDecibelsOnly = await _volumeManager.ReceiveVolumeAsync();

        Assert.True(receivedAll);
        Assert.True(receivedDecibelsOnly);
        Assert.Equal(0.20f, _volumeManager.Volume);
        Assert.Equal("-40.5 dB", _volumeManager.VolumeDecibels);
        Assert.True(_volumeManager.IsVolumeInitialized);
    }

    [Fact]
    public async Task ReceiveVolumeAsync_ReceivesDecibelsOnlyInvalid_DoesNotReceiveResult_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(OscTimeTag.Now, new OscMessage("/1/mastervolumeVal", -1.0f))
                )
            );

        var received = await _volumeManager.ReceiveVolumeAsync();

        Assert.False(received);
        Assert.False(_volumeManager.IsVolumeInitialized);
    }

    [Fact]
    public async Task ReceiveVolumeAsync_ReceivesOtherVolume_DoesNotReceiveResult_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/volume1", 0.20f),
                        new OscMessage("/1/volume1Val", "-38.2 dB")
                    )
                )
            );

        var received = await _volumeManager.ReceiveVolumeAsync();

        Assert.False(received);
        Assert.False(_volumeManager.IsVolumeInitialized);
    }

    [Fact]
    public async Task ReceiveVolumeAsync_IncorrectPacketType_DoesNotReceiveResult_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(new OscMessage("/1/mastervolume", 0.20f))
            );

        var received = await _volumeManager.ReceiveVolumeAsync();

        Assert.False(received);
        Assert.False(_volumeManager.IsVolumeInitialized);
    }

    [Fact]
    public async Task ReceiveVolumeAsync_PacketMalformed_DoesNotReceiveResult_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromException<OscPacket>(new OscException(OscError.MissingComma, "weov"))
            );

        var received = await _volumeManager.ReceiveVolumeAsync();

        Assert.False(received);
        Assert.False(_volumeManager.IsVolumeInitialized);
    }

    [Fact]
    public async Task ReceiveVolumeAsync_Timeout_ThrowsExceptionAndResetsVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB")
                    )
                ),
                Task.FromException<OscPacket>(new TimeoutException("weov"))
            );

        var received = await _volumeManager.ReceiveVolumeAsync();
        var initializedAfterVolumeReceived = _volumeManager.IsVolumeInitialized;

        Assert.True(received);
        Assert.True(initializedAfterVolumeReceived);

        await Assert.ThrowsAsync<TimeoutException>(
            async () => await _volumeManager.ReceiveVolumeAsync()
        );
        Assert.False(_volumeManager.IsVolumeInitialized);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_RegularVolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        var updated = await _volumeManager.IncreaseVolumeAsync();
        Assert.False(updated);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_RegularAfterVolumeInitializedPercentage_UpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB")
                    )
                )
            );

        _volumeManager.VolumeRegularIncrement = 0.02f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.22f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.22f, _volumeManager.Volume);
    }

    [Theory]
    [InlineData(0.19964331f, "-38.2 dB", -38.0f)]
    [InlineData(0.2372316f, "-34.0 dB", -33.0f)]
    [InlineData(0.2764855f, "-29.9 dB", -29.0f)]
    public async Task IncreaseVolumeAsync_RegularAfterVolumeInitializedDecibels_UpdatesVolume_Async(
        float masterVolume,
        string masterVolumeVal,
        float expectedUpdatedVolumeDB
    )
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", masterVolume),
                        new OscMessage("/1/mastervolumeVal", masterVolumeVal)
                    )
                )
            );

        _volumeManager.UseDecibels = true;
        _volumeManager.VolumeRegularIncrement = 1.0f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync();
        var updatedValue = VolumeManager.DecibelsToValue(expectedUpdatedVolumeDB);

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { updatedValue })
                )
            );

        Assert.True(updated);
        Assert.Equal(updatedValue, _volumeManager.Volume);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_RegularExceedsMaxPercentage_IsCappedAndUpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.48f),
                        new OscMessage("/1/mastervolumeVal", "-13.3 dB")
                    )
                )
            );

        _volumeManager.VolumeRegularIncrement = 0.05f;
        _volumeManager.VolumeMax = 0.50f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.50f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.50f, _volumeManager.Volume);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_RegularExceedsMaxDecibels_IsCappedAndUpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.48f),
                        new OscMessage("/1/mastervolumeVal", "-13.3 dB")
                    )
                )
            );

        _volumeManager.UseDecibels = true;
        _volumeManager.VolumeRegularIncrement = 1.0f;
        _volumeManager.VolumeMax = -13.0f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync();
        var updatedValue = VolumeManager.DecibelsToValue(-13.0f);

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { updatedValue })
                )
            );

        Assert.True(updated);
        Assert.Equal(updatedValue, _volumeManager.Volume);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_RegularAlreadyMaxPercentage_DoesNotUpdateVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.50f),
                        new OscMessage("/1/mastervolumeVal", "-12.1 dB")
                    )
                )
            );

        _volumeManager.VolumeRegularIncrement = 0.05f;
        _volumeManager.VolumeMax = 0.50f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync();

        Assert.False(updated);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_RegularAlreadyMaxDecibels_DoesNotUpdateVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.5004354f),
                        new OscMessage("/1/mastervolumeVal", "-12.1 dB")
                    )
                )
            );

        _volumeManager.UseDecibels = true;
        _volumeManager.VolumeRegularIncrement = 1.0f;
        _volumeManager.VolumeMax = -12.1f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync();

        Assert.False(updated);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_FineVolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        var updated = await _volumeManager.IncreaseVolumeAsync(fine: true);
        Assert.False(updated);
    }

    [Fact]
    public async Task FineAfterVolumeInitializedPercentage_UpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB")
                    )
                )
            );

        _volumeManager.VolumeFineIncrement = 0.01f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync(fine: true);

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.21000001f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.21000001f, _volumeManager.Volume);
    }

    [Fact]
    public async Task FineAfterVolumeInitializedDecibels_UpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.19532223f),
                        new OscMessage("/1/mastervolumeVal", "-38.7 dB")
                    )
                )
            );

        _volumeManager.UseDecibels = true;
        _volumeManager.VolumeFineIncrement = 0.5f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync(fine: true);
        var updatedValue = VolumeManager.DecibelsToValue(-38.5f);

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { updatedValue })
                )
            );

        Assert.True(updated);
        Assert.Equal(updatedValue, _volumeManager.Volume);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_RegularVolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        var updated = await _volumeManager.DecreaseVolumeAsync();
        Assert.False(updated);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_RegularAfterVolumeInitializedPercentage_UpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB")
                    )
                )
            );

        _volumeManager.VolumeRegularIncrement = 0.02f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.18f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.18f, _volumeManager.Volume);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_RegularAfterVolumeInitializedDecibels_UpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.5004354f),
                        new OscMessage("/1/mastervolumeVal", "-12.1 dB")
                    )
                )
            );

        _volumeManager.UseDecibels = true;
        _volumeManager.VolumeRegularIncrement = 1.0f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync();
        var updatedValue = VolumeManager.DecibelsToValue(-13.0f);

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { updatedValue })
                )
            );

        Assert.True(updated);
        Assert.Equal(updatedValue, _volumeManager.Volume);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_RegularBelowSilentPercentage_IsSetToSilentAndUpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.02f),
                        new OscMessage("/1/mastervolumeVal", "-62.0 dB")
                    )
                )
            );

        _volumeManager.VolumeRegularIncrement = 0.05f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.00f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.00f, _volumeManager.Volume);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_RegularBelowSilentDecibels_IsSetToSilentAndUpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.0065550446f),
                        new OscMessage("/1/mastervolumeVal", "-64.0 dB")
                    )
                )
            );

        _volumeManager.UseDecibels = true;
        _volumeManager.VolumeRegularIncrement = 1.0f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.0f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.0f, _volumeManager.Volume);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_RegularAlreadySilentPercentage_DoesNotUpdateVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.0f),
                        new OscMessage("/1/mastervolumeVal", "-oo")
                    )
                )
            );

        _volumeManager.VolumeRegularIncrement = 0.05f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync();

        Assert.False(updated);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_RegularAlreadySilentDecibels_DoesNotUpdateVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.0f),
                        new OscMessage("/1/mastervolumeVal", "-oo")
                    )
                )
            );

        _volumeManager.UseDecibels = true;
        _volumeManager.VolumeRegularIncrement = 1.0f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync();

        Assert.False(updated);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_FineVolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        var updated = await _volumeManager.DecreaseVolumeAsync(fine: true);
        Assert.False(updated);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_FineAfterVolumeInitializedPercentage_UpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB")
                    )
                )
            );

        _volumeManager.VolumeFineIncrement = 0.01f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync(fine: true);

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.19f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.19f, _volumeManager.Volume);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_FineAfterVolumeInitializedDecibels_UpdatesVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.19532223f),
                        new OscMessage("/1/mastervolumeVal", "-38.7 dB")
                    )
                )
            );

        _volumeManager.UseDecibels = true;
        _volumeManager.VolumeFineIncrement = 0.5f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync(fine: true);
        var updateValue = VolumeManager.DecibelsToValue(-39.0f);

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { updateValue })
                )
            );

        Assert.True(updated);
        Assert.Equal(updateValue, _volumeManager.Volume);
    }

    [Fact]
    public async Task ToggloDimAsync_VolumeNotInitialized_DoesNotUpdateDim_Async()
    {
        var updated = await _volumeManager.ToggloDimAsync();
        Assert.False(updated);
    }

    [Fact]
    public async Task ToggloDimAsync_AfterVolumeInitialized_EnableDim_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB")
                    )
                )
            );

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.ToggloDimAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mainDim" && m.SequenceEqual(new object[] { 1.0f })
                )
            );

        Assert.True(updated);
        Assert.Equal(1f, _volumeManager.Dim);
    }

    [Fact]
    public async Task ToggloDimAsync_AfterVolumeInitialized_DisableDim_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 1f),
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB")
                    )
                )
            );

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.ToggloDimAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mainDim" && m.SequenceEqual(new object[] { 1.0f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0f, _volumeManager.Dim);
    }

    [Theory]
    [InlineData(-65.0, 0.0)]
    [InlineData(-34.0, 0.2372316)]
    [InlineData(-62.1, 0.019291464)]
    [InlineData(-42.0, 0.16753459)]
    [InlineData(-1.5, 0.7715054)]
    [InlineData(3.0, 0.90860224)]
    [InlineData(6.0, 1.0)]
    public void DecibelsToValue_GivenVolumeInDecibels_ReturnsExpectedValue(
        float dBValue,
        float expectedValue
    )
    {
        var value = VolumeManager.DecibelsToValue(dBValue);
        Assert.Equal(expectedValue, value, precision: 3);
    }

    [Theory]
    [InlineData(0.0, -65.0)]
    [InlineData(0.2372316, -34.0)]
    [InlineData(0.019291464, -62.1)]
    [InlineData(0.16753459, -42.0)]
    [InlineData(0.7715054, -1.5)]
    [InlineData(0.90860224, 3.0)]
    [InlineData(1.0, 6.0)]
    public void ValueToDecibels_GivenVolumeValue_ReturnsExpectedDecibels(
        float value,
        float expectedDB
    )
    {
        var dB = VolumeManager.ValueToDecibels(value);
        Assert.Equal(expectedDB, dB, precision: 1);
    }
}
