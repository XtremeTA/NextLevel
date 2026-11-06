using UnityEngine;
using System.Collections;

public class GroundBreak : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private int brickHealth = 4;
    public int Health => brickHealth;

    [SerializeField] private Sprite[] alternativeSprites;
    [SerializeField] private Sprite[] stageTwoSprites;
    [SerializeField] private Sprite[] stageThreeSprites;
    [SerializeField, Min(0.1f)] private float refreshDuration = 0.65f;
    private Sprite[] activeSprites;
    private GameManager game;
    private Coroutine refresh;
    private GameObject overlay;
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        activeSprites = alternativeSprites;
    }

    private void Start()
    {
        game = GameManager.Instance;
        if (game != null)
        {
            game.StageChanged += RefreshStage;
            if (game.CurrentStage > 1) RefreshStage(game.CurrentStage);
        }
    }

    private void RefreshStage(int stage)
    {
        if (refresh != null) StopCoroutine(refresh);
        if (overlay != null) Destroy(overlay);
        Sprite[] frames = stage >= 3 ? stageThreeSprites : stage == 2 ? stageTwoSprites : alternativeSprites;
        activeSprites = frames != null && frames.Length > 0 ? frames : alternativeSprites;
        refresh = StartCoroutine(LiftFreshTile());
    }

    private IEnumerator LiftFreshTile()
    {
        overlay = new GameObject("Rising fresh floor");
        overlay.transform.SetParent(transform, false);
        SpriteRenderer cover = overlay.AddComponent<SpriteRenderer>();
        cover.sprite = activeSprites != null && activeSprites.Length > 0 ? activeSprites[0] : spriteRenderer.sprite;
        cover.sharedMaterial = spriteRenderer.sharedMaterial;
        cover.color = spriteRenderer.color;
        cover.sortingLayerID = spriteRenderer.sortingLayerID;
        cover.sortingOrder = spriteRenderer.sortingOrder + 1;
        Vector3 end = transform.position;
        Camera view = Camera.main;
        float startY = view != null && view.orthographic
            ? Mathf.Min(end.y - 1f, view.transform.position.y - view.orthographicSize - cover.bounds.extents.y)
            : end.y - 2f;
        Vector3 start = new Vector3(end.x, startY, end.z);
        float elapsed = 0f;
        while (elapsed < refreshDuration)
        {
            overlay.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / refreshDuration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        brickHealth = 4;
        spriteRenderer.sprite = cover.sprite;
        spriteRenderer.enabled = true;
        GetComponent<Collider2D>().enabled = true;
        Destroy(overlay);
        overlay = null;
        refresh = null;
    }

    private void OnDestroy()
    {
        if (game != null) game.StageChanged -= RefreshStage;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Ball>() != null)
            TakeDamage(1);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || brickHealth <= 0 || refresh != null || (GameManager.Instance != null && GameManager.Instance.IsGameOver))
            return;

        brickHealth = Mathf.Max(0, brickHealth - amount);
        if (brickHealth == 0)
        {
            // Remove support immediately, including during the player's recovery lift.
            GetComponent<Collider2D>().enabled = false;
            GameAudio.Instance?.Play(SoundCue.BrickBreak);
            // Keep the empty slot subscribed so the next stage can rebuild it.
            spriteRenderer.enabled = false;
            return;
        }

        if (activeSprites != null && activeSprites.Length > 0)
            spriteRenderer.sprite = activeSprites[Mathf.Min(4 - brickHealth, activeSprites.Length - 1)];
        GameAudio.Instance?.Play(SoundCue.TileCrack);
    }
}
