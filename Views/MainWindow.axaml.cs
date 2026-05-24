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

        private void TriggerAudioSignalPlayback()
        {
            var baseFreqSlider = this.FindControl<Slider>("BaseFreqSlider");
            var modFreqSlider = this.FindControl<Slider>("ModFreqSlider");
            var modDepthSlider = this.FindControl<Slider>("ModDepthSlider");
            var durationSlider = this.FindControl<Slider>("DurationSlider");
            var noiseSlider = this.FindControl<Slider>("NoiseSlider");
            var waveCombo = this.FindControl<ComboBox>("WaveTypeCombo");
            var delayCheck = this.FindControl<CheckBox>("DelayCheck");
            var flangerCheck = this.FindControl<CheckBox>("FlangerCheck");

            double baseFreq = baseFreqSlider?.Value ?? 220;
            double modFreq = modFreqSlider?.Value ?? 5;
            double modDepth = modDepthSlider?.Value ?? 50;
            double duration = durationSlider?.Value ?? 1.0;
            double noiseAmount = noiseSlider?.Value ?? 0.0;

            string waveType = (waveCombo?.SelectedIndex) switch { 1 => "Square", 2 => "Sawtooth", 3 => "Triangle", _ => "Sine" };
            bool useDelay = delayCheck?.IsChecked ?? false;
            bool useFlanger = flangerCheck?.IsChecked ?? false;

            float[] audioData = AudioEngine.GenerateAdvancedBuffer(baseFreq, modFreq, modDepth, duration, waveType, useDelay, useFlanger, noiseAmount);

            try
            {
                string tempWavPath = Path.Combine(Path.GetTempPath(), "sfx_studio_runtime.wav");
                AudioEngine.ExportToWav(tempWavPath, audioData);

                if (OperatingSystem.IsLinux())
                {
                    System.Diagnostics.Process.Start("aplay", tempWavPath)?.WaitForExit();
                }
                else if (OperatingSystem.IsWindows())
                {
                    string command = $"$p = New-Object Media.SoundPlayer '{tempWavPath}'; $p.PlaySync()";
                    System.Diagnostics.Process.Start("powershell", $"-Command \"{command}\"")?.WaitForExit();
                }
                else if (OperatingSystem.IsMacOS())
                {
                    System.Diagnostics.Process.Start("afplay", tempWavPath)?.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audio output failed: {ex.Message}");
            }
        }

        private void OnPlayPreviewClick(object sender, RoutedEventArgs e) => TriggerAudioSignalPlayback();

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                TriggerAudioSignalPlayback();
                e.Handled = true;
            }
        }

        private void OnExportWavClick(object sender, RoutedEventArgs e)
        {
            var baseFreqSlider = this.FindControl<Slider>("BaseFreqSlider");
            var modFreqSlider = this.FindControl<Slider>("ModFreqSlider");
            var modDepthSlider = this.FindControl<Slider>("ModDepthSlider");
            var durationSlider = this.FindControl<Slider>("DurationSlider");
            var noiseSlider = this.FindControl<Slider>("NoiseSlider");
            var waveCombo = this.FindControl<ComboBox>("WaveTypeCombo");
            var delayCheck = this.FindControl<CheckBox>("DelayCheck");
            var flangerCheck = this.FindControl<CheckBox>("FlangerCheck");

            string waveType = (waveCombo?.SelectedIndex) switch { 1 => "Square", 2 => "Sawtooth", 3 => "Triangle", _ => "Sine" };
            float[] audioData = AudioEngine.GenerateAdvancedBuffer(
                baseFreqSlider?.Value ?? 220, modFreqSlider?.Value ?? 5, modDepthSlider?.Value ?? 50, 
                durationSlider?.Value ?? 1.0, waveType, delayCheck?.IsChecked ?? false, flangerCheck?.IsChecked ?? false, noiseSlider?.Value ?? 0.0);

            string filename = $"SpaceSFX_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), filename);
            AudioEngine.ExportToWav(outputPath, audioData);
        }

        // --- PRESET LOGIC BINDINGS ---
        private void LoadLaserPreset(object sender, RoutedEventArgs e)
        {
            SetSliderValues(800, 60, 450, 0.25, 0.0, 2); // Fast Sawtooth drop, 0 noise
            SetFX(false, true);
        }

        private void LoadDronePreset(object sender, RoutedEventArgs e)
        {
            SetSliderValues(55, 3, 15, 3.5, 0.1, 0); // Bass hum, 10% background engine static noise
            SetFX(true, false);
        }

        // Turning the old data preset into a Heavy Deep-Space Explosion preset!
        private void LoadDataPreset(object sender, RoutedEventArgs e)
        {
            SetSliderValues(40, 10, 5, 2.0, 1.0, 0); // Low frequency, 100% white noise texture
            SetFX(true, false); // Space echo delay for debris echo
        }

        private void TriggerRandomizer(object sender, RoutedEventArgs e)
        {
            SetSliderValues(
                _rand.Next(40, 1600),   // Base Freq
                _rand.Next(0, 110),     // Mod Speed
                _rand.Next(0, 500),     // Mod Depth
                Math.Round(_rand.NextDouble() * 1.8 + 0.1, 2), // Duration
                Math.Round(_rand.NextDouble(), 2), // Random Noise Amount (0.0 to 1.0)
                _rand.Next(0, 4)        // Wave Profile
            );
            SetFX(_rand.Next(0, 2) == 1, _rand.Next(0, 2) == 1);
            TriggerAudioSignalPlayback();
        }

        private void SetSliderValues(double baseF, double modF, double modD, double dur, double noise, int waveIdx)
        {
            if (this.FindControl<Slider>("BaseFreqSlider") is Slider b) b.Value = baseF;
            if (this.FindControl<Slider>("ModFreqSlider") is Slider m) m.Value = modF;
            if (this.FindControl<Slider>("ModDepthSlider") is Slider d) d.Value = modD;
            if (this.FindControl<Slider>("DurationSlider") is Slider t) t.Value = dur;
            if (this.FindControl<Slider>("NoiseSlider") is Slider n) n.Value = noise;
            if (this.FindControl<ComboBox>("WaveTypeCombo") is ComboBox c) c.SelectedIndex = waveIdx;
        }

        private void SetFX(bool delay, bool flanger)
        {
            if (this.FindControl<CheckBox>("DelayCheck") is CheckBox d) d.IsChecked = delay;
            if (this.FindControl<CheckBox>("FlangerCheck") is CheckBox f) f.IsChecked = flanger;
        }
    }
}
