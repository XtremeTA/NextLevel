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
        if (collision.gameObject.GetComponent<Ball>() != null){
            if (brickHealth - 1 == 0)
            {
                Destroy(gameObject);
            } else {
                brickHealth -= 1;
                spriteRenderer.sprite = alternativeSprites[4 - brickHealth];
            }
        }
    }
}
