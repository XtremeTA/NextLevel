using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField, Min(1)] private int startingLives = 3;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;

    public int Lives { get; private set; }
    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Lives = startingLives;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        UpdateLivesText();
    }

    private void Update()
    {
        if (IsGameOver && (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return)))
            RestartGame();
    }

    public bool LoseLife()
    {
        if (IsGameOver)
            return false;

        Lives = Mathf.Max(0, Lives - 1);
        UpdateLivesText();
        if (Lives == 0)
        {
            IsGameOver = true;
            StartCoroutine(ShowGameOver());
        }
        return true;
    }

    private void UpdateLivesText()
    {
        if (livesText != null)
            livesText.text = "Lives: " + Lives;
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
