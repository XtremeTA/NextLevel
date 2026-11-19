using System.Collections.Generic;
using UnityEngine;

public sealed class LatcherSpike : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float riseSpeed = 15f;
    [SerializeField, Min(0.1f)] private float width = 0.5f;
    private readonly List<Collider2D> hits = new List<Collider2D>(16);
    private readonly ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
    private Camera view;
    private float age;

    private void Start()
    {
        view = Camera.main;
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
}
