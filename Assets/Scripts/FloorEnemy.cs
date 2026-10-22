using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class FloorEnemy : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 8f;
    [SerializeField, Min(0.1f)] private float riseSpeed = 10f;
    [SerializeField, Min(1f)] private float maximumLifetime = 12f;

    private readonly RaycastHit2D[] floorHits = new RaycastHit2D[16];
    private readonly RaycastHit2D[] movementHits = new RaycastHit2D[16];
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private Camera view;
    private float feetOffset;
    private float verticalVelocity;
    private float age;
    private int direction = 1;
    private bool climbing;
    private bool consumed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>();
        view = Camera.main;
        feetOffset = body.position.y - hitbox.bounds.min.y;
    }

    public void Launch(int travelDirection, float floorY)
    {
        direction = travelDirection < 0 ? -1 : 1;
        GetComponent<SpriteRenderer>().flipX = direction < 0;
        feetOffset = body.position.y - hitbox.bounds.min.y;
        body.position = new Vector2(body.position.x, floorY + feetOffset + 0.01f);
        transform.position = new Vector3(body.position.x, body.position.y, transform.position.z);
    }

    private void FixedUpdate()
    {
        age += Time.fixedDeltaTime;
        bool aboveScreen = climbing && view != null && view.orthographic &&
            body.position.y > view.transform.position.y + view.orthographicSize + 3f;
        if (age >= maximumLifetime || EnemyGroundProbe.IsBelowScreen(body.position, view) || aboveScreen)
        {
            Destroy(gameObject);
            return;
        }

        if (climbing)
        {
            Rise();
            return;
        }

        float horizontalStep = moveSpeed * Time.fixedDeltaTime;
        if (FindWall(horizontalStep + 0.01f, out RaycastHit2D wall))
        {
            climbing = true;
            verticalVelocity = 0f;
            Vector2 corner = body.position + Vector2.right * (direction * Mathf.Max(0f, wall.distance - 0.01f));
            body.MovePosition(corner);
            // The left-facing sprite needs the opposite rotation to face upward.
            body.MoveRotation(direction > 0 ? 90f : -90f);
            return;
        }

        verticalVelocity += Physics2D.gravity.y * Time.fixedDeltaTime;
        Vector2 next = body.position + new Vector2(direction * moveSpeed, verticalVelocity) * Time.fixedDeltaTime;
        Vector2 probe = new Vector2(next.x, body.position.y);
        if (verticalVelocity <= 0f && EnemyGroundProbe.FindFloor(probe, feetOffset + body.position.y - next.y + 0.02f, floorHits, out RaycastHit2D floor))
        {
            next.y = floor.point.y + feetOffset + 0.01f;
            verticalVelocity = 0f;
        }
        body.MovePosition(next);
    }

    private bool FindWall(float distance, out RaycastHit2D wall)
    {
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        int count = hitbox.Cast(Vector2.right * direction, filter, movementHits, distance);
        wall = default;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = movementHits[i];
            if (hit.normal.x * direction >= -0.5f || hit.distance >= nearest ||
                hit.collider.GetComponent<GroundBreak>() != null || hit.collider.GetComponent<Brick>() != null ||
                hit.collider.GetComponent<Player>() != null || hit.collider.GetComponent<Ball>() != null)
                continue;

            wall = hit;
            nearest = hit.distance;
        }
        return wall.collider != null;
    }

    private void Rise()
    {
        float step = riseSpeed * Time.fixedDeltaTime;
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        int count = hitbox.Cast(Vector2.up, filter, movementHits, step + 0.01f);
        Brick target = null;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Brick brick = movementHits[i].collider.GetComponent<Brick>();
            if (brick != null && !brick.IsBreaking && movementHits[i].distance < nearest)
            {
                target = brick;
                nearest = movementHits[i].distance;
            }
        }

        if (target != null)
            BreakBrick(target);
        else
            body.MovePosition(body.position + Vector2.up * step);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        BreakBrick(other.GetComponent<Brick>());
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        BreakBrick(other.GetComponent<Brick>());
    }

    private void BreakBrick(Brick brick)
    {
        if (!climbing || consumed || !isActiveAndEnabled || brick == null || brick.IsBreaking)
            return;

        consumed = true;
        brick.Break();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
