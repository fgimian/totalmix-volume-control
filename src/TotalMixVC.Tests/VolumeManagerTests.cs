using NSubstitute;
using OscCore;
using TotalMixVC.Communicator;
using Xunit;

namespace TotalMixVC.Tests;

public class VolumeManagerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private readonly IListener _listener = Substitute.For<IListener>();

    public VolumeManagerTests()
    {
        _sender = Substitute.For<ISender>();
        _listener = Substitute.For<IListener>();
    }

    [Fact]
    public void Constructor_ValidVolumeRegularIncrement_SetsProperty()
    {
        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeRegularIncrement = 0.03f,
        };

        Assert.Equal(0.03f, volumeManager.VolumeRegularIncrement);
    }

    [Theory]
    [InlineData(0.30f)]
    [InlineData(-0.01f)]
    public void Constructor_InvalidVolumeRegularIncrement_ThrowsException(
        float volumeRegularIncrement
    )
    {
        using var volumeManager = new VolumeManager(_sender, _listener);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => volumeManager.VolumeRegularIncrement = volumeRegularIncrement
        );
    }

    [Fact]
    public void Constructor_ValidVolumeFineIncrement_SetsProperty()
    {
        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeFineIncrement = 0.01f,
        };

        Assert.Equal(0.01f, volumeManager.VolumeFineIncrement);
    }

    [Theory]
    [InlineData(0.10f)]
    [InlineData(-0.03f)]
    public void Constructor_InvalidVolumeFineIncrement_ThrowsException(float volumeFineIncrement)
    {
        using var volumeManager = new VolumeManager(_sender, _listener);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => volumeManager.VolumeFineIncrement = volumeFineIncrement
        );
    }

    [Fact]
    public void Constructor_ValidVolumeMax_SetsProperty()
    {
        using var volumeManager = new VolumeManager(_sender, _listener) { VolumeMax = 0.90f };
        Assert.Equal(0.90f, volumeManager.VolumeMax);
    }

    [Theory]
    [InlineData(1.10f)]
    [InlineData(-0.15f)]
    public void Constructor_InvalidVolumeMax_ThrowsException(float volumeMax)
    {
        using var volumeManager = new VolumeManager(_sender, _listener);
        Assert.Throws<ArgumentOutOfRangeException>(() => volumeManager.VolumeMax = volumeMax);
    }

    [Fact]
    public async Task RequestVolumeAsync_RegularRequest_RequestsVolume_Async()
    {
        using var volumeManager = new VolumeManager(_sender, _listener);
        await volumeManager.RequestVolumeAsync();
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

        using var volumeManager = new VolumeManager(_sender, _listener);
        var received = await volumeManager.ReceiveVolumeAsync();

        Assert.True(received);
        Assert.Equal(0.20f, volumeManager.Volume);
        Assert.Equal("-38.2 dB", volumeManager.VolumeDecibels);
        Assert.False(volumeManager.IsDimmed);
        Assert.True(volumeManager.IsVolumeInitialized);
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

        using var volumeManager = new VolumeManager(_sender, _listener);
        var received = await volumeManager.ReceiveVolumeAsync();

        Assert.True(received);
        Assert.Equal(0.20f, volumeManager.Volume);
        Assert.Equal("-38.2 dB", volumeManager.VolumeDecibels);
        Assert.True(volumeManager.IsDimmed);
        Assert.True(volumeManager.IsVolumeInitialized);
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

        using var volumeManager = new VolumeManager(_sender, _listener);
        var received = await volumeManager.ReceiveVolumeAsync();

        Assert.False(received);
        Assert.False(volumeManager.IsVolumeInitialized);
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

        using var volumeManager = new VolumeManager(_sender, _listener);
        var receivedAll = await volumeManager.ReceiveVolumeAsync();
        var receivedDecibelsOnly = await volumeManager.ReceiveVolumeAsync();

        Assert.True(receivedAll);
        Assert.True(receivedDecibelsOnly);
        Assert.Equal(0.20f, volumeManager.Volume);
        Assert.Equal("-40.5 dB", volumeManager.VolumeDecibels);
        Assert.True(volumeManager.IsVolumeInitialized);
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

        using var volumeManager = new VolumeManager(_sender, _listener);
        var received = await volumeManager.ReceiveVolumeAsync();

        Assert.False(received);
        Assert.False(volumeManager.IsVolumeInitialized);
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

        using var volumeManager = new VolumeManager(_sender, _listener);
        var received = await volumeManager.ReceiveVolumeAsync();

        Assert.False(received);
        Assert.False(volumeManager.IsVolumeInitialized);
    }

    [Fact]
    public async Task ReceiveVolumeAsync_IncorrectPacketType_DoesNotReceiveResult_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(new OscMessage("/1/mastervolume", 0.20f))
            );

        using var volumeManager = new VolumeManager(_sender, _listener);
        var received = await volumeManager.ReceiveVolumeAsync();

        Assert.False(received);
        Assert.False(volumeManager.IsVolumeInitialized);
    }

    [Fact]
    public async Task ReceiveVolumeAsync_PacketMalformed_DoesNotReceiveResult_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromException<OscPacket>(new OscException(OscError.MissingComma, "weov"))
            );

        using var volumeManager = new VolumeManager(_sender, _listener);
        var received = await volumeManager.ReceiveVolumeAsync();

        Assert.False(received);
        Assert.False(volumeManager.IsVolumeInitialized);
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

        using var volumeManager = new VolumeManager(_sender, _listener);

        var received = await volumeManager.ReceiveVolumeAsync();
        var initializedAfterVolumeReceived = volumeManager.IsVolumeInitialized;

        Assert.True(received);
        Assert.True(initializedAfterVolumeReceived);

        await Assert.ThrowsAsync<TimeoutException>(
            async () => await volumeManager.ReceiveVolumeAsync()
        );
        Assert.False(volumeManager.IsVolumeInitialized);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_RegularVolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeRegularIncrement = 0.02f,
        };

        var updated = await volumeManager.IncreaseVolumeAsync();

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

        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeRegularIncrement = 0.02f,
        };

        await volumeManager.ReceiveVolumeAsync();
        var updated = await volumeManager.IncreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.22f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.22f, volumeManager.Volume);
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

        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeRegularIncrement = 0.05f,
            VolumeMax = 0.50f,
        };

        await volumeManager.ReceiveVolumeAsync();
        var updated = await volumeManager.IncreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.50f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.50f, volumeManager.Volume);
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

        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeRegularIncrement = 0.05f,
            VolumeMax = 0.50f,
        };

        await volumeManager.ReceiveVolumeAsync();
        var updated = await volumeManager.IncreaseVolumeAsync();

        Assert.False(updated);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_FineVolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeFineIncrement = 0.01f,
        };

        var updated = await volumeManager.IncreaseVolumeAsync(fine: true);

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

        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeFineIncrement = 0.01f,
        };

        await volumeManager.ReceiveVolumeAsync();
        var updated = await volumeManager.IncreaseVolumeAsync(fine: true);

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.21000001f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.21000001f, volumeManager.Volume);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_RegularVolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeRegularIncrement = 0.02f,
        };

        var updated = await volumeManager.DecreaseVolumeAsync();

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

        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeRegularIncrement = 0.02f,
        };

        await volumeManager.ReceiveVolumeAsync();
        var updated = await volumeManager.DecreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.18f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.18f, volumeManager.Volume);
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

        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeRegularIncrement = 0.05f,
        };

        await volumeManager.ReceiveVolumeAsync();
        var updated = await volumeManager.DecreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.00f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.00f, volumeManager.Volume);
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

        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeRegularIncrement = 0.05f,
            VolumeMax = 0.50f,
        };

        await volumeManager.ReceiveVolumeAsync();
        var updated = await volumeManager.DecreaseVolumeAsync();

        Assert.False(updated);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_FineVolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeFineIncrement = 0.01f,
        };

        var updated = await volumeManager.DecreaseVolumeAsync(fine: true);

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

        using var volumeManager = new VolumeManager(_sender, _listener)
        {
            VolumeFineIncrement = 0.01f,
        };

        await volumeManager.ReceiveVolumeAsync();
        var updated = await volumeManager.DecreaseVolumeAsync(fine: true);

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.19f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0.19f, volumeManager.Volume);
    }

    [Fact]
    public async Task ToggloDimAsync_VolumeNotInitialized_DoesNotUpdateDim_Async()
    {
        using var volumeManager = new VolumeManager(_sender, _listener);
        var updated = await volumeManager.ToggloDimAsync();
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

        using var volumeManager = new VolumeManager(_sender, _listener);
        await volumeManager.ReceiveVolumeAsync();
        var updated = await volumeManager.ToggloDimAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mainDim" && m.SequenceEqual(new object[] { 1.0f })
                )
            );

        Assert.True(updated);
        Assert.Equal(1f, volumeManager.Dim);
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

        using var volumeManager = new VolumeManager(_sender, _listener);
        await volumeManager.ReceiveVolumeAsync();
        var updated = await volumeManager.ToggloDimAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mainDim" && m.SequenceEqual(new object[] { 1.0f })
                )
            );

        Assert.True(updated);
        Assert.Equal(0f, volumeManager.Dim);
    }
}
