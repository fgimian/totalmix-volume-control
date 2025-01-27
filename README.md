# TotalMix Volume Control

[![Build Status](https://github.com/fgimian/totalmix-volume-control/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/fgimian/totalmix-volume-control/actions)
[![Coverage Status](https://codecov.io/gh/fgimian/totalmix-volume-control/branch/main/graph/badge.svg?token=tp21mkIuFm)](https://codecov.io/gh/fgimian/totalmix-volume-control)
[![License](https://img.shields.io/github/license/fgimian/totalmix-volume-control)](https://github.com/fgimian/totalmix-volume-control/blob/main/LICENSE)
[![Latest Release](https://img.shields.io/github/v/release/fgimian/totalmix-volume-control?include_prereleases)](https://github.com/fgimian/totalmix-volume-control/releases)

![Logo](https://raw.githubusercontent.com/fgimian/totalmix-volume-control/main/images/Logo.png)

This project uses the OSC protocol to communicate with RME TotalMixFX software and binds
the volume keys on your keyboard to control the master volume and mute key to control dim in
Windows.

You may see the [related thread on the RME forums](https://forum.rme-audio.de/viewtopic.php?pid=174137)
and the [thread on the Gearspace forums](https://gearspace.com/board/music-computers/1358200-my-new-little-open-source-project-rme-totalmix-volume-control-windows.html)
for a discussion about the tool.

## Getting Started

### Downloading the Installer

You may download the latest release from the
[releases page](https://github.com/fgimian/totalmix-volume-control/releases). Simply expand the
**Assets** item under the latest release and download the release installer.

### Usage

After starting the application, the keyboard shortcut bindings are enabled:

* **Volume Up / Down**: Increase or decrease the volume by 1% respectively.
* **Shift + Volume Up / Down**: Increase or decrease the volume by 0.5% respectively
  (fine adjustment).
* **Mute**: Toggles the dim model of the device.

**Note**: You can still access the regular volume indicator by using **Ctrl + Volume Up / Down**
or **Alt + Volume Up / Down** and the regular mute functionality using **Ctrl + Mute**
or **Alt + Mute**.

When the volume is changed, a volume indicator will appear in the top left of your screen:

![Volume Indicator](https://raw.githubusercontent.com/fgimian/totalmix-volume-control/main/images/VolumeIndicator.png)

The text and volume bar will turn orange if the device is dimmed.

If you hover over the tray icon, a tooltip will appear that indicates the health of the connection
with your device.

![Tray Tooltip](https://raw.githubusercontent.com/fgimian/totalmix-volume-control/main/images/TrayTooltip.png)

### Configuring TotalMixFX

The following instructions will be provided when you hover over the tray icon in the case that
there is a communication issue with your device. However, I provide them here for your convenience
also:

1. Open TotalMixFX
2. Tick **Options** / **Enable OSC Control**
3. Open **Options** / **Settings** and select the **OSC** tab
4. Ensure **Remote Controller Select** is set to **1**
5. Ensure that **In Use** is ticked
6. Ensure that **Port incoming** is set to **7001**
7. Ensure that **Port outgoing** is set to **9001**
8. Ensure that **IP or Host Name** is set to **127.0.0.1**

### Configuring TotalMix Volume Control

If you wish to dig deeper, you can configure almost every aspect of TotalMix Volume Control via
a configuration file.

Start by browsing to `%APPDATA%` in Windows Explorer and creating a directory named
"TotalMix Volume Control". Now download the
[sample configuration file](https://github.com/fgimian/totalmix-volume-control/blob/main/config.sample.toml),
rename it to "config.toml" and place it in the directory you created.

You may open this file in any text editor and read the included instructions to configure the
application. All the pre-filled values you see in the sample configuration are optional and
represent the application defaults.

You may live-reload your updated configuration by right-clicking on the TotalMix Volume Control
tray icon and selecting "Reload config" which will update all settings except the incoming OSC
endpoint which requires an application restart to be changed.

## Building from Source

After cloning the source code from GitHub, follow the steps below:

1. Install the [.NET 7.0 SDK](https://dotnet.microsoft.com/download).
2. Install Task and the cross-platform coreutils using WinGet

    ```
    winget install Task.Task uutils.coreutils
    ```

3. Build the project using the default **Debug** configuration and create the installer

    ```
    task build
    ```

    You may create a release build by passing the **CONFIGURATION** variable to the task:

    ```
    task build CONFIGURATION=Release
    ```

4. Once you're ready, you can publish a self-container version of the application and create the
   installer using the **installer** target

    ```
    task installer CONFIGURATION=Release
    ```

You'll now find an installer under the **artifacts** sub-directory.

You may find various other build targets by simply typing `task` with no arguments.

## Building the Icon

You may follow the steps below to build the icon from the provided SVG:

1. Install ImageMagick and Inkscape using WinGet

    ```
    winget install ImageMagick.ImageMagick Inkscape.Inkscape
    ```

2. Add **C:\Program Files\Inkscape\bin** to your **Path** environment variable
3. Build the icon

    ```
    task icon
    ```

## License

TotalMix Volume Control is released under the **MIT** license. Please see the
[LICENSE](https://github.com/fgimian/totalmix-volume-control/blob/main/LICENSE) file for more
details.
