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
    private const float FrameHeight = 1080f;
    private const float SidebarWidth = 240f;
    private static readonly Color Paper = new Color32(242, 230, 192, 255);
    private static readonly Color Muted = new Color32(169, 167, 134, 255);
    private static readonly Color Edge = new Color32(83, 86, 61, 255);

    private RectTransform frame;
    private RectTransform leftColumn;
    private RectTransform rightColumn;
    private readonly TMP_Text[] scoreRows = new TMP_Text[LocalHighScores.Capacity];

    private void Awake()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        frame = MakeRect("Arcade HUD", transform);
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.sizeDelta = Vector2.zero;
        frame.SetAsFirstSibling();

        RectTransform left = leftColumn = Panel("Score column", frame);
        RectTransform right = rightColumn = Panel("Controls column", frame);
        Label(left, "LIVES", 24, 40, 192, 30, 22, Muted);
        for (int i = 0; i < hearts.Length; i++)
        {
            Place(hearts[i], left, 28 + i * 64, 84, 52, 42);
            hearts[i].GetComponent<Image>().preserveAspect = true;
        }

        Rule(left, 158);
        Label(left, "CURRENT SCORE", 24, 188, 192, 30, 22, Muted);
        // Keep the board and its digit positions intact, scaling the whole existing group.
        Place(scoreboard, left, 20, 234, 128, 64);
        scoreboard.localScale = Vector3.one * 1.5625f;
        Rule(left, 374);
        Label(left, "HIGH SCORES", 24, 404, 192, 32, 24, Paper);
        Label(left, "LOCAL TOP 10", 24, 442, 192, 24, 16, Muted);
        for (int i = 0; i < scoreRows.Length; i++)
        {
            Label(left, (i + 1).ToString("00"), 24, 496 + i * 45, 40, 32, 22, Muted);
            scoreRows[i] = Label(left, "—", 72, 496 + i * 45, 140, 32, 25, Paper);
            scoreRows[i].alignment = TextAlignmentOptions.MidlineRight;
        }

        Label(right, "CONTROLS", 28, 404, 184, 32, 24, Paper);
        Rule(right, 462);
        Control(right, "A / D", "MOVE (OR ARROWS)", 492);
        Control(right, "W", "JUMP", 600);
        Control(right, "SPACE", "PARRY", 708);
        Control(right, "LEFT SHIFT", "DASH", 816);
        Label(right, "R / ENTER\nRestart after game over", 28, 972, 184, 62, 18, Muted);

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
        float availableWidth = Mathf.Max(0f, right - left - 24f);
        column.gameObject.SetActive(availableWidth > 0f);
        float scale = Mathf.Min(availableWidth / SidebarWidth, Mathf.Max(0f, pixels.height - 24f) / FrameHeight);
        column.localScale = Vector3.one * scale;
        column.anchoredPosition = new Vector2((left + right - SidebarWidth * scale) * 0.5f,
            -(Screen.height - pixels.center.y - FrameHeight * scale * 0.5f));
    }

    private void RefreshScores()
    {
        int[] scores = LocalHighScores.Read();
        for (int i = 0; i < scoreRows.Length; i++)
            if (scoreRows[i] != null)
                scoreRows[i].text = scores[i] > 0 ? scores[i].ToString("D3") : "—";
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
        Place(rect, parent, 24, y, 192, 2);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Edge;
        image.raycastTarget = false;
    }

    private void Control(Transform parent, string keys, string action, float y)
    {
        Label(parent, keys, 28, y, 184, 34, 24, Paper);
        Label(parent, action, 28, y + 40, 184, 26, 18, Muted);
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
        return label;
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
