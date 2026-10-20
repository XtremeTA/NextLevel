using UnityEngine;
using TMPro;

public class ScoreCounter : MonoBehaviour
{
    public static ScoreCounter Instance;

    public TMP_Text scoreText;
    private int score = 0;

    void Awake()
    {
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateScoreText();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;
    }

}
