using System.Collections.Generic;
using UnityEngine;

public sealed class LatcherSpike : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float riseSpeed = 15f;
    [SerializeField, Min(0.1f)] private float width = 0.5f;
    private static Sprite sprite;
    private readonly List<Collider2D> hits = new List<Collider2D>(16);
    private readonly ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
    private Camera view;
    private float age;

    private void Start()
    {
        view = Camera.main;
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : sprite = CreateSprite();
        renderer.sortingOrder = 5;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) { Destroy(gameObject); return; }
        float step = riseSpeed * Time.fixedDeltaTime;
        hits.Clear();
        Physics2D.OverlapBox(new Vector2(transform.position.x, transform.position.y + step * 0.5f),
            new Vector2(width, step + 0.5f), 0f, filter, hits);
        Brick target = null;
        foreach (Collider2D hit in hits)
        {
            Brick brick = hit.GetComponent<Brick>();
            if (brick != null && !brick.IsBreaking && Mathf.Abs(brick.transform.position.x - transform.position.x) < 0.6f &&
                (target == null || brick.transform.position.y < target.transform.position.y))
                target = brick;
        }
        if (target != null) { target.Break(); Destroy(gameObject); return; }
        transform.position += Vector3.up * step;
        age += Time.fixedDeltaTime;
        if (age > 5f || (view != null && transform.position.y > view.transform.position.y + view.orthographicSize + 0.5f))
            Destroy(gameObject);
    }

    private static Sprite CreateSprite()
    {
        const int w = 20, h = 36;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            int half = Mathf.Max(0, (h - y) / 5);
            for (int x = 0; x < w; x++)
                if (Mathf.Abs(x - 9.5f) <= half)
                    pixels[y * w + x] = x == 9 || x == 10 ? new Color(0.7f, 0.88f, 0.56f) : new Color(0.23f, 0.52f, 0.31f);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, w, h), Vector2.one * 0.5f, 32f);
    }
}
