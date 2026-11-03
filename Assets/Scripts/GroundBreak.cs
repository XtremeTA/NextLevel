using UnityEngine;

public class GroundBreak : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private int brickHealth = 4;
    public int Health => brickHealth;

    [SerializeField] private Sprite[] alternativeSprites;
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Ball>() != null)
            TakeDamage(1);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || brickHealth <= 0 || (GameManager.Instance != null && GameManager.Instance.IsGameOver))
            return;

        brickHealth = Mathf.Max(0, brickHealth - amount);
        if (brickHealth == 0)
        {
            // Remove support immediately, including during the player's recovery lift.
            GetComponent<Collider2D>().enabled = false;
            GameAudio.Instance?.Play(SoundCue.BrickBreak);
            Destroy(gameObject);
            return;
        }

        if (alternativeSprites != null && alternativeSprites.Length > 0)
            spriteRenderer.sprite = alternativeSprites[Mathf.Min(4 - brickHealth, alternativeSprites.Length - 1)];
        GameAudio.Instance?.Play(SoundCue.TileCrack);
    }
}
