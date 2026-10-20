using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    private Rigidbody2D rb;
    private CircleCollider2D ballCollider;
    private SpriteRenderer spriteRenderer;

    [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
    [SerializeField] private Camera playAreaCamera;
    [SerializeField, Min(0f)] private float screenPadding = 0.02f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        rb.linearVelocity = new Vector2(1f, 0.65f).normalized * moveSpeed;
    }

    private void FixedUpdate()
    {
        if (rb.linearVelocity.sqrMagnitude < 0.0001f)
            Launch();
        else
            rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;

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
            GameAudio.Instance?.Play(SoundCue.BallHit);
        }
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
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Bricks and floor blocks own their more specific impact sounds.
        if (collision.gameObject.GetComponent<Brick>() == null &&
            collision.gameObject.GetComponent<GroundBreak>() == null &&
            collision.gameObject.GetComponent<Player>() == null)
        {
            GameAudio.Instance?.Play(SoundCue.BallHit);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Parry"))
        {
            rb.linearVelocity = new Vector2(Random.Range(-1f, 1f), 1f).normalized * moveSpeed;
            GameAudio.Instance?.Play(SoundCue.Parry);
        }
    }
}
