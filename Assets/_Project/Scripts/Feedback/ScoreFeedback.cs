using TMPro;
using UnityEngine;

/// <summary>
/// Improvement 4: Score Interface Feedback.
/// Takes ownership of the HUD score text via HUD.ScoreHandledExternally, plays a rewarding punch scale
/// and highlight flash when points are earned, and guarantees accurate score synchronization.
/// </summary>
public class ScoreFeedback : MonoBehaviour
{
    [Header("HUD References")]
    [SerializeField] private HUD hud;
    [SerializeField] private TMP_Text scoreText;

    [Header("Punch Animation Settings")]
    [SerializeField] private Vector3 punchPeakScale = new Vector3(1.36f, 1.36f, 1f);
    [SerializeField] private float punchDuration = 0.22f;
    [SerializeField] private float countRollDuration = 0.12f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.86f, 0.32f, 1f);

    private RectTransform scoreRect;
    private Vector3 baseScale = Vector3.one;
    private Color baseColor = Color.white;

    private int targetScore;
    private int startRollScore;
    private int currentDisplayedScore;
    private float punchTimer = -1f;
    private float rollTimer = -1f;

    private void Awake()
    {
        if (hud == null)
            hud = FindAnyObjectByType<HUD>();

        if (scoreText != null)
        {
            scoreRect = scoreText.rectTransform;
            baseScale = scoreRect.localScale;
            baseColor = scoreText.color;
        }
    }

    private void OnEnable()
    {
        if (hud != null)
            hud.ScoreHandledExternally = true;

        int initialScore = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        targetScore = initialScore;
        startRollScore = initialScore;
        currentDisplayedScore = initialScore;
        WriteScoreText(targetScore);

        GameEvents.ScoreChanged += OnScoreChanged;
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= OnScoreChanged;

        punchTimer = -1f;
        rollTimer = -1f;

        if (scoreRect != null)
            scoreRect.localScale = baseScale;
        if (scoreText != null)
            scoreText.color = baseColor;

        if (hud != null)
        {
            hud.ScoreHandledExternally = false;
            hud.RefreshScore();
        }
    }

    private void OnScoreChanged(int newScore)
    {
        bool pointsIncreased = newScore > targetScore;
        startRollScore = currentDisplayedScore;
        targetScore = newScore;

        if (!pointsIncreased)
        {
            // Immediate update on score reset (e.g., when all lives are lost)
            rollTimer = -1f;
            currentDisplayedScore = targetScore;
            WriteScoreText(targetScore);
            return;
        }

        punchTimer = 0f;
        rollTimer = 0f;
        WriteScoreText(targetScore);
    }

    private void Update()
    {
        if (rollTimer >= 0f)
        {
            rollTimer += Time.deltaTime;
            float rt = rollTimer / Mathf.Max(0.01f, countRollDuration);
            if (rt >= 1f)
            {
                rollTimer = -1f;
                currentDisplayedScore = targetScore;
                WriteScoreText(targetScore);
            }
            else
            {
                currentDisplayedScore = Mathf.RoundToInt(Mathf.Lerp(startRollScore, targetScore, rt));
                WriteScoreText(currentDisplayedScore);
            }
        }

        if (punchTimer >= 0f && scoreRect != null)
        {
            punchTimer += Time.deltaTime;
            float t = punchTimer / Mathf.Max(0.01f, punchDuration);

            if (t >= 1f)
            {
                punchTimer = -1f;
                scoreRect.localScale = baseScale;
                if (scoreText != null)
                    scoreText.color = baseColor;
                WriteScoreText(targetScore);
            }
            else
            {
                // Fast pop up over first 22% of duration, then smooth settle back to 1.0
                Vector3 scale;
                if (t < 0.22f)
                {
                    float p = t / 0.22f;
                    scale = Vector3.Lerp(baseScale, punchPeakScale, 1f - (1f - p) * (1f - p));
                }
                else
                {
                    float p = (t - 0.22f) / 0.78f;
                    scale = Vector3.Lerp(punchPeakScale, baseScale, Mathf.SmoothStep(0f, 1f, p));
                }

                scoreRect.localScale = scale;

                if (scoreText != null)
                    scoreText.color = Color.Lerp(highlightColor, baseColor, Mathf.SmoothStep(0f, 1f, t));
            }
        }
    }

    private void WriteScoreText(int value)
    {
        if (scoreText != null)
            scoreText.text = $"SCORE {value}";
    }
}
