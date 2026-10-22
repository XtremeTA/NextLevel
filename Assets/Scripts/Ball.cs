using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    private Rigidbody2D rb;
    private CircleCollider2D ballCollider;
    private SpriteRenderer spriteRenderer;
    private Player player;
    private BallVisuals visuals;
    private float boostRemaining;
    private float lastShotFacing = 1f;

    private struct BounceSample
    {
        public Vector2 position;
        public Vector2 direction;
    }

    private readonly BounceSample[] bounceHistory = new BounceSample[8];
    private int bounceHistoryCount;
    private int bounceHistoryIndex;
    private int bouncesWithoutProgress;
    private int loopEscapeCount;
    private float lastBounceStep = float.NegativeInfinity;
    private Vector2 lastBouncePosition;

    [Tooltip("Normal ball speed at the start of a run.")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 7f;
    [SerializeField, Min(0.1f)] private float maximumMoveSpeed = 13f;
    [SerializeField] private Camera playAreaCamera;
    [SerializeField, Min(0f)] private float screenPadding = 0.02f;
    [Header("Player Shots")]
    [Tooltip("Angle away from vertical when parrying while standing still.")]
    [SerializeField, Range(10f, 75f)] private float playerShotAngle = 25f;
    [SerializeField, Range(10f, 75f)] private float movingShotAngle = 50f;
    [SerializeField, Range(1f, 2f)] private float perfectParrySpeedMultiplier = 1.35f;
    [SerializeField, Min(0.01f)] private float perfectParryBoostDuration = 0.85f;

    [Header("Prevent Repeating Paths")]
    [SerializeField, Range(5f, 30f)] private float minimumBounceAngle = 18f;
    [SerializeField, Min(3)] private int repeatingBounceLimit = 4;
    [SerializeField, Min(0.01f)] private float repeatingPathTolerance = 0.35f;
    [SerializeField, Range(1f, 25f)] private float escapeAngleNudge = 12f;

    private float BoostStrength => Mathf.Clamp01(boostRemaining / Mathf.Min(0.25f, Mathf.Max(0.01f, perfectParryBoostDuration)));
    private float BaseSpeed => Mathf.Lerp(moveSpeed, Mathf.Max(moveSpeed, maximumMoveSpeed),
        GameManager.Instance != null ? GameManager.Instance.SpeedRampProgress : 0f);
    private float CurrentSpeed => BaseSpeed * Mathf.Lerp(1f, perfectParrySpeedMultiplier, BoostStrength);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        visuals = GetComponent<BallVisuals>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Start()
    {
        Launch();
        KeepInsideScreen();
    }

    private void Launch()
    {
        if (player == null)
            player = FindAnyObjectByType<Player>();
        boostRemaining = 0f;
        SendUpward(player);
    }

    private void SendUpward(Player shooter)
    {
        float facingDirection = shooter != null ? shooter.FacingDirection : 1f;
        lastShotFacing = facingDirection;
        ResetBounceHistory();
        loopEscapeCount = 0;
        float movement = shooter != null ? shooter.MovementAmount : 0f;
        float angle = Mathf.Clamp(Mathf.Lerp(playerShotAngle, movingShotAngle, movement), 10f, 75f) * Mathf.Deg2Rad;
        rb.linearVelocity = new Vector2(facingDirection * Mathf.Sin(angle), Mathf.Cos(angle)) * CurrentSpeed;
        visuals?.SetPower(BoostStrength);
    }

    private void FixedUpdate()
    {
        boostRemaining = Mathf.Max(0f, boostRemaining - Time.fixedDeltaTime);
        if (rb.linearVelocity.sqrMagnitude < 0.0001f)
            Launch();
        else
            rb.linearVelocity = rb.linearVelocity.normalized * CurrentSpeed;

        visuals?.SetPower(BoostStrength);
        KeepInsideScreen();
    }

    private void LateUpdate()
    {
        // Keep the rendered frame in view, but leave all bounces to FixedUpdate.
        KeepInsideScreen(false);
    }

    private void KeepInsideScreen(bool reflectVelocity = true)
    {
        if (playAreaCamera == null)
            playAreaCamera = Camera.main;
        if (playAreaCamera == null || !playAreaCamera.orthographic)
            return;

        GetBallFootprint(out Vector2 footprintMin, out Vector2 footprintMax);
        Vector2 position = rb.position;
        Vector2 velocity = rb.linearVelocity;
        Vector2 cameraCenter = playAreaCamera.transform.position;
        float halfHeight = playAreaCamera.orthographicSize;
        float halfWidth = halfHeight * playAreaCamera.aspect;

        float minX = cameraCenter.x - halfWidth + screenPadding - footprintMin.x;
        float maxX = cameraCenter.x + halfWidth - screenPadding - footprintMax.x;
        float minY = cameraCenter.y - halfHeight + screenPadding - footprintMin.y;
        float maxY = cameraCenter.y + halfHeight - screenPadding - footprintMax.y;

        bool bouncedX = ConstrainAxis(ref position.x, ref velocity.x, minX, maxX, reflectVelocity);
        bool bouncedY = ConstrainAxis(ref position.y, ref velocity.y, minY, maxY, reflectVelocity);
        if (position != rb.position)
            rb.position = position;

        if (bouncedX || bouncedY)
        {
            rb.linearVelocity = velocity;
            Vector2 normal = new Vector2(
                position.x <= minX ? 1f : position.x >= maxX ? -1f : 0f,
                position.y <= minY ? 1f : position.y >= maxY ? -1f : 0f);
            CorrectBounce(normal.normalized);
            if (bouncedY)
                GameAudio.Instance?.Play(SoundCue.BallHit);
        }
    }

    private Vector2 ConstrainBounceAngle(Vector2 direction, Vector2 normal)
    {
        if (Vector2.Dot(direction, normal) < 0f)
            direction = Vector2.Reflect(direction, normal);

        float signX = Mathf.Abs(direction.x) > 0.00001f ? Mathf.Sign(direction.x)
            : Mathf.Abs(normal.x) > 0.00001f ? Mathf.Sign(normal.x) : lastShotFacing;
        float signY = Mathf.Abs(direction.y) > 0.00001f ? Mathf.Sign(direction.y)
            : Mathf.Abs(normal.y) > 0.00001f ? Mathf.Sign(normal.y) : 1f;
        float minimum = Mathf.Sin(Mathf.Clamp(minimumBounceAngle, 5f, 30f) * Mathf.Deg2Rad);
        float x = Mathf.Abs(direction.x);
        float y = Mathf.Abs(direction.y);
        if (x < minimum)
        {
            x = minimum;
            y = Mathf.Sqrt(1f - minimum * minimum);
        }
        else if (y < minimum)
        {
            y = minimum;
            x = Mathf.Sqrt(1f - minimum * minimum);
        }

        Vector2 result = new Vector2(signX * x, signY * y).normalized;
        return Vector2.Dot(result, normal) < 0f ? Vector2.Reflect(result, normal) : result;
    }

    private void CorrectBounce(Vector2 normal, bool trackLoop = true)
    {
        if (rb.linearVelocity.sqrMagnitude < 0.0001f)
            return;

        Vector2 direction = ConstrainBounceAngle(rb.linearVelocity.normalized, normal);
        Vector2 position = rb.position;
        // A corner or coincident screen/physical wall is one bounce, not several loop detections.
        bool duplicate = lastBounceStep == Time.fixedTime && (position - lastBouncePosition).sqrMagnitude < 0.01f;
        if (trackLoop && !duplicate)
        {
            bouncesWithoutProgress++;
            float toleranceSquared = repeatingPathTolerance * repeatingPathTolerance;
            bool repeated = false;
            if (bouncesWithoutProgress >= Mathf.Max(3, repeatingBounceLimit))
            {
                for (int i = 0; i < bounceHistoryCount; i++)
                {
                    if ((bounceHistory[i].position - position).sqrMagnitude <= toleranceSquared &&
                        Vector2.Dot(bounceHistory[i].direction, direction) > 0.995f)
                    {
                        repeated = true;
                        break;
                    }
                }
            }
            if (repeated)
            {
                direction = NudgeBounce(direction, normal);
                loopEscapeCount++;
                ResetBounceHistory();
            }
            bounceHistory[bounceHistoryIndex] = new BounceSample { position = position, direction = direction };
            bounceHistoryIndex = (bounceHistoryIndex + 1) % bounceHistory.Length;
            bounceHistoryCount = Mathf.Min(bounceHistoryCount + 1, bounceHistory.Length);
        }
        lastBounceStep = Time.fixedTime;
        lastBouncePosition = position;
        rb.linearVelocity = direction * CurrentSpeed;
    }

    private Vector2 NudgeBounce(Vector2 direction, Vector2 normal)
    {
        float angle = Mathf.Atan2(Mathf.Abs(direction.y), Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
        float minimum = Mathf.Clamp(minimumBounceAngle, 5f, 30f);
        float nudge = Mathf.Clamp(escapeAngleNudge, 1f, 25f) * (angle <= 45f ? 1f : -1f);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            float target = Mathf.Clamp(angle + nudge, minimum, 90f - minimum) * Mathf.Deg2Rad;
            Vector2 candidate = new Vector2(Mathf.Sign(direction.x) * Mathf.Cos(target), Mathf.Sign(direction.y) * Mathf.Sin(target));
            if (Vector2.Dot(candidate, normal) > 0.01f)
                return candidate;
            nudge = -nudge;
        }
        return direction;
    }

    private void ResetBounceHistory()
    {
        bounceHistoryCount = 0;
        bounceHistoryIndex = 0;
        bouncesWithoutProgress = 0;
        lastBounceStep = float.NegativeInfinity;
    }

    private void GetBallFootprint(out Vector2 min, out Vector2 max)
    {
        // Measure shape offsets without world bounds, which can lag a Rigidbody
        // correction and otherwise make the apparent screen edge shift each frame.
        Vector3 scale = transform.lossyScale;
        float radius = ballCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        Vector2 colliderOffset = transform.TransformVector(ballCollider.offset);
        min = colliderOffset - Vector2.one * radius;
        max = colliderOffset + Vector2.one * radius;

        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Bounds spriteBounds = spriteRenderer.localBounds;
        Vector2 spriteOffset = transform.TransformVector(spriteBounds.center);
        Vector2 horizontalExtent = transform.TransformVector(new Vector3(spriteBounds.extents.x, 0f, 0f));
        Vector2 verticalExtent = transform.TransformVector(new Vector3(0f, spriteBounds.extents.y, 0f));
        Vector2 spriteExtent = new Vector2(
            Mathf.Abs(horizontalExtent.x) + Mathf.Abs(verticalExtent.x),
            Mathf.Abs(horizontalExtent.y) + Mathf.Abs(verticalExtent.y));
        min = Vector2.Min(min, spriteOffset - spriteExtent);
        max = Vector2.Max(max, spriteOffset + spriteExtent);
    }

    private static bool ConstrainAxis(ref float position, ref float velocity, float min, float max, bool reflectVelocity)
    {
        // Keep the ball centered if a very narrow view cannot fit its full size.
        if (min > max)
        {
            position = (min + max) * 0.5f;
            return false;
        }

        bool bounced = reflectVelocity &&
            ((position <= min && velocity < 0f) || (position >= max && velocity > 0f));
        position = Mathf.Clamp(position, min, max);
        if (bounced)
            velocity = -velocity;
        return bounced;
    }

    public void ResetBall()
    {
        rb.position = new Vector2(0f, -1f);
        Launch();
        KeepInsideScreen();
        GetComponent<BallVisuals>()?.ClearTrail();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Player hitPlayer = collision.gameObject.GetComponentInParent<Player>();
        if (hitPlayer != null)
        {
            boostRemaining = 0f;
            SendUpward(hitPlayer);
            return;
        }

        bool productiveHit = collision.gameObject.GetComponent<Brick>() != null ||
            collision.gameObject.GetComponent<GroundBreak>() != null;
        if (productiveHit)
        {
            ResetBounceHistory();
            loopEscapeCount = 0;
        }
        if (collision.contactCount > 0)
            CorrectBounce(collision.GetContact(0).normal, !productiveHit);

        // Bricks and floor blocks own their more specific impact sounds.
        if (!productiveHit)
        {
            // Physical side walls stay silent, just like the screen's side edges.
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector2 normal = collision.GetContact(i).normal;
                if (Mathf.Abs(normal.y) > Mathf.Abs(normal.x))
                {
                    GameAudio.Instance?.Play(SoundCue.BallHit);
                    break;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Parry"))
        {
            Player parryingPlayer = collision.GetComponentInParent<Player>();
            if (parryingPlayer == null)
                return;

            bool perfect = parryingPlayer.IsPerfectParry;
            boostRemaining = perfect ? perfectParryBoostDuration : 0f;
            SendUpward(parryingPlayer);
            visuals?.ShowParryImpact(perfect);
            if (playAreaCamera == null)
                playAreaCamera = Camera.main;
            playAreaCamera?.GetComponent<CameraFeedback>()?.Pulse(perfect);
            GameAudio.Instance?.Play(SoundCue.Parry);
        }
    }
}
