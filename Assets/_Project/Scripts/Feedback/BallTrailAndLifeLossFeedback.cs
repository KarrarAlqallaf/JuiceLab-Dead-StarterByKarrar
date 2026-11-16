using TMPro;
using UnityEngine;

/// <summary>
/// Improvement 5C: Heavy Ball Motion Trail & Life-Loss Visual Cue.
/// Adds a dynamic tapered TrailRenderer to the Ball's Visual child (clearing cleanly on BallLost
/// so respawn never streaks) and plays a restrained crimson screen/HUD pulse when a life is lost.
/// </summary>
public class BallTrailAndLifeLossFeedback : MonoBehaviour
{
    [Header("Ball Trail References")]
    [SerializeField] private Transform ballVisual;
    [SerializeField] private Material trailMaterial;
    [SerializeField] private float trailTime = 0.18f;
    [SerializeField] private float trailStartWidth = 0.38f;

    [Header("Life Loss Feedback References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private Color lifeLossPulseColor = new Color(0.28f, 0.06f, 0.11f, 1f);
    [SerializeField] private Color livesTextFlashColor = new Color(1f, 0.32f, 0.38f, 1f);
    [SerializeField] private float lifeLossDuration = 0.35f;

    private TrailRenderer trailRenderer;
    private Color baseCameraBgColor;
    private bool cameraColorCached;

    private RectTransform livesRect;
    private Vector3 baseLivesScale = Vector3.one;
    private Color baseLivesColor = Color.white;

    private float lifeLossTimer = -1f;
    private int clearTrailFramesRemaining = 0;

    private void Awake()
    {
        SetupBallTrail();

        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera != null)
        {
            baseCameraBgColor = targetCamera.backgroundColor;
            cameraColorCached = true;
        }

        if (livesText != null)
        {
            livesRect = livesText.rectTransform;
            baseLivesScale = livesRect.localScale;
            baseLivesColor = livesText.color;
        }
    }

    private void OnEnable()
    {
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = true;
        }

        GameEvents.BallLost += OnBallLost;
        GameEvents.BrickDestroyed += OnBrickDestroyed;
    }

    private void OnDisable()
    {
        GameEvents.BallLost -= OnBallLost;
        GameEvents.BrickDestroyed -= OnBrickDestroyed;

        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
            trailRenderer.Clear();
        }

        if (targetCamera != null && cameraColorCached)
            targetCamera.backgroundColor = baseCameraBgColor;

        if (livesRect != null)
            livesRect.localScale = baseLivesScale;
        if (livesText != null)
            livesText.color = baseLivesColor;
    }

    private void SetupBallTrail()
    {
        if (ballVisual == null) return;

        trailRenderer = ballVisual.GetComponent<TrailRenderer>();
        if (trailRenderer == null)
            trailRenderer = ballVisual.gameObject.AddComponent<TrailRenderer>();

        trailRenderer.time = trailTime;
        trailRenderer.minVertexDistance = 0.05f;
        trailRenderer.numCornerVertices = 4;
        trailRenderer.numCapVertices = 4;
        trailRenderer.sortingOrder = -1;

        AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0f, trailStartWidth),
            new Keyframe(0.5f, trailStartWidth * 0.45f),
            new Keyframe(1f, 0f));
        trailRenderer.widthCurve = widthCurve;

        ApplyTrailGradient(Color.white);

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            trailRenderer.sharedMaterial = new Material(shader);
        else if (trailMaterial != null)
            trailRenderer.sharedMaterial = trailMaterial;
    }

    private void ApplyTrailGradient(Color headTint)
    {
        if (trailRenderer == null) return;

        Gradient grad = new Gradient();
        Color startCol = Color.Lerp(Color.white, headTint, 0.45f);
        Color midCol = new Color(0.55f, 0.82f, 1f, 1f);
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(startCol, 0f),
                new GradientColorKey(midCol, 0.5f),
                new GradientColorKey(midCol, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.65f, 0f),
                new GradientAlphaKey(0.25f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        trailRenderer.colorGradient = grad;
    }

    private void OnBrickDestroyed(ImpactInfo info)
    {
        ApplyTrailGradient(info.Color);
    }

    private void OnBallLost()
    {
        lifeLossTimer = 0f;
        ApplyTrailGradient(Color.white);

        // Clear trail across the respawn frame so it does not draw a line from DeathZone to startPosition
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
            trailRenderer.Clear();
            clearTrailFramesRemaining = 2;
        }
    }

    private void LateUpdate()
    {
        if (clearTrailFramesRemaining > 0)
        {
            clearTrailFramesRemaining--;
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                if (clearTrailFramesRemaining == 0)
                    trailRenderer.emitting = true;
            }
        }

        if (lifeLossTimer >= 0f)
        {
            lifeLossTimer += Time.deltaTime;
            float t = lifeLossTimer / Mathf.Max(0.01f, lifeLossDuration);

            if (t >= 1f)
            {
                lifeLossTimer = -1f;
                if (targetCamera != null && cameraColorCached)
                    targetCamera.backgroundColor = baseCameraBgColor;
                if (livesRect != null)
                    livesRect.localScale = baseLivesScale;
                if (livesText != null)
                    livesText.color = baseLivesColor;
            }
            else
            {
                float pulse = Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f);
                if (targetCamera != null && cameraColorCached)
                    targetCamera.backgroundColor = Color.Lerp(baseCameraBgColor, lifeLossPulseColor, pulse);

                if (livesRect != null)
                {
                    float scaleOffset = Mathf.Sin(t * Mathf.PI * 3f) * (1f - t) * 0.25f;
                    livesRect.localScale = baseLivesScale * (1f + scaleOffset);
                }

                if (livesText != null)
                    livesText.color = Color.Lerp(livesTextFlashColor, baseLivesColor, t);
            }
        }
    }
}

