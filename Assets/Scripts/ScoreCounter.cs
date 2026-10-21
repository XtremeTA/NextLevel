using UnityEngine;
using UnityEngine.UI;

public class ScoreCounter : MonoBehaviour
{
    public static ScoreCounter Instance;

    private int score = 0;

    [SerializeField] private Sprite[] digitSprites; // 0-9
    [SerializeField] private Image hundredsImage;
    [SerializeField] private Image tensImage;
    [SerializeField] private Image onesImage;


    void Awake()
    {
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateScore(score);
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScore(score);
    }


    public void UpdateScore(int score)
    {
        score = Mathf.Clamp(score, 0, 999);

        int hundreds = score / 100;
        int tens = (score / 10) % 10;
        int ones = score % 10;

        hundredsImage.sprite = digitSprites[hundreds];
        tensImage.sprite = digitSprites[tens];
        onesImage.sprite = digitSprites[ones];
    }

}
