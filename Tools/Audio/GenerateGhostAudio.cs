using System;
using System.Collections.Generic;
using System.IO;

// Run from PowerShell: Add-Type -Path Tools/Audio/GenerateGhostAudio.cs;
// [GhostSoundDesigner]::Generate('C:\NextLevel\Assets\Audio\Ghosts')
public static class GhostSoundDesigner
{
    private const int SampleRate = 48000;

    public static string[] Generate(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var reports = new List<string>();
        for (int kind = 0; kind < 3; kind++)
        {
            for (int variant = 0; variant < 2; variant++)
            {
                string name = new[] { "Ghost_Appear", "Ghost_Burst", "Ghost_Split" }[kind] + "_" + (variant + 1);
                float[] samples = Create(kind, variant);
                WriteWav(Path.Combine(outputDirectory, name + ".wav"), samples);
                double energy = 0;
                float peak = 0;
                foreach (float sample in samples) { energy += sample * sample; peak = Math.Max(peak, Math.Abs(sample)); }
                reports.Add(name + ": " + (samples.Length / (double)SampleRate).ToString("F2") + "s, peak " + peak.ToString("F3") + ", RMS " + Math.Sqrt(energy / samples.Length).ToString("F3"));
            }
        }
        return reports.ToArray();
    }

    private static float[] Create(int kind, int variant)
    {
        double duration = (kind == 0 ? 0.90 : kind == 1 ? 0.43 : 0.36) + variant * 0.035;
        int count = (int)Math.Ceiling((duration + 0.24) * SampleRate);
        var dry = new float[count];
        var result = new float[count];
        var random = new Random(49031 + kind * 971 + variant * 413);
        double phase = 0, phase2 = 0, low = 0, rumble = 0;
        for (int i = 0; i < count; i++)
        {
            double time = i / (double)SampleRate;
            double u = time / duration;
            if (u >= 1) continue;
            double noise = random.NextDouble() * 2 - 1;
            double cutoff = kind == 0 ? 1900 - 900 * u : kind == 1 ? 2900 - 1700 * u : 2500;
            low += (1 - Math.Exp(-2 * Math.PI * cutoff / SampleRate)) * (noise - low);
            rumble += (1 - Math.Exp(-2 * Math.PI * 190 / SampleRate)) * (noise - rumble);
            double breath = low - rumble;
            double frequency;
            if (kind == 0) frequency = 172 + variant * 13 - 70 * (1 - Math.Exp(-3.5 * u)) + 4 * Math.Sin(2 * Math.PI * 5.1 * time);
            else if (kind == 1) frequency = 112 * Math.Exp(-7 * time) + 52 + variant * 5;
            else frequency = 290 + variant * 35 + 440 * (1 - Math.Exp(-4 * u)) + 8 * Math.Sin(2 * Math.PI * 6 * time);
            phase += 2 * Math.PI * frequency / SampleRate;
            phase2 += 2 * Math.PI * frequency * (kind == 2 ? 1.2599 : 2.012) / SampleRate;
            double attack = Smooth(Math.Min(1, time / (kind == 1 ? 0.012 : 0.065)));
            double release = Smooth(Math.Min(1, (duration - time) / (kind == 0 ? 0.4 : 0.18)));
            double tone;
            double envelope;
            if (kind == 0)
            {
                envelope = attack * release * (0.72 + 0.28 * Math.Sin(Math.PI * u));
                tone = 0.25 * Math.Sin(phase) + 0.13 * Math.Sin(phase2) + 0.045 * Math.Sin(phase * 3.006) + 0.27 * breath;
            }
            else if (kind == 1)
            {
                envelope = attack * release * Math.Exp(-3.4 * u);
                tone = 0.43 * breath + 0.16 * rumble + 0.25 * Math.Sin(phase) + 0.035 * Math.Sin(phase2 * 2.2) * Math.Exp(-13 * time);
            }
            else
            {
                envelope = attack * release * Math.Pow(Math.Sin(Math.PI * u), 0.65);
                tone = 0.22 * Math.Sin(phase) + 0.13 * Math.Sin(phase2) + 0.23 * breath;
            }
            dry[i] = (float)(Math.Tanh(tone * 1.4) * envelope);
        }

        double previous = 0, highpass = 0;
        float maximum = 0;
        int firstDelay = (int)((0.047 + variant * 0.004) * SampleRate);
        int secondDelay = (int)(0.089 * SampleRate);
        int thirdDelay = (int)(0.173 * SampleRate);
        for (int i = 0; i < count; i++)
        {
            double value = dry[i];
            if (i >= firstDelay) value += dry[i - firstDelay] * 0.19;
            if (i >= secondDelay) value += dry[i - secondDelay] * 0.11;
            if (i >= thirdDelay) value += dry[i - thirdDelay] * 0.065;
            highpass = 0.997 * (highpass + value - previous);
            previous = value;
            double edgeFade = Math.Min(1, (count - 1 - i) / (SampleRate * 0.008));
            result[i] = (float)(highpass * edgeFade);
            maximum = Math.Max(maximum, Math.Abs(result[i]));
        }
        float gain = maximum > 0 ? 0.55f / maximum : 0;
        for (int i = 0; i < count; i++) result[i] *= gain;
        return result;
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
            foreach (float value in samples) writer.Write((short)Math.Round(Math.Max(-1, Math.Min(1, value)) * 32767));
        }
    }
}
