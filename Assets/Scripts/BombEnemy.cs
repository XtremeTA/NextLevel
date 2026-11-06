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
    [Header("Audio")]
    [SerializeField] private AudioClip tickingClip;
    [SerializeField, Range(0f, 1f)] private float tickingVolume = 0.38f;
    [SerializeField, Min(0.1f)] private float tickingStartPitch = 0.9f;
    [SerializeField, Min(0.1f)] private float tickingEndPitch = 1.7f;
    [SerializeField, Min(0.1f)] private float fallSpeed = 2f;
    [SerializeField] private float launchSpeed = 8f;
    [SerializeField, Min(0.1f)] private float fuseDuration = 3f;
    [SerializeField, Min(1f)] private float maximumLifetime = 30f;
    [Header("Fuse Warning")]
    [SerializeField] private Color warningColor = new Color(0.52f, 1f, 0.48f, 0.34f);
    [SerializeField, Min(0.1f)] private float startingFlashRate = 1.5f;
    [SerializeField, Min(0.1f)] private float finalFlashRate = 7f;

    private enum BombState { Falling, Launched, Armed, Resolved }
    private BombState state;
    private readonly RaycastHit2D[] floorHits = new RaycastHit2D[16];
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private SpriteFrameAnimation frameAnimation;
    private AudioSource tickingSource;
    private SpriteRenderer bombRenderer;
    private SpriteRenderer warningCircle;
    private readonly Collider2D[] warningTiles = new Collider2D[3];
    private Color originalColor;
    private float flashPhase;
    private static Sprite circleSprite;
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
        bombRenderer = GetComponent<SpriteRenderer>();
        originalColor = bombRenderer.color;
        CreateWarning();
        GameObject tickingAudio = new GameObject("Bomb Ticking");
        tickingAudio.transform.SetParent(transform, false);
        tickingSource = tickingAudio.AddComponent<AudioSource>();
        tickingSource.clip = tickingClip;
        tickingSource.playOnAwake = false;
        tickingSource.loop = true;
        tickingSource.spatialBlend = 0f;
        tickingSource.dopplerLevel = 0f;
        tickingSource.volume = tickingVolume;
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
            HideWarnings();
            StopTicking();
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
                flashPhase += Mathf.PI * 2f * Mathf.Lerp(startingFlashRate, finalFlashRate,
                    Mathf.Clamp01(fuseElapsed / Mathf.Max(0.1f, fuseDuration))) * Time.fixedDeltaTime;
                if (tickingSource != null)
                    tickingSource.pitch = Mathf.Lerp(tickingStartPitch, tickingEndPitch,
                        Mathf.Clamp01(fuseElapsed / Mathf.Max(0.1f, fuseDuration)));
                if (fuseElapsed >= fuseDuration)
                    Explode();
                return;
            }

            // A broken platform drops the bomb again; landing starts a fresh fuse.
            state = BombState.Falling;
            HideWarnings();
            StopTicking();
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
            flashPhase = 0f;
            SetWarningTiles();
            StartTicking();
            frameAnimation.Play(groundedFrames, false, groundedFrames.Length / Mathf.Max(0.1f, fuseDuration));
        }
        else
            body.MovePosition(body.position + Vector2.down * step);
    }

    private void LateUpdate()
    {
        if (state != BombState.Armed || GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        float progress = Mathf.Clamp01(fuseElapsed / Mathf.Max(0.1f, fuseDuration));
        float flash = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(flashPhase)), 4f);
        Color color = originalColor;
        color.a *= 1f - Mathf.Lerp(0.25f, 0.65f, progress) * flash;
        bombRenderer.color = color;

        float radius = 0.65f;
        for (int i = 0; i < warningTiles.Length; i++)
        {
            Collider2D tile = warningTiles[i];
            bool valid = tile != null && tile.enabled && tile.gameObject.activeInHierarchy;
            if (!valid)
                continue;

            Bounds bounds = tile.bounds;
            float reachX = Mathf.Max(Mathf.Abs(bounds.min.x - body.position.x),
                Mathf.Abs(bounds.max.x - body.position.x));
            float reachY = Mathf.Max(Mathf.Abs(bounds.min.y - body.position.y),
                Mathf.Abs(bounds.max.y - body.position.y));
            radius = Mathf.Max(radius, new Vector2(reachX, reachY).magnitude);
        }

        // At detonation, the circle contains the surviving tiles the blast will hit.
        warningCircle.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, radius * 2f, progress);
        warningCircle.enabled = true;
    }

    private void CreateWarning()
    {
        if (circleSprite == null)
            circleSprite = EffectSprites.CreateFilledCircle();

        GameObject circle = new GameObject("Blast Area");
        circle.transform.SetParent(transform, false);
        warningCircle = circle.AddComponent<SpriteRenderer>();
        warningCircle.sprite = circleSprite;
        warningCircle.sharedMaterial = bombRenderer.sharedMaterial;
        warningCircle.color = warningColor;
        warningCircle.sortingLayerID = bombRenderer.sortingLayerID;
        warningCircle.sortingOrder = bombRenderer.sortingOrder - 1;
        warningCircle.enabled = false;
    }

    private void SetWarningTiles()
    {
        GroundBreak left = Neighbor(-1);
        GroundBreak right = Neighbor(1);
        warningTiles[0] = supportingFloor;
        warningTiles[1] = left != null ? left.GetComponent<Collider2D>() : null;
        warningTiles[2] = right != null ? right.GetComponent<Collider2D>() : null;
    }

    private void HideWarnings()
    {
        if (bombRenderer != null)
            bombRenderer.color = originalColor;
        if (warningCircle != null)
            warningCircle.enabled = false;
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
                HideWarnings();
                StopTicking();
                explosionHitbox.SetActive(true);
                GameAudio.Instance?.Play(SoundCue.BombExplosion);
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
        HideWarnings();
        StopTicking();

        supportingFloor = null;
        supportingTile = null;

        body.linearVelocity = Vector2.up * launchSpeed;

        GameAudio.Instance?.Play(SoundCue.Parry);

        return true;
    }

    private void Explode()
    {
        state = BombState.Resolved;
        HideWarnings();
        StopTicking();
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

        GameAudio.Instance?.Play(SoundCue.BombExplosion);
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

    private void StartTicking()
    {
        if (tickingSource == null || tickingClip == null)
            return;

        tickingSource.pitch = tickingStartPitch;
        tickingSource.volume = tickingVolume;
        tickingSource.Play();
    }

    private void StopTicking()
    {
        if (tickingSource != null && tickingSource.isPlaying)
            tickingSource.Stop();
    }

    private void OnDisable()
    {
        HideWarnings();
        StopTicking();
    }
}
