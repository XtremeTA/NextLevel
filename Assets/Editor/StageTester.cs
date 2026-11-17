using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Session-only settings: test selections never become scene or build settings.
[InitializeOnLoad]
public sealed class StageTester : EditorWindow
{
    private const string StageKey = "NextLevel.TestStage";
    private const string PreviousSceneKey = "NextLevel.TestPreviousPlayScene";
    private const string OverrideKey = "NextLevel.TestPlaySceneOverridden";
    private const string GameplayScene = "Assets/Scenes/SampleScene.unity";

    static StageTester()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("NextLevel/Stage Tester")]
    public static void Open()
    {
        GetWindow<StageTester>("Stage Tester").minSize = new Vector2(340, 340);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Quick Stage Testing", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Launch a fresh board directly into a stage. The stage stays locked and scores are not saved. The option below guarantees an eligible ghost, dragon, or bomb. Latcher remains available in Stages 1, 3, and 5, with at most two present at once. The bottom two starting rows stay safe.", MessageType.Info);
        bool guarantee = EditorGUILayout.ToggleLeft("Guarantee stage enemy in each eligible row",
            SessionState.GetBool("NextLevel.TestGuaranteeEnemy", true));
        SessionState.SetBool("NextLevel.TestGuaranteeEnemy", guarantee);
        EditorGUILayout.LabelField("Option applies when you click a Play button.", EditorStyles.miniLabel);
        int stage = SessionState.GetInt(StageKey, 0);
        EditorGUILayout.LabelField("Current mode", stage > 0 ? "Testing Stage " + stage : "Normal progression");
        using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying))
        {
            for (int i = 1; i <= 5; i++)
                if (GUILayout.Button("Play Stage " + i, GUILayout.Height(28))) Launch(i);
            if (GUILayout.Button("Play Normal Game", GUILayout.Height(28))) Launch(0);
        }
        EditorGUILayout.LabelField("Stopping Play mode clears the test selection.", EditorStyles.wordWrappedLabel);
    }

    private static void Launch(int stage)
    {
        string path = stage == 0 ? "Assets/Scenes/TitleScreen.unity" : GameplayScene;
        SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        if (scene == null) { Debug.LogError("Missing scene: " + path); return; }
        SessionState.SetInt(StageKey, stage);
        if (EditorApplication.isPlaying)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(path);
            return;
        }
        if (!SessionState.GetBool(OverrideKey, false))
        {
            SessionState.SetString(PreviousSceneKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(OverrideKey, true);
        }
        EditorSceneManager.playModeStartScene = scene;
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        SessionState.SetInt(StageKey, 0);
        if (SessionState.GetBool(OverrideKey, false))
        {
            string path = SessionState.GetString(PreviousSceneKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path)
                ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            SessionState.SetBool(OverrideKey, false);
        }
    }
}
