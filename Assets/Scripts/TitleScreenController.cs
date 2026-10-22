using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TitleScreenController : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private Button startButton;
    [SerializeField] private CanvasGroup menuGroup;
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.25f;

    private bool starting;

    private void Awake()
    {
        Time.timeScale = 1f;
        if (menuGroup != null)
            menuGroup.alpha = 0f;
    }

    private void Start()
    {
        if (EventSystem.current != null && startButton != null)
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    private void Update()
    {
        if (starting)
            return;

        if (menuGroup != null)
            menuGroup.alpha = Mathf.MoveTowards(menuGroup.alpha, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeInDuration));
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            StartGame();
    }

    public void StartGame()
    {
        if (starting)
            return;

        starting = true;
        BackgroundAudio.Instance?.BeginGameplayTransition();
        if (startButton != null)
            startButton.interactable = false;
        StartCoroutine(LoadGame());
    }

    private IEnumerator LoadGame()
    {
        float elapsed = 0f;
        float initialAlpha = menuGroup != null ? menuGroup.alpha : 1f;
        float duration = Mathf.Max(0.01f, transitionDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (menuGroup != null)
                menuGroup.alpha = Mathf.Lerp(initialAlpha, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        SceneManager.LoadSceneAsync(gameplaySceneName);
    }
}
