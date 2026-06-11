using System.Collections.ObjectModel;
using TotalMixVC.Configuration;
using Xunit;

namespace TotalMixVC.Tests;

public sealed class ConfigTests
{
    [Fact]
    public void FromToml_ValidConfiguration_LoadsAllProperties()
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
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
            diagnostics
        );

        var expectedConfig = new Config()
        {
            Osc = new Osc()
            {
                RawOutgoingEndPoint = "127.0.0.1:7002",
                RawIncomingEndPoint = "127.0.0.1:9002",
            },
            Volume = new Volume()
            {
                UseDecibels = true,
                IncrementPercent = 0.04f,
                FineIncrementPercent = 0.02f,
                MaxPercent = 0.8f,
                IncrementDecibels = 1.0f,
                FineIncrementDecibels = 0.5f,
                MaxDecibels = 0.0f,
            },
            Theme = new Theme()
            {
                BackgroundRounding = 5.0,
                RawBackgroundColor = "#1e4328",
                RawHeadingTotalmixColor = "#eeeeee",
                RawHeadingVolumeColor = "#e05454",
                RawVolumeReadoutColorNormal = "#eeeeee",
                RawVolumeReadoutColorDimmed = "#eefa50",
                RawVolumeBarBackgroundColor = "#222222",
                RawVolumeBarForegroundColorNormal = "#888888",
                RawVolumeBarForegroundColorDimmed = "#886500",
                RawTrayTooltipMessageColor = "#eeeeee",
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
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void FromToml_SemiValidConfiguration_LoadsSomeProperties()
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            """
            [osc]
            outgoing_endpoint = "poop"
            incoming_endpoint = "127.0.0.1:9002"

            [volume]
            use_decibels = true
            increment_percent = 0.4
            fine_increment_percent = 0.02
            max_percent = 0.8
            increment_decibels = 1.0
            fine_increment_decibels = 0.5
            max_decibels = 0.0

            [theme]
            background_rounding = 5.0
            background_color = "#1e4328"
            heading_totalmix_color = "#eeeeee"
            heading_volume_color = "oops"
            volume_readout_color_normal = "#eeeeee"
            volume_readout_color_dimmed = "#eefa50"
            volume_bar_background_color = "#222222"
            volume_bar_foreground_color_normal = "#888888"
            volume_bar_foreground_color_dimmed = "#886500"
            tray_tooltip_message_color = "#eeeeee"

            [interface]
            scaling = 0.0
            position_offset = 45.0
            hide_delay = 3.0
            fade_out_time = 0.5
            show_remote_volume_changes = true
            """,
            diagnostics
        );

        var expectedConfig = new Config()
        {
            Osc = new Osc() { RawIncomingEndPoint = "127.0.0.1:9002" },
            Volume = new Volume()
            {
                UseDecibels = true,
                FineIncrementPercent = 0.02f,
                MaxPercent = 0.8f,
                IncrementDecibels = 1.0f,
                FineIncrementDecibels = 0.5f,
                MaxDecibels = 0.0f,
            },
            Theme = new Theme()
            {
                BackgroundRounding = 5.0,
                RawBackgroundColor = "#1e4328",
                RawHeadingTotalmixColor = "#eeeeee",
                RawVolumeReadoutColorNormal = "#eeeeee",
                RawVolumeReadoutColorDimmed = "#eefa50",
                RawVolumeBarBackgroundColor = "#222222",
                RawVolumeBarForegroundColorNormal = "#888888",
                RawVolumeBarForegroundColorDimmed = "#886500",
                RawTrayTooltipMessageColor = "#eeeeee",
            },
            Interface = new Interface()
            {
                PositionOffset = 45.0,
                HideDelay = 3.0,
                FadeOutTime = 0.5,
                ShowRemoteVolumeChanges = true,
            },
        };
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Equal(4, diagnostics.Count);
        Assert.Equal(
            [
                "(osc.outgoing_endpoint) : error : An invalid endpoint address was specified.",
                "(volume.increment_percent) : error : The value must be greater than 0 and less "
                    + "than or equal to 0.1.",
                "(theme.heading_volume_color) : error : The color specified was invalid.",
                "(interface.scaling) : error : The value must be greater than 0.",
            ],
            diagnostics
        );
    }

    [Fact]
    public void FromToml_InvalidConfiguration_ResetsPropertiesToDefaults()
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            """
            [osc]
            outgoing_endpoint = "oops"
            incoming_endpoint = "oops"

            [volume]
            increment_percent = 0.4
            fine_increment_percent = 0.2
            max_percent = 8.0
            increment_decibels = 10.0
            fine_increment_decibels = 5.0
            max_decibels = 16.0

            [theme]
            background_rounding = -1.0
            background_color = "oops"
            heading_totalmix_color = "oops"
            heading_volume_color = "oops"
            volume_readout_color_normal = "oops"
            volume_readout_color_dimmed = "oops"
            volume_bar_background_color = "oops"
            volume_bar_foreground_color_normal = "oops"
            volume_bar_foreground_color_dimmed = "oops"
            tray_tooltip_message_color = "oops"

            [interface]
            scaling = 0.0
            position_offset = -1.0
            hide_delay = 0.0
            fade_out_time = -1.0
            """,
            diagnostics
        );

        var expectedConfig = new Config();
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Equal(22, diagnostics.Count);
    }

    [Fact]
    public void FromToml_InvalidToml_ReportsDiagnostics()
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            """
            [osc]
            outgoing_endpoint
            """,
            diagnostics
        );

        var expectedConfig = new Config();
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Single(diagnostics);
        Assert.Equal("(2,18) : error : Expected `=` after key but was `Eof`.", diagnostics[0]);
    }

    [Theory]
    [InlineData("background_color")]
    [InlineData("heading_totalmix_color")]
    [InlineData("heading_volume_color")]
    [InlineData("volume_readout_color_normal")]
    [InlineData("volume_readout_color_dimmed")]
    [InlineData("volume_bar_background_color")]
    [InlineData("volume_bar_foreground_color_normal")]
    [InlineData("volume_bar_foreground_color_dimmed")]
    [InlineData("tray_tooltip_message_color")]
    public void FromToml_InvalidColor_SkipsLoadingProperty(string name)
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            $"""
            [theme]
            {name} = "wow"
            """,
            diagnostics
        );

        var expectedConfig = new Config();
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Single(diagnostics);
        Assert.Equal($"(theme.{name}) : error : The color specified was invalid.", diagnostics[0]);
    }

    [Theory]
    [InlineData("outgoing_endpoint")]
    [InlineData("incoming_endpoint")]
    public void FromToml_InvalidIPEndPoint_SkipsLoadingProperty(string name)
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            $"""
            [osc]
            {name} = "oopsies"
            """,
            diagnostics
        );

        var expectedConfig = new Config();
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Single(diagnostics);
        Assert.Equal(
            $"(osc.{name}) : error : An invalid endpoint address was specified.",
            diagnostics[0]
        );
    }

    [Fact]
    public void FromToml_ValidVolumeIncrementPercent_LoadsProperty()
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            """
            [volume]
            increment_percent = 0.03
            """,
            diagnostics
        );

        var expectedConfig = new Config() { Volume = new Volume() { IncrementPercent = 0.03f } };
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(0.30f)]
    [InlineData(-0.01f)]
    public void FromToml_InvalidVolumeIncrementPercent_SkipsLoadingProperty(
        float volumeIncrementPercent
    )
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            $"""
            [volume]
            increment_percent = {volumeIncrementPercent:F2}
            """,
            diagnostics
        );

        var expectedConfig = new Config();
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Single(diagnostics);
        Assert.Equal(
            "(volume.increment_percent) : error : The value must be greater than 0 and less "
                + "than or equal to 0.1.",
            diagnostics[0]
        );
    }

    [Fact]
    public void FromToml_ValidVolumeFineIncrementPercent_LoadsProperty()
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            """
            [volume]
            fine_increment_percent = 0.01
            """,
            diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { FineIncrementPercent = 0.01f },
        };
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(0.10f)]
    [InlineData(-0.03f)]
    public void FromToml_InvalidVolumeFineIncrementPercent_SkipsLoadingProperty(
        float volumeFineIncrementPercent
    )
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            $"""
            [volume]
            fine_increment_percent = {volumeFineIncrementPercent:F2}
            """,
            diagnostics
        );

        var expectedConfig = new Config();
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Single(diagnostics);
        Assert.Equal(
            "(volume.fine_increment_percent) : error : The value must be greater than 0 and "
                + "less than or equal to 0.05.",
            diagnostics[0]
        );
    }

    [Fact]
    public void FromToml_ValidVolumeMaxPercent_LoadsProperty()
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            """
            [volume]
            max_percent = 0.90
            """,
            diagnostics
        );

        var expectedConfig = new Config() { Volume = new Volume() { MaxPercent = 0.90f } };
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(1.10f)]
    [InlineData(-0.15f)]
    public void FromToml_InvalidVolumeMaxPercent_SkipsLoadingProperty(float volumeMaxPercent)
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            $"""
            [volume]
            max_percent = {volumeMaxPercent:F2}
            """,
            diagnostics
        );

        var expectedConfig = new Config();
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Single(diagnostics);
        Assert.Equal(
            "(volume.max_percent) : error : The value must be greater than 0 and less than or "
                + "equal to 1.0.",
            diagnostics[0]
        );
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
    public void FromToml_ValidVolumeIncrementDecibels_LoadsProperty(float volumeIncrementDecibels)
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            $"""
            [volume]
            increment_decibels = {volumeIncrementDecibels:F1}
            """,
            diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { IncrementDecibels = volumeIncrementDecibels },
        };
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Empty(diagnostics);
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
    public void FromToml_InvalidVolumeIncrementDecibels_SkipsLoadingProperty(
        float volumeIncrementDecibels
    )
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            $"""
            [volume]
            increment_decibels = {volumeIncrementDecibels:F2}
            """,
            diagnostics
        );

        var expectedConfig = new Config();
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Single(diagnostics);
        Assert.Equal(
            "(volume.increment_decibels) : error : The value must be a multiple of 0.5 while "
                + "being greater than 0 and less than or equal to 6.0.",
            diagnostics[0]
        );
    }

    [Theory]
    [InlineData(0.25f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2.0f)]
    [InlineData(2.75f)]
    public void FromToml_ValidVolumeFineIncrementDecibels_LoadsProperty(
        float volumeFineIncrementDecibels
    )
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            $"""
            [volume]
            fine_increment_decibels = {volumeFineIncrementDecibels:F2}
            """,
            diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { FineIncrementDecibels = volumeFineIncrementDecibels },
        };
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(-0.03f)]
    [InlineData(0.3f)]
    [InlineData(1.1f)]
    [InlineData(1.9f)]
    [InlineData(3.25f)]
    [InlineData(3.5f)]
    public void FromToml_InvalidVolumeFineIncrementDecibels_SkipsLoadingProperty(
        float volumeFineIncrementDecibels
    )
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            $"""
            [volume]
            fine_increment_decibels = {volumeFineIncrementDecibels}
            """,
            diagnostics
        );

        var expectedConfig = new Config();
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Single(diagnostics);
        Assert.Equal(
            "(volume.fine_increment_decibels) : error : The value must be a multiple of 0.25 "
                + "while being greater than 0 and less than or equal to 3.0.",
            diagnostics[0]
        );
    }

    [Theory]
    [InlineData(-61.2f)]
    [InlineData(-32.0f)]
    [InlineData(0.0f)]
    [InlineData(3.5f)]
    [InlineData(6.0f)]
    public void FromToml_ValidVolumeMaxDecibels_LoadsProperty(float volumeMaxDecibels)
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            $"""
            [volume]
            max_decibels = {volumeMaxDecibels:F1}
            """,
            diagnostics
        );

        var expectedConfig = new Config()
        {
            Volume = new Volume() { MaxDecibels = volumeMaxDecibels },
        };
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(6.1f)]
    [InlineData(10.0f)]
    public void FromToml_InvalidVolumeMaxDecibels_SkipsLoadingProperty(float volumeMaxDecibels)
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            $"""
            [volume]
            max_decibels = {volumeMaxDecibels:F1}
            """,
            diagnostics
        );

        var expectedConfig = new Config();
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Single(diagnostics);
        Assert.Equal(
            "(volume.max_decibels) : error : The value must be less than or equal to 6.0.",
            diagnostics[0]
        );
    }

    [Fact]
    public void FromToml_InvalidDoubles_ResetsPropertiesToDefaults()
    {
        var diagnostics = new Collection<string>();
        var config = Config.FromToml(
            """
            [theme]
            background_rounding = -1.0

            [interface]
            scaling = 0.0
            position_offset = -1.0
            hide_delay = -10.0
            fade_out_time = -5.0
            """,
            diagnostics
        );

        var expectedConfig = new Config()
        {
            Theme = new Theme() { BackgroundRounding = 1.0 },
            Interface = new Interface()
            {
                Scaling = 1.0,
                PositionOffset = 40.0,
                HideDelay = 2.0,
                FadeOutTime = 0.75,
            },
        };
        var expectedConfigDiagnostics = new Collection<string>();
        expectedConfig.ParseAndValidate(expectedConfigDiagnostics);

        Assert.Empty(expectedConfigDiagnostics);
        Assert.Equal(expectedConfig, config);
        Assert.Equal(5, diagnostics.Count);
        Assert.Equal(
            [
                "(theme.background_rounding) : error : The value must be greater than or equal "
                    + "to 0.",
                "(interface.scaling) : error : The value must be greater than 0.",
                "(interface.position_offset) : error : The value must be greater than "
                    + "or equal to 0.",
                "(interface.hide_delay) : error : The value must be greater than 0.",
                "(interface.fade_out_time) : error : The value must be greater than or equal to 0.",
            ],
            diagnostics
        );
    }
}
