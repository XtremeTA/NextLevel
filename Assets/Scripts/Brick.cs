using UnityEngine;
using System.Collections;

public class Brick : MonoBehaviour
{
    [SerializeField, Min(0)] private int pointValue = 1;
    [SerializeField] private FallingEnemy enemyPrefab;
    [SerializeField] private LifeHeartPickup lifeHeartPrefab;
    [SerializeField] private Sprite[] breakFrames;
    private Animator animator;
    private bool isBreaking;
    public bool IsBreaking => isBreaking;
    public int PointValue => Mathf.Max(0, pointValue);
    public bool DropsLifeHeart => lifeHeartPrefab != null;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Ball>() != null)
            Break();
    }

    public void Break()
    {
        if (isBreaking)
            return;

        isBreaking = true;
        GameAudio.Instance?.Play(SoundCue.BrickBreak);
        ScoreCounter.Instance?.AddScore(PointValue);
        if (animator != null)
            animator.SetTrigger("Break");
        StartCoroutine(DestroyAfterAnimation());
    }

    private IEnumerator DestroyAfterAnimation()
    {
        float duration = 0.3f;
        SpriteFrameAnimation frameAnimation = GetComponent<SpriteFrameAnimation>();
        if (frameAnimation != null && breakFrames != null && breakFrames.Length > 0)
        {
            frameAnimation.Play(breakFrames, false, 12f);
            duration = frameAnimation.Duration;
        }

        yield return new WaitForSeconds(duration);
        if (enemyPrefab != null)
            Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        if (lifeHeartPrefab != null)
            Instantiate(lifeHeartPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
