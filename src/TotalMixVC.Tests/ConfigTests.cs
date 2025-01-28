using System.Net;
using System.Windows.Media;
using TotalMixVC.Configuration;
using Xunit;

namespace TotalMixVC.Tests;

public sealed class ConfigTests
{
    [Fact]
    public void TryFromToml_ValidConfiguration_LoadsAllProperties()
    {
        var isValid = Config.TryFromToml(
            """
            [osc]
            outgoing_endpoint = "127.0.0.1:7002"
            incoming_endpoint = "127.0.0.1:9002"

            [volume]
            use_decibels = true
            increment_percent = 0.04
            fine_increment_percent = 0.02
            max_percent = 0.8
            increment_decibels = 1.0
            fine_increment_decibels = 0.5
            max_decibels = 0.0

            [theme]
            background_rounding = 5.0
            background_color = "#1e4328"
            heading_totalmix_color = "#eeeeee"
            heading_volume_color = "#e05454"
            volume_readout_color_normal = "#eeeeee"
            volume_readout_color_dimmed = "#eefa50"
            volume_bar_background_color = "#222222"
            volume_bar_foreground_color_normal = "#888888"
            volume_bar_foreground_color_dimmed = "#886500"
            tray_tooltip_message_color = "#eeeeee"

            [interface]
            scaling = 1.1
            position_offset = 45.0
            hide_delay = 3.0
            fade_out_time = 0.5
            show_remote_volume_changes = true
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Osc = new Osc()
            {
                OutgoingEndPoint = new IPEndPoint(IPAddress.Loopback, 7002),
                IncomingEndPoint = new IPEndPoint(IPAddress.Loopback, 9002),
            },
            Volume = new Volume()
            {
                UseDecibels = true,
                IncrementPercent = new(0.04f),
                FineIncrementPercent = new(0.02f),
                MaxPercent = new(0.8f),
                IncrementDecibels = new(1.0f),
                FineIncrementDecibels = new(0.5f),
                MaxDecibels = new(0.0f),
            },
            Theme = new Theme()
            {
                BackgroundRounding = 5.0,
                BackgroundColor = Color.FromRgb(0x1e, 0x43, 0x28),
                HeadingTotalmixColor = Color.FromRgb(0xee, 0xee, 0xee),
                HeadingVolumeColor = Color.FromRgb(0xe0, 0x54, 0x54),
                VolumeReadoutColorNormal = Color.FromRgb(0xee, 0xee, 0xee),
                VolumeReadoutColorDimmed = Color.FromRgb(0xee, 0xfa, 0x50),
                VolumeBarBackgroundColor = Color.FromRgb(0x22, 0x22, 0x22),
                VolumeBarForegroundColorNormal = Color.FromRgb(0x88, 0x88, 0x88),
                VolumeBarForegroundColorDimmed = Color.FromRgb(0x88, 0x65, 0x00),
                TrayTooltipMessageColor = Color.FromRgb(0xee, 0xee, 0xee),
            },
            Interface = new Interface()
            {
                Scaling = 1.1,
                PositionOffset = 45.0,
                HideDelay = 3.0,
                FadeOutTime = 0.5,
                ShowRemoteVolumeChanges = true,
            },
        };

        Assert.True(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Empty(diagnostics);
        Assert.Equal(expectedConfig, config);
    }

    [Fact]
    public void TryFromToml_InvalidColor_SkipsLoadingProperty()
    {
        var isValid = Config.TryFromToml(
            """
            [theme]
            background_color = "#1e4328"
            heading_totalmix_color = "wow"
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Theme = new Theme() { BackgroundColor = Color.FromRgb(0x1e, 0x43, 0x28) },
        };

        Assert.False(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(expectedConfig, config);
    }

    [Fact]
    public void TryFromToml_InvalidIPEndPoint_SkipsLoadingProperty()
    {
        var isValid = Config.TryFromToml(
            """
            [osc]
            outgoing_endpoint = "127.0.0.1:7002"
            incoming_endpoint = "oopsies"
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Osc = new Osc()
            {
                OutgoingEndPoint = new IPEndPoint(IPAddress.Loopback, 7002),
                IncomingEndPoint = new IPEndPoint(IPAddress.Loopback, 9001),
            },
        };

        Assert.False(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(expectedConfig, config);
    }

    [Fact]
    public void TryFromToml_ValidVolumeIncrementPercent_LoadsProperty()
    {
        var isValid = Config.TryFromToml(
            """
            [volume]
            increment_percent = 0.03
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { IncrementPercent = new(0.03f) },
        };

        Assert.True(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Empty(diagnostics);
        Assert.Equal(expectedConfig, config);
    }

    [Theory]
    [InlineData(0.30f)]
    [InlineData(-0.01f)]
    public void TryFromToml_InvalidVolumeIncrementPercent_SkipsLoadingProperty(
        float volumeIncrementPercent
    )
    {
        var isValid = Config.TryFromToml(
            $"""
            [volume]
            increment_percent = {volumeIncrementPercent:F2}
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { IncrementPercent = new(0.02f) },
        };

        Assert.False(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(expectedConfig, config);
    }

    [Fact]
    public void TryFromToml_ValidVolumeFineIncrementPercent_LoadsProperty()
    {
        var isValid = Config.TryFromToml(
            """
            [volume]
            fine_increment_percent = 0.01
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { FineIncrementPercent = new(0.01f) },
        };

        Assert.True(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Empty(diagnostics);
        Assert.Equal(expectedConfig, config);
    }

    [Theory]
    [InlineData(0.10f)]
    [InlineData(-0.03f)]
    public void TryFromToml_InvalidVolumeFineIncrementPercent_SkipsLoadingProperty(
        float volumeFineIncrementPercent
    )
    {
        var isValid = Config.TryFromToml(
            $"""
            [volume]
            fine_increment_percent = {volumeFineIncrementPercent:F2}
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { FineIncrementPercent = new(0.01f) },
        };

        Assert.False(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(expectedConfig, config);
    }

    [Fact]
    public void TryFromToml_ValidVolumeMaxPercent_LoadsProperty()
    {
        var isValid = Config.TryFromToml(
            """
            [volume]
            max_percent = 0.90
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config() { Volume = new Volume() { MaxPercent = new(0.90f) } };

        Assert.True(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Empty(diagnostics);
        Assert.Equal(expectedConfig, config);
    }

    [Theory]
    [InlineData(1.10f)]
    [InlineData(-0.15f)]
    public void TryFromToml_InvalidVolumeMaxPercent_SkipsLoadingProperty(float volumeMaxPercent)
    {
        var isValid = Config.TryFromToml(
            $"""
            [volume]
            max_percent = {volumeMaxPercent:F2}
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config() { Volume = new Volume() { MaxPercent = new(1.0f) } };

        Assert.False(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(expectedConfig, config);
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(1.5f)]
    [InlineData(2.0f)]
    [InlineData(2.5f)]
    [InlineData(4.0f)]
    [InlineData(5.0f)]
    [InlineData(5.5f)]
    public void TryFromToml_ValidVolumeIncrementDecibels_LoadsProperty(
        float volumeIncrementDecibels
    )
    {
        var isValid = Config.TryFromToml(
            $"""
            [volume]
            increment_decibels = {volumeIncrementDecibels:F1}
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { IncrementDecibels = new(volumeIncrementDecibels) },
        };

        Assert.True(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Empty(diagnostics);
        Assert.Equal(expectedConfig, config);
    }

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(0.0f)]
    [InlineData(0.25f)]
    [InlineData(0.75f)]
    [InlineData(1.1f)]
    [InlineData(1.25f)]
    [InlineData(1.75f)]
    [InlineData(2.7f)]
    [InlineData(3.1f)]
    [InlineData(5.75f)]
    [InlineData(6.25f)]
    [InlineData(6.5f)]
    public void TryFromToml_InvalidVolumeIncrementDecibels_SkipsLoadingProperty(
        float volumeIncrementDecibels
    )
    {
        var isValid = Config.TryFromToml(
            $"""
            [volume]
            increment_decibels = {volumeIncrementDecibels:F2}
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { IncrementDecibels = new(2.0f) },
        };

        Assert.False(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(expectedConfig, config);
    }

    [Theory]
    [InlineData(0.25f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2.0f)]
    [InlineData(2.75f)]
    public void TryFromToml_ValidVolumeFineIncrementDecibels_LoadsProperty(
        float volumeFineIncrementDecibels
    )
    {
        var isValid = Config.TryFromToml(
            $"""
            [volume]
            fine_increment_decibels = {volumeFineIncrementDecibels:F2}
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { FineIncrementDecibels = new(volumeFineIncrementDecibels) },
        };

        Assert.True(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Empty(diagnostics);
        Assert.Equal(expectedConfig, config);
    }

    [Theory]
    [InlineData(-0.03f)]
    [InlineData(0.3f)]
    [InlineData(1.1f)]
    [InlineData(1.9f)]
    [InlineData(3.25f)]
    [InlineData(3.5f)]
    public void TryFromToml_InvalidVolumeFineIncrementDecibels_SkipsLoadingProperty(
        float volumeFineIncrementDecibels
    )
    {
        var isValid = Config.TryFromToml(
            $"""
            [volume]
            fine_increment_decibels = {volumeFineIncrementDecibels}
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { FineIncrementDecibels = new(1.0f) },
        };

        Assert.False(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(expectedConfig, config);
    }

    [Theory]
    [InlineData(-61.2f)]
    [InlineData(-32.0f)]
    [InlineData(0.0f)]
    [InlineData(3.5f)]
    [InlineData(6.0f)]
    public void TryFromToml_ValidVolumeMaxDecibels_LoadsProperty(float volumeMaxDecibels)
    {
        var isValid = Config.TryFromToml(
            $"""
            [volume]
            max_decibels = {volumeMaxDecibels:F1}
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { MaxDecibels = new(volumeMaxDecibels) },
        };

        Assert.True(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Empty(diagnostics);
        Assert.Equal(expectedConfig, config);
    }

    [Theory]
    [InlineData(6.1f)]
    [InlineData(10.0f)]
    public void TryFromToml_InvalidVolumeMaxDecibels_SkipsLoadingProperty(float volumeMaxDecibels)
    {
        var isValid = Config.TryFromToml(
            $"""
            [volume]
            max_decibels = {volumeMaxDecibels:F1}
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config() { Volume = new Volume() { MaxDecibels = new(6.0f) } };

        Assert.False(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(expectedConfig, config);
    }

    [Fact]
    public void TryFromToml_InvalidDoubles_ResetsPropertiesToDefaults()
    {
        var isValid = Config.TryFromToml(
            """
            [theme]
            background_rounding = -1.0

            [interface]
            scaling = 0.0
            position_offset = -1.0
            hide_delay = -10.0
            fade_out_time = -5.0
            """,
            out var config,
            out var diagnostics
        );

        var expectedConfig = new Config()
        {
            Theme = new Theme() { BackgroundRounding = 0.0 },
            Interface = new Interface()
            {
                Scaling = double.Epsilon,
                PositionOffset = 0.0,
                HideDelay = double.Epsilon,
                FadeOutTime = 0.0,
            },
        };

        Assert.True(isValid);
        Assert.NotNull(config);
        Assert.NotNull(diagnostics);
        Assert.Empty(diagnostics);
        Assert.Equal(expectedConfig, config);
    }
}
