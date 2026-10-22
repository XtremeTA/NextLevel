using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class BallVisuals : MonoBehaviour
{
    [SerializeField] private Material trailMaterial;
    [SerializeField, Min(0.01f)] private float trailDuration = 0.22f;
    [SerializeField, Min(0.01f)] private float trailWidth = 0.32f;
    [SerializeField, Min(0.01f)] private float impactDuration = 0.18f;

    private TrailRenderer trail;
    private SpriteRenderer ballSprite;
    private SpriteRenderer impact;
    private Sprite impactSprite;
    private MaterialPropertyBlock glowProperties;
    private Color baseGlow;
    private float impactElapsed;
    private bool perfectImpact;
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        ballSprite = GetComponent<SpriteRenderer>();
        glowProperties = new MaterialPropertyBlock();
        baseGlow = ballSprite.sharedMaterial.GetColor(ColorId);
        GameObject trailObject = new GameObject("Green Trail");
        trailObject.layer = gameObject.layer;
        trailObject.transform.SetParent(transform, false);
        trail = trailObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = trailMaterial;
        trail.time = trailDuration;
        trail.minVertexDistance = 0.025f;
        trail.widthMultiplier = trailWidth;
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        trail.numCapVertices = 4;
        trail.numCornerVertices = 4;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.sortingLayerID = ballSprite.sortingLayerID;
        trail.sortingOrder = ballSprite.sortingOrder - 1;

        // The material supplies the HDR green; vertex alpha fades the tail out.
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.35f, 0.5f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = fade;

        GameObject impactObject = new GameObject("Parry Impact");
        impact = impactObject.AddComponent<SpriteRenderer>();
        impactSprite = EffectSprites.CreateRing();
        impact.sprite = impactSprite;
        impact.sharedMaterial = ballSprite.sharedMaterial;
        impact.sortingLayerID = ballSprite.sortingLayerID;
        impact.sortingOrder = ballSprite.sortingOrder + 1;
        impact.enabled = false;
    }

    private void OnEnable()
    {
        if (trail == null)
            return;

        trail.Clear();
        trail.emitting = true;
    }

    private void OnDisable()
    {
        if (trail == null)
            return;

        trail.emitting = false;
        trail.Clear();
        if (impact != null)
            impact.enabled = false;
    }

    public void ClearTrail()
    {
        if (trail != null)
            trail.Clear();
        SetPower(0f);
        if (impact != null)
            impact.enabled = false;
    }

    public void SetPower(float strength)
    {
        if (trail == null)
            return;

        strength = Mathf.Clamp01(strength);
        trail.widthMultiplier = trailWidth * Mathf.Lerp(1f, 1.35f, strength);
        trail.time = trailDuration * Mathf.Lerp(1f, 1.4f, strength);
        Color bright = Color.Lerp(Color.white, new Color(1.8f, 1.8f, 1.2f), strength);
        trail.startColor = new Color(bright.r, bright.g, bright.b, 0.8f);
        trail.endColor = new Color(bright.r, bright.g, bright.b, 0f);
        ballSprite.GetPropertyBlock(glowProperties);
        Color glow = baseGlow * Mathf.Lerp(1f, 1.6f, strength);
        glow.a = baseGlow.a;
        glowProperties.SetColor(ColorId, glow);
        ballSprite.SetPropertyBlock(glowProperties);
    }

    public void ShowParryImpact(bool perfect)
    {
        perfectImpact = perfect;
        impactElapsed = 0f;
        impact.transform.position = transform.position;
        impact.transform.localScale = Vector3.one * 0.3f;
        impact.color = new Color(1f, 1f, 1f, perfect ? 0.9f : 0.55f);
        impact.enabled = true;
    }

    private void Update()
    {
        if (impact == null || !impact.enabled)
            return;

        float progress = Mathf.Clamp01(impactElapsed / (impactDuration * (perfectImpact ? 1.2f : 1f)));
        impact.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, perfectImpact ? 1.5f : 1.05f, progress);
        impact.color = new Color(1f, 1f, 1f, (perfectImpact ? 0.9f : 0.55f) * (1f - progress) * (1f - progress));
        if (progress >= 1f)
            impact.enabled = false;
        impactElapsed += Time.deltaTime;
    }

    private void OnDestroy()
    {
        if (impact != null)
            Destroy(impact.gameObject);
        if (impactSprite != null)
        {
            Destroy(impactSprite.texture);
            Destroy(impactSprite);
        }
    }
}
