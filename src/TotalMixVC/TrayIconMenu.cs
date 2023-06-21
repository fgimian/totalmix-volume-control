namespace TotalMixVC;

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WindowsShortcutFactory;

/// <summary>
/// Implements the system tray menu functionality.
/// </summary>
public static class TrayIconMenu
{
    /// <summary>
    /// Gets or sets a value indicating whether the application will start automatically when
    /// Windows starts.
    /// </summary>
    public static bool RunOnStartup
    {
        get
        {
            string shortcutPath = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                "TotalMix Volume Control.lnk");

            return File.Exists(shortcutPath);
        }

        set
        {
            string shortcutPath = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                "TotalMix Volume Control.lnk");
            string? appExecutablePath = Environment.ProcessPath;

            if (value)
            {
                using WindowsShortcut shortcut = new()
                {
                    Description = "TotalMix Volume Control",
                    WorkingDirectory = Directory.GetParent(appExecutablePath!)?.FullName,
                    Path = appExecutablePath,
                    IconLocation = new IconLocation(appExecutablePath!, index: 0)
                };

                shortcut.Save(shortcutPath);
            }
            else
            {
                File.Delete(shortcutPath);
            }
        }
    }

    /// <summary>
    /// Gets the command which reloads the application configuration.
    /// </summary>
    public static ICommand ReloadConfig => new DelegateCommand(() =>
    {
        App app = (App)Application.Current;
        app.ReloadConfig();
    });

    /// <summary>
    /// Gets the ommand which shuts down the application.
    /// </summary>
    public static ICommand ExitCommand => new DelegateCommand(() => Application.Current.Shutdown());
}
