using UnityEngine;

// Samples the run when a new row is created. Random rolls happen in the row
// spawner, so a good or bad stretch shifts the odds without fixing a pattern.
public sealed class AdaptiveDifficulty : MonoBehaviour
{
    public static AdaptiveDifficulty Instance { get; private set; }

    [Header("Pressure")]
    [SerializeField, Range(0f, 1f)] private float startingPressure = 0.42f;
    [SerializeField, Range(0f, 1f)] private float scoreInfluence = 0.16f;
    [SerializeField, Range(0f, 1f)] private float paceInfluence = 0.18f;
    [SerializeField, Range(0f, 1f)] private float highScoreInfluence = 0.12f;
    [SerializeField, Range(0f, 1f)] private float timeInfluence = 0.08f;
    [SerializeField, Range(0f, 1f)] private float lostHealthRelief = 0.2f;
    [SerializeField, Range(0f, 1f)] private float lowLivesRelief = 0.15f;
    [SerializeField, Range(0f, 1f)] private float floorDamageRelief = 0.17f;

    [Header("Latcher")]
    [SerializeField, Range(0f, 1f)] private float secondLatcherMinimumPressure = 0.85f;
    [SerializeField, Range(0f, 1f)] private float secondLatcherMaximumChance = 0.06f;

    private int bestScore;

    private void Awake()
    {
        Instance = this;
        bestScore = LocalHighScores.ReadEntries()[0].Score;
    }

    public float GetPressure()
    {
        GameManager game = GameManager.Instance;
        if (game == null) return startingPressure;

        int score = ScoreCounter.Instance != null ? ScoreCounter.Instance.Score : 0;
        float scoreLevel = Mathf.Clamp01(score / 300f);
        float scorePerSecond = score / Mathf.Max(30f, game.ActivePlaySeconds);
        float pace = Mathf.Clamp01((scorePerSecond - 0.5f) / 1.5f);
        float recordProximity = bestScore > 0
            ? Mathf.Clamp01((score / (float)bestScore - 0.65f) / 0.35f) : 0f;
        float duration = Mathf.Clamp01(game.ActivePlaySeconds / 300f);
        float lostHealth = Mathf.Clamp01(game.TotalLivesLost / (float)game.StartingLives);
        float lowLives = 1f - game.Lives / (float)game.StartingLives;

        GroundBreak[] tiles = FindObjectsByType<GroundBreak>(FindObjectsSortMode.None);
        float floorHealth = 1f;
        if (tiles.Length > 0)
        {
            int remaining = 0;
            foreach (GroundBreak tile in tiles) remaining += tile.Health;
            floorHealth = remaining / (4f * tiles.Length);
        }

        return Mathf.Clamp01(startingPressure
            + scoreInfluence * scoreLevel
            + paceInfluence * pace
            + highScoreInfluence * recordProximity
            + timeInfluence * duration
            - lostHealthRelief * lostHealth
            - lowLivesRelief * lowLives
            - floorDamageRelief * (1f - floorHealth));
    }

    public int RollSpecialCount(int stage, int maximum, float stageOneGhostChance, float pressure)
    {
        if (maximum <= 0) return 0;
        if (stage == 1)
        {
            float chance = Mathf.Clamp01(stageOneGhostChance + (pressure - 0.5f) * 0.5f);
            return Random.value < chance ? 1 : 0;
        }

        if (stage == 2 || stage == 4)
            return Random.value < Mathf.Lerp(0.5f, 0.85f, pressure) ? 1 : 0;

        int count = Random.value < Mathf.Lerp(0.6f, 0.9f, pressure) ? 1 : 0;
        for (int slot = 1; slot < maximum; slot++)
        {
            float chance = Mathf.Clamp01(Mathf.Lerp(0.15f, 0.65f, pressure) - 0.15f * (slot - 1));
            if (Random.value < chance) count++;
        }
        return count;
    }

    public bool ShouldAddLatcher(int currentLatchers, float pressure)
    {
        if (currentLatchers == 0) return true;
        if (currentLatchers >= 2 || pressure < secondLatcherMinimumPressure) return false;
        float chance = Mathf.Lerp(0f, secondLatcherMaximumChance,
            Mathf.InverseLerp(secondLatcherMinimumPressure, 1f, pressure));
        return Random.value < chance;
    }

    public float EnemyWeightMultiplier(bool ghost, bool bomb, bool dragon, float pressure)
    {
        if (ghost) return Mathf.Lerp(1.3f, 0.75f, pressure);
        if (bomb) return Mathf.Lerp(0.4f, 1.5f, pressure);
        if (dragon) return Mathf.Lerp(0.5f, 2f, pressure);
        return 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
