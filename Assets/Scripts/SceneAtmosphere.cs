using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class SceneAtmosphere : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Material hazeMaterial;
    [SerializeField] private Material moteMaterial;
    [SerializeField, Range(0f, 0.2f)] private float hazeOpacity = 0.085f;
    [SerializeField] private Vector2 hazeDrift = new Vector2(0.018f, 0.006f);
    [SerializeField, Range(0, 64)] private int moteCount = 24;
    [SerializeField, Range(0f, 0.5f)] private float moteOpacity = 0.22f;

    private struct Mote
    {
        public SpriteRenderer renderer;
        public float x, y, speed, phase, size;
    }

    private MeshRenderer haze;
    private Mesh hazeMesh;
    private Sprite moteSprite;
    private Mote[] motes;
    private float elapsed;
    private MaterialPropertyBlock properties;
    private static readonly int TimeId = Shader.PropertyToID("_AtmosphereTime");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int DriftId = Shader.PropertyToID("_Drift");
    private static readonly int TintId = Shader.PropertyToID("_Tint");

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (hazeMaterial == null || moteMaterial == null)
        {
            Debug.LogError("Assign the haze and mote materials on Scene Atmosphere.", this);
            enabled = false;
            return;
        }

        GameObject hazeObject = new GameObject("Drifting Haze");
        hazeObject.transform.SetParent(transform, false);
        hazeMesh = new Mesh { name = "Atmosphere quad" };
        hazeMesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
        hazeMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        hazeMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        hazeMesh.RecalculateBounds();
        hazeObject.AddComponent<MeshFilter>().sharedMesh = hazeMesh;
        haze = hazeObject.AddComponent<MeshRenderer>();
        haze.sharedMaterial = hazeMaterial;
        haze.sortingOrder = -50;
        haze.shadowCastingMode = ShadowCastingMode.Off;
        haze.receiveShadows = false;
        properties = new MaterialPropertyBlock();

        moteSprite = EffectSprites.CreateSoftDot();
        motes = new Mote[Mathf.Clamp(moteCount, 0, 64)];
        System.Random random = new System.Random(78249);
        for (int i = 0; i < motes.Length; i++)
        {
            GameObject mote = new GameObject("Drifting Mote " + (i + 1));
            mote.transform.SetParent(transform, false);
            SpriteRenderer renderer = mote.AddComponent<SpriteRenderer>();
            renderer.sprite = moteSprite;
            renderer.sharedMaterial = moteMaterial;
            renderer.sortingOrder = -20;
            motes[i] = new Mote
            {
                renderer = renderer,
                x = (float)random.NextDouble(),
                y = (float)random.NextDouble(),
                speed = Mathf.Lerp(0.003f, 0.007f, (float)random.NextDouble()),
                phase = (float)random.NextDouble() * Mathf.PI * 2f,
                size = Mathf.Lerp(0.04f, 0.09f, (float)random.NextDouble())
            };
        }
        FitToCamera();
    }

    private void Update()
    {
        if (Time.timeScale > 0f && GameManager.Instance != null && !GameManager.Instance.IsGameOver)
            elapsed += Time.deltaTime;
    }

    private void LateUpdate()
    {
        FitToCamera();
    }

    private void FitToCamera()
    {
        if (targetCamera == null || !targetCamera.orthographic || haze == null)
            return;

        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;
        Vector3 center = targetCamera.transform.position;
        haze.transform.SetPositionAndRotation(center + targetCamera.transform.forward * 15f, targetCamera.transform.rotation);
        haze.transform.localScale = new Vector3(width * 1.02f, height * 1.02f, 1f);
        Color tint = hazeMaterial.GetColor(TintId);
        tint.a = hazeOpacity;
        properties.SetColor(TintId, tint);
        properties.SetFloat(TimeId, elapsed);
        properties.SetFloat(AspectId, targetCamera.aspect);
        properties.SetVector(DriftId, new Vector4(hazeDrift.x, hazeDrift.y, 0f, 0f));
        haze.SetPropertyBlock(properties);

        foreach (Mote mote in motes)
        {
            float x = Mathf.Repeat(mote.x + Mathf.Sin(elapsed * 0.23f + mote.phase) * 0.008f, 1f);
            float y = Mathf.Repeat(mote.y + elapsed * mote.speed, 1f);
            Vector3 position = center + targetCamera.transform.forward * 14f;
            position += targetCamera.transform.right * ((x - 0.5f) * width);
            position += targetCamera.transform.up * ((y - 0.5f) * height);
            mote.renderer.transform.SetPositionAndRotation(position, targetCamera.transform.rotation);
            mote.renderer.transform.localScale = Vector3.one * mote.size;
            mote.renderer.color = new Color(1f, 0.8f, 0.48f, moteOpacity * (0.75f + 0.25f * Mathf.Sin(elapsed * 0.7f + mote.phase)));
        }
    }

    private void OnDisable()
    {
        if (haze != null)
            haze.enabled = false;
        if (motes != null)
            foreach (Mote mote in motes) mote.renderer.enabled = false;
    }

    private void OnEnable()
    {
        if (haze != null)
            haze.enabled = true;
        if (motes != null)
            foreach (Mote mote in motes) mote.renderer.enabled = true;
    }

    private void OnDestroy()
    {
        if (hazeMesh != null)
            Destroy(hazeMesh);
        if (moteSprite != null)
        {
            Destroy(moteSprite.texture);
            Destroy(moteSprite);
        }
    }
}
