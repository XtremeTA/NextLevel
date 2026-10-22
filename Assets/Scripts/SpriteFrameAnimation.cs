using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteFrameAnimation : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1f)] private float frameRate = 10f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool playOnEnable = true;

    private SpriteRenderer spriteRenderer;
    private float elapsed;
    private bool playing;

    public float Duration => frames == null ? 0f : frames.Length / Mathf.Max(1f, frameRate);

    private void OnEnable()
    {
        if (playOnEnable)
            Play(frames, loop, frameRate);
    }

    public void Play(Sprite[] sprites, bool repeat = false, float framesPerSecond = 10f)
    {
        frames = sprites;
        loop = repeat;
        frameRate = Mathf.Max(1f, framesPerSecond);
        elapsed = 0f;
        playing = frames != null && frames.Length > 0;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (playing)
            spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (!playing)
            return;

        elapsed += Time.deltaTime;
        int frame = Mathf.FloorToInt(elapsed * frameRate);
        spriteRenderer.sprite = frames[loop ? frame % frames.Length : Mathf.Min(frame, frames.Length - 1)];
        if (!loop && elapsed >= Duration)
            playing = false;
    }
}
