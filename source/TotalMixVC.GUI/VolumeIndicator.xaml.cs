using System;
using System.Windows;
using System.Windows.Threading;

namespace TotalMixVC.GUI
{
    /// <summary>
    /// Interaction logic for VolumeIndicator.xaml.
    /// </summary>
    public partial class VolumeIndicator : Window
    {
        private readonly DispatcherTimer _closeWindowTimer;

        public void DisplayCurrentVolume(float volume, string volumeDecibels)
        {
            // Display the volume indicator with the current volume details.
            //
            // This method may be called from inside a task, so we must use the dispatcher to
            // ensure that this work will occur in the UI thread.
            Dispatcher.BeginInvoke((Action)(() =>
            {
                Show();
                VolumeReadingCurrentRectangle.Width =
                    (int)(VolumeReadingBackgroundRectangle.ActualWidth * volume);
                VolumeDecibelsTextBox.Text = volumeDecibels;
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
                Hide();
            };
        }
    }
}
