using System.Collections.Generic;
using UnityEngine;

// The falling wisp reserves one column; only the returning dragon deals damage.
public sealed class DragonLaneEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float fallSpeed = 7f;
    [SerializeField, Min(0.1f)] private float warningDuration = 1.1f;
    [SerializeField, Min(0.1f)] private float riseSpeed = 18f;
    [SerializeField, Min(0.1f)] private float laneWidth = 1.4f;
    [SerializeField, Min(0.1f)] private float dragonLength = 2.4f;
    [Header("Art (optional until final sprites are ready)")]
    [SerializeField] private Sprite wispSprite;
    [SerializeField] private Sprite dragonSprite;
    [SerializeField] private Material visualMaterial;
    [SerializeField] private Color warningColor = new Color(0.65f, 1f, 0.48f, 0.5f);

    private enum Phase { Falling, Warning, Rising }
    private Phase phase;
    private Camera view;
    private SpriteRenderer visual;
    private SpriteRenderer beam;
    private float laneX;
    private float warningElapsed;
    private float age;
    private bool hitPlayer;
    private readonly List<Collider2D> hits = new List<Collider2D>(32);
    private readonly ContactFilter2D hitFilter = new ContactFilter2D { useTriggers = false };

    private float Bottom => view.transform.position.y - view.orthographicSize;
    private float Top => view.transform.position.y + view.orthographicSize;

    private void Start()
    {
        view = Camera.main;
        if (view == null || !view.orthographic)
        {
            Debug.LogError("Dragon lane enemy needs an orthographic gameplay camera.", this);
            Destroy(gameObject);
            return;
        }

        laneX = transform.position.x;
        visual = CreateRenderer("Wisp / Dragon", 4);
        visual.sprite = wispSprite != null ? wispSprite : DragonPrototypeArt.Wisp;
        FitVisual(0.45f, 0.7f);
        beam = CreateRenderer("Lane Warning", 3);
        beam.sprite = DragonPrototypeArt.Beam;
        beam.enabled = false;
        GameAudio.Instance?.Play(SoundCue.DragonWisp);
    }

    private SpriteRenderer CreateRenderer(string objectName, int order)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        if (visualMaterial != null)
            renderer.sharedMaterial = visualMaterial;
        renderer.sortingOrder = order;
        return renderer;
    }

    private void FixedUpdate()
    {
        if (view == null || visual == null)
            return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            Destroy(gameObject);
            return;
        }

        age += Time.fixedDeltaTime;
        if (age > 30f)
        {
            Destroy(gameObject);
            return;
        }

        if (phase == Phase.Falling)
        {
            transform.position += Vector3.down * fallSpeed * Time.fixedDeltaTime;
            // Wait until the entire wisp has passed through the floor and left the camera.
            if (visual.bounds.max.y < Bottom)
            {
                phase = Phase.Warning;
                visual.enabled = false;
                beam.enabled = true;
                warningElapsed = 0f;
                GameAudio.Instance?.Play(SoundCue.DragonWarning);
            }
        }
        else if (phase == Phase.Warning)
        {
            warningElapsed += Time.fixedDeltaTime;
            if (warningElapsed >= warningDuration)
            {
                phase = Phase.Rising;
                GameAudio.Instance?.Play(SoundCue.DragonRise);
                beam.enabled = false;
                visual.sprite = dragonSprite != null ? dragonSprite : DragonPrototypeArt.Dragon;
                visual.color = Color.white;
                FitVisual(laneWidth, dragonLength);
                transform.position = new Vector3(laneX, Bottom - dragonLength * 0.5f - 0.1f, transform.position.z);
                visual.enabled = true;
            }
        }
        else
        {
            float step = riseSpeed * Time.fixedDeltaTime;
            Vector2 sweepCenter = new Vector2(laneX, transform.position.y + step * 0.5f);
            // Sweep the whole step so the dragon cannot skip a player or brick at high speed.
            hits.Clear();
            Physics2D.OverlapBox(sweepCenter, new Vector2(laneWidth, dragonLength + step), 0f, hitFilter, hits);
            foreach (Collider2D hit in hits)
            {
                Brick brick = hit.GetComponent<Brick>();
                if (brick != null)
                    brick.Break();
                if (!hitPlayer)
                {
                    Player player = hit.GetComponentInParent<Player>();
                    if (player != null)
                        hitPlayer = player.TakeEnemyHit(transform.position);
                }
                // GroundBreak is deliberately never damaged by this attack.
            }
            transform.position += Vector3.up * step;
            if (visual.bounds.min.y > Top)
                Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        if (visual == null || view == null)
            return;
        if (phase == Phase.Falling && wispSprite == null)
            visual.color = new Color(0.65f, 1f, 0.48f, 0.85f + 0.15f * Mathf.Sin(age * 10f));
        if (phase != Phase.Warning)
            return;

        float height = Top - Bottom;
        beam.transform.position = new Vector3(laneX, (Bottom + Top) * 0.5f, transform.position.z);
        beam.transform.localScale = new Vector3(laneWidth / beam.sprite.bounds.size.x,
            height / beam.sprite.bounds.size.y, 1f);
        Color color = warningColor;
        color.a *= 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(warningElapsed * Mathf.PI * 3f / warningDuration));
        beam.color = color;
    }

    private void FitVisual(float width, float height)
    {
        Vector2 size = visual.sprite.bounds.size;
        visual.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
    }
}
