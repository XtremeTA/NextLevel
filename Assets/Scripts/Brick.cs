using UnityEngine;
using System.Collections;

public class Brick : MonoBehaviour
{
    [SerializeField, Min(0)] private int pointValue = 1;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private LifeHeartPickup lifeHeartPrefab;
    [SerializeField] private Sprite[] breakFrames;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Sprite[] stageBreakFrames;
    private bool isBreaking;
    public bool IsBreaking => isBreaking;
    public int PointValue => Mathf.Max(0, pointValue);
    public bool DropsLifeHeart => lifeHeartPrefab != null;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetAppearance(Sprite idleSprite, Sprite[] breakSprites)
    {
        if (isBreaking)
            return;

        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;
        stageBreakFrames = breakSprites;
        // Stage-specific bricks use their supplied idle and break sprites instead
        // of the prefab's Stage 1 Animator, which would restore the old idle art.
        if (animator != null)
            animator.enabled = false;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Ball>() != null)
            Break();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Explosion"))
        {
            Break();
        }
    }

    public void Break()
    {
        if (isBreaking)
            return;

        isBreaking = true;
        GameAudio.Instance?.Play(SoundCue.BrickBreak);
        ScoreCounter.Instance?.AddScore(PointValue);
        if ((stageBreakFrames == null || stageBreakFrames.Length == 0) && animator != null)
            animator.SetTrigger("Break");
        StartCoroutine(DestroyAfterAnimation());
    }

    private IEnumerator DestroyAfterAnimation()
    {
        float duration = 0.3f;
        if (stageBreakFrames != null && stageBreakFrames.Length > 0 && spriteRenderer != null)
        {
            if (animator != null)
                animator.enabled = false;
            float frameDuration = 1f / 12f;
            foreach (Sprite frame in stageBreakFrames)
            {
                if (frame != null)
                    spriteRenderer.sprite = frame;
                yield return new WaitForSeconds(frameDuration);
            }
            duration = 0f;
        }
        else
        {
            SpriteFrameAnimation frameAnimation = GetComponent<SpriteFrameAnimation>();
            if (frameAnimation != null && breakFrames != null && breakFrames.Length > 0)
            {
                frameAnimation.Play(breakFrames, false, 12f);
                duration = frameAnimation.Duration;
            }
        }

        if (duration > 0f)
            yield return new WaitForSeconds(duration);
        if (enemyPrefab != null)
            Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        if (lifeHeartPrefab != null)
            Instantiate(lifeHeartPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
