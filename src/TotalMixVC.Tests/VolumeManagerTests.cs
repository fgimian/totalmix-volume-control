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
    public void Constructor_ValidVolumeRegularIncrement_SetsProperty()
    {
        _volumeManager.VolumeRegularIncrement = 0.03f;
        Assert.Equal(0.03f, _volumeManager.VolumeRegularIncrement);
    }

    [Theory]
    [InlineData(0.30f)]
    [InlineData(-0.01f)]
    public void Constructor_InvalidVolumeRegularIncrement_ThrowsException(
        float volumeRegularIncrement
    )
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _volumeManager.VolumeRegularIncrement = volumeRegularIncrement
        );
    }

    [Fact]
    public void Constructor_ValidVolumeFineIncrement_SetsProperty()
    {
        _volumeManager.VolumeFineIncrement = 0.01f;
        Assert.Equal(0.01f, _volumeManager.VolumeFineIncrement);
    }

    [Theory]
    [InlineData(0.10f)]
    [InlineData(-0.03f)]
    public void Constructor_InvalidVolumeFineIncrement_ThrowsException(float volumeFineIncrement)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _volumeManager.VolumeFineIncrement = volumeFineIncrement
        );
    }

    [Fact]
    public void Constructor_ValidVolumeMax_SetsProperty()
    {
        _volumeManager.VolumeMax = 0.90f;
        Assert.Equal(0.90f, _volumeManager.VolumeMax);
    }

    [Theory]
    [InlineData(1.10f)]
    [InlineData(-0.15f)]
    public void Constructor_InvalidVolumeMax_ThrowsException(float volumeMax)
    {
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
        _volumeManager.VolumeRegularIncrement = 0.02f;
        var updated = await _volumeManager.IncreaseVolumeAsync();
        Assert.False(updated);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_RegularAfterVolumeInitialized_UpdatesVolume_Async()
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

    [Fact]
    public async Task IncreaseVolumeAsync_RegularExceedsMax_IsCappedAndUpdatesVolume_Async()
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
    public async Task IncreaseVolumeAsync_RegularAlreadyMax_DoesNotUpdateVolume_Async()
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
    public async Task IncreaseVolumeAsync_FineVolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        _volumeManager.VolumeFineIncrement = 0.01f;
        var updated = await _volumeManager.IncreaseVolumeAsync(fine: true);
        Assert.False(updated);
    }

    [Fact]
    public async Task FineAfterVolumeInitialized_UpdatesVolume_Async()
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
    public async Task DecreaseVolumeAsync_RegularVolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        _volumeManager.VolumeRegularIncrement = 0.02f;
        var updated = await _volumeManager.DecreaseVolumeAsync();
        Assert.False(updated);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_RegularAfterVolumeInitialized_UpdatesVolume_Async()
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
    public async Task DecreaseVolumeAsync_RegularBelowSilent_IsSetToSilentAndUpdatesVolume_Async()
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
    public async Task DecreaseVolumeAsync_RegularAlreadySilent_DoesNotUpdateVolume_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(
                    new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mainDim", 0f),
                        new OscMessage("/1/mastervolume", 0.00f),
                        new OscMessage("/1/mastervolumeVal", "-oo")
                    )
                )
            );

        _volumeManager.VolumeRegularIncrement = 0.05f;
        _volumeManager.VolumeMax = 0.50f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync();

        Assert.False(updated);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_FineVolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        _volumeManager.VolumeFineIncrement = 0.01f;
        var updated = await _volumeManager.DecreaseVolumeAsync(fine: true);
        Assert.False(updated);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_FineAfterVolumeInitialized_UpdatesVolume_Async()
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
}
