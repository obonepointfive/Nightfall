using System;
using System.Windows;
using System.Windows.Threading;
using Nightfall.UI.ViewModels;

namespace Nightfall.UI.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            _vm = new MainWindowViewModel();
            DataContext = _vm;

            // TEMP HUD Telemetry demo
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            timer.Tick += (s, e) =>
            {
                var rnd = new Random();
                var t = new HudTelemetry
                {
                    ModeLabel = "ACTIVE TRAFFIC",
                    EmotionLabel = $"Mood: Warmth {rnd.NextDouble():0.00}",
                    MoodWarmth = (float)rnd.NextDouble(),
                    MoodIntensity = (float)rnd.NextDouble(),
                    MoodFocus = (float)rnd.NextDouble()
                };

                _vm.UpdateFromTelemetry(t);
            };

            timer.Start();
        }
    }
}
