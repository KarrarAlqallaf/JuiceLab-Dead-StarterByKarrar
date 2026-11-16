using UnityEngine;

/// <summary>
/// Improvement 2: Reactive Collision Animation.
/// Animates only the child Visual transforms of Paddle and Ball (squash & stretch, recoil, and impact flash)
/// without modifying colliders, physical dimensions, or physics behavior.
/// </summary>
public class CollisionReactionFeedback : MonoBehaviour
{
    [Header("Visual References (Child Visual Objects Only)")]
    [SerializeField] private Transform paddleVisual;
    [SerializeField] private SpriteRenderer paddleRenderer;
    [SerializeField] private Transform ballVisual;
    [SerializeField] private SpriteRenderer ballRenderer;

    [Header("Paddle Heavy Recoil & Squash")]
    [SerializeField] private Vector2 paddleSquashScale = new Vector2(1.28f, 0.58f);
    [SerializeField] private Vector2 paddleReboundScale = new Vector2(0.94f, 1.12f);
    [SerializeField] private float paddleRecoilDistance = 0.22f;
    [SerializeField] private float paddleAnimDuration = 0.22f;
    [SerializeField] private Color paddleFlashColor = new Color(0.65f, 0.92f, 1f, 1f);

    [Header("Ball Heavy Impact Deformation")]
    [SerializeField] private Vector2 ballSquashScale = new Vector2(1.42f, 0.62f);
    [SerializeField] private Vector2 ballReboundScale = new Vector2(0.88f, 1.16f);
    [SerializeField] private float ballAnimDuration = 0.16f;
    [SerializeField] private Color ballFlashColor = new Color(1f, 0.92f, 0.55f, 1f);

    private Vector3 paddleBaseScale = Vector3.one;
    private Vector3 paddleBaseLocalPos = Vector3.zero;
    private Color paddleBaseColor = Color.white;
    private float paddleTimer = -1f;
    private Vector2 paddleRecoilDir = Vector2.down;

    private Vector3 ballBaseScale = Vector3.one;
    private Quaternion ballBaseRotation = Quaternion.identity;
    private Color ballBaseColor = Color.white;
    private float ballTimer = -1f;
    private float ballImpactAngle = 0f;

    private void Awake()
    {
        CacheBaseStates();
    }

    private void CacheBaseStates()
    {
        if (paddleVisual != null)
        {
            paddleBaseScale = paddleVisual.localScale;
            paddleBaseLocalPos = paddleVisual.localPosition;
            if (paddleRenderer == null)
                paddleRenderer = paddleVisual.GetComponent<SpriteRenderer>();
        }
        if (paddleRenderer != null)
            paddleBaseColor = paddleRenderer.color;

        if (ballVisual != null)
        {
            ballBaseScale = ballVisual.localScale;
            ballBaseRotation = ballVisual.localRotation;
            if (ballRenderer == null)
                ballRenderer = ballVisual.GetComponent<SpriteRenderer>();
        }
        if (ballRenderer != null)
            ballBaseColor = ballRenderer.color;
    }

    private void OnEnable()
    {
        GameEvents.BallHitPaddle += OnBallHitPaddle;
        GameEvents.BallHitWall += OnBallHitWall;
        GameEvents.BrickDestroyed += OnBrickDestroyed;
    }

    private void OnDisable()
    {
        GameEvents.BallHitPaddle -= OnBallHitPaddle;
        GameEvents.BallHitWall -= OnBallHitWall;
        GameEvents.BrickDestroyed -= OnBrickDestroyed;

        RestoreImmediate();
    }

    private void OnBallHitPaddle(ImpactInfo info)
    {
        // Trigger Paddle heavy squash and downward recoil
        paddleTimer = 0f;
        paddleRecoilDir = new Vector2(0f, -1f);

        // Trigger Ball impact deformation
        TriggerBallDeformation(info.Normal, 1f);
    }

    private void OnBallHitWall(ImpactInfo info)
    {
        TriggerBallDeformation(info.Normal, 0.8f);
    }

    private void OnBrickDestroyed(ImpactInfo info)
    {
        TriggerBallDeformation(info.Normal, 1.1f);
    }

    private void TriggerBallDeformation(Vector2 normal, float intensity)
    {
        ballTimer = 0f;
        if (normal.sqrMagnitude > 0.001f)
        {
            ballImpactAngle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
        }
        else
        {
            ballImpactAngle = 0f;
        }
    }

