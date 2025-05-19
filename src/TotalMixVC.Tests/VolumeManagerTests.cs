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
        _volumeManager = new(_sender) { Listener = _listener };
    }

    public void Dispose()
    {
        _volumeManager.Dispose();
    }

    [Fact]
    public async Task RequestVolumeAsync_Request_RequestsVolume_Async()
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

        var snapshot = await _volumeManager.ReceiveVolumeAsync();

        Assert.NotNull(snapshot);
        Assert.Equal(0.20f, snapshot.Volume);
        Assert.Equal("-38.2 dB", snapshot.VolumeDecibels);
        Assert.False(snapshot.IsDimmed);
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

        var snapshot = await _volumeManager.ReceiveVolumeAsync();

        Assert.NotNull(snapshot);
        Assert.Equal(0.20f, snapshot.Volume);
        Assert.Equal("-38.2 dB", snapshot.VolumeDecibels);
        Assert.True(snapshot.IsDimmed);
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

        var snapshot = await _volumeManager.ReceiveVolumeAsync();
        var isVolumeInitialized = await _volumeManager.IsVolumeInitializedAsync();

        Assert.Null(snapshot);
        Assert.False(isVolumeInitialized);
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

        var snapshotAll = await _volumeManager.ReceiveVolumeAsync();
        var snapshotDecibelsOnly = await _volumeManager.ReceiveVolumeAsync();

        Assert.NotNull(snapshotAll);
        Assert.Equal(0.20f, snapshotAll.Volume);
        Assert.Equal("-38.2 dB", snapshotAll.VolumeDecibels);
        Assert.NotNull(snapshotDecibelsOnly);
        Assert.Equal(0.20f, snapshotDecibelsOnly.Volume);
        Assert.Equal("-40.5 dB", snapshotDecibelsOnly.VolumeDecibels);
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

        var snapshot = await _volumeManager.ReceiveVolumeAsync();

        Assert.Null(snapshot);
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

        var snapshot = await _volumeManager.ReceiveVolumeAsync();

        Assert.Null(snapshot);
    }

    [Fact]
    public async Task ReceiveVolumeAsync_IncorrectPacketType_DoesNotReceiveResult_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromResult<OscPacket>(new OscMessage("/1/mastervolume", 0.20f))
            );

        var snapshot = await _volumeManager.ReceiveVolumeAsync();

        Assert.Null(snapshot);
    }

    [Fact]
    public async Task ReceiveVolumeAsync_PacketMalformed_DoesNotReceiveResult_Async()
    {
        _listener
            .ReceiveAsync(default)
            .ReturnsForAnyArgs(
                Task.FromException<OscPacket>(new OscException(OscError.MissingComma, "weov"))
            );

        var snapshot = await _volumeManager.ReceiveVolumeAsync();

        Assert.Null(snapshot);
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

        var snapshot = await _volumeManager.ReceiveVolumeAsync();

        Assert.NotNull(snapshot);

        await Assert.ThrowsAsync<TimeoutException>(async () =>
            await _volumeManager.ReceiveVolumeAsync()
        );

        var isVolumeInitializedAfterTimeout = await _volumeManager.IsVolumeInitializedAsync();

        Assert.False(isVolumeInitializedAfterTimeout);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_VolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        var updated = await _volumeManager.IncreaseVolumeAsync();
        Assert.False(updated);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_AfterVolumeInitializedPercent_UpdatesVolume_Async()
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

        _volumeManager.VolumeIncrementPercent = 0.02f;
        _volumeManager.VolumeMaxPercent = 1.0f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.22f })
                )
            );

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.Equal(0.22f, snapshot.Volume);
    }

    [Theory]
    [InlineData(0.19964331f, "-38.2 dB", -38.0f)]
    [InlineData(0.2372316f, "-34.0 dB", -33.0f)]
    [InlineData(0.2764855f, "-29.9 dB", -29.0f)]
    public async Task IncreaseVolumeAsync_AfterVolumeInitializedDecibels_UpdatesVolume_Async(
        float masterVolume,
        string masterVolumeVal,
        float expectedUpdatedVolumeDecibels
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
        _volumeManager.VolumeIncrementDecibels = 1.0f;
        _volumeManager.VolumeMaxDecibels = 6.0f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync();
        var updatedValue = VolumeManager.DecibelsToValue(expectedUpdatedVolumeDecibels);

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { updatedValue })
                )
            );

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.Equal(updatedValue, snapshot.Volume);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_ExceedsMaxPercent_IsCappedAndUpdatesVolume_Async()
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

        _volumeManager.VolumeIncrementPercent = 0.05f;
        _volumeManager.VolumeMaxPercent = 0.50f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.50f })
                )
            );

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.Equal(0.50f, snapshot.Volume);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_ExceedsMaxDecibels_IsCappedAndUpdatesVolume_Async()
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
        _volumeManager.VolumeIncrementDecibels = 1.0f;
        _volumeManager.VolumeMaxDecibels = -13.0f;

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

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.Equal(updatedValue, snapshot.Volume);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_AlreadyMaxPercent_DoesNotUpdateVolume_Async()
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

        _volumeManager.VolumeIncrementPercent = 0.05f;
        _volumeManager.VolumeMaxPercent = 0.50f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync();

        Assert.False(updated);
    }

    [Fact]
    public async Task IncreaseVolumeAsync_AlreadyMaxDecibels_DoesNotUpdateVolume_Async()
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
        _volumeManager.VolumeIncrementDecibels = 1.0f;
        _volumeManager.VolumeMaxDecibels = -12.1f;

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
    public async Task FineAfterVolumeInitializedPercent_UpdatesVolume_Async()
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

        _volumeManager.VolumeFineIncrementPercent = 0.01f;
        _volumeManager.VolumeMaxPercent = 1.0f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.IncreaseVolumeAsync(fine: true);

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.21000001f })
                )
            );

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.Equal(0.21000001f, snapshot.Volume);
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
        _volumeManager.VolumeFineIncrementDecibels = 0.5f;
        _volumeManager.VolumeMaxDecibels = 6.0f;

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

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.Equal(updatedValue, snapshot.Volume);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_VolumeNotInitialized_DoesNotUpdateVolume_Async()
    {
        var updated = await _volumeManager.DecreaseVolumeAsync();
        Assert.False(updated);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_AfterVolumeInitializedPercent_UpdatesVolume_Async()
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

        _volumeManager.VolumeIncrementPercent = 0.02f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.18f })
                )
            );

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.Equal(0.18f, snapshot.Volume);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_AfterVolumeInitializedDecibels_UpdatesVolume_Async()
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
        _volumeManager.VolumeIncrementDecibels = 1.0f;
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

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.Equal(updatedValue, snapshot.Volume);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_BelowSilentPercent_IsSetToSilentAndUpdatesVolume_Async()
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

        _volumeManager.VolumeIncrementPercent = 0.05f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.00f })
                )
            );

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.Equal(0.00f, snapshot.Volume);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_BelowSilentDecibels_IsSetToSilentAndUpdatesVolume_Async()
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
        _volumeManager.VolumeIncrementDecibels = 1.0f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync();

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.0f })
                )
            );

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.Equal(0.0f, snapshot.Volume);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_AlreadySilentPercent_DoesNotUpdateVolume_Async()
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

        _volumeManager.VolumeIncrementPercent = 0.05f;

        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync();

        Assert.False(updated);
    }

    [Fact]
    public async Task DecreaseVolumeAsync_AlreadySilentDecibels_DoesNotUpdateVolume_Async()
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
        _volumeManager.VolumeIncrementDecibels = 1.0f;

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
    public async Task DecreaseVolumeAsync_FineAfterVolumeInitializedPercent_UpdatesVolume_Async()
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

        _volumeManager.VolumeFineIncrementPercent = 0.01f;
        await _volumeManager.ReceiveVolumeAsync();
        var updated = await _volumeManager.DecreaseVolumeAsync(fine: true);

        await _sender
            .Received()
            .SendAsync(
                Arg.Is<OscMessage>(m =>
                    m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.19f })
                )
            );

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.Equal(0.19f, snapshot.Volume);
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
        _volumeManager.VolumeFineIncrementDecibels = 0.5f;
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

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.Equal(updateValue, snapshot.Volume);
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

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.True(snapshot.IsDimmed);
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

        var snapshot = await _volumeManager.GetDeviceSnapshotAsync();

        Assert.True(updated);
        Assert.NotNull(snapshot);
        Assert.False(snapshot.IsDimmed);
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
        float expectedDecibels
    )
    {
        var dB = VolumeManager.ValueToDecibels(value);
        Assert.Equal(expectedDecibels, dB, precision: 1);
    }
}
