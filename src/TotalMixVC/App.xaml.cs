using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.VisualStudio.Threading;
using TotalMixVC.Communicator;
using TotalMixVC.Configuration;
using TotalMixVC.Hotkeys;

namespace TotalMixVC;

/// <summary>Interaction logic for App.xaml.</summary>
[SuppressMessage(
    "Roslynator",
    "RCS1043:Remove 'partial' modifier from type with a single part",
    Justification = "WPF Application classes must be partial."
)]
public partial class App : Application, IDisposable
{
    private static readonly CompositeFormat s_listenerErrorFormatString = CompositeFormat.Parse(
        "Unable to open a listener to receive events from your RME device.\n"
            + "\n"
            + "1. Ensure that the address {0} is not being used by another application\n"
            + "2. Right-click the tray icon and reload configuration to try again"
    );

    private static readonly CompositeFormat s_communicationErrorFormatString =
        CompositeFormat.Parse(
            "Unable to communicate with your RME device.\n"
                + "\n"
                + "1. Open TotalMix\n"
                + "2. Enable OSC under Options / Enable OSC Control\n"
                + "3. Open Options / Settings and select the OSC tab\n"
                + "4. Ensure one of the Remote Controller Select slots is In Use and is selected\n"
                + "5. Ensure the incoming port is {0} and outgoing port is {1}\n"
                + "6. Ensure the remote IP or Host Name is set to {2}"
        );

    private static readonly string s_configPath = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TotalMix Volume Control",
        "config.toml"
    );

    private readonly GlobalHotKeyManager _hotKeyManager = new();

    // Disable non-nullable field must contain a non-null value when exiting constructor. These
    // fields are initialized in OnStartup which is called by the constructor.
#pragma warning disable CS8618
    private CancellationTokenSource _taskCancellationTokenSource;

    private JoinableTaskContext _joinableTaskContext;

    private JoinableTaskFactory _joinableTaskFactory;

    private VolumeIndicator _volumeIndicator;

    private TaskbarIcon _trayIcon;

    private TextBlock _trayToolTipStatus;

    private Sender _sender;

    private Listener? _listener;

    private VolumeManager _volumeManager;

