﻿using NSubstitute;
using OscCore;
using TotalMixVC.Communicator;
using Xunit;

namespace TotalMixVC.Tests;

public static class VolumeManagerTests
{
    public class Constructor
    {
        [Fact]
        public void ValidVolumeRegularIncrement_SetsProperty()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();

            VolumeManager volumeManager = new(sender, listener);

            // Act
            volumeManager.VolumeRegularIncrement = 0.03f;

            // Assert
            Assert.Equal(0.03f, volumeManager.VolumeRegularIncrement);
        }

        [Theory]
        [InlineData(0.30f)]
        [InlineData(-0.01f)]
        public void InvalidVolumeRegularIncrement_ThrowsException(float volumeRegularIncrement)
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();

            VolumeManager volumeManager = new(sender, listener);

            // Act
            Action action = () => volumeManager.VolumeRegularIncrement = volumeRegularIncrement;

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void ValidVolumeFineIncrement_SetsProperty()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();

            VolumeManager volumeManager = new(sender, listener);

            // Act
            volumeManager.VolumeFineIncrement = 0.01f;

            // Assert
            Assert.Equal(0.01f, volumeManager.VolumeFineIncrement);
        }

        [Theory]
        [InlineData(0.10f)]
        [InlineData(-0.03f)]
        public void InvalidVolumeFineIncrement_ThrowsException(float volumeFineIncrement)
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();

            VolumeManager volumeManager = new(sender, listener);

            // Act
            Action action = () => volumeManager.VolumeFineIncrement = volumeFineIncrement;

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void ValidVolumeMax_SetsProperty()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();

            VolumeManager volumeManager = new(sender, listener);

            // Act
            volumeManager.VolumeMax = 0.90f;

            // Assert
            Assert.Equal(0.90f, volumeManager.VolumeMax);
        }

        [Theory]
        [InlineData(1.10f)]
        [InlineData(-0.15f)]
        public void InvalidVolumeMax_ThrowsException(float volumeMax)
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();

            VolumeManager volumeManager = new(sender, listener);

            // Act
            Action action = () => volumeManager.VolumeMax = volumeMax;

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(action);
        }
    }

    public class RequestVolumeAsync
    {
        [Fact]
        public async Task RegularRequest_RequestsVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();

            VolumeManager volumeManager = new(sender, listener);

            // Act
            await volumeManager.RequestVolumeAsync();

            // Assert
            await sender
                .Received()
                .SendAsync(
                    Arg.Is<OscMessage>(m =>
                        m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { -1.0f })
                    )
                );
        }
    }

    public class ReceiveVolumeAsync
    {
        [Fact]
        public async Task ReceivesAllNormal_UpdatesVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager = new(sender, listener);

            // Act
            bool received = await volumeManager.ReceiveVolumeAsync();

            // Assert
            Assert.True(received);
            Assert.Equal(0.20f, volumeManager.Volume);
            Assert.Equal("-38.2 dB", volumeManager.VolumeDecibels);
            Assert.False(volumeManager.IsDimmed);
            Assert.True(volumeManager.IsVolumeInitialized);
        }

        [Fact]
        public async Task ReceivesAllDimmed_UpdatesVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager = new(sender, listener);

            // Act
            bool received = await volumeManager.ReceiveVolumeAsync();

            // Assert
            Assert.True(received);
            Assert.Equal(0.20f, volumeManager.Volume);
            Assert.Equal("-38.2 dB", volumeManager.VolumeDecibels);
            Assert.True(volumeManager.IsDimmed);
            Assert.True(volumeManager.IsVolumeInitialized);
        }

        [Fact]
        public async Task ReceivesInvalidTypes_DoesNotReceiveResult_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager = new(sender, listener);

            // Act
            bool received = await volumeManager.ReceiveVolumeAsync();

            // Assert
            Assert.False(received);
            Assert.False(volumeManager.IsVolumeInitialized);
        }

        [Fact]
        public async Task ReceivesDecibelsOnly_UpdatesVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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
                        new OscBundle(
                            OscTimeTag.Now,
                            new OscMessage("/1/mastervolumeVal", "-40.5 dB")
                        )
                    )
                );

            VolumeManager volumeManager = new(sender, listener);

            // Act
            bool receivedAll = await volumeManager.ReceiveVolumeAsync();
            bool receivedDecibelsOnly = await volumeManager.ReceiveVolumeAsync();

            // Assert
            Assert.True(receivedAll);
            Assert.True(receivedDecibelsOnly);
            Assert.Equal(0.20f, volumeManager.Volume);
            Assert.Equal("-40.5 dB", volumeManager.VolumeDecibels);
            Assert.True(volumeManager.IsVolumeInitialized);
        }

        [Fact]
        public async Task ReceivesDecibelsOnlyInvalid_DoesNotReceiveResult_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
                .ReceiveAsync(default)
                .ReturnsForAnyArgs(
                    Task.FromResult<OscPacket>(
                        new OscBundle(OscTimeTag.Now, new OscMessage("/1/mastervolumeVal", -1.0f))
                    )
                );

            VolumeManager volumeManager = new(sender, listener);

            // Act
            bool received = await volumeManager.ReceiveVolumeAsync();

            // Assert
            Assert.False(received);
            Assert.False(volumeManager.IsVolumeInitialized);
        }

        [Fact]
        public async Task ReceivesOtherVolume_DoesNotReceiveResult_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager = new(sender, listener);

            // Act
            bool received = await volumeManager.ReceiveVolumeAsync();

            // Assert
            Assert.False(received);
            Assert.False(volumeManager.IsVolumeInitialized);
        }

        [Fact]
        public async Task IncorrectPacketType_DoesNotReceiveResult_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
                .ReceiveAsync(default)
                .ReturnsForAnyArgs(
                    Task.FromResult<OscPacket>(new OscMessage("/1/mastervolume", 0.20f))
                );

            VolumeManager volumeManager = new(sender, listener);

            // Act
            bool received = await volumeManager.ReceiveVolumeAsync();

            // Assert
            Assert.False(received);
            Assert.False(volumeManager.IsVolumeInitialized);
        }

        [Fact]
        public async Task PacketMalformed_DoesNotReceiveResult_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
                .ReceiveAsync(default)
                .ReturnsForAnyArgs(
                    Task.FromException<OscPacket>(new OscException(OscError.MissingComma, "weov"))
                );

            VolumeManager volumeManager = new(sender, listener);

            // Act
            bool received = await volumeManager.ReceiveVolumeAsync();

            // Assert
            Assert.False(received);
            Assert.False(volumeManager.IsVolumeInitialized);
        }

        [Fact]
        public async Task Timeout_ThrowsExceptionAndResetsVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager = new(sender, listener);

            // Act
            bool received = await volumeManager.ReceiveVolumeAsync();
            bool initializedAfterVolumeReceived = volumeManager.IsVolumeInitialized;

            Func<Task> task = async () => await volumeManager.ReceiveVolumeAsync();

            // Assert
            Assert.True(received);
            Assert.True(initializedAfterVolumeReceived);

            await Assert.ThrowsAsync<TimeoutException>(task);
            Assert.False(volumeManager.IsVolumeInitialized);
        }
    }

    public class IncreaseVolumeAsync
    {
        [Fact]
        public async Task RegularVolumeNotInitialized_DoesNotUpdateVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();

            VolumeManager volumeManager = new(sender, listener) { VolumeRegularIncrement = 0.02f };

            // Act
            bool updated = await volumeManager.IncreaseVolumeAsync();

            // Assert
            Assert.False(updated);
        }

        [Fact]
        public async Task RegularAfterVolumeInitialized_UpdatesVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager = new(sender, listener) { VolumeRegularIncrement = 0.02f };

            // Act
            await volumeManager.ReceiveVolumeAsync();
            bool updated = await volumeManager.IncreaseVolumeAsync();

            // Assert
            await sender
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
        public async Task RegularExceedsMax_IsCappedAndUpdatesVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager =
                new(sender, listener) { VolumeRegularIncrement = 0.05f, VolumeMax = 0.50f };

            // Act
            await volumeManager.ReceiveVolumeAsync();
            bool updated = await volumeManager.IncreaseVolumeAsync();

            // Assert
            await sender
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
        public async Task RegularAlreadyMax_DoesNotUpdateVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager =
                new(sender, listener) { VolumeRegularIncrement = 0.05f, VolumeMax = 0.50f };

            // Act
            await volumeManager.ReceiveVolumeAsync();
            bool updated = await volumeManager.IncreaseVolumeAsync();

            // Assert
            Assert.False(updated);
        }

        [Fact]
        public async Task FineVolumeNotInitialized_DoesNotUpdateVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();

            VolumeManager volumeManager = new(sender, listener) { VolumeFineIncrement = 0.01f };

            // Act
            bool updated = await volumeManager.IncreaseVolumeAsync(fine: true);

            // Assert
            Assert.False(updated);
        }

        [Fact]
        public async Task FineAfterVolumeInitialized_UpdatesVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager = new(sender, listener) { VolumeFineIncrement = 0.01f };

            // Act
            await volumeManager.ReceiveVolumeAsync();
            bool updated = await volumeManager.IncreaseVolumeAsync(fine: true);

            // Assert
            await sender
                .Received()
                .SendAsync(
                    Arg.Is<OscMessage>(m =>
                        m.Address == "/1/mastervolume"
                        && m.SequenceEqual(new object[] { 0.21000001f })
                    )
                );

            Assert.True(updated);
            Assert.Equal(0.21000001f, volumeManager.Volume);
        }
    }

    public class DecreaseVolumeAsync
    {
        [Fact]
        public async Task RegularVolumeNotInitialized_DoesNotUpdateVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();

            VolumeManager volumeManager = new(sender, listener) { VolumeRegularIncrement = 0.02f };

            // Act
            bool updated = await volumeManager.DecreaseVolumeAsync();

            // Assert
            Assert.False(updated);
        }

        [Fact]
        public async Task RegularAfterVolumeInitialized_UpdatesVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager = new(sender, listener) { VolumeRegularIncrement = 0.02f };

            // Act
            await volumeManager.ReceiveVolumeAsync();
            bool updated = await volumeManager.DecreaseVolumeAsync();

            // Assert
            await sender
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
        public async Task RegularBelowSilent_IsSetToSilentAndUpdatesVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager = new(sender, listener) { VolumeRegularIncrement = 0.05f };

            // Act
            await volumeManager.ReceiveVolumeAsync();
            bool updated = await volumeManager.DecreaseVolumeAsync();

            // Assert
            await sender
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
        public async Task RegularAlreadySilent_DoesNotUpdateVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager =
                new(sender, listener) { VolumeRegularIncrement = 0.05f, VolumeMax = 0.50f };

            // Act
            await volumeManager.ReceiveVolumeAsync();
            bool updated = await volumeManager.DecreaseVolumeAsync();

            // Assert
            Assert.False(updated);
        }

        [Fact]
        public async Task FineVolumeNotInitialized_DoesNotUpdateVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();

            VolumeManager volumeManager = new(sender, listener) { VolumeFineIncrement = 0.01f };

            // Act
            bool updated = await volumeManager.DecreaseVolumeAsync(fine: true);

            // Assert
            Assert.False(updated);
        }

        [Fact]
        public async Task FineAfterVolumeInitialized_UpdatesVolume_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager = new(sender, listener) { VolumeFineIncrement = 0.01f };

            // Act
            await volumeManager.ReceiveVolumeAsync();
            bool updated = await volumeManager.DecreaseVolumeAsync(fine: true);

            // Assert
            await sender
                .Received()
                .SendAsync(
                    Arg.Is<OscMessage>(m =>
                        m.Address == "/1/mastervolume" && m.SequenceEqual(new object[] { 0.19f })
                    )
                );

            Assert.True(updated);
            Assert.Equal(0.19f, volumeManager.Volume);
        }
    }

    public class ToggloDimAsync
    {
        [Fact]
        public async Task VolumeNotInitialized_DoesNotUpdateDim_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();

            VolumeManager volumeManager = new(sender, listener);

            // Act
            bool updated = await volumeManager.ToggloDimAsync();

            // Assert
            Assert.False(updated);
        }

        [Fact]
        public async Task AfterVolumeInitialized_EnableDim_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager = new(sender, listener);

            // Act
            await volumeManager.ReceiveVolumeAsync();
            bool updated = await volumeManager.ToggloDimAsync();

            // Assert
            await sender
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
        public async Task AfterVolumeInitialized_DisableDim_Async()
        {
            // Arrange
            ISender sender = Substitute.For<ISender>();
            IListener listener = Substitute.For<IListener>();
            listener
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

            VolumeManager volumeManager = new(sender, listener);

            // Act
            await volumeManager.ReceiveVolumeAsync();
            bool updated = await volumeManager.ToggloDimAsync();

            // Assert
            await sender
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
}
