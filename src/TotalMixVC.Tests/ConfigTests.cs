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
            increment = 1.0
            fine_increment = 0.5
            max = 0.0

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
                Increment = 1.0f,
                FineIncrement = 0.5f,
                Max = 0.0f,
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
