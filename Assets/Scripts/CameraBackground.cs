using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CameraBackground : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(0.1f)] private float distanceFromCamera = 20f;

    private SpriteRenderer backgroundRenderer;

    private void OnEnable()
    {
        backgroundRenderer = GetComponent<SpriteRenderer>();
        FitToCamera();
    }

    private void LateUpdate()
    {
        FitToCamera();
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
        // Fill the view without stretching the artwork; crop the excess at the edges.
        float scale = Mathf.Max(viewWidth / spriteBounds.size.x, viewHeight / spriteBounds.size.y);
        transform.localScale = new Vector3(scale, scale, 1f);
        transform.SetPositionAndRotation(
            targetCamera.transform.position + targetCamera.transform.forward * distanceFromCamera,
            targetCamera.transform.rotation);
        transform.position -= transform.TransformVector(spriteBounds.center);
    }
}
