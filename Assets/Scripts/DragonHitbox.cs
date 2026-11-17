using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DragonHitbox : MonoBehaviour
{
    [SerializeField]
    private float riseSpeed = 18f;

    private Camera view;
    private Rigidbody2D rb;

    private bool hitPlayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        view = Camera.main;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity =
            Vector2.up * riseSpeed;

        if (view == null)
            return;

        float top =
            view.transform.position.y +
            view.orthographicSize;

        SpriteRenderer sr =
            GetComponent<SpriteRenderer>();

        if (sr != null &&
            sr.bounds.min.y > top)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Brick brick =
            other.GetComponent<Brick>();

        if (brick != null)
        {
            brick.Break();
            return;
        }

        if (hitPlayer)
            return;

        Player player =
            other.GetComponentInParent<Player>();

        if (player != null)
        {
            hitPlayer =
                player.TakeEnemyHit(
                    transform.position);
        }
    }
}
