using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Overlay the existing wall artwork without changing the camera or the arena.
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Canvas))]
public sealed class GameplayHud : MonoBehaviour
{
    [SerializeField] private Camera playAreaCamera;
    [SerializeField] private SpriteRenderer leftWall;
    [SerializeField] private SpriteRenderer rightWall;
    [SerializeField] private RectTransform scoreboard;
    [SerializeField] private RectTransform[] hearts;
    [SerializeField] private TMP_FontAsset font;
    private const float FrameHeight = 1000f;
    private const float SidebarWidth = 240f;
    private static readonly Color Paper = new Color32(242, 230, 192, 255);
    private static readonly Color Muted = new Color32(169, 167, 134, 255);
    private static readonly Color Edge = new Color32(83, 86, 61, 255);

    private RectTransform frame;
    private RectTransform leftColumn;
    private RectTransform rightColumn;
    private readonly TMP_Text[] scoreRows = new TMP_Text[LocalHighScores.Capacity];
    private readonly TMP_Text[] initialsRows = new TMP_Text[LocalHighScores.Capacity];
    private readonly List<HudElement> elements = new List<HudElement>();

    private struct HudElement
    {
        public RectTransform rect;
        public Rect layout;
        public TMP_Text label;
        public float fontSize;
    }

    private void Awake()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        frame = MakeRect("Arcade HUD", transform);
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.sizeDelta = Vector2.zero;

        RectTransform left = leftColumn = Panel("Score column", frame);
        RectTransform right = rightColumn = Panel("Controls column", frame);
        Label(left, "LIVES", 0, 0, 240, 44, 24, Muted);
        for (int i = 0; i < hearts.Length; i++)
        {
            Place(hearts[i], left, i * 84, 54, 72, 64);
            Track(hearts[i]);
            hearts[i].GetComponent<Image>().preserveAspect = true;
        }

        Rule(left, 142);
        Label(left, "CURRENT SCORE", 0, 170, 240, 40, 24, Muted);
        // Resize the original board as a group to preserve the art and digit positions.
        Place(scoreboard, left, 0, 218, 128, 64);
        Rule(left, 378);
        Label(left, "SCORES", 0, 405, 240, 44, 28, Paper);
        Label(left, "LOCAL TOP 10", 0, 452, 240, 32, 18, Muted);
        for (int i = 0; i < scoreRows.Length; i++)
        {
            Label(left, (i + 1).ToString("00"), 0, 506 + i * 47.5f, 38, 44, 22, Muted);
            initialsRows[i] = Label(left, "---", 48, 506 + i * 47.5f, 72, 44, 23, Paper);
            scoreRows[i] = Label(left, "—", 126, 506 + i * 47.5f, 114, 44, 28, Paper);
            scoreRows[i].enableAutoSizing = true;
            scoreRows[i].fontSizeMin = 8f;
            scoreRows[i].alignment = TextAlignmentOptions.MidlineRight;
        }

        Label(right, "CONTROLS", 0, 0, 240, 48, 30, Paper);
        Rule(right, 70);
        Control(right, "A / D", "MOVE (OR ARROWS)", 134);
        Control(right, "W", "JUMP", 320);
        Control(right, "SPACE", "PARRY", 506);
        Control(right, "SHIFT", "DASH", 692);
        Label(right, "SPACE / ENTER", 0, 884, 240, 52, 24, Paper);
        Label(right, "Confirm in menus\nWASD / arrows select", 0, 940, 240, 60, 19, Muted);

