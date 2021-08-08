# TotalMix Volume Control

This project uses the OSC protocol to communicate with RME TotalMixFX software and binds
the volume keys on your keyboard to control the master volume in Windows.

You may see the [related thread on the RME forums](https://forum.rme-audio.de/viewtopic.php?pid=174137)
and the [thread on the Gearspace forums](https://gearspace.com/board/music-computers/1358200-my-new-little-open-source-project-rme-totalmix-volume-control-windows.html)
for a discussion about the tool.

The keyboard bindings are as follows:

* **Volume Up / Down**: Increase or decrease the volume by 1% respectively.
* **Shift + Volume Up / Down**: Increase or decrease the volume by 0.5% respectively
  (fine adjustment).

**Note**: You can still access the regular volume indicator by using **Ctrl + Volume Up / Down**
or **Alt + Volume Up / Down**.

When the volume is changed, a volume indicator will appear in the top left of your screen:

![Volume Indicator](https://raw.githubusercontent.com/fgimian/totalmix-volume-control/main/images/VolumeIndicator.png)

## Configuring TotalMixFX

1. Open TotalMixFX
2. Tick **Options** / **Enable OSC Control**

And now verify the following settings (these should be set by default);

1. Open **Options** / **Settings**
2. Click the **OSC** tab and ensure **Remote Controller Select** is set to **1**
3. Ensure that **In Use** is ticked
4. Ensure that **Port incoming** is set to **7001**
5. Ensure that **Port outgoing** is set to **9001**
6. Ensure that **IP or Host Name** is set to **127.0.0.1**

## Building the Application

You'll need to download and extract the source code from GitHub or clone the Git repository
and then follow the steps below:

1. Install the **.NET 5.0 desktop app Runtime** from
   [Microsoft's Download .NET 5.0 Runtime](https://dotnet.microsoft.com/download/dotnet/5.0/runtime)
   page.
2. Open a PowerShell prompt and build the project

    ```powershell
    cd ~/Downloads/totalmix-volume-control
    .\build.ps1 --target Distribute --configuration Release
    ```

3. You'll now find an installer under the **artifacts** sub-directory

## Running the Application

You may simply execute the program which will run in the system tray and watch for your keystrokes.
A volume indicator will appear anytime you change the volume via your keyboard or your RME
hardware.

## License

TotalMix Volume Control is released under the **MIT** license. Please see the
[LICENSE](https://github.com/fgimian/totalmix-volume-control/blob/main/LICENSE) file for more
details.
