using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TotalMixVC.Communicator;
using TotalMixVC.GUI.Hotkeys;

namespace TotalMixVC.GUI
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        private bool _running = false;

        private Task _volumeReceiveTask;

        private void App_Startup(object sender, StartupEventArgs e)
        {
            _running = true;

            VolumeIndicator volumeIndicator = new();

            // Create a parent window which is not visible in the taskbar or Alt+Tab.
            Window hiddenParentWindow = new()
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

            VolumeManager volumeManager = new(
                outgoingEP: new IPEndPoint(IPAddress.Loopback, 7001),
                incomingEP: new IPEndPoint(IPAddress.Loopback, 9001))
            {
                VolumeRegularIncrement = 0.02f,
                VolumeFineIncrement = 0.01f
            };

            // Start a task that will receive and record volume changes.
            _volumeReceiveTask = Task.Run(async () =>
            {
                while (_running)
                {
                    bool initial = volumeManager.Volume == -1.0f;
                    if (await volumeManager.ReceiveVolumeAsync().ConfigureAwait(false) && !initial)
                    {
                        volumeIndicator.UpdateVolume(
                            volumeManager.Volume, volumeManager.VolumeDecibels);
                        volumeIndicator.DisplayCurrentVolume();
                    }
                }
            });

            // Ensure we obtain the current device volume before registering hotkeys.
            Task
                .Run(async () => await volumeManager.GetDeviceVolumeAsync().ConfigureAwait(false))
                .Wait();

            // Register all the hotkeys for changing the volume.  Note that doing this does not
            // work inside a task so this must be performed in the main method scope.
            GlobalHotKeyManager hotKeyManager = new();

            hotKeyManager.Register(
                new Hotkey { KeyModifier = KeyModifier.None, Key = Key.VolumeUp },
                async () =>
                {
                    await volumeManager.IncreaseVolumeAsync().ConfigureAwait(false);
                    volumeIndicator.DisplayCurrentVolume();
                });

            hotKeyManager.Register(
                new Hotkey { KeyModifier = KeyModifier.None, Key = Key.VolumeDown },
                async () =>
                {
                    await volumeManager.DecreaseVolumeAsync().ConfigureAwait(false);
                    volumeIndicator.DisplayCurrentVolume();
                });

            hotKeyManager.Register(
                new Hotkey { KeyModifier = KeyModifier.Shift, Key = Key.VolumeUp },
                async () =>
                {
                    await volumeManager.IncreaseVolumeAsync(fine: true).ConfigureAwait(false);
                    volumeIndicator.DisplayCurrentVolume();
                });

            hotKeyManager.Register(
                new Hotkey { KeyModifier = KeyModifier.Shift, Key = Key.VolumeDown },
                async () =>
                {
                    await volumeManager.DecreaseVolumeAsync(fine: true).ConfigureAwait(false);
                    volumeIndicator.DisplayCurrentVolume();
                });
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            _running = false;
            _volumeReceiveTask.Wait();
        }
    }
}
