using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.VisualStudio.Threading;
using TotalMixVC.Communicator;
using TotalMixVC.GUI.Hotkeys;

namespace TotalMixVC.GUI
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        private VolumeManager _volumeManager;

        private VolumeIndicator _volumeIndicator;

        private TextBlock _trayToolTipStatusTextBlock;

        private TaskbarIcon _trayIcon;

        private JoinableTaskFactory _joinableTaskFactory;

        private CancellationTokenSource _taskCancellationTokenSource;

        private JoinableTask _volumeReceiveTask;

        private JoinableTask _volumeInitializeTask;

        /// <summary>
        /// Starts the various required components after the application startup event is fired.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create the volume manager which will communicate with the device.
            _volumeManager = new(
                outgoingEP: new IPEndPoint(IPAddress.Loopback, 7001),
                incomingEP: new IPEndPoint(IPAddress.Loopback, 9001))
            {
                VolumeRegularIncrement = 0.02f,
                VolumeFineIncrement = 0.01f
            };

            // Create the volume indicator widget which displays volume changes.
            _volumeIndicator = new();

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
            _volumeIndicator.Owner = hiddenParentWindow;
            hiddenParentWindow.Hide();

            // Create the system tray icon and set the icon to match the application.
            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            _trayIcon.Icon = Icon.ExtractAssociatedIcon(
                Assembly.GetEntryAssembly().ManifestModule.Name);

            // Obtain the tooltip text area so the text may be updated as required while the app
            // is running.
            Border trayToolTipBorder = _trayIcon.TrayToolTip as Border;
            StackPanel trayToolTipStackPanel = trayToolTipBorder.Child as StackPanel;
            _trayToolTipStatusTextBlock =
                trayToolTipStackPanel
                    .Children
                    .OfType<TextBlock>()
                    .First(b => b.Name == "Status");

            // Create a task factory for the current thread (which is the UI thread).
            _joinableTaskFactory = new(new JoinableTaskContext());

            // Create a cancellation token source to allow cancellation of tasks on exit.
            _taskCancellationTokenSource = new();

            // Start a task that will receive and record volume changes.
            _volumeReceiveTask = _joinableTaskFactory.RunAsync(ReceiveVolumeAsync);

            // Obtain the current device volume.
            _volumeInitializeTask = _joinableTaskFactory.RunAsync(RequestVolumeAsync);

            // Register all the hotkeys for changing the volume.
            RegisterHotkeys();
        }

        /// <summary>
        /// Cleans up the various components after the application exit event is fired.
        /// </summary>
        /// <param name="e">The event data.</param>
        [SuppressMessage(
            "Usage",
            "VSTHRD100:Avoid async void methods",
            Justification = "Event handlers must be async void based on their definition.")]
        protected override async void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Cancel all running tasks.
            _taskCancellationTokenSource.Cancel();

            // Wait for running tasks to complete.
            await _volumeReceiveTask;
            await _volumeInitializeTask;

            // Dispose any objects which implement the IDisposable interface.
            _taskCancellationTokenSource.Dispose();
            _trayIcon.Dispose();
        }

        private async Task ReceiveVolumeAsync()
        {
            bool initial = true;

            while (true)
            {
                try
                {
                    // Switch to the background thread to avoid UI interruptions.
                    await TaskScheduler.Default;

                    // The device sends a ping roughly every 2 seconds (usually a pinch over
                    // 2 seconds), so we'll timeout at 3 seconds to be on the safe side.
                    bool received = await _volumeManager
                        .ReceiveVolumeAsync(3000, _taskCancellationTokenSource)
                        .ConfigureAwait(false);

                    // When volume updates are received, we display update the volume and display
                    // the volume indicator.
                    if (received)
                    {
                        await _volumeIndicator
                            .UpdateVolumeAsync(
                                _volumeManager.Volume, _volumeManager.VolumeDecibels)
                            .ConfigureAwait(false);

                        if (!initial)
                        {
                            await _volumeIndicator
                                .DisplayCurrentVolumeAsync()
                                .ConfigureAwait(false);
                        }
                    }

                    // Switch to the UI thread and update the tray tooltip text.
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    _trayToolTipStatusTextBlock.Text =
                        "Successfully communicating with your RME device.";
                }
                catch (TimeoutException)
                {
                    // Update the volume indicator values with initial values after a timeout.
                    await _volumeIndicator
                        .UpdateVolumeAsync(volume: 0.0f, volumeDecibels: "-")
                        .ConfigureAwait(false);

                    // Switch to the UI thread and update the tray tooltip text.
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    _trayToolTipStatusTextBlock.Text = string.Join(
                        '\n',
                        new string[]
                        {
                            "Unable to communicate with your RME device.",
                            string.Empty,
                            "1. Open TotalMix",
                            "2. Enable OSC under Options / Enable OSC Control",
                            "3. Open Options / Settings and select the OSC tab",
                            "4. Ensure that Remote Controller Select 1 is In Use",
                            "5. Ensure the incoming port is 7001 and outgoing port is 9001",
                            "6. Ensure the IP or Host Name is set to 127.0.0.1"
                        });
                }
                catch (OperationCanceledException)
                {
                    // This exception is raised when the app is exited so we exit the loop.
                    break;
                }

                initial = false;
            }
        }

        private async Task RequestVolumeAsync()
        {
            // Switch to the background thread to avoid UI interruptions.
            await TaskScheduler.Default;

            while (true)
            {
                // The volume is uninitialized so it is requested from the device.
                if (!_volumeManager.IsVolumeInitialized)
                {
                    await _volumeManager.RequestVolumeAsync().ConfigureAwait(false);
                }

                // A volume request was just sent or the volume is already known, so we sleep
                // for a second before checking again.
                try
                {
                    await Task
                        .Delay(1000, _taskCancellationTokenSource.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // This exception is raised when the app is exited so we exit the loop.
                    break;
                }
            }
        }

        private void RegisterHotkeys()
        {
            GlobalHotKeyManager hotKeyManager = new();

            hotKeyManager.Register(
                hotkey: new Hotkey { KeyModifier = KeyModifier.None, Key = Key.VolumeUp },
                action: () => _joinableTaskFactory
                    .RunAsync(async () =>
                    {
                        // Increase the volume and show the volume indicator.
                        await _volumeManager.IncreaseVolumeAsync().ConfigureAwait(false);
                        await _volumeIndicator
                            .DisplayCurrentVolumeAsync()
                            .ConfigureAwait(false);
                    })
                    .Join());

            hotKeyManager.Register(
                hotkey: new Hotkey { KeyModifier = KeyModifier.None, Key = Key.VolumeDown },
                action: () => _joinableTaskFactory
                    .RunAsync(async () =>
                    {
                        // Decrease the volume and show the volume indicator.
                        await _volumeManager.DecreaseVolumeAsync().ConfigureAwait(false);
                        await _volumeIndicator
                            .DisplayCurrentVolumeAsync()
                            .ConfigureAwait(false);
                    })
                    .Join());

            hotKeyManager.Register(
                hotkey: new Hotkey { KeyModifier = KeyModifier.Shift, Key = Key.VolumeUp },
                action: () => _joinableTaskFactory
                    .RunAsync(async () =>
                    {
                        // Finely increase the volume and show the volume indicator.
                        await _volumeManager.IncreaseVolumeAsync(fine: true).ConfigureAwait(false);
                        await _volumeIndicator
                            .DisplayCurrentVolumeAsync()
                            .ConfigureAwait(false);
                    })
                    .Join());

            hotKeyManager.Register(
                hotkey: new Hotkey { KeyModifier = KeyModifier.Shift, Key = Key.VolumeDown },
                action: () => _joinableTaskFactory
                    .RunAsync(async () =>
                    {
                        // Finely decrease the volume and show the volume indicator.
                        await _volumeManager.DecreaseVolumeAsync(fine: true).ConfigureAwait(false);
                        await _volumeIndicator
                            .DisplayCurrentVolumeAsync()
                            .ConfigureAwait(false);
                    })
                    .Join());
        }
    }
}