        LayoutColumn(left);
        LayoutColumn(right);
        RefreshScores();
        FitToWindow();
    }

    private void OnEnable()
    {
        LocalHighScores.Changed += RefreshScores;
        if (frame != null)
            FitToWindow();
    }
    private void OnDisable()
    {
        LocalHighScores.Changed -= RefreshScores;
    }

    private void Update()
    {
        FitToWindow();
    }

    private void FitToWindow()
    {
        if (playAreaCamera == null || !playAreaCamera.orthographic)
            return;
        FitOverWall(leftColumn, leftWall);
        FitOverWall(rightColumn, rightWall);
    }

    private void FitOverWall(RectTransform column, SpriteRenderer wall)
    {
        if (wall == null)
            return;

        // Use the camera's world bounds, so render-only parry bumps do not move the HUD.
        Rect pixels = playAreaCamera.pixelRect;
        float worldWidth = 2f * playAreaCamera.orthographicSize * playAreaCamera.aspect;
        float worldLeft = playAreaCamera.transform.position.x - worldWidth * 0.5f;
        float left = Mathf.Clamp(pixels.x + (wall.bounds.min.x - worldLeft) / worldWidth * pixels.width, pixels.xMin, pixels.xMax);
        float right = Mathf.Clamp(pixels.x + (wall.bounds.max.x - worldLeft) / worldWidth * pixels.width, pixels.xMin, pixels.xMax);
        float wallWidth = right - left;
        float horizontalPadding = Mathf.Min(wallWidth * 0.06f, pixels.height * 0.022f);
        float verticalPadding = pixels.height * 0.035f;

        // Convert viewport pixels into the canvas, including render textures and window resizes.
        float outputWidth = playAreaCamera.targetTexture != null ? playAreaCamera.targetTexture.width : Screen.width;
        float outputHeight = playAreaCamera.targetTexture != null ? playAreaCamera.targetTexture.height : Screen.height;
        Vector2 unitsPerPixel = new Vector2(frame.rect.width / outputWidth, frame.rect.height / outputHeight);
        float availableWidth = (wallWidth - horizontalPadding * 2f) * unitsPerPixel.x;
        float availableHeight = (pixels.height - verticalPadding * 2f) * unitsPerPixel.y;
        bool visible = availableWidth > 0f && availableHeight > 0f;
        column.gameObject.SetActive(visible);
        if (!visible)
            return;

        // Stretch the layout area across the wall; scale the artwork and text uniformly.
        float scale = availableHeight / FrameHeight;
        Vector2 size = new Vector2(availableWidth / scale, FrameHeight);
        column.localScale = Vector3.one * scale;
        column.anchoredPosition = new Vector2((left + horizontalPadding) * unitsPerPixel.x,
            -(outputHeight - pixels.yMax + verticalPadding) * unitsPerPixel.y);
        if (column.sizeDelta != size)
        {
            column.sizeDelta = size;
            LayoutColumn(column);
        }
    }

    private void LayoutColumn(RectTransform column)
    {
        float widthRatio = column.rect.width / SidebarWidth;
        float textScale = Mathf.Min(widthRatio, 1.35f);
        foreach (HudElement element in elements)
        {
            if (element.rect.parent != column)
                continue;

            element.rect.anchoredPosition = new Vector2(element.layout.x * widthRatio, -element.layout.y);
            element.rect.sizeDelta = new Vector2(element.layout.width * widthRatio, element.layout.height);
            if (element.label != null)
            {
                element.label.fontSize = element.fontSize * textScale;
                if (element.label.enableAutoSizing)
                    element.label.fontSizeMax = element.label.fontSize;
            }
        }

        if (column == leftColumn)
        {
            float boardScale = Mathf.Min(column.rect.width / 128f, 140f / 64f);
            scoreboard.localScale = Vector3.one * boardScale;
            scoreboard.anchoredPosition = new Vector2((column.rect.width - 128f * boardScale) * 0.5f,
                -(218f + (140f - 64f * boardScale) * 0.5f));
        }
    }

    private void RefreshScores()
    {
        LocalHighScores.Entry[] scores = LocalHighScores.ReadEntries();
        for (int i = 0; i < scoreRows.Length; i++)
            if (scoreRows[i] != null)
            {
                scoreRows[i].text = scores[i].Score > 0 ? scores[i].Score.ToString("D3") : "—";
                initialsRows[i].text = scores[i].Score > 0 ? scores[i].Initials : "---";
            }
    }

    private RectTransform Panel(string title, Transform parent)
    {
        RectTransform panel = MakeRect(title, parent);
        Place(panel, parent, 0, 0, SidebarWidth, FrameHeight);
        return panel;
    }

    private void Rule(Transform parent, float y)
    {
        RectTransform rect = MakeRect("Divider", parent);
        Place(rect, parent, 0, y, SidebarWidth, 2);
        Track(rect);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Edge;
        image.raycastTarget = false;
    }

    private void Control(Transform parent, string keys, string action, float y)
    {
        Label(parent, keys, 0, y, 240, 60, 38, Paper);
        Label(parent, action, 0, y + 64, 240, 42, 20, Muted);
    }

    private TMP_Text Label(Transform parent, string value, float x, float y, float w, float h, float size, Color color)
    {
        RectTransform rect = MakeRect(value, parent);
        Place(rect, parent, x, y, w, h);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = value;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        Track(rect, label);
        return label;
    }

    private void Track(RectTransform rect, TMP_Text label = null)
    {
        elements.Add(new HudElement
        {
            rect = rect,
            layout = new Rect(rect.anchoredPosition.x, -rect.anchoredPosition.y, rect.sizeDelta.x, rect.sizeDelta.y),
            label = label,
            fontSize = label != null ? label.fontSize : 0f
        });
    }

    private static RectTransform MakeRect(string title, Transform parent)
    {
        RectTransform rect = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Place(RectTransform rect, Transform parent, float x, float y, float width, float height)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
    }
}
