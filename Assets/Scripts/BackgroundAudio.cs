using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
public sealed class BackgroundAudio : MonoBehaviour
{
    public static BackgroundAudio Instance { get; private set; }

    [SerializeField] private AudioClip mainTheme;
    [SerializeField] private AudioClip ambience;
    [SerializeField] private string titleSceneName = "TitleScreen";
    [SerializeField, Range(0f, 1f)] private float titleThemeVolume = 0.4f;
    [SerializeField, Range(0f, 1f)] private float gameplayAmbienceVolume = 0.24f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.6f;

    private AudioSource themeSource;
    private AudioSource ambienceSource;
    private AudioSource activeSource;
    private bool titleDismissed;
    private float fadeElapsed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        themeSource = CreateSource("Main Theme", mainTheme);
        ambienceSource = CreateSource("Loopable Ambience", ambience);
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshPlayback(SceneManager.GetActiveScene());
    }

    private AudioSource CreateSource(string sourceName, AudioClip clip)
    {
        GameObject child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.pitch = 1f;
        source.volume = 0f;
        source.priority = 200;
        return source;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        titleDismissed = false;
        RefreshPlayback(scene);
    }

    public void BeginGameplayTransition()
    {
        titleDismissed = true;
        SetActiveSource(null);
    }

    private void RefreshPlayback(Scene scene)
    {
        if (scene.name == titleSceneName)
        {
            SetActiveSource(titleDismissed ? null : themeSource);
            return;
        }

        bool playing = GameManager.Instance != null && !GameManager.Instance.IsGameOver
            && Time.timeScale > 0f && !AudioListener.pause;
        SetActiveSource(playing ? ambienceSource : null);
    }

    private void SetActiveSource(AudioSource source)
    {
        if (activeSource == source)
            return;

        if (activeSource != null)
        {
            activeSource.Stop();
            activeSource.volume = 0f;
        }
        activeSource = source;
        fadeElapsed = 0f;
        if (activeSource != null && activeSource.clip != null)
        {
            activeSource.volume = 0f;
            activeSource.Play();
        }
    }

    private void Update()
    {
        if (Instance != this)
            return;

        RefreshPlayback(SceneManager.GetActiveScene());
        if (activeSource == null)
            return;

        float duration = Mathf.Max(0.01f, fadeDuration);
        fadeElapsed = Mathf.Min(fadeElapsed + Time.unscaledDeltaTime, duration);
        float blend = Mathf.SmoothStep(0f, 1f, fadeElapsed / duration);
        float volume = activeSource == themeSource ? titleThemeVolume : gameplayAmbienceVolume;
        activeSource.volume = Mathf.Lerp(0f, volume, blend);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }
}
