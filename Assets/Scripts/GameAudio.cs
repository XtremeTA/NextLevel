using System;
using UnityEngine;

public enum SoundCue
{
    BallHit,
    BrickBreak,
    TileCrack,
    Parry
}

[DefaultExecutionOrder(-100)]
public sealed class GameAudio : MonoBehaviour
{
    [Serializable]
    private sealed class SoundEffect
    {
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float volume = 0.7f;
        [Range(0f, 0.15f)] public float pitchVariation = 0.035f;
        [Range(0f, 0.2f)] public float volumeVariation = 0.06f;
        [Min(0f)] public float minimumInterval = 0.04f;

        [NonSerialized] public double nextPlayTime;
        [NonSerialized] private int lastClipIndex = -1;

        public AudioClip NextClip(System.Random random)
        {
            if (clips == null || clips.Length == 0)
                return null;

            int index = random.Next(clips.Length);
            if (clips.Length > 1 && lastClipIndex >= 0)
            {
                // Pick from every clip except the one we just played.
                index = random.Next(clips.Length - 1);
                if (index >= lastClipIndex)
                    index++;
            }

            lastClipIndex = index;
            return clips[index];
        }
    }

    public static GameAudio Instance { get; private set; }

    [Header("Effects")]
    [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.85f;
    [SerializeField] private SoundEffect ballHit = new SoundEffect();
    [SerializeField] private SoundEffect brickBreak = new SoundEffect();
    [SerializeField] private SoundEffect tileCrack = new SoundEffect();
    [SerializeField] private SoundEffect parry = new SoundEffect();

    // Keep audio randomness independent of the ball's random bounce direction.
    private readonly System.Random random = new System.Random();
    private readonly AudioSource[] effectSources = new AudioSource[16];
    private readonly double[] busyUntil = new double[16];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        for (int i = 0; i < effectSources.Length; i++)
            effectSources[i] = CreateSource("Effect " + (i + 1));
    }

    private AudioSource CreateSource(string sourceName)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        return source;
    }

    public void Play(SoundCue cue)
    {
        SoundEffect effect = GetEffect(cue);
        double now = AudioSettings.dspTime;
        if (effect == null || now < effect.nextPlayTime)
            return;

        int voiceIndex = -1;
        for (int i = 0; i < effectSources.Length; i++)
        {
            if (now >= busyUntil[i])
            {
                voiceIndex = i;
                break;
            }
        }

        // Preserve currently playing effects when the pool is full.
        if (voiceIndex < 0)
            return;

        AudioClip clip = effect.NextClip(random);
        if (clip == null)
            return;

        AudioSource source = effectSources[voiceIndex];
        source.clip = clip;
        source.pitch = 1f + Variation(effect.pitchVariation);
        source.volume = Mathf.Clamp01(effectsVolume * effect.volume * (1f + Variation(effect.volumeVariation)));
        source.Play();
        effect.nextPlayTime = now + effect.minimumInterval;
        busyUntil[voiceIndex] = now + clip.length / source.pitch;
    }

    private float Variation(float amount)
    {
        return ((float)random.NextDouble() * 2f - 1f) * amount;
    }

    private SoundEffect GetEffect(SoundCue cue)
    {
        switch (cue)
        {
            case SoundCue.BallHit: return ballHit;
            case SoundCue.BrickBreak: return brickBreak;
            case SoundCue.TileCrack: return tileCrack;
            case SoundCue.Parry: return parry;
            default: return null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
