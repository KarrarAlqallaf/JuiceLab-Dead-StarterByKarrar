using UnityEngine;

/// <summary>
/// Improvement 5A: Restrained Directional Camera Shake.
/// Adds directional impact weight and trauma-based camera shake during brick destruction,
/// paddle hits, and ball loss, while always returning strictly to the camera's base position.
/// </summary>
public class CameraShakeFeedback : MonoBehaviour
{
    [Header("Target Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Shake Tuning")]
    [SerializeField] private float brickKickStrength = 0.14f;
    [SerializeField] private float paddleKickStrength = 0.07f;
    [SerializeField] private float wallKickStrength = 0.03f;
    [SerializeField] private float ballLostShakeStrength = 0.22f;
    [SerializeField] private float maxOffsetDistance = 0.28f;
    [SerializeField] private float recoverySpeed = 16f;
    [SerializeField] private float shakeFrequency = 42f;

    private Vector3 baseLocalPosition;
    private bool basePositionCached;
    private Vector2 directionalKick;
    private float trauma;
    private float seed;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
        {
            baseLocalPosition = cameraTransform.localPosition;
            basePositionCached = true;
        }
        seed = Random.Range(0f, 100f);
    }

    private void OnEnable()
    {
        if (!basePositionCached && cameraTransform != null)
        {
            baseLocalPosition = cameraTransform.localPosition;
            basePositionCached = true;
        }

        GameEvents.BrickDestroyed += OnBrickDestroyed;
        GameEvents.BallHitPaddle += OnBallHitPaddle;
        GameEvents.BallHitWall += OnBallHitWall;
        GameEvents.BallLost += OnBallLost;
    }

    private void OnDisable()
    {
        GameEvents.BrickDestroyed -= OnBrickDestroyed;
        GameEvents.BallHitPaddle -= OnBallHitPaddle;
        GameEvents.BallHitWall -= OnBallHitWall;
        GameEvents.BallLost -= OnBallLost;

        trauma = 0f;
        directionalKick = Vector2.zero;
        if (cameraTransform != null && basePositionCached)
            cameraTransform.localPosition = baseLocalPosition;
    }

    private void OnBrickDestroyed(ImpactInfo info)
    {
        Vector2 dir = info.Normal.sqrMagnitude > 0.001f ? -info.Normal.normalized : Vector2.up;
        directionalKick = Vector2.ClampMagnitude(directionalKick + dir * brickKickStrength, maxOffsetDistance);
        trauma = Mathf.Clamp01(trauma + 0.48f);
    }

    private void OnBallHitPaddle(ImpactInfo info)
    {
        directionalKick = Vector2.ClampMagnitude(directionalKick + Vector2.down * paddleKickStrength, maxOffsetDistance);
        trauma = Mathf.Clamp01(trauma + 0.26f);
    }

    private void OnBallHitWall(ImpactInfo info)
    {
        Vector2 dir = info.Normal.sqrMagnitude > 0.001f ? -info.Normal.normalized : Vector2.zero;
        directionalKick = Vector2.ClampMagnitude(directionalKick + dir * wallKickStrength, maxOffsetDistance);
        trauma = Mathf.Clamp01(trauma + 0.14f);
    }

    private void OnBallLost()
    {
        directionalKick = Vector2.ClampMagnitude(directionalKick + Vector2.down * ballLostShakeStrength, maxOffsetDistance);
        trauma = Mathf.Clamp01(trauma + 0.75f);
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || !basePositionCached) return;

        if (trauma <= 0.0001f && directionalKick.sqrMagnitude <= 0.00001f)
        {
            cameraTransform.localPosition = baseLocalPosition;
            return;
        }

        float dt = Time.deltaTime;
        directionalKick = Vector2.MoveTowards(directionalKick, Vector2.zero, recoverySpeed * dt * 0.55f);
        trauma = Mathf.MoveTowards(trauma, 0f, dt * 3.4f);

        float shake = trauma * trauma;
        float time = Time.unscaledTime * shakeFrequency;
        float noiseX = (Mathf.PerlinNoise(seed, time) - 0.5f) * 2f;
        float noiseY = (Mathf.PerlinNoise(seed + 31.7f, time) - 0.5f) * 2f;

        Vector2 shakeOffset = new Vector2(noiseX, noiseY) * (shake * 0.18f);
        Vector2 totalOffset = Vector2.ClampMagnitude(directionalKick + shakeOffset, maxOffsetDistance);

        cameraTransform.localPosition = new Vector3(
            baseLocalPosition.x + totalOffset.x,
            baseLocalPosition.y + totalOffset.y,
            baseLocalPosition.z);
    }
}

