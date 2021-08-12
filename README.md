# TotalMix Volume Control

[![Build Status](https://dev.azure.com/fgimian/TotalMixVC/_apis/build/status/fgimian.totalmix-volume-control?branchName=main)](https://dev.azure.com/fgimian/TotalMixVC/_build/latest?definitionId=1&branchName=main)
[![Coverage Status](https://img.shields.io/azure-devops/coverage/fgimian/TotalMixVC/1.svg)](https://dev.azure.com/fgimian/TotalMixVC/_build/latest?definitionId=1&branchName=main)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/fgimian/totalmix-volume-control/blob/main/LICENSE)

![Logo](https://raw.githubusercontent.com/fgimian/totalmix-volume-control/main/images/Logo.png)

This project uses the OSC protocol to communicate with RME TotalMixFX software and binds
the volume keys on your keyboard to control the master volume in Windows.

You may see the [related thread on the RME forums](https://forum.rme-audio.de/viewtopic.php?pid=174137)
and the [thread on the Gearspace forums](https://gearspace.com/board/music-computers/1358200-my-new-little-open-source-project-rme-totalmix-volume-control-windows.html)
for a discussion about the tool.

## Getting Started

### Downloading the Installer

You may download the latest release from the
[releases page](https://github.com/fgimian/totalmix-volume-control/releases).  Simply expand the
**Assets** item under the latest release and download the release installer.

### Usage

After starting the application, the keyboard shortcut bindings are enabled:

* **Volume Up / Down**: Increase or decrease the volume by 1% respectively.
* **Shift + Volume Up / Down**: Increase or decrease the volume by 0.5% respectively
  (fine adjustment).

**Note**: You can still access the regular volume indicator by using **Ctrl + Volume Up / Down**
or **Alt + Volume Up / Down**.

When the volume is changed, a volume indicator will appear in the top left of your screen:

![Volume Indicator](https://raw.githubusercontent.com/fgimian/totalmix-volume-control/main/images/VolumeIndicator.png)

If you hover over the tray icon, a tooltip will appear that indicates the health of the connection
with your device.

![Tray Tooltip](https://raw.githubusercontent.com/fgimian/totalmix-volume-control/main/images/TrayTooltip.png)

### Configuring TotalMixFX

The following instructions will be provided when you hover over the tray icon in the case that
there is a communication issue with your device.  However, I provide them here for your convenience
also:

1. Open TotalMixFX
2. Tick **Options** / **Enable OSC Control**
3. Open **Options** / **Settings** and select the **OSC** tab
4. Ensure **Remote Controller Select** is set to **1**
5. Ensure that **In Use** is ticked
6. Ensure that **Port incoming** is set to **7001**
7. Ensure that **Port outgoing** is set to **9001**
8. Ensure that **IP or Host Name** is set to **127.0.0.1**

## Building from Source

You'll need to download and extract the source code from GitHub or clone the Git repository
and then follow the steps below:

1. Install the **.NET 5.0 desktop app Runtime** from
   [Microsoft's Download .NET 5.0 Runtime](https://dotnet.microsoft.com/download/dotnet/5.0/runtime)
   page.
2. Open a PowerShell prompt and build the project

    ```powershell
    cd ~\Downloads\totalmix-volume-control
    .\build.ps1 --target Distribute --configuration Release
    ```

3. You'll now find an installer under the **artifacts** sub-directory

## License

TotalMix Volume Control is released under the **MIT** license. Please see the
[LICENSE](https://github.com/fgimian/totalmix-volume-control/blob/main/LICENSE) file for more
details.
