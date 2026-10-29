using System;
using UnityEngine;

public static class LocalHighScores
{
    public const int Capacity = 10;
    private const string Prefix = "TenCourts.HighScores.";
    public static event Action Changed;

    public static int[] Read()
    {
        int[] scores = new int[Capacity];
        for (int i = 0; i < scores.Length; i++)
            scores[i] = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + i, 0));
        Array.Sort(scores);
        Array.Reverse(scores);
        return scores;
    }

    public static void Record(int score)
    {
        if (score <= 0)
            return;

        int[] scores = Read();
        int place = Array.FindIndex(scores, value => score > value);
        if (place < 0)
            return;

        for (int i = scores.Length - 1; i > place; i--)
            scores[i] = scores[i - 1];
        scores[place] = score;
        for (int i = 0; i < scores.Length; i++)
            PlayerPrefs.SetInt(Prefix + i, scores[i]);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
