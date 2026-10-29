using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The side columns surround the original 16:9 view; they never take world space from the arena.
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Canvas))]
public sealed class GameplayHud : MonoBehaviour
{
    [SerializeField] private Camera playAreaCamera;
    [SerializeField] private RectTransform scoreboard;
    [SerializeField] private RectTransform[] hearts;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private RectTransform gameOverPanel;

    private const float ArenaWidth = 1920f;
    private const float FrameHeight = 1080f;
    private const float SidebarWidth = 240f;
    private const float FrameWidth = ArenaWidth + SidebarWidth * 2f;
    private static readonly Color Ink = new Color32(24, 28, 23, 255);
    private static readonly Color Paper = new Color32(242, 230, 192, 255);
    private static readonly Color Muted = new Color32(169, 167, 134, 255);
    private static readonly Color Edge = new Color32(83, 86, 61, 255);

    private RectTransform frame;
    private readonly RectTransform[] margins = new RectTransform[4];
    private readonly TMP_Text[] scoreRows = new TMP_Text[LocalHighScores.Capacity];
    private int screenWidth;
    private int screenHeight;
    private Rect originalCameraRect;

    private void Awake()
    {
        originalCameraRect = playAreaCamera.rect;
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        for (int i = 0; i < margins.Length; i++)
        {
            margins[i] = MakeRect("Window margin", transform);
            margins[i].SetAsFirstSibling();
            Image shade = margins[i].gameObject.AddComponent<Image>();
            shade.color = Ink;
            shade.raycastTarget = false;
        }

        frame = MakeRect("Arcade HUD", transform);
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
        frame.sizeDelta = new Vector2(FrameWidth, FrameHeight);
        frame.SetAsFirstSibling();

        RectTransform left = Panel("Score column", frame, 0f);
        RectTransform right = Panel("Controls column", frame, FrameWidth - SidebarWidth);
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

        if (gameOverPanel != null)
        {
            gameOverPanel.SetParent(frame, false);
            gameOverPanel.anchorMin = gameOverPanel.anchorMax = gameOverPanel.pivot = new Vector2(0.5f, 0.5f);
            gameOverPanel.anchoredPosition = Vector2.zero;
            gameOverPanel.sizeDelta = new Vector2(ArenaWidth, FrameHeight);
            gameOverPanel.localScale = Vector3.one;
        }

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
        if (playAreaCamera != null)
            playAreaCamera.rect = originalCameraRect;
    }

    private void Update()
    {
        if (Screen.width != screenWidth || Screen.height != screenHeight)
            FitToWindow();
    }

    private void FitToWindow()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        float scale = Mathf.Min(screenWidth / FrameWidth, screenHeight / FrameHeight);
        frame.localScale = Vector3.one * scale;
        float width = ArenaWidth * scale / screenWidth;
        float height = FrameHeight * scale / screenHeight;
        playAreaCamera.rect = new Rect((1f - width) * 0.5f, (1f - height) * 0.5f, width, height);

        float side = (screenWidth - FrameWidth * scale) * 0.5f;
        float top = (screenHeight - FrameHeight * scale) * 0.5f;
        Place(margins[0], transform, 0, 0, screenWidth, top);
        Place(margins[1], transform, 0, screenHeight - top, screenWidth, top);
        Place(margins[2], transform, 0, top, side, FrameHeight * scale);
        Place(margins[3], transform, screenWidth - side, top, side, FrameHeight * scale);
    }

    private void RefreshScores()
    {
        int[] scores = LocalHighScores.Read();
        for (int i = 0; i < scoreRows.Length; i++)
            if (scoreRows[i] != null)
                scoreRows[i].text = scores[i] > 0 ? scores[i].ToString("D3") : "—";
    }

    private RectTransform Panel(string title, Transform parent, float x)
    {
        RectTransform panel = MakeRect(title, parent);
        Place(panel, parent, x, 0, SidebarWidth, FrameHeight);
        Image image = panel.gameObject.AddComponent<Image>();
        image.color = Ink;
        image.raycastTarget = false;
        RectTransform edge = MakeRect("Inner edge", panel);
        Place(edge, panel, x == 0 ? SidebarWidth - 2 : 0, 0, 2, FrameHeight);
        Image line = edge.gameObject.AddComponent<Image>();
        line.color = Edge;
        line.raycastTarget = false;
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
