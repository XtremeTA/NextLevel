using UnityEngine;

internal static class EffectSprites
{
    public static Sprite CreateRing() => Create(true);
    public static Sprite CreateSoftDot() => Create(false);

    private static Sprite Create(bool ring)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = ring ? "Parry ring" : "Soft mote",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f).magnitude * 2f;
                float alpha = ring
                    ? (1f - Mathf.SmoothStep(0.73f, 0.9f, radius)) * Mathf.SmoothStep(0.52f, 0.7f, radius)
                    : Mathf.Pow(Mathf.Clamp01(1f - radius * radius), 3f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, size, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        return sprite;
    }
}
