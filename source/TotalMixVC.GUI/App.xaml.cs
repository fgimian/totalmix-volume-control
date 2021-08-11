﻿using System;
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
using TotalMixVC.Communicator;
using TotalMixVC.GUI.Hotkeys;

namespace TotalMixVC.GUI
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        private CancellationTokenSource _taskCancellationTokenSource;

        private Task _volumeReceiveTask;

        private Task _volumeInitializeTask;

        private TaskbarIcon _trayIcon;

        private TextBlock _trayToolTipStatusTextBlock;

        private void App_Startup(object sender, StartupEventArgs e)
        {
            _taskCancellationTokenSource = new();

            _trayIcon = (TaskbarIcon)FindResource("NotifyIcon");
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
                bool initial = true;

                while (true)
                {
                    try
                    {
                        bool received = await volumeManager
                            .ReceiveVolumeAsync(5000, _taskCancellationTokenSource)
                            .ConfigureAwait(false);

                        if (received)
                        {
                            volumeIndicator.UpdateVolume(
                                volumeManager.Volume, volumeManager.VolumeDecibels);

                            if (!initial)
                            {
                                volumeIndicator.DisplayCurrentVolume();
                            }
                        }

                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            _trayToolTipStatusTextBlock.Text =
                                "Successfully communicating with your RME device.";
                        }));
                    }
                    catch (TimeoutException)
                    {
                        volumeIndicator.UpdateVolume(volume: 0.0f, volumeDecibels: "-");

                        Dispatcher.BeginInvoke((Action)(() =>
                        {
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
                        }));
                    }
                    catch (OperationCanceledException)
                    {
                        // This exception is raised when the app is exited so we exit the loop.
                        break;
                    }

                    initial = false;
                }
            });

            // Obtain the current device volume.
            _volumeInitializeTask = Task.Run(async () =>
            {
                while (true)
                {
                    // The volume is uninitialized so it is requested from the device.
                    if (!volumeManager.IsVolumeInitialized)
                    {
                        await volumeManager.RequestDeviceVolumeAsync().ConfigureAwait(false);
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
            });

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
            _taskCancellationTokenSource.Cancel();
            _volumeReceiveTask.Wait();
            _volumeInitializeTask.Wait();
            _taskCancellationTokenSource.Dispose();
            _trayIcon.Dispose();
        }
    }
}
