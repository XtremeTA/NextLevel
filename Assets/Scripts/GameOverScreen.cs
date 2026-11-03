using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
public sealed class GameOverScreen : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Sprite background;
    [SerializeField] private Sprite gameOverArtwork;
    [SerializeField] private Sprite retryArtwork;

    private enum ScreenState { Hidden, Initials, Final }
    private ScreenState state;
    private GameObject surface;
    private GameObject initialsGroup;
    private GameObject finalGroup;
    private CanvasGroup fade;
    private TMP_Text entryScore;
    private TMP_Text finalScore;
    private TMP_Text savedRecord;
    private readonly TMP_Text[] letterLabels = new TMP_Text[3];
    private readonly Image[] selectionImages = new Image[4];
    private readonly Outline[] selectionOutlines = new Outline[4];
    private readonly char[] initials = { 'A', 'A', 'A' };
    private int selected;
    private int score;
    private int rank;
    private bool confirmReady;
    private float nextLetterRepeat;

    private static readonly Color Paper = new Color32(242, 230, 192, 255);
    private static readonly Color Muted = new Color32(185, 170, 143, 255);

    public bool IsEnteringInitials => state == ScreenState.Initials;
    public bool CanRetry => state == ScreenState.Final;

    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366, 768);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        RectTransform root = MakeRect("End of run", transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.sizeDelta = Vector2.zero;
        surface = root.gameObject;
        fade = surface.AddComponent<CanvasGroup>();

        Image backdrop = MakeRect("Game over background", root).gameObject.AddComponent<Image>();
        backdrop.sprite = background;
        backdrop.raycastTarget = true;
        AspectRatioFitter cover = backdrop.gameObject.AddComponent<AspectRatioFitter>();
        cover.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        cover.aspectRatio = background != null ? background.rect.width / background.rect.height : 16f / 9f;

        RectTransform entry = Element("Initials entry", root, 0, 0, 1366, 768);
        entry.anchorMin = entry.anchorMax = entry.pivot = new Vector2(0.5f, 0.5f);
        initialsGroup = entry.gameObject;
        Label(entry, "NEW HIGH SCORE", 0, 114, 800, 56, 40, Paper);
        entryScore = Label(entry, "", 0, 184, 800, 42, 26, Muted);
        Label(entry, "ENTER YOUR INITIALS", 0, 266, 800, 36, 22, Paper);

        for (int i = 0; i < letterLabels.Length; i++)
        {
            int slot = i;
            RectTransform box = Element("Letter " + (i + 1), entry, (i - 1) * 146, 330, 112, 116);
            Button button = SelectionButton(box, i);
            button.onClick.AddListener(() => { selected = slot; RefreshSelection(); });
            letterLabels[i] = Label(box, "A", 0, 14, 110, 84, 64, Paper);
        }
        RectTransform save = Element("Save initials", entry, 0, 510, 340, 64);
        SelectionButton(save, 3).onClick.AddListener(SaveInitials);
        Label(save, "SAVE INITIALS", 0, 0, 340, 64, 25, Paper);
        Label(entry, "A / D  SELECT     W / S  CHANGE LETTER     (OR ARROWS)", 0, 626, 1100, 32, 20, Muted);
        Label(entry, "SPACE / ENTER  NEXT / SAVE", 0, 668, 1000, 32, 20, Paper);

        RectTransform final = Element("Final screen", root, 0, 0, 1366, 768);
        final.anchorMin = final.anchorMax = final.pivot = new Vector2(0.5f, 0.5f);
        finalGroup = final.gameObject;
        Art(final, "Game over logo", gameOverArtwork, 0, 35, 740, 380);
        finalScore = Label(final, "", 0, 420, 900, 48, 30, Paper);
        savedRecord = Label(final, "", 0, 478, 1000, 36, 20, Muted);
        Image retry = Art(final, "Retry", retryArtwork, 0, 455, 600, 240);
        retry.transform.SetAsFirstSibling();
        retry.raycastTarget = true;
        Button retryButton = retry.gameObject.AddComponent<Button>();
        retryButton.targetGraphic = retry;
        retryButton.navigation = new Navigation { mode = Navigation.Mode.None };
        retryButton.onClick.AddListener(Retry);
        Label(final, "SPACE / ENTER TO RETRY", 0, 704, 800, 32, 20, Paper);
        surface.SetActive(false);
    }

    public void Show(int finalPoints)
    {
        score = finalPoints;
        rank = LocalHighScores.QualifyingRank(score);
        selected = 0;
        nextLetterRepeat = Time.unscaledTime + 0.35f;
        for (int i = 0; i < initials.Length; i++) initials[i] = 'A';
        // Keyboard navigation is handled here so a confirm cannot also click Retry in the same frame.
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.sendNavigationEvents = false;
        }
        surface.SetActive(true);
        fade.alpha = 0f;
        entryScore.text = "#" + (rank + 1).ToString("00") + "   /   " + score.ToString("D3") + " POINTS";
        if (rank >= 0)
        {
            SetState(ScreenState.Initials);
            RefreshSelection();
        }
        else
            ShowFinal(null);
    }

    private void Update()
    {
        if (state == ScreenState.Hidden)
            return;

        fade.alpha = Mathf.MoveTowards(fade.alpha, 1f, Time.unscaledDeltaTime / 0.25f);
        if (!ArcadeInput.ConfirmHeld)
            confirmReady = true;
        bool confirm = confirmReady && ArcadeInput.ConfirmPressed;
        HandleMenuInput(ArcadeInput.HorizontalPressed, LetterStep(), confirm);
    }

    private int LetterStep()
    {
        int pressed = ArcadeInput.VerticalPressed;
        if (pressed != 0)
        {
            nextLetterRepeat = Time.unscaledTime + 0.35f;
            return pressed;
        }
        int held = ArcadeInput.VerticalHeld;
        if (held == 0 || Time.unscaledTime < nextLetterRepeat)
            return 0;
        nextLetterRepeat = Time.unscaledTime + 0.1f;
        return held;
    }

    private void HandleMenuInput(int horizontal, int vertical, bool confirm)
    {
        if (state == ScreenState.Final)
        {
            if (confirm) Retry();
            return;
        }
        if (!IsEnteringInitials)
            return;
        if (horizontal == 0 && vertical == 0 && !confirm)
            return;

        selected = (selected + horizontal + 4) % 4;
        if (selected < initials.Length && vertical != 0)
            initials[selected] = (char)('A' + (initials[selected] - 'A' + vertical + 26) % 26);
        if (confirm)
        {
            if (selected < initials.Length) selected++;
            else SaveInitials();
        }
        RefreshSelection();
    }

    private void SaveInitials()
    {
        if (!IsEnteringInitials)
            return;
        string value = new string(initials);
        if (GameManager.Instance != null && GameManager.Instance.SubmitHighScore(value))
            ShowFinal(value);
    }

    private void ShowFinal(string savedInitials)
    {
        finalScore.text = "FINAL SCORE   " + score.ToString("D3");
        savedRecord.text = savedInitials != null
            ? savedInitials + "   /   #" + (rank + 1).ToString("00") + " ON THE LOCAL TOP 10"
            : "";
        SetState(ScreenState.Final);
    }

    private void SetState(ScreenState next)
    {
        state = next;
        confirmReady = false;
        initialsGroup.SetActive(IsEnteringInitials);
        finalGroup.SetActive(CanRetry);
    }

    private void Retry()
    {
        if (CanRetry)
            GameManager.Instance?.RestartGame();
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < letterLabels.Length; i++) letterLabels[i].text = initials[i].ToString();
        for (int i = 0; i < selectionImages.Length; i++)
        {
            selectionImages[i].color = i == selected ? new Color32(112, 34, 29, 245) : new Color32(20, 24, 19, 225);
            selectionOutlines[i].enabled = i == selected;
        }
    }

    private Button SelectionButton(RectTransform rect, int index)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        selectionImages[index] = image;
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = Paper;
        outline.effectDistance = new Vector2(2, -2);
        selectionOutlines[index] = outline;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }

    private Image Art(Transform parent, string title, Sprite sprite, float x, float y, float width, float height)
    {
        Image image = Element(title, parent, x, y, width, height).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text Label(Transform parent, string text, float x, float y, float width, float height, float size, Color color)
    {
        TMP_Text label = Element(text, parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Element(string title, Transform parent, float x, float y, float width, float height)
    {
        RectTransform rect = MakeRect(title, parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private static RectTransform MakeRect(string title, Transform parent)
    {
        RectTransform rect = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        return rect;
    }
}
