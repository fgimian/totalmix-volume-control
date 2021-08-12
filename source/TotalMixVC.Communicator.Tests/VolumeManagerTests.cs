using System;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using OscCore;
using Xunit;

namespace TotalMixVC.Communicator.Tests
{
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
            public void InvalidVolumeRegularIncrement_ThrowsException(
                float volumeRegularIncrement)
            {
                // Arrange
                ISender sender = Substitute.For<ISender>();
                IListener listener = Substitute.For<IListener>();

                VolumeManager volumeManager = new(sender, listener);

                // Act & Assert
                Assert.Throws<ArgumentException>(
                    () => volumeManager.VolumeRegularIncrement = volumeRegularIncrement);
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
            public void InvalidVolumeFineIncrement_ThrowsException(
                float volumeFineIncrement)
            {
                // Arrange
                ISender sender = Substitute.For<ISender>();
                IListener listener = Substitute.For<IListener>();

                VolumeManager volumeManager = new(sender, listener);

                // Act & Assert
                Assert.Throws<ArgumentException>(
                    () => volumeManager.VolumeFineIncrement = volumeFineIncrement);
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

                // Act & Assert
                Assert.Throws<ArgumentException>(() => volumeManager.VolumeMax = volumeMax);
            }
        }

        public class RequestDeviceVolumeAsync
        {
            [Fact]
            public async Task RegularRequest_RequestsVolume_Async()
            {
                // Arrange
                ISender sender = Substitute.For<ISender>();
                IListener listener = Substitute.For<IListener>();

                VolumeManager volumeManager = new(sender, listener);

                // Act
                await volumeManager.RequestDeviceVolumeAsync().ConfigureAwait(false);

                // Assert
                await sender
                    .Received()
                    .SendAsync(Arg.Is<OscMessage>(
                        m =>
                            m.Address == "/1/mastervolume"
                            && m.SequenceEqual(new object[] { -1.0f })))
                    .ConfigureAwait(false);
            }
        }

        public class ReceiveVolumeAsync
        {
            [Fact]
            public async Task ReceivesBoth_UpdatesVolume_Async()
            {
                // Arrange
                ISender sender = Substitute.For<ISender>();
                IListener listener = Substitute.For<IListener>();
                listener
                    .ReceiveAsync()
                    .Returns(Task.FromResult<OscPacket>(new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB"))));

                VolumeManager volumeManager = new(sender, listener);

                // Act
                bool received = await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);

                // Assert
                Assert.True(received);
                Assert.Equal(0.20f, volumeManager.Volume);
                Assert.Equal("-38.2 dB", volumeManager.VolumeDecibels);
                Assert.True(volumeManager.IsVolumeInitialized);
            }

            [Fact]
            public async Task ReceivesBothInvalid_DoesNotReceiveResult_Async()
            {
                // Arrange
                ISender sender = Substitute.For<ISender>();
                IListener listener = Substitute.For<IListener>();
                listener
                    .ReceiveAsync()
                    .Returns(Task.FromResult<OscPacket>(new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mastervolume", "-38.2 dB"),
                        new OscMessage("/1/mastervolumeVal", 0.20f))));

                VolumeManager volumeManager = new(sender, listener);

                // Act
                bool received = await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(
                        Task.FromResult<OscPacket>(new OscBundle(
                            OscTimeTag.Now,
                            new OscMessage("/1/mastervolume", 0.20f),
                            new OscMessage("/1/mastervolumeVal", "-38.2 dB"))),
                        Task.FromResult<OscPacket>(new OscBundle(
                            OscTimeTag.Now,
                            new OscMessage("/1/mastervolumeVal", "-40.5 dB"))));

                VolumeManager volumeManager = new(sender, listener);

                // Act
                bool received1 = await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);
                bool received2 = await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);

                // Assert
                Assert.True(received1);
                Assert.True(received2);
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
                    .ReceiveAsync()
                    .Returns(
                        Task.FromResult<OscPacket>(new OscBundle(
                            OscTimeTag.Now,
                            new OscMessage("/1/mastervolumeVal", -1.0f))));

