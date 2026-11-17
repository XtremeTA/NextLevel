using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteFrameAnimation))]
public sealed class LatcherEnemy : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float fallSpeed = 7f;
    [SerializeField, Min(0.1f)] private float latchDuration = 5f;
    [SerializeField] private LatcherSpike spikePrefab;

    private readonly RaycastHit2D[] floorHits = new RaycastHit2D[16];
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private SpriteRenderer visual;
    private Camera view;
    private Collider2D tile;
    private float feetOffset;
    private float tileInset;
    private float latchElapsed;
    private bool chargePlayed;

    private void Start()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        view = Camera.main;
        feetOffset = visual != null ? body.position.y - visual.bounds.min.y : hitbox.size.y * 0.5f;
        tileInset = visual != null && visual.sprite != null ? 8f / visual.sprite.pixelsPerUnit : 0.25f;
        hitbox.enabled = false; // The falling enemy is harmless until it lands.
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            Destroy(gameObject);
            return;
        }

        if (tile != null)
        {
            if (!tile.enabled || !tile.gameObject.activeInHierarchy)
            {
                tile = null;
                hitbox.enabled = false;
                latchElapsed = 0f;
                chargePlayed = false;
            }
            else
            {
                latchElapsed += Time.fixedDeltaTime;
                if (!chargePlayed && latchElapsed >= Mathf.Max(0f, latchDuration - 0.7f))
                {
                    chargePlayed = true;
                    GameAudio.Instance?.Play(SoundCue.LatcherCharge);
                }
                if (latchElapsed >= latchDuration)
                {
                    if (spikePrefab != null)
                    {
                        Instantiate(spikePrefab, new Vector3(body.position.x, tile.bounds.max.y + 0.3f, transform.position.z), Quaternion.identity);
                        GameAudio.Instance?.Play(SoundCue.LatcherSpike);
                    }
                    Destroy(gameObject);
                }
                return;
            }
        }

        float step = fallSpeed * Time.fixedDeltaTime;
        if (EnemyGroundProbe.FindFloor(body.position, feetOffset + step, floorHits, out RaycastHit2D floor))
        {
            tile = floor.collider;
            // Sink eight sprite pixels into the top of the tile.
            body.position = new Vector2(body.position.x, floor.point.y + feetOffset - tileInset);
            latchElapsed = 0f;
            chargePlayed = false;
            hitbox.enabled = true;
            GameAudio.Instance?.Play(SoundCue.LatcherAttach);
        }
        else
        {
            body.MovePosition(body.position + Vector2.down * step);
            if (EnemyGroundProbe.IsBelowScreen(body.position, view)) Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    private void OnTriggerStay2D(Collider2D other) => TryHit(other);

    private void TryHit(Collider2D other)
    {
        if (tile == null || other.isTrigger) return;
        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;
        Vector2 hitSource = body.position;
        // An exact center hit pushes toward the arena center rather than an arbitrary edge.
        if (Mathf.Abs(player.transform.position.x - hitSource.x) < 0.05f)
            hitSource.x += hitSource.x >= 0f ? 0.1f : -0.1f;
        // The player's stun and invulnerability prevent immediate repeat damage.
        player.TakeEnemyHit(hitSource, 1.5f);
    }
}
