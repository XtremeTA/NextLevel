using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(50)]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteFrameAnimation))]
public sealed class BombEnemy : MonoBehaviour
{
    [SerializeField] private GameObject explosionHitbox;
    [SerializeField] private Sprite[] fallingFrames;
    [SerializeField] private Sprite[] groundedFrames;
    [SerializeField] private Sprite[] explosionFrames;
    [SerializeField, Min(0.1f)] private float fallSpeed = 2f;
    [SerializeField] private float launchSpeed = 8f;
    [SerializeField, Min(0.1f)] private float fuseDuration = 3f;
    [SerializeField, Min(1f)] private float maximumLifetime = 30f;

    private enum BombState { Falling, Launched, Armed, Resolved }
    private BombState state;
    private readonly RaycastHit2D[] floorHits = new RaycastHit2D[16];
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private SpriteFrameAnimation frameAnimation;
    private GroundBreak supportingTile;
    private Collider2D supportingFloor;
    private Camera view;
    private float feetOffset;
    private float age;
    private float fuseElapsed;

    public bool IsArmed => state == BombState.Armed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>();
        frameAnimation = GetComponent<SpriteFrameAnimation>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        hitbox.isTrigger = true;
        view = Camera.main;
        feetOffset = body.position.y - hitbox.bounds.min.y;
    }

    private void Start()
    {
        if (state == BombState.Falling)
            frameAnimation.Play(fallingFrames, true, 10f);
    }

    private void FixedUpdate()
    {
        if (state == BombState.Launched)
        {
            body.MovePosition(
            body.position + Vector2.up * launchSpeed * Time.fixedDeltaTime);

        return;
        }
        if (state == BombState.Resolved || (GameManager.Instance != null && GameManager.Instance.IsGameOver))
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        age += Time.fixedDeltaTime;
        if (age >= maximumLifetime || EnemyGroundProbe.IsBelowScreen(body.position, view))
        {
            Destroy(gameObject);
            return;
        }

        if (IsArmed)
        {
            if (supportingFloor != null && supportingFloor.enabled && supportingFloor.gameObject.activeInHierarchy)
            {
                body.linearVelocity = Vector2.zero;
                fuseElapsed += Time.fixedDeltaTime;
                if (fuseElapsed >= fuseDuration)
                    Explode();
                return;
            }

            // A broken platform drops the bomb again; landing starts a fresh fuse.
            state = BombState.Falling;
            supportingFloor = null;
            supportingTile = null;
            frameAnimation.Play(fallingFrames, true, 10f);
        }

        float step = fallSpeed * Time.fixedDeltaTime;
        if (EnemyGroundProbe.FindFloor(body.position, feetOffset + step, floorHits, out RaycastHit2D floor))
        {
            body.position = new Vector2(body.position.x, floor.point.y + feetOffset);
            body.linearVelocity = Vector2.zero;
            supportingFloor = floor.collider;
            supportingTile = floor.collider.GetComponent<GroundBreak>();
            state = BombState.Armed;
            fuseElapsed = 0f;
            frameAnimation.Play(groundedFrames, false, groundedFrames.Length / Mathf.Max(0.1f, fuseDuration));
        }
        else
            body.MovePosition(body.position + Vector2.down * step);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckDash(other);

        if (state == BombState.Launched)
        {
            Brick brick = other.GetComponent<Brick>();

            if (brick != null)
            {
                brick.Break();
                state = BombState.Resolved;
                explosionHitbox.SetActive(true);
                GameAudio.Instance?.Play(SoundCue.GhostBurst);
                StartCoroutine(PlayBurst(2f));
            }
        }
    }
    private void OnTriggerStay2D(Collider2D other) => CheckDash(other);

    private void CheckDash(Collider2D other)
    {
        if (!other.isTrigger)
            TryDashDefeat(other.GetComponentInParent<Player>());
    }

    public bool TryDashDefeat(Player player)
    {
        if (state == BombState.Resolved ||
            player == null ||
            !player.IsPerfectParry)
            return false;

        state = BombState.Launched;

        supportingFloor = null;
        supportingTile = null;

        body.linearVelocity = Vector2.up * launchSpeed;

        GameAudio.Instance?.Play(SoundCue.Parry);

        return true;
    }

    private void Explode()
    {
        state = BombState.Resolved;
        hitbox.enabled = false;
        GroundBreak left = Neighbor(-1);
        GroundBreak right = Neighbor(1);

        // Snapshot the player's footing before damage can destroy their supporting tile.
        Player player = FindAnyObjectByType<Player>();
        bool hitPlayer = player != null &&
            (player.IsStandingOn(supportingFloor) ||
             (left != null && player.IsStandingOn(left.GetComponent<Collider2D>())) ||
             (right != null && player.IsStandingOn(right.GetComponent<Collider2D>())));
        supportingTile?.TakeDamage(2);
        left?.TakeDamage(1);
        right?.TakeDamage(1);
        if (hitPlayer)
            player.TakeEnemyHit(body.position);

        GameAudio.Instance?.Play(SoundCue.GhostBurst);
        if (view != null) view.GetComponent<CameraFeedback>()?.Pulse(false);
        StartCoroutine(PlayBurst(2f));
    }

    private GroundBreak Neighbor(int direction)
    {
        if (supportingFloor == null)
            return null;
        Bounds center = supportingFloor.bounds;
        GroundBreak nearest = null;
        float closestEdge = float.PositiveInfinity;
        foreach (GroundBreak tile in FindObjectsByType<GroundBreak>())
        {
            Collider2D floor = tile.GetComponent<Collider2D>();
            if (tile == supportingTile || floor == null || !floor.enabled || !tile.gameObject.activeInHierarchy)
                continue;
            Bounds candidate = floor.bounds;
            if (Mathf.Abs(candidate.max.y - center.max.y) > 0.05f ||
                (candidate.center.x - center.center.x) * direction <= 0f)
                continue;
            float gap = direction < 0 ? center.min.x - candidate.max.x : candidate.min.x - center.max.x;
            // Only touching neighbors count; a missing tile must not extend the blast across a gap.
            if (gap >= -0.05f && gap <= 0.05f && Mathf.Abs(gap) < closestEdge)
            {
                closestEdge = Mathf.Abs(gap);
                nearest = tile;
            }
        }
        return nearest;
    }

    private IEnumerator PlayBurst(float scale)
    {
        transform.localScale *= scale;
        frameAnimation.Play(explosionFrames, false, 12f);
        yield return new WaitForSeconds(frameAnimation.Duration);
        Destroy(gameObject);
    }
}
