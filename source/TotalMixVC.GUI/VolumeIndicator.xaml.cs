using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TotalMixVC.GUI
{
    /// <summary>
    /// Interaction logic for VolumeIndicator.xaml.
    /// </summary>
    public partial class VolumeIndicator : Window
    {
        private readonly DispatcherTimer _closeWindowTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeIndicator"/> class.
        /// </summary>
        public VolumeIndicator()
        {
            InitializeComponent();

            // Create the timer that will close the window after not used for a little while.
            _closeWindowTimer = new();
            _closeWindowTimer.Interval = TimeSpan.FromSeconds(2.0);

            // When the timer elapses, close the window.
            _closeWindowTimer.Tick += (object sender, EventArgs e) =>
            {
                var timer = (DispatcherTimer)sender;
                timer.Stop();

                var hideStoryboard = FindResource("hide") as Storyboard;
                hideStoryboard?.Begin(this);
            };

            var showStoryboard = FindResource("show") as Storyboard;
            showStoryboard.Completed += (s, e) => Show();
        }

        /// <summary>
        /// Displays the volume indicator with the current volume details.  This method may be
        /// safely called from inside an async task.
        /// </summary>
        public void DisplayCurrentVolume()
        {
            // Display the volume indicator with the current volume details.  We use a dispatcher
            // to ensure that this work occurs in the UI thread.
            Dispatcher.BeginInvoke((Action)(() =>
            {
                var showStoryboard = FindResource("show") as Storyboard;
                showStoryboard?.Begin(this);
            }));

            // Restart the timer to close the window.
            if (_closeWindowTimer.IsEnabled)
            {
                _closeWindowTimer.Stop();
            }

            _closeWindowTimer.Start();
        }

        /// <summary>
        /// Updates the volume in the volume indicator given the provided volume details.  This
        /// method may be safely called from inside an async task.
        /// </summary>
        /// <param name="volume">The current volume as a float.</param>
        /// <param name="volumeDecibels">The current volume in decibels as a string.</param>
        public void UpdateVolume(float volume, string volumeDecibels)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                VolumeReadingCurrentRectangle.Width
                    = (int)(VolumeReadingBackgroundRectangle.ActualWidth * volume);
                VolumeDecibelsTextBox.Text = volumeDecibels;
            }));
        }

        /// <summary>
        /// Ensures that it is impossible to close the volume indicator window.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
