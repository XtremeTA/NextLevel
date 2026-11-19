using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// This Editor-only choice is baked into the scene copy used for each player build.
public sealed class BackgroundAnimationBuildToggle : IProcessSceneWithReport
{
    private const string MenuPath = "NextLevel/Build/Animated Background";
    private const string PreferenceKey = "NextLevel.Build.AnimatedBackground";
    private const string GameplayScene = "Assets/Scenes/SampleScene.unity";

    public int callbackOrder => 0;

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        EditorPrefs.SetBool(PreferenceKey, !EditorPrefs.GetBool(PreferenceKey, true));
        ShowCheckmark();
    }

    [MenuItem(MenuPath, true)]
    private static bool ShowCheckmark()
    {
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PreferenceKey, true));
        return true;
    }

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        // Unity also invokes this callback when entering Play mode.
        if (report == null || !BuildPipeline.isBuildingPlayer || scene.path != GameplayScene)
            return;

        ApplyToScene(scene, EditorPrefs.GetBool(PreferenceKey, true));
    }

    internal static void ApplyToScene(Scene scene, bool animated)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != "Background")
                continue;

            SpriteFrameAnimation animation = root.GetComponent<SpriteFrameAnimation>();
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            if (animation == null || renderer == null)
                throw new BuildFailedException("Gameplay Background is missing its sprite renderer or animation.");

            SerializedProperty frames = new SerializedObject(animation).FindProperty("frames");
            Sprite firstFrame = frames != null && frames.arraySize > 0
                ? frames.GetArrayElementAtIndex(0).objectReferenceValue as Sprite
                : null;
            if (firstFrame == null)
                throw new BuildFailedException("Gameplay Background is missing its first animation frame.");

            renderer.sprite = firstFrame;
            animation.enabled = animated;
            Debug.Log("Building with " + (animated ? "animated" : "still") + " gameplay background.");
            return;
        }

        throw new BuildFailedException("Gameplay scene is missing its Background object.");
    }
}
