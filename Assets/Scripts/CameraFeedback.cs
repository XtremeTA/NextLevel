using UnityEngine;

[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CameraFeedback : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float bumpDuration = 0.14f;
    [SerializeField, Range(0f, 0.1f)] private float bumpDistance = 0.035f;

    private Camera view;
    private float elapsed;
    private float strength;
    private bool bumping;

    private void Awake()
    {
        view = GetComponent<Camera>();
    }

    public void Pulse(bool perfect)
    {
        elapsed = 0f;
        strength = perfect ? 1.4f : 1f;
        bumping = true;
    }

    private void LateUpdate()
    {
        if (!bumping)
            return;

        float progress = Mathf.Clamp01(elapsed / bumpDuration);
        if (progress >= 1f || Time.timeScale <= 0f || (GameManager.Instance != null && GameManager.Instance.IsGameOver))
        {
            RestoreProjection();
            return;
        }

        // Shift only the rendered view: camera transforms and gameplay bounds stay fixed.
        view.ResetProjectionMatrix();
        Matrix4x4 projection = view.projectionMatrix;
        float envelope = (1f - progress) * (1f - progress) * strength;
        float halfHeight = view.orthographicSize;
        float halfWidth = halfHeight * view.aspect;
        float offsetX = Mathf.Sin(progress * Mathf.PI * 4f) * bumpDistance * envelope;
        float offsetY = Mathf.Cos(progress * Mathf.PI * 5f) * bumpDistance * envelope;
        float zoom = 1f + 0.007f * envelope;
        projection.m00 *= zoom;
        projection.m11 *= zoom;
        projection.m03 += offsetX / halfWidth;
        projection.m13 += offsetY / halfHeight;
        view.projectionMatrix = projection;
        elapsed += Time.deltaTime;
    }

    private void OnDisable()
    {
        RestoreProjection();
    }

    private void RestoreProjection()
    {
        if (view != null)
            view.ResetProjectionMatrix();
        bumping = false;
    }
}
