using UnityEngine;

public sealed class DragonLaneEnemy : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float fallSpeed = 7f;
    [SerializeField] private float warningDuration = 1.1f;

    [Header("Lane")]
    [SerializeField] private float laneWidth = 1.25f;
    [SerializeField, Min(0f)] private float wispSwayDistance = 0.12f;
    [SerializeField, Min(0f)] private float wispSwaySpeed = 4f;

    [Header("References")]
    [SerializeField] private GameObject dragonPrefab;
    [SerializeField] private Sprite wispSprite;
    [SerializeField] private Material visualMaterial;

    [Header("Warning")]
    [SerializeField]
    private Color warningColor = new Color(0.68f, 0.81f, 0.65f, 0.2f);

    private enum Phase
    {
        Falling,
        Warning
    }

    private Phase phase;

    private Camera view;
    private SpriteRenderer visual;
    private SpriteRenderer beam;

    private float laneX;
    private float warningElapsed;
    private float age;

    private float Bottom =>
        view.transform.position.y - view.orthographicSize;

    private float Top =>
        view.transform.position.y + view.orthographicSize;

    private void Start()
    {
        view = Camera.main;

        if (view == null || !view.orthographic)
        {
            Debug.LogError(
                "Dragon lane enemy requires an orthographic camera.",
                this);

            Destroy(gameObject);
            return;
        }

        laneX = transform.position.x;

        visual = CreateRenderer("Wisp", 4);
        visual.sprite = wispSprite != null ? wispSprite : DragonPrototypeArt.Wisp;

        FitVisual(0.45f, 0.7f);

        beam = CreateRenderer("Warning", 3);
        beam.sprite = DragonPrototypeArt.Beam;
        beam.enabled = false;

        GameAudio.Instance?.Play(SoundCue.DragonWisp);
    }

    private SpriteRenderer CreateRenderer(
        string objectName,
        int order)
    {
        GameObject child = new GameObject(objectName);

        child.transform.SetParent(transform, false);

        SpriteRenderer renderer =
            child.AddComponent<SpriteRenderer>();

        renderer.sortingOrder = order;

        if (visualMaterial != null)
            renderer.sharedMaterial = visualMaterial;

        return renderer;
    }

    private void FixedUpdate()
    {
        if (view == null)
            return;

        age += Time.fixedDeltaTime;
        if (age > 30f || (GameManager.Instance != null && GameManager.Instance.IsGameOver))
        {
            Destroy(gameObject);
            return;
        }

        if (phase == Phase.Falling)
        {
            transform.position +=
                Vector3.down *
                fallSpeed *
                Time.fixedDeltaTime;

            if (visual.bounds.max.y < Bottom)
            {
                phase = Phase.Warning;

                visual.enabled = false;
                beam.enabled = true;

                warningElapsed = 0f;

                GameAudio.Instance?.Play(
                    SoundCue.DragonWarning);
            }
        }
        else if (phase == Phase.Warning)
        {
            warningElapsed += Time.fixedDeltaTime;

            if (warningElapsed >= warningDuration)
            {
                SpawnDragon();

                Destroy(gameObject);
            }
        }
    }

    private void SpawnDragon()
    {
        if (dragonPrefab == null)
        {
            Debug.LogWarning(
                "Dragon prefab missing.",
                this);

            return;
        }

        // Start the complete new sprite below the camera, not halfway into the arena.
        SpriteRenderer dragonRenderer = dragonPrefab.GetComponent<SpriteRenderer>();
        float topOffset = dragonRenderer != null && dragonRenderer.sprite != null
            ? dragonRenderer.sprite.bounds.max.y * Mathf.Abs(dragonPrefab.transform.localScale.y)
            : 1f;
        Vector3 spawnPosition =
            new Vector3(
                laneX,
                Bottom - topOffset - 0.1f,
                0f);

        Instantiate(
            dragonPrefab,
            spawnPosition,
            Quaternion.identity);

        GameAudio.Instance?.Play(
            SoundCue.DragonRise);
    }

    private void LateUpdate()
    {
        if (view == null)
            return;

        if (phase == Phase.Falling)
        {
            visual.transform.localPosition = new Vector3(
                Mathf.Sin(age * wispSwaySpeed) * wispSwayDistance, 0f, 0f);
            visual.color = new Color(0.74f, 0.86f, 0.7f, 0.55f);
        }

        if (phase != Phase.Warning)
            return;

        float height = Top - Bottom;

        beam.transform.position =
            new Vector3(
                laneX,
                (Top + Bottom) * 0.5f,
                0f);

        beam.transform.localScale =
            new Vector3(
                laneWidth /
                beam.sprite.bounds.size.x,

                height /
                beam.sprite.bounds.size.y,

                1f);

        Color color = warningColor;

        color.a *=
            0.55f +
            0.45f *
            Mathf.Abs(
                Mathf.Sin(
                    warningElapsed *
                    Mathf.PI *
                    3f /
                    warningDuration));

        beam.color = color;
    }

    private void FitVisual(float width, float height)
    {
        Vector2 size = visual.sprite.bounds.size;

        visual.transform.localScale =
            new Vector3(
                width / size.x,
                height / size.y,
                1f);
    }
}
