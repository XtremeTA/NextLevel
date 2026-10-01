using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
public class Ball : MonoBehaviour
{
    private Rigidbody2D rb;

    [SerializeField] private float moveSpeed = 6f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Launch();
    }

    void Launch()
    {
        // Random initial direction
        rb.linearVelocity = new Vector2(1f, 0.65f).normalized * moveSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Keep a steady speed because the default 2D material is not bouncy.
        Vector2 normal = collision.GetContact(0).normal;
        rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, normal).normalized * moveSpeed;
    }

    private void FixedUpdate()
    {
        // Keep the ball inside the visible play area.
        Vector2 position = rb.position;
        Vector2 velocity = rb.linearVelocity;

        if (Mathf.Abs(position.x) > 8.5f)
        {
            position.x = Mathf.Sign(position.x) * 8.5f;
            velocity.x = -velocity.x;
        }

        if (position.y > 4.5f)
        {
            position.y = 4.5f;
            velocity.y = -velocity.y;
        }

        rb.position = position;
        rb.linearVelocity = velocity.normalized * moveSpeed;
    }

    public void ResetBall()
    {
        transform.position = new Vector2(0f, -1f);
        rb.linearVelocity = Vector2.zero;
        Launch();
    }
}
