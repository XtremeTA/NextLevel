using System;
using System.Collections.Generic;
using System.IO;

// Run from PowerShell: Add-Type -Path Tools/Audio/GenerateBombAudio.cs;
// [BombSoundDesigner]::Generate('C:\NextLevel\Assets\Audio\Bombs')
public static class BombSoundDesigner
{
    private const int SampleRate = 48000;

    public static string[] Generate(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var sounds = new Dictionary<string, float[]>
        {
            { "Bomb_Tick", CreateTick() },
            { "Bomb_Explosion", CreateExplosion() }
        };

        var reports = new List<string>();
        foreach (KeyValuePair<string, float[]> sound in sounds)
        {
            WriteWav(Path.Combine(outputDirectory, sound.Key + ".wav"), sound.Value);
            double energy = 0;
            float peak = 0;
            foreach (float sample in sound.Value)
            {
                energy += sample * sample;
                peak = Math.Max(peak, Math.Abs(sample));
            }
            reports.Add(sound.Key + ": " + (sound.Value.Length / (double)SampleRate).ToString("F2") +
                "s, peak " + peak.ToString("F3") + ", RMS " +
                Math.Sqrt(energy / sound.Value.Length).ToString("F3"));
        }
        return reports.ToArray();
    }

    private static float[] CreateTick()
    {
        const double duration = 0.46;
        int count = (int)(duration * SampleRate);
        var result = new float[count];
        var random = new Random(73109);
        double phaseA = 0, phaseB = 0, low = 0, previous = 0, highpass = 0;
        for (int i = 0; i < count; i++)
        {
            double time = i / (double)SampleRate;
            double noise = random.NextDouble() * 2 - 1;
            low += 0.18 * (noise - low);
            highpass = 0.91 * (highpass + low - previous);
            previous = low;

            phaseA += 2 * Math.PI * (1750 - 420 * time) / SampleRate;
            phaseB += 2 * Math.PI * 2870 / SampleRate;
            double metal = Math.Sin(phaseA) * 0.34 + Math.Sin(phaseB) * 0.16 + highpass * 0.42;
            double primary = Math.Exp(-time * 78) * Smooth(Math.Min(1, time / 0.0015));
            double reboundTime = time - 0.055;
            double rebound = reboundTime >= 0 ? Math.Exp(-reboundTime * 95) * 0.24 : 0;
            result[i] = (float)(Math.Tanh(metal * 1.7) * (primary + rebound));
        }
        Normalize(result, 0.52f);
        return result;
    }

    private static float[] CreateExplosion()
    {
        const double duration = 1.45;
        int count = (int)(duration * SampleRate);
        var dry = new float[count];
        var result = new float[count];
        var random = new Random(91823);
        double lowNoise = 0, midNoise = 0, phase = 0;
        for (int i = 0; i < count; i++)
        {
            double time = i / (double)SampleRate;
            double u = time / duration;
            double noise = random.NextDouble() * 2 - 1;
            lowNoise += 0.015 * (noise - lowNoise);
            midNoise += 0.11 * (noise - midNoise);
            double bandNoise = midNoise - lowNoise;

            double frequency = 82 * Math.Exp(-time * 3.4) + 34;
            phase += 2 * Math.PI * frequency / SampleRate;
            double boom = Math.Sin(phase) * Math.Exp(-time * 3.8);
            double body = lowNoise * Math.Exp(-time * 2.9);
            double crack = bandNoise * Math.Exp(-time * 15);
            double transient = noise * Math.Exp(-time * 55);
            double attack = Smooth(Math.Min(1, time / 0.004));
            double tail = Smooth(Math.Min(1, (duration - time) / 0.12));
            dry[i] = (float)(Math.Tanh((boom * 0.72 + body * 2.8 + crack * 1.15 + transient * 0.7) * 1.45) * attack * tail);
        }

        int echoA = (int)(0.071 * SampleRate);
        int echoB = (int)(0.139 * SampleRate);
        int echoC = (int)(0.223 * SampleRate);
        for (int i = 0; i < count; i++)
        {
            double value = dry[i];
            if (i >= echoA) value += dry[i - echoA] * 0.19;
            if (i >= echoB) value += dry[i - echoB] * 0.12;
            if (i >= echoC) value += dry[i - echoC] * 0.075;
            result[i] = (float)value;
        }
        Normalize(result, 0.86f);
        return result;
    }

    private static void Normalize(float[] samples, float targetPeak)
    {
        float maximum = 0;
        foreach (float sample in samples)
            maximum = Math.Max(maximum, Math.Abs(sample));
        float gain = maximum > 0 ? targetPeak / maximum : 0;
        for (int i = 0; i < samples.Length; i++)
            samples[i] *= gain;
    }

    private static double Smooth(double x) { return x * x * (3 - 2 * x); }

    private static void WriteWav(string path, float[] samples)
    {
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)1);
            writer.Write(SampleRate); writer.Write(SampleRate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (float value in samples)
                writer.Write((short)Math.Round(Math.Max(-1, Math.Min(1, value)) * 32767));
        }
    }
}
