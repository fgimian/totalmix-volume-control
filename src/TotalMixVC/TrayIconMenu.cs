﻿using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace TotalMixVC;

/// <summary>
/// Implements the system tray menu functionality.
/// </summary>
public static class TrayIconMenu
{
    /// <summary>
    /// Gets the command that shows the About window for the application.
    /// </summary>
    public static ICommand About =>
        new DelegateCommand(() =>
        {
            var app = (App)Application.Current;
            app.About();
        });

    /// <summary>
    /// Gets the command which reloads the application configuration.
    /// </summary>
    public static ICommand ReloadConfig =>
        new DelegateCommand(() =>
        {
            var app = (App)Application.Current;
            app.ReloadConfig();
        });

    /// <summary>
    /// Gets or sets a value indicating whether the application will start automatically when
    /// Windows starts.
    /// </summary>
    public static bool RunOnStartup
    {
        get
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run"
            );

            return runKey?.GetValue("TotalMix Volume Control") is not null;
        }
        set
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run",
                writable: true
            );

            if (value)
            {
                var appExecutablePath = Environment.ProcessPath;
                runKey?.SetValue("TotalMix Volume Control", appExecutablePath!);
            }
            else
            {
                runKey?.DeleteValue("TotalMix Volume Control");
            }
        }
    }

    /// <summary>
    /// Gets the ommand which shuts down the application.
    /// </summary>
    public static ICommand ExitCommand => new DelegateCommand(Application.Current.Shutdown);
}