#pragma warning restore CS8618
    private JoinableTask? _volumeReceiveTask;

    private JoinableTask? _volumeInitializeTask;

    private Config _config = new();

    /// <summary>
    /// Shows general information about the application in a message box.
    /// </summary>
    public void About()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

        MessageBox.Show(
            owner: _volumeIndicator,
            messageBoxText: $"TotalMix Volume Control v{versionInfo.ProductVersion}.",
            caption: "About TotalMix Volume Control",
            button: MessageBoxButton.OK,
            icon: MessageBoxImage.Information
        );
    }

    /// <summary>
    /// Loads the configuration file and reports errors in message boxes if they occur.
    /// </summary>
    /// <param name="running">
    /// Whether the application is already running with a previous loaded configuration.
    /// </param>
    /// <returns>Whether or not the config was loaded successfully.</returns>
    public bool LoadConfig(bool running = false)
    {
        var configText = File.ReadAllText(s_configPath);
        var isValid = Config.TryFromToml(configText, out var config, out var diagnostics);

        if (!isValid && diagnostics is not null)
        {
            var configDescription = running ? "existing" : "default";
            var message = new StringBuilder();

            if (config is null)
            {
                message.Append(
                    CultureInfo.InvariantCulture,
                    $"Unable to parse the config file at {s_configPath}.\n\n"
                );
            }
            else
            {
                message.Append(
                    CultureInfo.InvariantCulture,
                    $"Unable to parse one or more more properties from the config file at {s_configPath}.\n\n"
                );
            }

            foreach (var diagnosticMessage in Config.CleanDiagnostics(diagnostics))
            {
                message.Append(CultureInfo.InvariantCulture, $"- {diagnosticMessage}\n");
            }

            message.Append(
                CultureInfo.InvariantCulture,
                $"\nThe application will continue with the {configDescription} values for affected properties."
            );

            if (running)
            {
                MessageBox.Show(
                    _volumeIndicator,
                    message.ToString(),
                    caption: "Configuration File Error",
                    button: MessageBoxButton.OK,
                    icon: MessageBoxImage.Exclamation
                );
            }
            else
            {
                MessageBox.Show(
                    message.ToString(),
                    caption: "Configuration File Error",
                    button: MessageBoxButton.OK,
                    icon: MessageBoxImage.Exclamation
                );
            }
        }

        if (config is not null)
        {
            _config = config;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reloads the application configuration and updates all components accordingly.
    /// </summary>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public async Task ReloadConfigAsync()
    {
        if (!File.Exists(s_configPath))
        {
            // It is important to use specify the owner of the message box or it will be closed
            // when the context menu is closed.
            // See https://github.com/hardcodet/wpf-notifyicon/issues/74 for more information.
            MessageBox.Show(
                _volumeIndicator,
                $"A configuration file at {s_configPath} could not be found.",
                caption: "Configuration File Error",
                button: MessageBoxButton.OK,
                icon: MessageBoxImage.Exclamation
            );
            return;
        }

        if (!LoadConfig(running: true))
        {
            return;
        }

        _sender.EP = _config.Osc.OutgoingEndPoint;

        if (_listener?.EP.ToString() != _config.Osc.IncomingEndPoint.ToString())
        {
            _volumeManager.Listener = null;
            _listener?.Dispose();
            _trayToolTipStatus.Text = "TotalMix Volume Manager is initializing.";

            try
            {
                _listener = new(_config.Osc.IncomingEndPoint);
            }
            catch (SocketException)
            {
                _listener = null;
            }

            _volumeManager.Listener = _listener;
        }

        ConfigureVolumeManager();
        ConfigureInterface();
        ConfigureTheme();

        _volumeIndicator.UpdateConfig(_config);

        MessageBox.Show(
            _volumeIndicator,
            "Configuration has been reloaded successfully.",
            caption: "Configuration Reloaded",
            button: MessageBoxButton.OK,
            icon: MessageBoxImage.Information
        );
    }

    /// <summary>Disposes the current app.</summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Starts the various required components after the application startup event is fired.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Create a cancellation token source to allow cancellation of tasks on exit.
        _taskCancellationTokenSource = new();

        // Create a task factory for the current thread (which is the UI thread).
        _joinableTaskContext = new();
        _joinableTaskFactory = new(_joinableTaskContext);

        // Create the system tray icon.
        _trayIcon = (TaskbarIcon)Resources["TrayIcon"];

        // Obtain the tooltip text area so the text may be updated as required while the app
        // is running.
        var trayToolTipBorder = (Border)_trayIcon.TrayToolTip;
        _trayToolTipStatus = (TextBlock)
            LogicalTreeHelper.FindLogicalNode(trayToolTipBorder, "TrayToolTipStatus");

        // Attempt to load the configuration if it exists.
        if (File.Exists(s_configPath))
        {
            LoadConfig();
        }

        // Configure the tray tooltip interface and theme.
        ConfigureInterface();
        ConfigureTheme();

        // Create the volume indicator widget which displays volume changes.
        _volumeIndicator = new(_config);

        // Create a parent window which is not visible in the taskbar or Alt+Tab.
        var hiddenParentWindow = new Window()
        {
            Top = -100,
            Left = -100,
            Width = 0,
            Height = 0,
            WindowStyle = WindowStyle.ToolWindow,
            ShowInTaskbar = false,
        };

        // Set the owner of the volume indicator window to the hidden parent.
        hiddenParentWindow.Show();
        _volumeIndicator.Owner = hiddenParentWindow;
        hiddenParentWindow.Hide();

        // Silently display the volume indicator so the volume bar rectangle background
        // width is initialized.
        _volumeIndicator.Opacity = 0.0;
        _volumeIndicator.Show();
        _volumeIndicator.Hide();
        _volumeIndicator.Opacity = 1.0;

        // Create the volume manager which will communicate with the device.
        try
        {
            _sender = new Sender(_config.Osc.OutgoingEndPoint);
        }
        catch (SocketException)
        {
            MessageBox.Show(
                "Unable to bind to an available port to send events to the device. The "
                    + "application will now exit.",
                caption: "Socket Error",
                button: MessageBoxButton.OK,
                icon: MessageBoxImage.Exclamation
            );
            Shutdown();
            return;
        }

        _volumeManager = new(_sender);

        try
        {
            _listener = new Listener(_config.Osc.IncomingEndPoint);
        }
        catch (SocketException)
        {
            _listener = null;
        }

        _volumeManager.Listener = _listener;

        ConfigureVolumeManager();

        // Start a task that will receive and record volume changes.
        _volumeReceiveTask = _joinableTaskFactory.RunAsync(ReceiveVolumeAsync);

        // Obtain the current device volume.
        _volumeInitializeTask = _joinableTaskFactory.RunAsync(RequestVolumeAsync);

        // Register all the hotkeys for changing the volume.
        try
        {
            RegisterHotkeys();
        }
        catch (Win32Exception)
        {
            MessageBox.Show(
                "Unable to map the required volume hotkeys. Please exit any applications that may "
                    + "be using them and try again.\n\nThe application will now exit.",
                caption: "Hotkey Registration Error",
                button: MessageBoxButton.OK,
                icon: MessageBoxImage.Exclamation
            );
            Shutdown();
        }
    }

    /// <summary>
    /// Cleans up the various components after the application exit event is fired.
    /// </summary>
    /// <param name="e">The event data.</param>
    [SuppressMessage(
        "Usage",
        "VSTHRD100:Avoid async void methods",
        Justification = "Event handlers must be async void based on their definition."
    )]
    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        // Cancel all running tasks.
        await _taskCancellationTokenSource.CancelAsync().ConfigureAwait(false);

        // Wait for running tasks to complete.
        if (_volumeReceiveTask is not null)
        {
            await _volumeReceiveTask;
        }

        if (_volumeInitializeTask is not null)
        {
            await _volumeInitializeTask;
        }

        // Dispose any objects which implement the IDisposable interface.
        Dispose();
    }

    /// <summary>Disposes the current app.</summary>
    /// <param name="disposing">Whether managed resources should be disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _taskCancellationTokenSource.Dispose();
            _joinableTaskContext.Dispose();
            _hotKeyManager.Dispose();
            _volumeManager.Dispose();
            _listener?.Dispose();
            _sender.Dispose();
            _volumeIndicator.Dispose();
            _trayIcon.Dispose();
        }
    }

    private async Task ReceiveVolumeAsync()
    {
        while (true)
        {
            // Switch to the background thread to avoid UI interruptions.
            await TaskScheduler.Default;

            try
            {
                // Obtain the initialized state before setting the volume.
                var initializedBeforeReceive = _volumeManager.IsVolumeInitialized;

                // The device sends a ping roughly every 2 seconds (usually a pinch over
                // 2 seconds), so we'll timeout at 3 seconds to be on the safe side.
                var received = await _volumeManager
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
                            _volumeManager.IsDimmed
                        )
                        .ConfigureAwait(false);

                    if (initializedBeforeReceive && _config.Interface.ShowRemoteVolumeChanges)
                    {
                        await _volumeIndicator.DisplayCurrentVolumeAsync().ConfigureAwait(false);
                    }
                }

                // Switch to the UI thread and update the tray tooltip text.
                await _joinableTaskFactory.SwitchToMainThreadAsync(
                    _taskCancellationTokenSource.Token
                );
                _trayToolTipStatus.Text = "Successfully communicating with your RME device.";
                _trayIcon.ToolTipText = "TotalMixVC - Connection established.";
            }
            catch (InvalidOperationException)
            {
                // Update the volume indicator values with initial values after not being able to
                // communicate with the device.
                await _volumeIndicator
                    .UpdateVolumeAsync(volume: 0.0f, volumeDecibels: "-", isDimmed: false)
                    .ConfigureAwait(false);

                // Switch to the UI thread and update the tray tooltip text.
                await _joinableTaskFactory.SwitchToMainThreadAsync(
                    _taskCancellationTokenSource.Token
                );
                _trayToolTipStatus.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    s_listenerErrorFormatString,
                    _config.Osc.IncomingEndPoint
                );
                _trayIcon.ToolTipText =
                    "TotalMixVC - Unable to open listener to receive events from your RME device";

                // Switch to the background thread to avoid UI interruptions.
                await TaskScheduler.Default;

                // Sleep for a second before trying again.
                await Task.Delay(1000, _taskCancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                // Update the volume indicator values with initial values after a timeout.
                await _volumeIndicator
                    .UpdateVolumeAsync(volume: 0.0f, volumeDecibels: "-", isDimmed: false)
                    .ConfigureAwait(false);

                // Switch to the UI thread and update the tray tooltip text.
                await _joinableTaskFactory.SwitchToMainThreadAsync(
                    _taskCancellationTokenSource.Token
                );
                _trayToolTipStatus.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    s_communicationErrorFormatString,
                    _config.Osc.OutgoingEndPoint.Port,
                    _config.Osc.IncomingEndPoint.Port,
                    _config.Osc.IncomingEndPoint.Address
                );
                _trayIcon.ToolTipText = "TotalMixVC - Unable to connect to your device";
            }
            catch (SocketException)
            {
                // This exception is raised during a reconnect which can be ignored.
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
                await Task.Delay(1000, _taskCancellationTokenSource.Token).ConfigureAwait(false);
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
        _hotKeyManager.Register(
            hotkey: new Hotkey { KeyModifier = KeyModifier.None, Key = Key.VolumeUp },
            action: () =>
                _joinableTaskFactory
                    .RunAsync(async () =>
                    {
                        // Increase the volume and show the volume indicator.
                        await _volumeManager.IncreaseVolumeAsync().ConfigureAwait(false);
                        await _volumeIndicator.DisplayCurrentVolumeAsync().ConfigureAwait(false);
                    })
                    .Join(_taskCancellationTokenSource.Token)
        );

        _hotKeyManager.Register(
            hotkey: new Hotkey { KeyModifier = KeyModifier.None, Key = Key.VolumeDown },
            action: () =>
                _joinableTaskFactory
                    .RunAsync(async () =>
                    {
                        // Decrease the volume and show the volume indicator.
                        await _volumeManager.DecreaseVolumeAsync().ConfigureAwait(false);
                        await _volumeIndicator.DisplayCurrentVolumeAsync().ConfigureAwait(false);
                    })
                    .Join(_taskCancellationTokenSource.Token)
        );

        _hotKeyManager.Register(
            hotkey: new Hotkey { KeyModifier = KeyModifier.Shift, Key = Key.VolumeUp },
            action: () =>
                _joinableTaskFactory
                    .RunAsync(async () =>
                    {
                        // Finely increase the volume and show the volume indicator.
                        await _volumeManager.IncreaseVolumeAsync(fine: true).ConfigureAwait(false);
                        await _volumeIndicator.DisplayCurrentVolumeAsync().ConfigureAwait(false);
                    })
                    .Join(_taskCancellationTokenSource.Token)
        );

        _hotKeyManager.Register(
            hotkey: new Hotkey { KeyModifier = KeyModifier.Shift, Key = Key.VolumeDown },
            action: () =>
                _joinableTaskFactory
                    .RunAsync(async () =>
                    {
                        // Finely decrease the volume and show the volume indicator.
                        await _volumeManager.DecreaseVolumeAsync(fine: true).ConfigureAwait(false);
                        await _volumeIndicator.DisplayCurrentVolumeAsync().ConfigureAwait(false);
                    })
                    .Join(_taskCancellationTokenSource.Token)
        );

        _hotKeyManager.Register(
            hotkey: new Hotkey { KeyModifier = KeyModifier.None, Key = Key.VolumeMute },
            action: () =>
                _joinableTaskFactory
                    .RunAsync(async () =>
                    {
                        await _volumeManager.ToggloDimAsync().ConfigureAwait(false);
                        await _volumeIndicator.DisplayCurrentVolumeAsync().ConfigureAwait(false);
                    })
                    .Join(_taskCancellationTokenSource.Token)
        );
    }

    private void ConfigureVolumeManager()
    {
        _volumeManager.UseDecibels = _config.Volume.UseDecibels;
        _volumeManager.VolumeIncrementPercent = _config.Volume.IncrementPercent.Value;
        _volumeManager.VolumeFineIncrementPercent = _config.Volume.FineIncrementPercent.Value;
        _volumeManager.VolumeMaxPercent = _config.Volume.MaxPercent.Value;
        _volumeManager.VolumeIncrementDecibels = _config.Volume.IncrementDecibels.Value;
        _volumeManager.VolumeFineIncrementDecibels = _config.Volume.FineIncrementDecibels.Value;
        _volumeManager.VolumeMaxDecibels = _config.Volume.MaxDecibels.Value;
    }

    private void ConfigureInterface()
    {
        var scaleTransform = (ScaleTransform)_trayIcon.Resources["TrayIconScaleTransform"];
        scaleTransform.ScaleX = _config.Interface.Scaling;
        scaleTransform.ScaleY = _config.Interface.Scaling;
    }

    private void ConfigureTheme()
    {
        var trayToolTipBorder = (Border)_trayIcon.TrayToolTip;

        // TODO: Determine why binding this to border brush doesn't work.
        var trayToolTipPanel = (StackPanel)
            LogicalTreeHelper.FindLogicalNode(trayToolTipBorder, "TrayToolTipPanel");

        var trayToolTipTitleTotalMix = (TextBlock)
            LogicalTreeHelper.FindLogicalNode(trayToolTipBorder, "TrayToolTipTitleTotalMix");

        var trayToolTipTitleVolume = (TextBlock)
            LogicalTreeHelper.FindLogicalNode(trayToolTipBorder, "TrayToolTipTitleVolume");

        var trayToolTipStatus = (TextBlock)
            LogicalTreeHelper.FindLogicalNode(trayToolTipBorder, "TrayToolTipStatus");

        trayToolTipBorder.BorderBrush = new SolidColorBrush(_config.Theme.BackgroundColor);
        trayToolTipBorder.CornerRadius = new CornerRadius(_config.Theme.BackgroundRounding);
        trayToolTipPanel.Background = new SolidColorBrush(_config.Theme.BackgroundColor);
        trayToolTipTitleTotalMix.Foreground = new SolidColorBrush(
            _config.Theme.HeadingTotalmixColor
        );
        trayToolTipTitleVolume.Foreground = new SolidColorBrush(_config.Theme.HeadingVolumeColor);
        trayToolTipStatus.Foreground = new SolidColorBrush(_config.Theme.TrayTooltipMessageColor);
    }
}
