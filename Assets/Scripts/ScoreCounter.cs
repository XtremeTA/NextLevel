using UnityEngine;
using UnityEngine.UI;

public class ScoreCounter : MonoBehaviour
{
    public static ScoreCounter Instance;

    [Tooltip("Board digits from left (hundreds) to right (ones).")]
    [SerializeField] private Image[] digitImages = new Image[3];
    [Tooltip("Number sprites 0 through 9, in order.")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];
    [Tooltip("Seconds between each +1 step while the board counts up.")]
    [SerializeField, Min(0.01f)] private float tickInterval = 0.06f;
    [Tooltip("Longest one count-up may take; large jumps tick faster to fit.")]
    [SerializeField, Min(0.01f)] private float maxTickDuration = 0.75f;

    private int score = 0;
    private int displayedScore;
    private float nextTickTime;
    public int Score => score;

    private int MaxDisplayable => (int)Mathf.Pow(10, digitImages.Length) - 1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ShowScore(displayedScore);
    }

    public void AddScore(int amount)
    {
        score += amount;
    }

    private void Update()
    {
        int target = Mathf.Clamp(score, 0, MaxDisplayable);
        if (displayedScore > target)
        {
            displayedScore = target;
            ShowScore(displayedScore);
            return;
        }

        // Unscaled so the board still finishes counting after game over pauses time.
        float now = Time.unscaledTime;
        if (displayedScore == target || now < nextTickTime)
            return;

        nextTickTime = now + Mathf.Min(tickInterval, maxTickDuration / (target - displayedScore));
        displayedScore++;
        ShowScore(displayedScore);
    }

    private void ShowScore(int value)
    {
        for (int i = digitImages.Length - 1; i >= 0; i--)
        {
            if (digitImages[i] != null)
                digitImages[i].sprite = digitSprites[value % 10];
            value /= 10;
        }
    }
}
