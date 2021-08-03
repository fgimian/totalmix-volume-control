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
        public float Volume { get; set; }

        public string VolumeDecibels { get; set; }

        private readonly DispatcherTimer _closeWindowTimer;

        public void DisplayCurrentVolume(float volume, string volumeDecibels)
        {
            Volume = volume;
            VolumeDecibels = volumeDecibels;

            // Display the volume indicator with the current volume details.
            //
            // This method may be called from inside a task, so we must use the dispatcher to
            // ensure that this work will occur in the UI thread.
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

        public VolumeIndicator()
        {
            InitializeComponent();

            // Create the timer that will close the window after not used for a little while.
            _closeWindowTimer = new DispatcherTimer();
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
            showStoryboard.Completed += (s, e) =>
            {
                Show();
                VolumeReadingCurrentRectangle.Width =
                    (int)(VolumeReadingBackgroundRectangle.ActualWidth * Volume);
                VolumeDecibelsTextBox.Text = VolumeDecibels;
            };
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