                VolumeManager volumeManager = new(sender, listener);

                // Act
                bool received = await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(Task.FromResult<OscPacket>(new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/volume1", 0.20f),
                        new OscMessage("/1/volume1Val", "-38.2 dB"))));

                VolumeManager volumeManager = new(sender, listener);

                // Act
                bool received = await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(Task.FromResult<OscPacket>(
                        new OscMessage("/1/mastervolume", 0.20f)));

                VolumeManager volumeManager = new(sender, listener);

                // Act
                bool received = await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(Task.FromException<OscPacket>(
                        new OscException(OscError.MissingComma, "weov")));

                VolumeManager volumeManager = new(sender, listener);

                // Act
                bool received = await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(
                        Task.FromResult<OscPacket>(new OscBundle(
                            OscTimeTag.Now,
                            new OscMessage("/1/mastervolume", 0.20f),
                            new OscMessage("/1/mastervolumeVal", "-38.2 dB"))),
                        Task.FromException<OscPacket>(new TimeoutException("weov")));

                VolumeManager volumeManager = new(sender, listener);

                // Act & Assert
                Assert.True(await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false));
                Assert.Equal(0.20f, volumeManager.Volume);
                Assert.Equal("-38.2 dB", volumeManager.VolumeDecibels);
                Assert.True(volumeManager.IsVolumeInitialized);

                await Assert
                    .ThrowsAsync<TimeoutException>(async () =>
                        await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false))
                    .ConfigureAwait(false);
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

                VolumeManager volumeManager = new(sender, listener)
                {
                    VolumeRegularIncrement = 0.02f
                };

                // Act
                bool updated = await volumeManager.IncreaseVolumeAsync().ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(Task.FromResult<OscPacket>(new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB"))));

                VolumeManager volumeManager = new(sender, listener)
                {
                    VolumeRegularIncrement = 0.02f
                };

                // Act
                await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);
                bool updated = await volumeManager.IncreaseVolumeAsync().ConfigureAwait(false);

                // Assert
                await sender
                    .Received()
                    .SendAsync(Arg.Is<OscMessage>(
                        m =>
                            m.Address == "/1/mastervolume"
                            && m.SequenceEqual(new object[] { 0.22f })))
                    .ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(Task.FromResult<OscPacket>(new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mastervolume", 0.48f),
                        new OscMessage("/1/mastervolumeVal", "-13.3 dB"))));

                VolumeManager volumeManager = new(sender, listener)
                {
                    VolumeRegularIncrement = 0.05f,
                    VolumeMax = 0.50f
                };

                // Act
                await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);
                bool updated = await volumeManager.IncreaseVolumeAsync().ConfigureAwait(false);

                // Assert
                await sender
                    .Received()
                    .SendAsync(Arg.Is<OscMessage>(
                        m =>
                            m.Address == "/1/mastervolume"
                            && m.SequenceEqual(new object[] { 0.50f })))
                    .ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(Task.FromResult<OscPacket>(new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mastervolume", 0.50f),
                        new OscMessage("/1/mastervolumeVal", "-12.1 dB"))));

                VolumeManager volumeManager = new(sender, listener)
                {
                    VolumeRegularIncrement = 0.05f,
                    VolumeMax = 0.50f
                };

                // Act
                await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);
                bool updated = await volumeManager.IncreaseVolumeAsync().ConfigureAwait(false);

                // Assert
                Assert.False(updated);
            }

            [Fact]
            public async Task FineVolumeNotInitialized_DoesNotUpdateVolume_Async()
            {
                // Arrange
                ISender sender = Substitute.For<ISender>();
                IListener listener = Substitute.For<IListener>();

                VolumeManager volumeManager = new(sender, listener)
                {
                    VolumeFineIncrement = 0.01f
                };

                // Act
                bool updated = await volumeManager
                    .IncreaseVolumeAsync(fine: true)
                    .ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(Task.FromResult<OscPacket>(new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB"))));

                VolumeManager volumeManager = new(sender, listener)
                {
                    VolumeFineIncrement = 0.01f
                };

                // Act
                await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);
                bool updated = await volumeManager
                    .IncreaseVolumeAsync(fine: true)
                    .ConfigureAwait(false);

                // Assert
                await sender
                    .Received()
                    .SendAsync(Arg.Is<OscMessage>(
                        m =>
                            m.Address == "/1/mastervolume"
                            && m.SequenceEqual(new object[] { 0.21000001f })))
                    .ConfigureAwait(false);

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

                VolumeManager volumeManager = new(sender, listener)
                {
                    VolumeRegularIncrement = 0.02f
                };

                // Act
                bool updated = await volumeManager.DecreaseVolumeAsync().ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(Task.FromResult<OscPacket>(new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB"))));

                VolumeManager volumeManager = new(sender, listener)
                {
                    VolumeRegularIncrement = 0.02f
                };

                // Act
                await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);
                bool updated = await volumeManager.DecreaseVolumeAsync().ConfigureAwait(false);

                // Assert
                await sender
                    .Received()
                    .SendAsync(Arg.Is<OscMessage>(
                        m =>
                            m.Address == "/1/mastervolume"
                            && m.SequenceEqual(new object[] { 0.18f })))
                    .ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(Task.FromResult<OscPacket>(new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mastervolume", 0.02f),
                        new OscMessage("/1/mastervolumeVal", "-62.0 dB"))));

                VolumeManager volumeManager = new(sender, listener)
                {
                    VolumeRegularIncrement = 0.05f
                };

                // Act
                await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);
                bool updated = await volumeManager.DecreaseVolumeAsync().ConfigureAwait(false);

                // Assert
                await sender
                    .Received()
                    .SendAsync(Arg.Is<OscMessage>(
                        m =>
                            m.Address == "/1/mastervolume"
                            && m.SequenceEqual(new object[] { 0.00f })))
                    .ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(Task.FromResult<OscPacket>(new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mastervolume", 0.00f),
                        new OscMessage("/1/mastervolumeVal", "-oo"))));

                VolumeManager volumeManager = new(sender, listener)
                {
                    VolumeRegularIncrement = 0.05f,
                    VolumeMax = 0.50f
                };

                // Act
                await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);
                bool updated = await volumeManager.DecreaseVolumeAsync().ConfigureAwait(false);

                // Assert
                Assert.False(updated);
            }

            [Fact]
            public async Task FineVolumeNotInitialized_DoesNotUpdateVolume_Async()
            {
                // Arrange
                ISender sender = Substitute.For<ISender>();
                IListener listener = Substitute.For<IListener>();

                VolumeManager volumeManager = new(sender, listener)
                {
                    VolumeFineIncrement = 0.01f
                };

                // Act
                bool updated = await volumeManager
                    .DecreaseVolumeAsync(fine: true)
                    .ConfigureAwait(false);

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
                    .ReceiveAsync()
                    .Returns(Task.FromResult<OscPacket>(new OscBundle(
                        OscTimeTag.Now,
                        new OscMessage("/1/mastervolume", 0.20f),
                        new OscMessage("/1/mastervolumeVal", "-38.2 dB"))));

                VolumeManager volumeManager = new(sender, listener)
                {
                    VolumeFineIncrement = 0.01f
                };

                // Act
                await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false);
                bool updated = await volumeManager
                    .DecreaseVolumeAsync(fine: true)
                    .ConfigureAwait(false);

                // Assert
                await sender
                    .Received()
                    .SendAsync(Arg.Is<OscMessage>(
                        m =>
                            m.Address == "/1/mastervolume"
                            && m.SequenceEqual(new object[] { 0.19f })))
                    .ConfigureAwait(false);

                Assert.True(updated);
                Assert.Equal(0.19f, volumeManager.Volume);
            }
        }
    }
}
