using System;
using UnityEngine;

public static class LocalHighScores
{
    public const int Capacity = 5;
    private const string Prefix = "TenCourts.HighScores.";
    public static event Action Changed;

    public readonly struct Entry
    {
        public int Score { get; }
        public string Initials { get; }

        public Entry(int score, string initials)
        {
            Score = score;
            Initials = initials;
        }
    }

    public static int[] Read()
    {
        Entry[] entries = ReadEntries();
        int[] scores = new int[Capacity];
        for (int i = 0; i < scores.Length; i++)
            scores[i] = entries[i].Score;
        return scores;
    }

    public static Entry[] ReadEntries()
    {
        Entry[] entries = new Entry[Capacity];
        for (int i = 0; i < entries.Length; i++)
        {
            int score = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + i, 0));
            string initials = PlayerPrefs.GetString(Prefix + "Initials." + i, "---");
            entries[i] = new Entry(score, TryNormalize(initials, out string normalized) ? normalized : "---");
        }

        // Keep names attached to their scores, preserving the order of tied records.
        for (int i = 1; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            int position = i;
            while (position > 0 && entries[position - 1].Score < entry.Score)
            {
                entries[position] = entries[position - 1];
                position--;
            }
            entries[position] = entry;
        }
        return entries;
    }

    public static int QualifyingRank(int score)
    {
        return score > 0 ? Array.FindIndex(ReadEntries(), entry => score > entry.Score) : -1;
    }

    public static void Clear()
    {
        for (int i = 0; i < Capacity; i++)
        {
            PlayerPrefs.DeleteKey(Prefix + i);
            PlayerPrefs.DeleteKey(Prefix + "Initials." + i);
        }
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static bool Record(int score, string initials)
    {
        if (score <= 0 || !TryNormalize(initials, out string normalized))
            return false;

        Entry[] entries = ReadEntries();
        int place = Array.FindIndex(entries, entry => score > entry.Score);
        if (place < 0)
            return false;

        for (int i = entries.Length - 1; i > place; i--)
            entries[i] = entries[i - 1];
        entries[place] = new Entry(score, normalized);
        for (int i = 0; i < entries.Length; i++)
        {
            // Retain the original score keys so existing local records survive this update.
            PlayerPrefs.SetInt(Prefix + i, entries[i].Score);
            PlayerPrefs.SetString(Prefix + "Initials." + i, entries[i].Initials);
        }
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    private static bool TryNormalize(string value, out string normalized)
    {
        normalized = value != null ? value.Trim().ToUpperInvariant() : "";
        if (normalized.Length != 3)
            return false;
        foreach (char letter in normalized)
            if (letter < 'A' || letter > 'Z')
                return false;
        return true;
    }
}
