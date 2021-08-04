using System.Windows;
using System.Windows.Input;

namespace TotalMixVC.GUI
{
    /// <summary>
    /// Implements the system tray menu functionality.
    /// </summary>
    public class TrayMenu
    {
        /// <summary>
        /// Gets the Exit tray icon command which shuts down the application.
        /// </summary>
        public ICommand ExitAppCommand => new DelegateCommand
        {
            CommandAction = () => Application.Current.Shutdown()
        };
    }
}
