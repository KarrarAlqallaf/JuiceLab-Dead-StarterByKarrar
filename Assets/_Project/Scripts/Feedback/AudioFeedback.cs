using UnityEngine;

/// <summary>
/// Improvement 3: Event-Based Audio Feedback.
/// Plays distinct, heavy, balanced sound effects for Paddle Contact, Brick Destruction, and Ball Loss
/// (plus subtle wall impact feedback) with re-trigger cooldowns to prevent overlapping/clipping.
/// </summary>
public class AudioFeedback : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip paddleHitClip;
    [SerializeField] private AudioClip brickDestroyClip;
    [SerializeField] private AudioClip ballLostClip;
    [SerializeField] private AudioClip wallHitClip;

    [Header("Dedicated Audio Sources")]
    [SerializeField] private AudioSource paddleSource;
    [SerializeField] private AudioSource brickSource;
    [SerializeField] private AudioSource ballLostSource;
    [SerializeField] private AudioSource wallSource;

    [Header("Mix & Cooldown Settings")]
    [Range(0f, 1f)] [SerializeField] private float paddleVolume = 0.85f;
    [Range(0f, 1f)] [SerializeField] private float brickVolume = 0.90f;
    [Range(0f, 1f)] [SerializeField] private float ballLostVolume = 0.95f;
    [Range(0f, 1f)] [SerializeField] private float wallVolume = 0.45f;
    [SerializeField] private float minRetriggerInterval = 0.035f;

    private float lastPaddleSoundTime = -10f;
    private float lastBrickSoundTime = -10f;
    private float lastBallLostSoundTime = -10f;
    private float lastWallSoundTime = -10f;

    private void Awake()
    {
        EnsureAudioSources();
        EnsureAudioClips();
    }

    private void OnEnable()
    {
        GameEvents.BallHitPaddle += OnBallHitPaddle;
        GameEvents.BrickDestroyed += OnBrickDestroyed;
        GameEvents.BallLost += OnBallLost;
        GameEvents.BallHitWall += OnBallHitWall;
    }

    private void OnDisable()
    {
        GameEvents.BallHitPaddle -= OnBallHitPaddle;
        GameEvents.BrickDestroyed -= OnBrickDestroyed;
        GameEvents.BallLost -= OnBallLost;
        GameEvents.BallHitWall -= OnBallHitWall;
    }

    private void OnBallHitPaddle(ImpactInfo info)
    {
        float now = Time.unscaledTime;
        if (now - lastPaddleSoundTime < minRetriggerInterval) return;
        lastPaddleSoundTime = now;

        if (paddleSource != null && paddleHitClip != null)
        {
            paddleSource.pitch = Random.Range(0.95f, 1.04f);
            paddleSource.volume = paddleVolume * Mathf.Lerp(0.85f, 1f, info.Strength);
            paddleSource.Stop();
            paddleSource.clip = paddleHitClip;
            paddleSource.Play();
        }
    }

    private void OnBrickDestroyed(ImpactInfo info)
    {
        float now = Time.unscaledTime;
        if (now - lastBrickSoundTime < minRetriggerInterval) return;
        lastBrickSoundTime = now;

        if (brickSource != null && brickDestroyClip != null)
        {
            // Subtle combo pitch escalation while keeping low-end punch heavy
            int clampedCombo = Mathf.Clamp(info.Combo - 1, 0, 8);
            brickSource.pitch = 0.96f + clampedCombo * 0.035f + Random.Range(-0.015f, 0.015f);
            brickSource.volume = brickVolume;
            brickSource.Stop();
            brickSource.clip = brickDestroyClip;
            brickSource.Play();
        }
    }

    private void OnBallLost()
    {
        float now = Time.unscaledTime;
        if (now - lastBallLostSoundTime < 0.1f) return;
        lastBallLostSoundTime = now;

        if (ballLostSource != null && ballLostClip != null)
        {
            ballLostSource.pitch = 1.0f;
            ballLostSource.volume = ballLostVolume;
            ballLostSource.Stop();
            ballLostSource.clip = ballLostClip;
            ballLostSource.Play();
        }
    }

    private void OnBallHitWall(ImpactInfo info)
    {
        float now = Time.unscaledTime;
        if (now - lastWallSoundTime < minRetriggerInterval) return;
        lastWallSoundTime = now;

        if (wallSource != null && wallHitClip != null)
        {
            wallSource.pitch = Random.Range(0.94f, 1.04f);
            wallSource.volume = wallVolume;
            wallSource.Stop();
            wallSource.clip = wallHitClip;
            wallSource.Play();
        }
    }

    private void EnsureAudioSources()
    {
        if (paddleSource == null)
            paddleSource = CreateChannelSource("PaddleAudioSource");
        if (brickSource == null)
            brickSource = CreateChannelSource("BrickAudioSource");
        if (ballLostSource == null)
            ballLostSource = CreateChannelSource("BallLostAudioSource");
        if (wallSource == null)
            wallSource = CreateChannelSource("WallAudioSource");
    }

    private AudioSource CreateChannelSource(string channelName)
    {
        GameObject child = new GameObject(channelName);
        child.transform.SetParent(transform, false);
        AudioSource src = child.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        return src;
    }

    private void EnsureAudioClips()
    {
        // Procedural fallback if clips are ever missing
        if (paddleHitClip == null)
            paddleHitClip = CreateSynthesizedClip("PaddleHit_Synth", 0.20f, 130f, 45f, 0.2f);
        if (brickDestroyClip == null)
            brickDestroyClip = CreateSynthesizedClip("BrickDestroy_Synth", 0.28f, 160f, 40f, 0.45f);
        if (ballLostClip == null)
            ballLostClip = CreateSynthesizedClip("BallLost_Synth", 0.55f, 150f, 32f, 0.08f);
        if (wallHitClip == null)
            wallHitClip = CreateSynthesizedClip("WallHit_Synth", 0.12f, 105f, 44f, 0.12f);
    }

    private static AudioClip CreateSynthesizedClip(string name, float duration, float startFreq, float endFreq, float noiseAmount)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[sampleCount];
        float phase = 0f;
        System.Random rng = new System.Random(name.GetHashCode());

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float normT = t / duration;
            float freq = Mathf.Lerp(startFreq, endFreq, Mathf.Sqrt(normT));
            phase += 2f * Mathf.PI * freq / sampleRate;

            float env = Mathf.Exp(-normT * 4.5f);
            float tone = Mathf.Sin(phase) * 0.8f + Mathf.Sin(phase * 2f) * 0.2f;
            float noise = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-normT * 12f) * noiseAmount;
            data[i] = Mathf.Clamp((tone + noise) * env, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

