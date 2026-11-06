using System;
using System.IO;

// Add-Type -Path Tools/Audio/GenerateDragonAudio.cs
// [DragonSoundDesigner]::Generate('C:\NextLevel\Assets\Audio\Dragons')
public static class DragonSoundDesigner
{
    public static void Generate(string directory)
    {
        Directory.CreateDirectory(directory);
        Write(directory, "Dragon_Wisp", 0.5, 0);
        Write(directory, "Dragon_Warning", 1.1, 1);
        Write(directory, "Dragon_Rise", 0.85, 2);
    }

    private static void Write(string directory, string name, double duration, int kind)
    {
        const int rate = 48000;
        float[] samples = new float[(int)(duration * rate)];
        Random random = new Random(7300 + kind);
        double phase = 0, noise = 0, peak = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            double t = i / (double)rate, u = t / duration;
            noise += (kind == 2 ? .12 : .035) * (random.NextDouble() * 2 - 1 - noise);
            double frequency = kind == 0 ? 850 - 450 * u : kind == 1 ? 230 + 620 * u * u : 85 + 180 * u;
            phase += 2 * Math.PI * frequency / rate;
            double envelope = Math.Min(1, t / .025) * Math.Min(1, (duration - t) / .08);
            double signal;
            if (kind == 0)
                signal = (Math.Sin(phase) * .32 + Math.Sin(phase * 1.503) * .12 + noise * .6) * Math.Exp(-u * 3);
            else if (kind == 1)
                signal = (Math.Sin(phase) * .3 + noise * .5) * (.25 + .75 * u) * (.65 + .35 * Math.Sin(2 * Math.PI * (3 * t + 4 * t * t)));
            else
                signal = Math.Tanh((Math.Sin(phase + Math.Sin(phase * .48) * 2) * .4 + noise * 2) * 1.4) * Math.Pow(1 - u, .7);
            samples[i] = (float)(signal * envelope);
            peak = Math.Max(peak, Math.Abs(samples[i]));
        }
        using (BinaryWriter writer = new BinaryWriter(File.Create(Path.Combine(directory, name + ".wav"))))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)1);
            writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (float sample in samples) writer.Write((short)Math.Round(sample / peak * .75 * 32767));
        }
    }
}
