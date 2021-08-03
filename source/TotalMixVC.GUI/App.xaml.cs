using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TotalMixVC.Communicator;

namespace TotalMixVC.GUI
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        internal void App_Startup(object sender, StartupEventArgs e)
        {
            var volumeIndicator = new VolumeIndicator();

            // Create a parent window which is not visible in the taskbar or Alt+Tab.
            var hiddenParentWindow = new Window
            {
                Top = -100,
                Left = -100,
                Width = 0,
                Height = 0,
                WindowStyle = WindowStyle.ToolWindow,
                ShowInTaskbar = false
            };

            // Set the owner of our volume indicator window to the hidden parent.
            hiddenParentWindow.Show();
            volumeIndicator.Owner = hiddenParentWindow;
            hiddenParentWindow.Hide();

            var volumeManager = new VolumeManager(
                outgoingEP: new IPEndPoint(IPAddress.Loopback, 7001),
                incomingEP: new IPEndPoint(IPAddress.Loopback, 9001))
            {
                VolumeRegularIncrement = 0.02f,
                VolumeFineIncrement = 0.01f
            };

            // Start a task that will receive and record volume changes.
            Task.Run(async () =>
            {
                while (true)
                {
                    await volumeManager.ReceiveVolume().ConfigureAwait(false);
                }
            });

            // Ensure we obtain the current device volume before registering hotkeys.
            Task
                .Run(async () => await volumeManager.GetDeviceVolume().ConfigureAwait(false))
                .Wait();

            // Register all the hotkeys for changing the volume.  Note that doing this does not
            // work inside a task so this must be performed in the main method scope.
            var hotKeyManager = new HotKeyManager();

            hotKeyManager.Register(
                new Hotkey(KeyModifier.None, Key.VolumeUp),
                async () =>
                {
                    await volumeManager.IncreaseVolume().ConfigureAwait(false);
                    volumeIndicator.Volume = volumeManager.Volume;
                    volumeIndicator.VolumeDecibels = volumeManager.VolumeDecibels;
                    volumeIndicator.DisplayCurrentVolume(
                        volumeManager.Volume, volumeManager.VolumeDecibels);
                });

            hotKeyManager.Register(
                new Hotkey(KeyModifier.None, Key.VolumeDown),
                async () =>
                {
                    await volumeManager.DecreaseVolume().ConfigureAwait(false);
                    volumeIndicator.DisplayCurrentVolume(
                        volumeManager.Volume, volumeManager.VolumeDecibels);
                });

            hotKeyManager.Register(
                new Hotkey(KeyModifier.Shift, Key.VolumeUp),
                async () =>
                {
                    await volumeManager.IncreaseVolume(fine: true).ConfigureAwait(false);
                    volumeIndicator.DisplayCurrentVolume(
                        volumeManager.Volume, volumeManager.VolumeDecibels);
                });

            hotKeyManager.Register(
                new Hotkey(KeyModifier.Shift, Key.VolumeDown),
                async () =>
                {
                    await volumeManager.DecreaseVolume(fine: true).ConfigureAwait(false);
                    volumeIndicator.DisplayCurrentVolume(
                        volumeManager.Volume, volumeManager.VolumeDecibels);
                });
        }
    }
}
