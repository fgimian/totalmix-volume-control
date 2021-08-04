# TotalMix Volume Control

This project uses the OSC protocol to communicate with RME TotalMixFX software and binds
the volume keys on your keyboard to control the master volume in Windows.

You may see the [related thread on the RME forums](https://forum.rme-audio.de/viewtopic.php?pid=174137)
for a discussion about the tool.

The keyboard bindings are as follows:

* **Volume Up / Down**: Increase or decrease the volume by 1% respectively.
* **Shift + Volume Up / Down**: Increase or decrease the volume by 0.5% respectively
  (fine adjustment).

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

## Building the Application

You'll need to download and extract the source code from GitHub or clone the Git repository
and then follow the steps below:

1. Install the **.NET 5.0 SDK** from
   [Microsoft's Download .NET SDKs](https://dotnet.microsoft.com/download/visual-studio-sdks)
   page.
2. Open a PowerShell prompt and build the project

    ```powershell
    cd ~/Downloads/totalmix-volume-control
    dotnet build -c Release
    ```

3. You'll now find an executable named **TotalMixVC.GUI.exe** under the sub-directory
   **source\TotalMixVC.GUI\bin\Release\net5.0-windows** 

## Running the Application

You may simply execute the program which will run in the background and watch for your keystrokes.
A volume indicator will appear anytime you change the volume via your keyboard or your RME
hardware.  The application doesn't have a tray icon yet but that will be coming in the near
future.

## License

TotalMix Volume Control is released under the **MIT** license. Please see the
[LICENSE](https://github.com/fgimian/totalmix-volume-control/blob/main/LICENSE) file for more
details.
