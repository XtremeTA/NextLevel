using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteFrameAnimation))]
public sealed class FallingEnemy : MonoBehaviour
{
    [SerializeField] private FloorEnemy smallEnemyPrefab;
    [SerializeField] private Sprite[] explosionFrames;
    [SerializeField, Min(0.1f)] private float fallSpeed = 9f;
    [SerializeField, Min(1f)] private float explosionFrameRate = 12f;
    [SerializeField, Range(0f, 1f)] private float splitAnimationProgress = 0.4f;
    [SerializeField, Min(0f)] private float splitOffset = 0.45f;
    [SerializeField, Min(1f)] private float maximumLifetime = 20f;

    private readonly RaycastHit2D[] floorHits = new RaycastHit2D[16];
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private Camera view;
    private float feetOffset;
    private float age;
    private bool exploding;

    private void Start()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>();
        view = Camera.main;
        feetOffset = body.position.y - hitbox.bounds.min.y;
        GameAudio.Instance?.Play(SoundCue.GhostAppear);
    }

    private void FixedUpdate()
    {
        age += Time.fixedDeltaTime;
        if (age >= maximumLifetime || EnemyGroundProbe.IsBelowScreen(body.position, view))
        {
            Destroy(gameObject);
            return;
        }
        if (exploding)
            return;

        float step = fallSpeed * Time.fixedDeltaTime;
        if (EnemyGroundProbe.FindFloor(body.position, feetOffset + step, floorHits, out RaycastHit2D floor))
        {
            body.position = new Vector2(body.position.x, floor.point.y + feetOffset);
            transform.position = new Vector3(body.position.x, body.position.y, transform.position.z);
            StartCoroutine(Explode(floor.point.y));
        }
        else
        {
            body.MovePosition(body.position + Vector2.down * step);
        }
    }

    private IEnumerator Explode(float floorY)
    {
        exploding = true;
        hitbox.enabled = false;
        GameAudio.Instance?.Play(SoundCue.GhostBurst);
        SpriteFrameAnimation animation = GetComponent<SpriteFrameAnimation>();
        animation.Play(explosionFrames, false, explosionFrameRate);
        float duration = animation.Duration;
        float splitDelay = duration * Mathf.Clamp01(splitAnimationProgress);
        if (splitDelay > 0f)
            yield return new WaitForSeconds(splitDelay);

        if (smallEnemyPrefab != null)
        {
            GameAudio.Instance?.Play(SoundCue.GhostSplit);
            for (int direction = -1; direction <= 1; direction += 2)
            {
                Vector3 position = transform.position + Vector3.right * (direction * splitOffset);
                FloorEnemy enemy = Instantiate(smallEnemyPrefab, position, Quaternion.identity);
                enemy.Launch(direction, floorY);
            }
        }
        float remainingDuration = duration - splitDelay;
        if (remainingDuration > 0f)
            yield return new WaitForSeconds(remainingDuration);
        Destroy(gameObject);
    }
}
