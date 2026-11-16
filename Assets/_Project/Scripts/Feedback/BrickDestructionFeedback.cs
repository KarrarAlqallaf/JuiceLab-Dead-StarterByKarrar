using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Improvement 5B: Brick Disappearance Animation.
/// Spawns a pooled visual shatter/flash ghost at the destroyed brick's exact center and size
/// that briefly expands with a bright impact flash before collapsing cleanly.
/// </summary>
public class BrickDestructionFeedback : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Sprite brickSprite;
    [SerializeField] private Material spriteMaterial;
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private float animationDuration = 0.14f;
    [SerializeField] private Vector2 peakScaleMultiplier = new Vector2(1.22f, 1.28f);
    [SerializeField] private Vector2 endScaleMultiplier = new Vector2(1.45f, 0.05f);

    private class GhostItem
    {
        public GameObject GameObject;
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector2 BaseSize;
        public Color BaseColor;
        public float Timer;
        public float TiltSpeed;
        public bool Active;
    }

    private readonly List<GhostItem> pool = new List<GhostItem>();
    private Sprite fallbackSprite;

    private void Awake()
    {
        if (brickSprite == null)
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] cols = new Color[16];
            for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
            tex.SetPixels(cols);
            tex.Apply();
            fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            brickSprite = fallbackSprite;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            pool.Add(CreateGhostInstance(i));
        }
    }

    private void OnEnable()
    {
        GameEvents.BrickDestroyed += OnBrickDestroyed;
    }

    private void OnDisable()
    {
        GameEvents.BrickDestroyed -= OnBrickDestroyed;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].GameObject != null)
                pool[i].GameObject.SetActive(false);
            pool[i].Active = false;
        }
    }

    private void OnBrickDestroyed(ImpactInfo info)
    {
        GhostItem item = GetAvailableGhost();
        item.Active = true;
        item.Timer = 0f;
        item.BaseSize = info.Size;
        item.BaseColor = info.Color;
        item.TiltSpeed = (info.Point.x >= info.Center.x ? -1f : 1f) * 95f;

        item.Transform.position = new Vector3(info.Center.x, info.Center.y, -0.05f);
        item.Transform.localRotation = Quaternion.identity;
        item.Transform.localScale = new Vector3(
            info.Size.x * peakScaleMultiplier.x,
            info.Size.y * peakScaleMultiplier.y,
            1f);

        item.Renderer.color = Color.white;
        item.GameObject.SetActive(true);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float duration = Mathf.Max(0.01f, animationDuration);

        for (int i = 0; i < pool.Count; i++)
        {
            GhostItem item = pool[i];
            if (!item.Active) continue;

            item.Timer += dt;
            float t = item.Timer / duration;

            if (t >= 1f)
            {
                item.Active = false;
                item.GameObject.SetActive(false);
                continue;
            }

            Vector2 mult = Vector2.Lerp(peakScaleMultiplier, endScaleMultiplier, t * t);
            item.Transform.localScale = new Vector3(
                item.BaseSize.x * mult.x,
                item.BaseSize.y * mult.y,
                1f);
            item.Transform.localRotation = Quaternion.Euler(0f, 0f, item.TiltSpeed * item.Timer);

            Color c = Color.Lerp(Color.white, item.BaseColor, Mathf.Clamp01(t * 2.2f));
            c.a = 1f - t;
            item.Renderer.color = c;
        }
    }

    private GhostItem GetAvailableGhost()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].Active)
                return pool[i];
        }

        GhostItem created = CreateGhostInstance(pool.Count);
        pool.Add(created);
        return created;
    }

    private GhostItem CreateGhostInstance(int index)
    {
        GameObject go = new GameObject($"BrickShatterGhost_{index}");
        go.transform.SetParent(transform, false);
        go.SetActive(false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = brickSprite;
        if (spriteMaterial != null)
            sr.sharedMaterial = spriteMaterial;
        sr.sortingOrder = 5;

        return new GhostItem
        {
            GameObject = go,
            Transform = go.transform,
            Renderer = sr,
            Active = false
        };
    }
}

