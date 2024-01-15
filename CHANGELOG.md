# TotalMix Volume Control Changelog

## 0.6.0 (2023-12-02)

This is a maintenance release of TotalMix Volume Control which updates .NET to version 8 which is the latest LTS release. This release of .NET brings various performance improvements and optimisations.

## 0.5.0 (2023-07-01)

This marks the next major release and milestone for TotalMix Volume Control with various major new features and some important bug fixes:

* Vertical alignment of the elements on the volume indicator has been refined
* Cursor events now pass through the volume indicator making it completely transparent
* All project dependencies have been updated to their latest version including .NET itself which now uses .NET 7
* The application is now fully customizable via a configuration file including OSC communication, the theme, scaling and timing options for the volume indicator
* Your configuration may be reloaded while the app is running via the "Reload Configuration" tray menu item
* Errors will now correctly be displayed if hotkeys cannot be mapped or the UDP listener cannot be opened
* Version information is now embedded in the application executable
* Text in the tray icon menu has been refined and an About message box implemented showing the version of the application
* The application now uses the registry for starting on boot instead of a shortcut file in the Startup directory
* A critical bug has been fixed which could mean that the application would not recover if the RME device is offline for an extended period of time

**Important**: Due to several major changes in the application, it is recommended that you disable the older version from starting on boot, uninstalling it and reinstalling v0.5.0 from scratch. If you forget to do this, you may find that the app attempts to start twice on boot; in which case you can simply delete the TotalMix Volume Control shortcut from `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup` to resolve the problem.

## 0.4.0 (2021-12-21)

This release brings the much requested ability to dim the output of your device using the mute key on your keyboard. In addition, the tray icon will now show device connection status on Windows 11 via a basic tooltip.

On the tech side, I've now switched the project from Azure Pipelines to GitHub Actions too!

After using and testing TotalMix Volume Control for well over 8 hours a day now, I think it's time to finally mark this as the first production-ready release!

## 0.3.0 (2021-11-23)

This release updates the project to use .NET 6 and also embeds the runtime. This means that no longer need any separate downloads for the application to run. Simply download and run the installer, and you're good to go!

## 0.2.1 (2021-09-18)

Due to the fact that TotalMix sends the master volume via OSC at random times (particularly when closing videos or similar), I've changed the behaviour of the app to only show the volume indicator when it is changed via keyboard shortcuts. This should eliminate the problem but it does mean that the volume indicator won't be displayed if you're changing the volume from the device or TotalMix software.

If you want the volume to be displayed when adjusted on your hardware, please use v0.2.0 for now. I'll look at making this configurable in a future release.

## 0.2.0 (2021-08-15)

This is a rather major refinement and performance-oriented release of TotalMixVC! 🎵

* The application application download and install size is significantly reduced
* Code quality is generally improved and unit tests have been added
* Various improvements have been made which should ultimately make the application more stable
* The uninstaller for the application now has an icon
* Application exit is now immediate
* Volume is always re-requested after loss of communication with the device
* A critical bug is resolved where the application would stop responding if it could not communicate with the device

## 0.1.1 (2021-08-08)

This is a significant release for TotalMixVC which now provides all remaining essential features I had planned for the application! 😄

* The tray icon now will show a tooltip when you hover over it that will indicate whether communication is healthy between the app and your device
* The context menu in the tray icon will now let you set the application to start on boot
* The application is now much more robust if your device is not setup properly to communicate via OSC

## 0.1.0 (2021-08-07)

This is the first official release of TotalMix Volume Control. The core functionality is now implemented including OSC control of the master volume, a tray icon, installer and CI integration.

