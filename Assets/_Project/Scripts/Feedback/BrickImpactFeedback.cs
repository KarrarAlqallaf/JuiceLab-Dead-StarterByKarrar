using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Improvement 1: Brick Impact Effect.
/// Spawns a pooled, multi-layered Heavy & Powerful particle burst (heavy debris chunks + bright sparks)
/// at the exact position of a destroyed brick.
/// </summary>
public class BrickImpactFeedback : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private Material particleMaterial;

    [Header("Debris Settings")]
    [SerializeField] private int debrisCount = 18;
    [SerializeField] private float debrisMinSpeed = 4.5f;
    [SerializeField] private float debrisMaxSpeed = 10.5f;
    [SerializeField] private float debrisGravity = 2.6f;
    [SerializeField] private float debrisLifetime = 0.55f;

    [Header("Spark Settings")]
    [SerializeField] private int sparkCount = 14;
    [SerializeField] private float sparkSpeed = 13f;
    [SerializeField] private float sparkLifetime = 0.24f;

    private struct PooledBurst
    {
        public GameObject Root;
        public ParticleSystem DebrisSystem;
        public ParticleSystem SparkSystem;
        public float AvailableAtTime;
    }

    private readonly List<PooledBurst> pool = new List<PooledBurst>();
    private Material runtimeMaterial;
    private Texture2D squareTexture;

    private void Awake()
    {
        CreateFallbackMaterialIfNeeded();
        for (int i = 0; i < initialPoolSize; i++)
        {
            pool.Add(CreateBurstInstance(i));
        }
    }

    private void OnEnable()
    {
        GameEvents.BrickDestroyed += OnBrickDestroyed;
    }

    private void OnDisable()
    {
        GameEvents.BrickDestroyed -= OnBrickDestroyed;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
        if (squareTexture != null)
            Destroy(squareTexture);
    }

    private void OnBrickDestroyed(ImpactInfo info)
    {
        PooledBurst burst = GetAvailableBurst();

        Vector3 spawnPos = new Vector3(info.Center.x, info.Center.y, -0.2f);
        burst.Root.transform.position = spawnPos;
        burst.Root.SetActive(true);

        // Configure Debris System (heavy colored brick fragments)
        ParticleSystem.MainModule debrisMain = burst.DebrisSystem.main;
        Color primaryColor = info.Color;
        primaryColor.a = 1f;
        Color darkerColor = Color.Lerp(primaryColor, Color.black, 0.35f);
        darkerColor.a = 1f;
        debrisMain.startColor = new ParticleSystem.MinMaxGradient(darkerColor, primaryColor);

        ParticleSystem.ShapeModule debrisShape = burst.DebrisSystem.shape;
        debrisShape.scale = new Vector3(
            Mathf.Max(0.4f, info.Size.x * 0.85f),
            Mathf.Max(0.25f, info.Size.y * 0.85f),
            0.1f);

        // Bias velocity away from impact normal so debris blasts in the hit direction
        Vector2 impactDir = info.Normal.sqrMagnitude > 0.001f ? -info.Normal.normalized : Vector2.up;
        ParticleSystem.VelocityOverLifetimeModule vel = burst.DebrisSystem.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(impactDir.x * 2.2f - 1.2f, impactDir.x * 4.5f + 1.2f);
        vel.y = new ParticleSystem.MinMaxCurve(impactDir.y * 2.0f + 0.5f, impactDir.y * 4.2f + 2.8f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        burst.DebrisSystem.Clear();
        burst.DebrisSystem.Play();
        int scaledDebrisCount = Mathf.RoundToInt(debrisCount * Mathf.Lerp(0.85f, 1.35f, info.Strength));
        burst.DebrisSystem.Emit(scaledDebrisCount);

        // Position spark emitter at exact contact point for sharp impact punch
        burst.SparkSystem.transform.position = new Vector3(info.Point.x, info.Point.y, -0.25f);
        ParticleSystem.MainModule sparkMain = burst.SparkSystem.main;
        Color brightSpark = Color.Lerp(primaryColor, Color.white, 0.75f);
        brightSpark.a = 1f;
        sparkMain.startColor = new ParticleSystem.MinMaxGradient(Color.white, brightSpark);

        burst.SparkSystem.Clear();
        burst.SparkSystem.Play();
        burst.SparkSystem.Emit(sparkCount);
    }

    private PooledBurst GetAvailableBurst()
    {
        float now = Time.unscaledTime;
        for (int i = 0; i < pool.Count; i++)
        {
            if (now >= pool[i].AvailableAtTime || !pool[i].DebrisSystem.isPlaying)
            {
                PooledBurst item = pool[i];
                item.AvailableAtTime = now + debrisLifetime + 0.1f;
                pool[i] = item;
                return item;
            }
        }

        PooledBurst created = CreateBurstInstance(pool.Count);
        created.AvailableAtTime = now + debrisLifetime + 0.1f;
        pool.Add(created);
        return created;
    }

    private void CreateFallbackMaterialIfNeeded()
    {
        squareTexture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        squareTexture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        squareTexture.SetPixels(pixels);
        squareTexture.Apply();

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null && particleMaterial != null)
            shader = particleMaterial.shader;

        if (shader != null)
        {
            runtimeMaterial = new Material(shader);
            runtimeMaterial.mainTexture = squareTexture;
        }
        else if (particleMaterial != null)
        {
            runtimeMaterial = new Material(particleMaterial);
            runtimeMaterial.mainTexture = squareTexture;
        }
    }

    private PooledBurst CreateBurstInstance(int index)
    {
        GameObject root = new GameObject($"BrickImpactBurst_{index}");
        root.transform.SetParent(transform, false);
        root.SetActive(false);

        ParticleSystem debrisPs = root.AddComponent<ParticleSystem>();
        ConfigureDebrisSystem(debrisPs);

        GameObject sparkObj = new GameObject("Sparks");
        sparkObj.transform.SetParent(root.transform, false);
        ParticleSystem sparkPs = sparkObj.AddComponent<ParticleSystem>();
        ConfigureSparkSystem(sparkPs);

        return new PooledBurst
        {
            Root = root,
            DebrisSystem = debrisPs,
            SparkSystem = sparkPs,
            AvailableAtTime = 0f
        };
    }

    private void ConfigureDebrisSystem(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = debrisLifetime;
        main.startLifetime = new ParticleSystem.MinMaxCurve(debrisLifetime * 0.65f, debrisLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(debrisMinSpeed, debrisMaxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.32f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = debrisGravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;
        main.stopAction = ParticleSystemStopAction.Disable;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.5f, 0.5f, 0.1f);

        ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-10f, 10f);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.65f, 0.85f),
            new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        if (runtimeMaterial != null)
            psr.sharedMaterial = runtimeMaterial;
        psr.sortingOrder = 10;
    }

    private void ConfigureSparkSystem(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = sparkLifetime;
        main.startLifetime = new ParticleSystem.MinMaxCurve(sparkLifetime * 0.5f, sparkLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(sparkSpeed * 0.5f, sparkSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.gravityModifier = 0.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 32;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sparkCurve = new AnimationCurve(
            new Keyframe(0f, 1.2f),
            new Keyframe(0.3f, 0.7f),
            new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sparkCurve);

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Stretch;
        psr.velocityScale = 0.035f;
        psr.lengthScale = 1.8f;
        if (runtimeMaterial != null)
            psr.sharedMaterial = runtimeMaterial;
        psr.sortingOrder = 12;
    }
}

