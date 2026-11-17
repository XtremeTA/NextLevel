using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameManager : MonoBehaviour
{
    private const int LifeCap = 3;
    private static readonly long[] StageFiveFloorScores = { 401, 500, 650, 800, 1000 };
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
    [Tooltip("Score required to enter Stage 3.")]
    [SerializeField, Min(1)] private int stageThreeScore = 150;
    [Tooltip("Score required to enter Stage 4.")]
    [SerializeField, Min(1)] private int stageFourScore = 226;
    [Tooltip("Score required to enter Stage 5.")]
    [SerializeField, Min(1)] private int stageFiveScore = 301;
    private float speedRampElapsed;
    private float activePlaySeconds;
    private bool scoreRecorded;
    private int testingStage;
    private int stageFiveFloorScoreIndex;
    private long nextStageFiveFloorResetScore;
    private long nextStageFiveResetGap;
    public bool IsStageTest => testingStage > 0;
    public bool GuaranteeStageTestEnemy { get; private set; }

    public int Lives { get; private set; }
    public int StartingLives => Mathf.Clamp(startingLives, 1, LifeCap);
    public int TotalLivesLost { get; private set; }
    public float ActivePlaySeconds => activePlaySeconds;
    public int MaximumLives => LifeCap;
    public bool IsGameOver { get; private set; }
    public int FinalScore { get; private set; }
    public int CurrentStage { get; private set; } = 1;
    private int StageTwoThreshold => Mathf.Max(1, stageTwoScore);
    private int StageThreeThreshold => Mathf.Max(StageTwoThreshold + 1, stageThreeScore);
    private int StageFourThreshold => Mathf.Max(StageThreeThreshold + 1, stageFourScore);
    private int StageFiveThreshold => Mathf.Max(StageFourThreshold + 1, stageFiveScore);
    public float SpeedRampProgress => Mathf.Clamp01(speedRampElapsed / Mathf.Max(1f, speedRampDuration));
    public event Action<int> StageChanged;
    public event Action FloorResetRequested;

    private void Awake()
    {
        Instance = this;
#if UNITY_EDITOR
        testingStage = Mathf.Clamp(UnityEditor.SessionState.GetInt("NextLevel.TestStage", 0), 0, 5);
        GuaranteeStageTestEnemy = IsStageTest && UnityEditor.SessionState.GetBool("NextLevel.TestGuaranteeEnemy", true);
#endif
        CurrentStage = IsStageTest ? testingStage : 1;
        if (CurrentStage == 5) StartStageFiveFloorSchedule();
        Time.timeScale = 1f;
        speedRampElapsed = 0f;
        Lives = StartingLives;
        UpdateHearts();
    }

    private void Update()
    {
        if (!IsGameOver && Time.timeScale > 0f)
        {
            speedRampElapsed = Mathf.Min(speedRampElapsed + Time.deltaTime, Mathf.Max(1f, speedRampDuration));
            activePlaySeconds += Time.deltaTime;
        }

        int score = ScoreCounter.Instance != null ? ScoreCounter.Instance.Score : 0;
        int nextStage = score >= StageFiveThreshold ? 5 :
            score >= StageFourThreshold ? 4 :
            score >= StageThreeThreshold ? 3 :
            score >= StageTwoThreshold ? 2 : 1;
        if (IsStageTest) nextStage = testingStage;
        if (nextStage != CurrentStage)
        {
            CurrentStage = nextStage;
            if (CurrentStage == 5) StartStageFiveFloorSchedule();
            StageChanged?.Invoke(CurrentStage);
        }
        if (CurrentStage == 5 && score >= nextStageFiveFloorResetScore)
        {
            do
            {
                AdvanceStageFiveFloorSchedule();
            }
            while (score >= nextStageFiveFloorResetScore);
            FloorResetRequested?.Invoke();
        }
    }

    private void StartStageFiveFloorSchedule()
    {
        stageFiveFloorScoreIndex = 0;
        nextStageFiveFloorResetScore = StageFiveFloorScores[0];
        nextStageFiveResetGap = 250;
    }

    private void AdvanceStageFiveFloorSchedule()
    {
        stageFiveFloorScoreIndex++;
        if (stageFiveFloorScoreIndex < StageFiveFloorScores.Length)
        {
            nextStageFiveFloorResetScore = StageFiveFloorScores[stageFiveFloorScoreIndex];
            return;
        }

        nextStageFiveFloorResetScore += nextStageFiveResetGap;
        nextStageFiveResetGap = Math.Min(nextStageFiveResetGap + 50, (long)int.MaxValue);
    }

    public bool LoseLife()
    {
        if (IsGameOver)
            return false;

        Lives = Mathf.Max(0, Lives - 1);
        TotalLivesLost++;
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
        if (IsStageTest || !IsGameOver || scoreRecorded || gameOverScreen == null || !gameOverScreen.IsEnteringInitials)
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
