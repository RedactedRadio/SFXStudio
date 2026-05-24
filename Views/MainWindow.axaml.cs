using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using System;
using System.IO;

namespace SFXStudio.Views
{
    public partial class MainWindow : Window
    {
        private readonly Random _rand = new();

        public MainWindow()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        // Master compilation trigger function mapped to slider variables
        private void TriggerAudioSignalPlayback()
        {
            var baseFreqSlider = this.FindControl<Slider>("BaseFreqSlider");
            var modFreqSlider = this.FindControl<Slider>("ModFreqSlider");
            var modDepthSlider = this.FindControl<Slider>("ModDepthSlider");
            var durationSlider = this.FindControl<Slider>("DurationSlider");
            var waveCombo = this.FindControl<ComboBox>("WaveTypeCombo");
            var delayCheck = this.FindControl<CheckBox>("DelayCheck");
            var flangerCheck = this.FindControl<CheckBox>("FlangerCheck");

            double baseFreq = baseFreqSlider?.Value ?? 220;
            double modFreq = modFreqSlider?.Value ?? 5;
            double modDepth = modDepthSlider?.Value ?? 50;
            double duration = durationSlider?.Value ?? 1.0;

            // Extract pure string matching selection profile index
            string waveType = (waveCombo?.SelectedIndex) switch
            {
                1 => "Square",
                2 => "Sawtooth",
                3 => "Triangle",
                _ => "Sine"
            };

            bool useDelay = delayCheck?.IsChecked ?? false;
            bool useFlanger = flangerCheck?.IsChecked ?? false;

            float[] audioData = AudioEngine.GenerateAdvancedBuffer(baseFreq, modFreq, modDepth, duration, waveType, useDelay, useFlanger);

            try
            {
                // 1. Save the preview file to a universal temporary directory
                string tempWavPath = Path.Combine(Path.GetTempPath(), "sfx_studio_runtime.wav");
                AudioEngine.ExportToWav(tempWavPath, audioData);

                // 2. Detect the user's Operating System and use their native player
                if (OperatingSystem.IsLinux())
                {
                    System.Diagnostics.Process.Start("aplay", tempWavPath)?.WaitForExit();
                }
                else if (OperatingSystem.IsWindows())
                {
                    // Uses Windows PowerShell to play the sound natively
                    string command = $"$p = New-Object Media.SoundPlayer '{tempWavPath}'; $p.PlaySync()";
                    System.Diagnostics.Process.Start("powershell", $"-Command \"{command}\"")?.WaitForExit();
                }
                else if (OperatingSystem.IsMacOS())
                {
                    // Uses the native Mac audio player command
                    System.Diagnostics.Process.Start("afplay", tempWavPath)?.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"System audio playback failed: {ex.Message}");
            }
        }

        private void OnPlayPreviewClick(object sender, RoutedEventArgs e) => TriggerAudioSignalPlayback();

        // Detects the Spacebar key to play the sound instantly
        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                TriggerAudioSignalPlayback();
                e.Handled = true; // Stop the spacebar from moving sliders
            }
        }

        private void OnExportWavClick(object sender, RoutedEventArgs e)
        {
            var baseFreqSlider = this.FindControl<Slider>("BaseFreqSlider");
            var modFreqSlider = this.FindControl<Slider>("ModFreqSlider");
            var modDepthSlider = this.FindControl<Slider>("ModDepthSlider");
            var durationSlider = this.FindControl<Slider>("DurationSlider");
            var waveCombo = this.FindControl<ComboBox>("WaveTypeCombo");
            var delayCheck = this.FindControl<CheckBox>("DelayCheck");
            var flangerCheck = this.FindControl<CheckBox>("FlangerCheck");

            string waveType = (waveCombo?.SelectedIndex) switch { 1 => "Square", 2 => "Sawtooth", 3 => "Triangle", _ => "Sine" };
            float[] audioData = AudioEngine.GenerateAdvancedBuffer(
                baseFreqSlider?.Value ?? 220, modFreqSlider?.Value ?? 5, modDepthSlider?.Value ?? 50, 
                durationSlider?.Value ?? 1.0, waveType, delayCheck?.IsChecked ?? false, flangerCheck?.IsChecked ?? false);

            string filename = $"SpaceSFX_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), filename);
            AudioEngine.ExportToWav(outputPath, audioData);
            Console.WriteLine($"Export Complete -> Saved: {outputPath}");
        }

        // --- PRESET PROFILE MATRIX BINDINGS ---
        private void LoadLaserPreset(object sender, RoutedEventArgs e)
        {
            SetSliderValues(800, 60, 450, 0.25, 2); // Sawtooth fast drop
            SetFX(false, true);
        }

        private void LoadDronePreset(object sender, RoutedEventArgs e)
        {
            SetSliderValues(55, 3, 15, 3.5, 0); // Low Sine modulation
            SetFX(true, false);
        }

        private void LoadDataPreset(object sender, RoutedEventArgs e)
        {
            SetSliderValues(1400, 95, 550, 0.6, 1); // Square data wave
            SetFX(true, true);
        }

        private void TriggerRandomizer(object sender, RoutedEventArgs e)
        {
            SetSliderValues(
                _rand.Next(40, 1600),   // Base Frequency
                _rand.Next(0, 110),     // Mod Speed
                _rand.Next(0, 500),     // Mod Depth
                Math.Round(_rand.NextDouble() * 1.8 + 0.1, 2), // Duration
                _rand.Next(0, 4)        // Random wave pattern profile
            );
            SetFX(_rand.Next(0, 2) == 1, _rand.Next(0, 2) == 1);
            TriggerAudioSignalPlayback(); // Fire sound immediately upon scrambling variables
        }

        private void SetSliderValues(double baseF, double modF, double modD, double dur, int waveIdx)
        {
            if (this.FindControl<Slider>("BaseFreqSlider") is Slider b) b.Value = baseF;
            if (this.FindControl<Slider>("ModFreqSlider") is Slider m) m.Value = modF;
            if (this.FindControl<Slider>("ModDepthSlider") is Slider d) d.Value = modD;
            if (this.FindControl<Slider>("DurationSlider") is Slider t) t.Value = dur;
            if (this.FindControl<ComboBox>("WaveTypeCombo") is ComboBox c) c.SelectedIndex = waveIdx;
        }

        private void SetFX(bool delay, bool flanger)
        {
            if (this.FindControl<CheckBox>("DelayCheck") is CheckBox d) d.IsChecked = delay;
            if (this.FindControl<CheckBox>("FlangerCheck") is CheckBox f) f.IsChecked = flanger;
        }
    }
}
