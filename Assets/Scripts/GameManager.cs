using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameManager : MonoBehaviour
{
    private const int LifeCap = 3;
    public static GameManager Instance { get; private set; }

    [SerializeField, Range(1, LifeCap)] private int startingLives = 3;
    [Tooltip("One heart per life, left to right.")]
    [SerializeField] private Image[] heartImages = new Image[3];
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;
    [SerializeField] private GameOverScreen gameOverScreen;

    [Header("Speed Progression")]
    [Tooltip("Active gameplay seconds to reach the ball and player maximum speeds.")]
    [SerializeField, Min(1f)] private float speedRampDuration = 180f;
    [Header("Stages")]
    [Tooltip("Score required to enter Stage 2.")]
    [SerializeField, Min(1)] private int stageTwoScore = 100;
    [Tooltip("Score in Stage 2 when both ghost and bomb bricks begin appearing in new rows.")]
    [SerializeField, Min(1)] private int bothEnemiesScore = 150;
    [Tooltip("Score required to enter Stage 3 and introduce dragon bricks.")]
    [SerializeField, Min(1)] private int stageThreeScore = 250;
    private float speedRampElapsed;
    private bool scoreRecorded;

    public int Lives { get; private set; }
    public int MaximumLives => LifeCap;
    public bool IsGameOver { get; private set; }
    public int FinalScore { get; private set; }
    public int CurrentStage { get; private set; } = 1;
    private int StageTwoThreshold => Mathf.Max(1, stageTwoScore);
    private int BothEnemiesThreshold => Mathf.Max(StageTwoThreshold + 1, bothEnemiesScore);
    private int StageThreeThreshold => Mathf.Max(BothEnemiesThreshold + 1, stageThreeScore);
    public bool BothEnemiesUnlocked => ScoreCounter.Instance != null &&
        ScoreCounter.Instance.Score >= BothEnemiesThreshold;
    public float SpeedRampProgress => Mathf.Clamp01(speedRampElapsed / Mathf.Max(1f, speedRampDuration));
    public event Action<int> StageChanged;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        speedRampElapsed = 0f;
        Lives = Mathf.Clamp(startingLives, 1, MaximumLives);
        UpdateHearts();
    }

    private void Update()
    {
        if (!IsGameOver && Time.timeScale > 0f)
            speedRampElapsed = Mathf.Min(speedRampElapsed + Time.deltaTime, Mathf.Max(1f, speedRampDuration));

        int score = ScoreCounter.Instance != null ? ScoreCounter.Instance.Score : 0;
        int nextStage = score >= StageThreeThreshold ? 3 : score >= StageTwoThreshold ? 2 : 1;
        if (nextStage != CurrentStage)
        {
            CurrentStage = nextStage;
            StageChanged?.Invoke(CurrentStage);
        }
    }

    public bool LoseLife()
    {
        if (IsGameOver)
            return false;

        Lives = Mathf.Max(0, Lives - 1);
        if (Lives == 0)
            EndGame();
        else
            UpdateHearts();
        return true;
    }

    public void EndGame()
    {
        if (IsGameOver)
            return;

        Lives = 0;
        UpdateHearts();
        IsGameOver = true;
        FinalScore = ScoreCounter.Instance != null ? ScoreCounter.Instance.Score : 0;
        StartCoroutine(ShowGameOver());
    }

    public bool GainLife()
    {
        if (IsGameOver || Lives >= MaximumLives)
            return false;

        Lives++;
        UpdateHearts();
        return true;
    }

    private void UpdateHearts()
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
                heartImages[i].sprite = i < Lives ? fullHeart : emptyHeart;
        }
    }

    private IEnumerator ShowGameOver()
    {
        // Let the final hurt animation and knockback register before pausing.
        yield return new WaitForSeconds(0.4f);
        Player player = FindAnyObjectByType<Player>();
        while (player != null && player.IsHitStunned)
            yield return null;
        Time.timeScale = 0f;
        if (gameOverScreen != null)
            gameOverScreen.Show(FinalScore);
    }

    public void RestartGame()
    {
        // Finish initials entry before allowing a new run, including during the final hurt.
        if (IsGameOver && (gameOverScreen == null || !gameOverScreen.CanRetry))
            return;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public bool SubmitHighScore(string initials)
    {
        if (!IsGameOver || scoreRecorded || gameOverScreen == null || !gameOverScreen.IsEnteringInitials)
            return false;
        scoreRecorded = LocalHighScores.Record(FinalScore, initials);
        return scoreRecorded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
        }
    }
}
