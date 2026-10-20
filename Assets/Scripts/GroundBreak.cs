using UnityEngine;

public class GroundBreak : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private int brickHealth = 4;

    [SerializeField] private Sprite[] alternativeSprites;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (brickHealth > 0 && collision.gameObject.GetComponent<Ball>() != null){
            brickHealth -= 1;
            if (brickHealth == 0)
            {
                GameAudio.Instance?.Play(SoundCue.BrickBreak);
                Destroy(gameObject);
            } else {
                spriteRenderer.sprite = alternativeSprites[4 - brickHealth];
                GameAudio.Instance?.Play(SoundCue.TileCrack);
            }
        }
    }
}
