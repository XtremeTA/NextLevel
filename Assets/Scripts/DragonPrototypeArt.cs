using UnityEngine;

// Small generated placeholders; DragonLaneEnemy accepts replacement sprites in the Inspector.
internal static class DragonPrototypeArt
{
    private static Sprite wisp;
    private static Sprite beam;
    private static Sprite dragon;
    public static Sprite Wisp => wisp != null ? wisp : wisp = EffectSprites.CreateSoftDot();
    public static Sprite Beam => beam != null ? beam : beam = CreateBeam();
    public static Sprite Dragon => dragon != null ? dragon : dragon = CreateDragon();

    private static Sprite CreateBeam()
    {
        const int width = 32, height = 128;
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float across = Mathf.Abs((x + 0.5f) / width * 2f - 1f);
            float alpha = Mathf.Clamp01((1f - across) * 8f) * Mathf.Lerp(1f, 0.35f, (float)y / (height - 1));
            pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
        }
        return CreateSprite("Dragon lane light", width, height, pixels, FilterMode.Bilinear);
    }

    private static Sprite CreateDragon()
    {
        const int width = 32, height = 64;
        Color[] pixels = new Color[width * height];
        Color body = new Color(0.13f, 0.55f, 0.35f);
        Color highlight = new Color(0.65f, 1f, 0.48f);
        Color eye = new Color(1f, 0.9f, 0.4f);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int spine = 16 + Mathf.RoundToInt(3f * Mathf.Sin(y * 0.22f));
            int halfWidth = y < 40 ? 1 + y / 12 : 5;
            bool tail = y < 45 && Mathf.Abs(x - spine) <= halfWidth;
            bool head = y >= 44 && y <= 57 && Mathf.Abs(x - 16) <= 7 - Mathf.Abs(y - 50) / 2;
            bool wings = y >= 30 && y <= 45 && Mathf.Abs(x - 16) <= (45 - y) && Mathf.Abs(x - 16) >= 5;
            bool horns = y >= 54 && y < 63 && (x == 10 + (y - 54) / 3 || x == 22 - (y - 54) / 3);
            if (tail || head || wings || horns)
                pixels[y * width + x] = (Mathf.Abs(x - spine) <= 1 || horns) ? highlight : body;
            if (y >= 51 && y <= 53 && (x == 12 || x == 20))
                pixels[y * width + x] = eye;
        }
        return CreateSprite("Placeholder rising dragon", width, height, pixels, FilterMode.Point);
    }

    private static Sprite CreateSprite(string name, int width, int height, Color[] pixels, FilterMode filter)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = filter,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Sprite result = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * 0.5f, 32f, 0, SpriteMeshType.FullRect);
        result.name = name;
        return result;
    }
}
