using System;
using System.ComponentModel;
using System.Threading.Tasks;
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

    private readonly Config _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeIndicator"/> class.
    /// </summary>
    /// <param name="config">Configuration for the application.</param>
    public VolumeIndicator(Config config)
    {
        InitializeComponent();

        _config = config;

        ConfigureInterface();
        ConfigureTheme();

        // Create a task factory for the current thread (which is the UI thread).
        _joinableTaskFactory = new(new JoinableTaskContext());

        // Create the timer that will hide the window after not used for a little while.
        _hideWindowTimer = new() { Interval = TimeSpan.FromSeconds(_config.Interface.HideDelay) };

        // When the timer elapses, hide the window.
        _hideWindowTimer.Tick += Hide;
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
        Storyboard showStoryboard = Resources["Show"] as Storyboard;
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
        VolumeWidgetReadingCurrentRectangle.Fill =
            (SolidColorBrush)new BrushConverter().ConvertFrom(isDimmed
                ? _config.Theme.VolumeBarForegroundColorDimmed
                : _config.Theme.VolumeBarForegroundColorNormal);
        VolumeWidgetDecibelsTextBox.Foreground =
            (SolidColorBrush)new BrushConverter().ConvertFrom(isDimmed
                ? _config.Theme.VolumeReadoutColorDimmed
                : _config.Theme.VolumeReadoutColorNormal);

        // Update the volume rectangle with the percentage and text box decibel reading.
        VolumeWidgetReadingCurrentRectangle.Width =
            (int)(VolumeWidgetReadingBackgroundRectangle.ActualWidth * volume);
        VolumeWidgetDecibelsTextBox.Text = volumeDecibels;
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
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        WindowServices.SetWindowExTransparent(hwnd);
    }

    private void Hide(object sender, EventArgs e)
    {
        DispatcherTimer timer = (DispatcherTimer)sender;
        timer.Stop();

        Storyboard hideStoryboard = Resources["Hide"] as Storyboard;
        hideStoryboard.Begin(this);
    }

    private void ConfigureInterface()
    {
        ScaleTransform scaleTransform = Resources["WindowScaleTransform"] as ScaleTransform;
        scaleTransform.ScaleX = _config.Interface.Scaling;
        scaleTransform.ScaleY = _config.Interface.Scaling;

        Storyboard hideStoryboard = Resources["Hide"] as Storyboard;
        DoubleAnimation opacityAnimation = hideStoryboard.Children[0] as DoubleAnimation;
        ObjectAnimationUsingKeyFrames visibilityAnimation =
            hideStoryboard.Children[1] as ObjectAnimationUsingKeyFrames;
        TimeSpan animationTime = TimeSpan.FromSeconds(_config.Interface.FadeOutTime);

        opacityAnimation.Duration = new Duration(animationTime);
        visibilityAnimation.KeyFrames[0].KeyTime = KeyTime.FromTimeSpan(animationTime);

        Top = _config.Interface.PositionOffset;
        Left = _config.Interface.PositionOffset;
    }

    private void ConfigureTheme()
    {
        VolumeWidgetBorder.BorderBrush =
            (SolidColorBrush)new BrushConverter().ConvertFrom(_config.Theme.BackgroundColor);
        VolumeWidgetBorder.CornerRadius = new CornerRadius(_config.Theme.BackgroundRounding);

        VolumeWidgetTitleTotalMix.Foreground =
            (SolidColorBrush)new BrushConverter().ConvertFrom(
                _config.Theme.HeadingTotalmixColor);

        VolumeWidgetTitleVolume.Foreground =
            (SolidColorBrush)new BrushConverter().ConvertFrom(
                _config.Theme.HeadingVolumeColor);

        VolumeWidgetDecibelsTextBox.Foreground =
            (SolidColorBrush)new BrushConverter().ConvertFrom(
                _config.Theme.VolumeReadoutColorNormal);

        VolumeWidgetReadingBackgroundRectangle.Fill =
            (SolidColorBrush)new BrushConverter().ConvertFrom(
                _config.Theme.VolumeBarBackgroundColor);

        VolumeWidgetReadingCurrentRectangle.Fill =
            (SolidColorBrush)new BrushConverter().ConvertFrom(
                _config.Theme.VolumeBarForegroundColorNormal);
    }
}
