using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.VisualStudio.Threading;
using Tomlyn;
using TotalMixVC.Communicator;
using TotalMixVC.Hotkeys;

namespace TotalMixVC;

/// <summary>
/// Interaction logic for App.xaml.
/// </summary>
public partial class App : Application
{
    private const string CommunicationErrorFormatString =
        "Unable to communicate with your RME device.\n"
        + "\n"
        + "1. Open TotalMix\n"
        + "2. Enable OSC under Options / Enable OSC Control\n"
        + "3. Open Options / Settings and select the OSC tab\n"
        + "4. Ensure one of the Remote Controller Select slots is In Use and is selected\n"
        + "5. Ensure the incoming port is {0} and outgoing port is {1}\n"
        + "6. Ensure the remote IP or Host Name is set to {2}";

    private static readonly string ConfigPath = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TotalMix Volume Control",
        "config.toml");

    // Disable non-nullable field must contain a non-null value when exiting constructor. These
    // fields are initialized in OnStartup which is called by the constructor.
#pragma warning disable CS8618
    private VolumeManager _volumeManager;

    private VolumeIndicator _volumeIndicator;

    private TextBlock _trayToolTipStatus;

    private TaskbarIcon _trayIcon;

    private JoinableTaskFactory _joinableTaskFactory;

    private CancellationTokenSource _taskCancellationTokenSource;

    private JoinableTask _volumeReceiveTask;

    private JoinableTask _volumeInitializeTask;
