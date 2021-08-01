# TotalMix Volume Control

This project uses the OSC protocol to communicate with RME TotalMixFX software and binds
the volume keys on your keyboard to control the master volume in Windows.

The keyboard bindings are as follows:

* **Volume Up / Down**: Increase or decrease the volume by 1% respectively.
* **Shift + Volume Up / Down**: Increase or decrease the volume by 0.5% respectively
  (fine adjustment).

## Configuring TotalMixFX

1. Open TotalMixFX
2. Tick **Options** / **Enable OSC Control**

And now verify the following settings (these should be set by default);

1. Open **Options** / **Settings**
2. Click the **OSC** tab and ensure **Remote Controller Select** is set to **1**
3. Ensure that **In Use** is ticked
4. Ensure that **Port incoming** is set to 7001
5. Ensure that **Port outgoing** is set to 9001

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
The application doesn't have an interface or tray icon yet.
