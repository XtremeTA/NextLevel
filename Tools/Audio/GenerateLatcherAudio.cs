using System;
using System.IO;

// Add-Type -Path Tools/Audio/GenerateLatcherAudio.cs
// [LatcherSoundDesigner]::Generate('C:\NextLevel\Assets\Audio\Latchers')
public static class LatcherSoundDesigner
{
    public static void Generate(string directory)
    {
        Directory.CreateDirectory(directory);
        Write(directory, "Latcher_Attach", 0.38, 0);
        Write(directory, "Latcher_Charge", 0.68, 1);
        Write(directory, "Latcher_Spike", 0.58, 2);
    }

    private static void Write(string directory, string name, double duration, int kind)
    {
        const int rate = 48000;
        float[] samples = new float[(int)(duration * rate)];
        Random random = new Random(4137 + kind);
        double phase = 0, lowNoise = 0, peak = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            double t = i / (double)rate;
            double u = t / duration;
            double raw = random.NextDouble() * 2 - 1;
            lowNoise += .09 * (raw - lowNoise);
            double frequency = kind == 0 ? 420 - 240 * u : kind == 1 ? 310 + 520 * u * u : 950 - 670 * u;
            phase += 2 * Math.PI * frequency / rate;
            double attack = Math.Min(1, t / (kind == 1 ? .035 : .004));
            double release = Math.Min(1, (duration - t) / .08);
            double pulse = kind == 1 ? .65 + .35 * Math.Sin(2 * Math.PI * (5 * t + 6 * t * t)) : 1;
            double body = kind == 0
                ? Math.Sin(phase) * .26 + lowNoise * .8 + raw * .08
                : kind == 1
                    ? Math.Sin(phase) * .25 + Math.Sin(phase * 1.51) * .08 + lowNoise * .22
                    : Math.Sin(phase) * .25 + lowNoise * .85 + raw * .12;
            double envelope = kind == 1 ? (.2 + .8 * u) * pulse : Math.Exp(-u * (kind == 0 ? 4 : 3));
            samples[i] = (float)(Math.Tanh(body * 1.6) * envelope * attack * release);
            peak = Math.Max(peak, Math.Abs(samples[i]));
        }
        using (BinaryWriter writer = new BinaryWriter(File.Create(Path.Combine(directory, name + ".wav"))))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)1);
            writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(samples.Length * 2);
            foreach (float sample in samples)
                writer.Write((short)Math.Round(sample / peak * .72 * 32767));
        }
    }
}
