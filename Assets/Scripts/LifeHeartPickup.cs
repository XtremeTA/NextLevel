using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class LifeHeartPickup : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float fallSpeed = 1.5f;
    [SerializeField, Min(0f)] private float swayDistance = 0.1f;
    [SerializeField, Min(0f)] private float swayFrequency = 0.65f;

    private readonly RaycastHit2D[] floorHits = new RaycastHit2D[16];
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private Collider2D supportingFloor;
    private Camera view;
    private float feetOffset;
    private float driftOriginX;
    private float fallElapsed;
    private bool landed;
    private bool collected;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        hitbox.isTrigger = true;
        view = Camera.main;
        feetOffset = body.position.y - hitbox.bounds.min.y;
        driftOriginX = body.position.x;
    }

    private void FixedUpdate()
    {
        if (collected || (GameManager.Instance != null && GameManager.Instance.IsGameOver))
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (EnemyGroundProbe.IsBelowScreen(body.position, view))
        {
            Destroy(gameObject);
            return;
        }

        if (landed)
        {
            if (supportingFloor != null && supportingFloor.enabled && supportingFloor.gameObject.activeInHierarchy)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            landed = false;
            supportingFloor = null;
            driftOriginX = body.position.x;
            fallElapsed = 0f;
        }

        fallElapsed += Time.fixedDeltaTime;
        float x = driftOriginX + Mathf.Sin(fallElapsed * swayFrequency * Mathf.PI * 2f) * swayDistance;
        float step = fallSpeed * Time.fixedDeltaTime;
        Vector2 next = new Vector2(x, body.position.y - step);
        if (EnemyGroundProbe.FindFloor(new Vector2(x, body.position.y), feetOffset + step, floorHits, out RaycastHit2D floor))
        {
            next.y = floor.point.y + feetOffset;
            supportingFloor = floor.collider;
            landed = true;
        }
        body.MovePosition(next);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider2D other)
    {
        if (collected || !isActiveAndEnabled || other.isTrigger)
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null || !player.isActiveAndEnabled || GameManager.Instance == null || !GameManager.Instance.GainLife())
            return;

        collected = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
