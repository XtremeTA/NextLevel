using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField, Min(1)] private int startingLives = 3;
    [Tooltip("One heart per life, left to right.")]
    [SerializeField] private Image[] heartImages = new Image[3];
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;

    [Header("Speed Progression")]
    [Tooltip("Active gameplay seconds to reach the ball and player maximum speeds.")]
    [SerializeField, Min(1f)] private float speedRampDuration = 180f;
    private float speedRampElapsed;

    public int Lives { get; private set; }
    public bool IsGameOver { get; private set; }
    public float SpeedRampProgress => Mathf.Clamp01(speedRampElapsed / Mathf.Max(1f, speedRampDuration));

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        speedRampElapsed = 0f;
        Lives = startingLives;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        UpdateHearts();
    }

    private void Update()
    {
        if (!IsGameOver && Time.timeScale > 0f)
            speedRampElapsed = Mathf.Min(speedRampElapsed + Time.deltaTime, Mathf.Max(1f, speedRampDuration));

        if (IsGameOver && (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return)))
            RestartGame();
    }

    public bool LoseLife()
    {
        if (IsGameOver)
            return false;

        Lives = Mathf.Max(0, Lives - 1);
        UpdateHearts();
        if (Lives == 0)
        {
            IsGameOver = true;
            StartCoroutine(ShowGameOver());
        }
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
        if (finalScoreText != null)
            finalScoreText.text = "Score: " + (ScoreCounter.Instance != null ? ScoreCounter.Instance.Score : 0);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