    private void Update()
    {
        UpdatePaddleAnimation();
        UpdateBallAnimation();
    }

    private void UpdatePaddleAnimation()
    {
        if (paddleTimer < 0f || paddleVisual == null) return;

        paddleTimer += Time.deltaTime;
        float t = paddleTimer / Mathf.Max(0.01f, paddleAnimDuration);

        if (t >= 1f)
        {
            paddleTimer = -1f;
            paddleVisual.localScale = paddleBaseScale;
            paddleVisual.localPosition = paddleBaseLocalPos;
            if (paddleRenderer != null)
                paddleRenderer.color = paddleBaseColor;
            return;
        }

        // 3-stage heavy impact curve: rapid compression -> elastic rebound -> smooth settle
        Vector2 scaleMultiplier;
        if (t < 0.22f)
        {
            float p = t / 0.22f;
            float ease = 1f - (1f - p) * (1f - p);
            scaleMultiplier = Vector2.Lerp(Vector2.one, paddleSquashScale, ease);
        }
        else if (t < 0.58f)
        {
            float p = (t - 0.22f) / 0.36f;
            scaleMultiplier = Vector2.Lerp(paddleSquashScale, paddleReboundScale, Mathf.SmoothStep(0f, 1f, p));
        }
        else
        {
            float p = (t - 0.58f) / 0.42f;
            scaleMultiplier = Vector2.Lerp(paddleReboundScale, Vector2.one, Mathf.SmoothStep(0f, 1f, p));
        }

        paddleVisual.localScale = new Vector3(
            paddleBaseScale.x * scaleMultiplier.x,
            paddleBaseScale.y * scaleMultiplier.y,
            paddleBaseScale.z);

        // Downward visual recoil offset that springs back to zero
        float recoilFactor = Mathf.Sin(Mathf.Min(1f, t * 1.4f) * Mathf.PI) * (1f - t);
        paddleVisual.localPosition = paddleBaseLocalPos + (Vector3)(paddleRecoilDir * (paddleRecoilDistance * recoilFactor));

        if (paddleRenderer != null)
        {
            float flashT = Mathf.Clamp01(t * 2.2f);
            paddleRenderer.color = Color.Lerp(paddleFlashColor, paddleBaseColor, flashT);
        }
    }

    private void UpdateBallAnimation()
    {
        if (ballTimer < 0f || ballVisual == null) return;

        ballTimer += Time.deltaTime;
        float t = ballTimer / Mathf.Max(0.01f, ballAnimDuration);

        if (t >= 1f)
        {
            ballTimer = -1f;
            ballVisual.localScale = ballBaseScale;
            ballVisual.localRotation = ballBaseRotation;
            if (ballRenderer != null)
                ballRenderer.color = ballBaseColor;
            return;
        }

        Vector2 scaleMultiplier;
        if (t < 0.25f)
        {
            float p = t / 0.25f;
            scaleMultiplier = Vector2.Lerp(Vector2.one, ballSquashScale, p);
        }
        else if (t < 0.65f)
        {
            float p = (t - 0.25f) / 0.40f;
            scaleMultiplier = Vector2.Lerp(ballSquashScale, ballReboundScale, p);
        }
        else
        {
            float p = (t - 0.65f) / 0.35f;
            scaleMultiplier = Vector2.Lerp(ballReboundScale, Vector2.one, p);
        }

        ballVisual.localRotation = Quaternion.Euler(0f, 0f, ballImpactAngle);
        ballVisual.localScale = new Vector3(
            ballBaseScale.x * scaleMultiplier.x,
            ballBaseScale.y * scaleMultiplier.y,
            ballBaseScale.z);

        if (ballRenderer != null)
        {
            float flashT = Mathf.Clamp01(t * 2.0f);
            ballRenderer.color = Color.Lerp(ballFlashColor, ballBaseColor, flashT);
        }
    }

    private void RestoreImmediate()
    {
        paddleTimer = -1f;
        ballTimer = -1f;

        if (paddleVisual != null)
        {
            paddleVisual.localScale = paddleBaseScale;
            paddleVisual.localPosition = paddleBaseLocalPos;
        }
        if (paddleRenderer != null)
            paddleRenderer.color = paddleBaseColor;

        if (ballVisual != null)
        {
            ballVisual.localScale = ballBaseScale;
            ballVisual.localRotation = ballBaseRotation;
        }
        if (ballRenderer != null)
            ballRenderer.color = ballBaseColor;
    }
}

