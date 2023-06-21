using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.VisualStudio.Threading;

namespace TotalMixVC;

/// <summary>
/// Interaction logic for VolumeIndicator.xaml.
/// </summary>
public partial class VolumeIndicator : Window
{
    private readonly JoinableTaskFactory _joinableTaskFactory;

    private readonly DispatcherTimer _hideWindowTimer;

    private Config _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeIndicator"/> class.
    /// </summary>
    /// <param name="config">Configuration for the application.</param>
    public VolumeIndicator(Config config)
    {
        InitializeComponent();

        _config = config;

        // Create a task factory for the current thread (which is the UI thread).
        _joinableTaskFactory = new(new JoinableTaskContext());

        // Create the timer that will hide the window after not used for a little while.
        _hideWindowTimer = new();

        // When the timer elapses, hide the window.
        _hideWindowTimer.Tick += Hide;

        // Configure the volume indicator interface and theme.
        ConfigureInterface();
        ConfigureTheme();
    }

    /// <summary>
    /// Updates the volume indicator with the provided configuration.
    /// </summary>
    /// <param name="config">Configuration for the application.</param>
    public void UpdateConfig(Config config)
    {
        _config = config;
        ConfigureInterface();
        ConfigureTheme();
    }

    /// <summary>
    /// Displays the volume indicator with the current volume details.  This method may be
    /// safely called from inside an async task.
    /// </summary>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public async Task DisplayCurrentVolumeAsync()
    {
        // Switch to the UI thread.
        await _joinableTaskFactory.SwitchToMainThreadAsync();

        // Display the volume indicator with the current volume details.  We use a dispatcher
        // to ensure that this work occurs in the UI thread.
        Storyboard showStoryboard = (Storyboard)Resources["WindowStoryboardShow"];
        showStoryboard.Begin(this);

        // Switch to the background thread to avoid UI interruptions.
        await TaskScheduler.Default;

        // Restart the timer to hide the window after some time.
        if (_hideWindowTimer.IsEnabled)
        {
            _hideWindowTimer.Stop();
        }

        _hideWindowTimer.Start();
    }

    /// <summary>
    /// Updates the volume in the volume indicator given the provided volume details.  This
    /// method may be safely called from inside an async task.
    /// </summary>
    /// <param name="volume">The current volume as a float.</param>
    /// <param name="volumeDecibels">The current volume in decibels as a string.</param>
    /// <param name="isDimmed">Whether or not the volume is dimmed.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public async Task UpdateVolumeAsync(float volume, string volumeDecibels, bool isDimmed)
    {
        // Switch to the UI thread.
        await _joinableTaskFactory.SwitchToMainThreadAsync();

        // Update the color of text and the volume rectangle based on whether the volume is dimmed.
        BrushConverter brushConverter = new();
        VolumeWidgetBarForeground.Fill =
            (SolidColorBrush?)brushConverter.ConvertFrom(isDimmed
                ? _config.Theme.VolumeBarForegroundColorDimmed
                : _config.Theme.VolumeBarForegroundColorNormal);
        VolumeWidgetReadout.Foreground =
            (SolidColorBrush?)brushConverter.ConvertFrom(isDimmed
                ? _config.Theme.VolumeReadoutColorDimmed
                : _config.Theme.VolumeReadoutColorNormal);

        // Update the volume bar with the percentage and readout text box with the decibel reading.
        VolumeWidgetBarForeground.Width = (int)(VolumeWidgetBarBackground.ActualWidth * volume);
        VolumeWidgetReadout.Text = volumeDecibels;
    }

    /// <summary>
    /// Ensures that it is impossible to close the volume indicator window.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected override void OnClosing(CancelEventArgs e)
    {
        // Cancel the close window operation.
        e.Cancel = true;
    }

    /// <summary>
    /// Ensures that the mouse can click through the volume indicator window.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        nint hwnd = new WindowInteropHelper(this).Handle;
        WindowServices.SetWindowExTransparent(hwnd);
    }

    private void Hide(object? sender, EventArgs e)
    {
        DispatcherTimer? timer = (DispatcherTimer?)sender;
        timer?.Stop();

        Storyboard hideStoryboard = (Storyboard)Resources["WindowStoryboardHide"];
        hideStoryboard.Begin(this);
    }

    private void ConfigureInterface()
    {
        _hideWindowTimer.Interval = TimeSpan.FromSeconds(_config.Interface.HideDelay);

        ScaleTransform scaleTransform = (ScaleTransform)Resources["WindowScaleTransform"];
        scaleTransform.ScaleX = _config.Interface.Scaling;
        scaleTransform.ScaleY = _config.Interface.Scaling;

        Storyboard hideStoryboard = (Storyboard)Resources["WindowStoryboardHide"];

        DoubleAnimation opacityAnimation = (DoubleAnimation)hideStoryboard.Children[0];
        ObjectAnimationUsingKeyFrames visibilityAnimation =
            (ObjectAnimationUsingKeyFrames)hideStoryboard.Children[1];

        TimeSpan animationTime = TimeSpan.FromSeconds(_config.Interface.FadeOutTime);
        opacityAnimation.Duration = new Duration(animationTime);
        visibilityAnimation.KeyFrames[0].KeyTime = KeyTime.FromTimeSpan(animationTime);

        Top = _config.Interface.PositionOffset;
        Left = _config.Interface.PositionOffset;
    }

    private void ConfigureTheme()
    {
        BrushConverter brushConverter = new();

        VolumeWidgetBorder.BorderBrush =
            (SolidColorBrush?)brushConverter.ConvertFrom(_config.Theme.BackgroundColor);
        VolumeWidgetBorder.CornerRadius = new CornerRadius(_config.Theme.BackgroundRounding);

        VolumeWidgetTitleTotalMix.Foreground = (SolidColorBrush?)brushConverter.ConvertFrom(
            _config.Theme.HeadingTotalmixColor);

        VolumeWidgetTitleVolume.Foreground = (SolidColorBrush?)brushConverter.ConvertFrom(
            _config.Theme.HeadingVolumeColor);

        VolumeWidgetReadout.Foreground = (SolidColorBrush?)brushConverter.ConvertFrom(
            _config.Theme.VolumeReadoutColorNormal);

        VolumeWidgetBarBackground.Fill = (SolidColorBrush?)brushConverter.ConvertFrom(
            _config.Theme.VolumeBarBackgroundColor);

        VolumeWidgetBarForeground.Fill = (SolidColorBrush?)brushConverter.ConvertFrom(
            _config.Theme.VolumeBarForegroundColorNormal);
    }
}
