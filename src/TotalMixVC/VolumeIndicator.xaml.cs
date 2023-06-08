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

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeIndicator"/> class.
    /// </summary>
    public VolumeIndicator()
    {
        InitializeComponent();

        // Create a task factory for the current thread (which is the UI thread).
        _joinableTaskFactory = new(new JoinableTaskContext());

        // Create the timer that will hide the window after not used for a little while.
        _hideWindowTimer = new() { Interval = TimeSpan.FromSeconds(2.0) };

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
        Storyboard showStoryboard = FindResource("Show") as Storyboard;
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
        VolumeReadingCurrentRectangle.Fill =
            (SolidColorBrush)new BrushConverter().ConvertFrom(isDimmed ? "#996500" : "#999");
        VolumeDecibelsTextBox.Foreground = isDimmed ? Brushes.Orange : Brushes.White;

        // Update the volume rectangle with the percentage and text box decibel reading.
        VolumeReadingCurrentRectangle.Width =
            (int)(VolumeReadingBackgroundRectangle.ActualWidth * volume);
        VolumeDecibelsTextBox.Text = volumeDecibels;
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

        Storyboard hideStoryboard = FindResource("Hide") as Storyboard;
        hideStoryboard.Begin(this);
    }
}
