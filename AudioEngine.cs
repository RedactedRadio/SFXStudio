using System;
using System.IO;

namespace SFXStudio.Views;

public class AudioEngine
{
    public const int SampleRate = 44100;
    private static readonly Random _noiseRand = new();

    public static float[] GenerateAdvancedBuffer(
        double baseFreq, double modFreq, double modDepth, double duration, 
        string waveType, bool useDelay, bool useFlanger, double noiseAmount)
    {
        int totalSamples = (int)(SampleRate * duration);
        float[] buffer = new float[totalSamples];
        
        double carrierPhase = 0.0;
        double modulatorPhase = 0.0;

        // Low-pass filter variables for heavy explosion rumbles
        float filterLastSample = 0f;
        float filterCutoff = 0.15f; // Lower numbers make it bassier/heavier

        // 1. Core Synthesis Generation Loop
        for (int i = 0; i < totalSamples; i++)
        {
            double progress = (double)i / totalSamples;

            // Frequency Modulation (Wobble) Math
            modulatorPhase += (2 * Math.PI * modFreq) / SampleRate;
            double modValue = Math.Sin(modulatorPhase);
            double currentFrequency = baseFreq + (modValue * modDepth);
            if (currentFrequency < 10) currentFrequency = 10;

            carrierPhase += (2 * Math.PI * currentFrequency) / SampleRate;
            if (carrierPhase > 2 * Math.PI) carrierPhase -= 2 * Math.PI;

            // Generate Oscillator Wave Form
            double rawSample = waveType switch
            {
                "Square" => (Math.Sin(carrierPhase) >= 0) ? 1.0 : -1.0,
                "Sawtooth" => 2.0 * (carrierPhase / (2 * Math.PI)) - 1.0,
                "Triangle" => Math.Abs(4.0 * (carrierPhase / (2 * Math.PI)) - 2.0) - 1.0,
                _ => Math.Sin(carrierPhase) // Default: Sine
            };

            // --- WHITE NOISE & EXPLOSION GENERATOR ---
            // Generate a random value between -1.0 and 1.0
            double rawNoise = (_noiseRand.NextDouble() * 2.0) - 1.0;

            // Run the raw noise through a Low-Pass Filter to make it a heavy rumble
            float filteredNoise = filterLastSample + (filterCutoff * ((float)rawNoise - filterLastSample));
            filterLastSample = filteredNoise;

            // Mix the core wave shape with the filtered explosion noise
            double mixedSample = (rawSample * (1.0 - noiseAmount)) + (filteredNoise * noiseAmount);

            // Amplitude Envelope: Smooth exponential fade-out curve
            double envelope = Math.Exp(-3.0 * progress);
            buffer[i] = (float)(mixedSample * envelope * 0.4);
        }

        // 2. DSP FX: Sci-Fi Flanger
        if (useFlanger)
        {
            float[] flangerBuffer = new float[totalSamples];
            double flangerPhase = 0.0;
            for (int i = 0; i < totalSamples; i++)
            {
                flangerPhase += (2 * Math.PI * 2.0) / SampleRate;
                double dynamicDelay = 0.003 + (Math.Sin(flangerPhase) + 1.0) * 0.002;
                int delaySamples = (int)(dynamicDelay * SampleRate);
                int sourceIndex = i - delaySamples;
                float delayedSample = (sourceIndex >= 0) ? buffer[sourceIndex] : 0f;
                flangerBuffer[i] = (buffer[i] * 0.6f) + (delayedSample * 0.4f);
            }
            buffer = flangerBuffer;
        }

        // 3. DSP FX: Space Echo Delay
        if (useDelay)
        {
            int delaySamples = (int)(0.25 * SampleRate);
            float feedback = 0.4f;
            for (int i = delaySamples; i < totalSamples; i++)
            {
                buffer[i] += buffer[i - delaySamples] * feedback;
            }
        }

        return buffer;
    }

    public static void ExportToWav(string filePath, float[] floatBuffer)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            int dataLength = floatBuffer.Length * 2;
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataLength);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); 
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);

            foreach (float sample in floatBuffer)
            {
                short shortSample = (short)(Math.Clamp(sample, -1.0f, 1.0f) * short.MaxValue);
                writer.Write(shortSample);
            }
        }
    }
}
