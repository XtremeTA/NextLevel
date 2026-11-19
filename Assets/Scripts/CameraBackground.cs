using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CameraBackground : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(0.1f)] private float distanceFromCamera = 20f;
    [Tooltip("Requested shift as a fraction of the camera width. Negative moves the art left; the shift is limited to prevent empty edges.")]
    [SerializeField, Range(-0.5f, 0.5f)] private float horizontalOffset = -0.25f;
    [Header("Stages")]
    [SerializeField] private Color stageTwoTint = new Color(0.62f, 0.66f, 0.74f, 1f);
    [SerializeField, Min(0.01f)] private float stageTintDuration = 0.8f;

    private SpriteRenderer backgroundRenderer;
    private Color stageOneTint = Color.white;

    private void OnEnable()
    {
        backgroundRenderer = GetComponent<SpriteRenderer>();
        stageOneTint = backgroundRenderer.color;
        FitToCamera();
    }

    private void LateUpdate()
    {
        UpdateStageTint();
        FitToCamera();
    }

    private void UpdateStageTint()
    {
        if (!Application.isPlaying || backgroundRenderer == null)
            return;

        Color target = GameManager.Instance != null && GameManager.Instance.CurrentStage >= 2
            ? stageTwoTint
            : stageOneTint;
        float amount = Mathf.Clamp01(Time.unscaledDeltaTime / Mathf.Max(0.01f, stageTintDuration));
        backgroundRenderer.color = Color.Lerp(backgroundRenderer.color, target, amount);
    }

    private void FitToCamera()
    {
        if (targetCamera == null || !targetCamera.orthographic ||
            backgroundRenderer == null || backgroundRenderer.sprite == null)
            return;

        Bounds spriteBounds = backgroundRenderer.sprite.bounds;
        if (spriteBounds.size.x <= 0f || spriteBounds.size.y <= 0f)
            return;

        float viewHeight = targetCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * targetCamera.aspect;
        // Use the smallest uniform scale that covers the camera, then pan only
        // within the remaining width so neither side can reveal empty space.
        float scale = Mathf.Max(viewWidth / spriteBounds.size.x, viewHeight / spriteBounds.size.y);
        float excessWidth = spriteBounds.size.x * scale - viewWidth;
        float horizontalShift = Mathf.Clamp(viewWidth * horizontalOffset,
            -excessWidth * 0.5f, excessWidth * 0.5f);
        float excessHeight = spriteBounds.size.y * scale - viewHeight;
        transform.localScale = new Vector3(scale, scale, 1f);
        transform.SetPositionAndRotation(
            targetCamera.transform.position + targetCamera.transform.forward * distanceFromCamera
            + targetCamera.transform.right * horizontalShift
            - targetCamera.transform.up * (excessHeight * 0.5f),
            targetCamera.transform.rotation);
        transform.position -= transform.TransformVector(spriteBounds.center);
    }
}