#pragma warning restore CS8618

    private Config _config = new();

    /// <summary>
    /// Reloads the application configuration and updates all components accordingly.
    /// </summary>
    public void ReloadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            // TODO: Determine why the message box doesn't stay open.
            MessageBox.Show(
                $"A configuration file at {ConfigPath} could not be found.",
                caption: "Configuration File Error",
                button: MessageBoxButton.OK,
                icon: MessageBoxImage.Error);
            return;
        }

        // TODO: Handle possible errors here.
        string configText = File.ReadAllText(ConfigPath);
        _config = Toml.ToModel<Config>(configText);

        // TODO: Also reconnect to the hostname and port in the config after update.
        ConfigureVolumeManager();
        ConfigureInterface();
        ConfigureTheme();

        _volumeIndicator.UpdateConfig(_config);
    }

    /// <summary>
    /// Starts the various required components after the application startup event is fired.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Attempt to load the configuration if it exists.
        if (File.Exists(ConfigPath))
        {
            // TODO: Handle possible errors here.
            string configText = File.ReadAllText(ConfigPath);
            _config = Toml.ToModel<Config>(configText);
        }

        // Create the volume manager which will communicate with the device.
        // TODO: Handle possible parsing errors below.
        _volumeManager = new(
            outgoingEP: new IPEndPoint(
                IPAddress.Parse(_config.Osc.OutgoingHostname),
                _config.Osc.OutgoingPort),
            incomingEP: new IPEndPoint(
                IPAddress.Parse(_config.Osc.IncomingHostname),
                _config.Osc.IncomingPort));

        ConfigureVolumeManager();

        // Create the volume indicator widget which displays volume changes.
        _volumeIndicator = new(_config);

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

        // Silently display the volume indicator so the volume bar rectangle background
        // width is initialized.
        _volumeIndicator.Opacity = 0.0;
        _volumeIndicator.Show();
        _volumeIndicator.Hide();
        _volumeIndicator.Opacity = 1.0;

        // Create the system tray icon.
        _trayIcon = (TaskbarIcon)Resources["TrayIcon"];

        // Set the tray icon to match the application.
        Assembly? assembly = Assembly.GetEntryAssembly();
        if (assembly is not null)
        {
            _trayIcon.Icon = Icon.ExtractAssociatedIcon(assembly.ManifestModule.Name);
        }

        // Obtain the tooltip text area so the text may be updated as required while the app
        // is running.
        Border trayToolTipBorder = (Border)_trayIcon.TrayToolTip;
        _trayToolTipStatus = (TextBlock)LogicalTreeHelper.FindLogicalNode(
            trayToolTipBorder, "TrayToolTipStatus");

        // Configure the tray tooltip interface and theme.
        ConfigureInterface();
        ConfigureTheme();

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
        while (true)
        {
            try
            {
                // Switch to the background thread to avoid UI interruptions.
                await TaskScheduler.Default;

                // Obtain the initialized state before setting the volume.
                bool initializedBeforeReceive = _volumeManager.IsVolumeInitialized;

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
                            _volumeManager.Volume,
                            _volumeManager.VolumeDecibels!,
                            _volumeManager.IsDimmed)
                        .ConfigureAwait(false);

                    if (initializedBeforeReceive && _config.Interface.ShowRemoteVolumeChanges)
                    {
                        await _volumeIndicator
                            .DisplayCurrentVolumeAsync()
                            .ConfigureAwait(false);
                    }
                }

                // Switch to the UI thread and update the tray tooltip text.
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                _trayToolTipStatus.Text =
                    "Successfully communicating with your RME device.";
                _trayIcon.ToolTipText = "TotalMixVC - Connection established.";
            }
            catch (TimeoutException)
            {
                // Update the volume indicator values with initial values after a timeout.
                await _volumeIndicator
                    .UpdateVolumeAsync(volume: 0.0f, volumeDecibels: "-", isDimmed: false)
                    .ConfigureAwait(false);

                // Switch to the UI thread and update the tray tooltip text.
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                _trayToolTipStatus.Text = string.Format(
                    CommunicationErrorFormatString,
                    _config.Osc.OutgoingPort,
                    _config.Osc.IncomingPort,
                    _config.Osc.IncomingHostname);
                _trayIcon.ToolTipText = "TotalMixVC - Unable to connect to your device";
            }
            catch (OperationCanceledException)
            {
                // This exception is raised when the app is exited so we exit the loop.
                break;
            }
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

        hotKeyManager.Register(
            hotkey: new Hotkey { KeyModifier = KeyModifier.None, Key = Key.VolumeMute },
            action: () => _joinableTaskFactory
                .RunAsync(async () =>
                {
                    await _volumeManager.ToggloDimAsync().ConfigureAwait(false);
                    await _volumeIndicator
                        .DisplayCurrentVolumeAsync()
                        .ConfigureAwait(false);
                })
                .Join());
    }

    private void ConfigureVolumeManager()
    {
        _volumeManager.VolumeRegularIncrement = _config.Volume.Increment;
        _volumeManager.VolumeFineIncrement = _config.Volume.FineIncrement;
        _volumeManager.VolumeMax = _config.Volume.Max;
    }

    private void ConfigureInterface()
    {
        ScaleTransform scaleTransform =
            (ScaleTransform)_trayIcon.Resources["TrayIconScaleTransform"];
        scaleTransform.ScaleX = _config.Interface.Scaling;
        scaleTransform.ScaleY = _config.Interface.Scaling;
    }

    private void ConfigureTheme()
    {
        BrushConverter brushConverter = new();

        Border trayToolTipBorder = (Border)_trayIcon.TrayToolTip;

        // TODO: Determine why binding this to border brush doesn't work.
        StackPanel trayToolTipPanel = (StackPanel)LogicalTreeHelper.FindLogicalNode(
            trayToolTipBorder, "TrayToolTipPanel");

        TextBlock trayToolTipTitleTotalMix = (TextBlock)LogicalTreeHelper.FindLogicalNode(
            trayToolTipBorder, "TrayToolTipTitleTotalMix");

        TextBlock trayToolTipTitleVolume = (TextBlock)LogicalTreeHelper.FindLogicalNode(
            trayToolTipBorder, "TrayToolTipTitleVolume");

        TextBlock trayToolTipStatus = (TextBlock)LogicalTreeHelper.FindLogicalNode(
            trayToolTipBorder, "TrayToolTipStatus");

        trayToolTipBorder.BorderBrush =
            (SolidColorBrush?)brushConverter.ConvertFrom(_config.Theme.BackgroundColor);
        trayToolTipBorder.CornerRadius = new CornerRadius(_config.Theme.BackgroundRounding);

        trayToolTipPanel.Background =
            (SolidColorBrush?)brushConverter.ConvertFrom(_config.Theme.BackgroundColor);

        trayToolTipTitleTotalMix.Foreground =
            (SolidColorBrush?)brushConverter.ConvertFrom(
                _config.Theme.HeadingTotalmixColor);

        trayToolTipTitleVolume.Foreground =
            (SolidColorBrush?)brushConverter.ConvertFrom(
                _config.Theme.HeadingVolumeColor);

        trayToolTipStatus.Foreground =
            (SolidColorBrush?)brushConverter.ConvertFrom(
                _config.Theme.TrayTooltipMessageColor);
    }
}
