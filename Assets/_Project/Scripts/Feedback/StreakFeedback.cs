using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tracks and displays the single-rebound destruction streak counter on the HUD
/// and triggers tiered 0.3s full-screen visual effects:
/// - 3rd streak: Invert screen colors (0.3s)
/// - 4th streak: White and black monochrome screen (0.3s), immediately stopping previous effect
/// - 5th streak: Blood-like red screen (0.3s), immediately stopping previous effect
/// - 6th+ streak: Increases streak counter only without triggering or interrupting visual effects
/// </summary>
public class StreakFeedback : MonoBehaviour
{
    private enum ScreenEffectType
    {
        None = 0,
        Invert = 3,
        BlackAndWhite = 4,
        BloodRed = 5
    }

    [Header("HUD References")]
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;

    [Header("Camera Reference")]
    [SerializeField] private Camera targetCamera;

    [Header("Streak Counter Animation")]
    [SerializeField] private Vector3 punchPeakScale = new Vector3(1.38f, 1.38f, 1f);
    [SerializeField] private float punchDuration = 0.22f;
    [SerializeField] private Color baseStreakColor = new Color(0.75f, 0.92f, 1f, 1f);
    [SerializeField] private Color streakHighlightColor = new Color(1f, 0.90f, 0.35f, 1f);

    [Header("Screen Visual Effect Settings")]
    [SerializeField] private float effectDuration = 0.3f;
    [SerializeField] private Color invertHudTextColor = new Color(0.08f, 0.08f, 0.12f, 1f);
    [SerializeField] private Color blackAndWhiteHudTextColor = Color.white;
    [SerializeField] private Color bloodRedHudTextColor = new Color(1f, 0.14f, 0.14f, 1f);

    private RectTransform streakRect;
    private Vector3 baseStreakScale = Vector3.one;
    private Color baseScoreColor = Color.white;
    private Color baseLivesColor = Color.white;

    private int currentStreak;
    private float punchTimer = -1f;

    private ScreenEffectType activeEffect = ScreenEffectType.None;
    private float effectTimer = -1f;

    private Volume invertVolume;
    private Volume blackAndWhiteVolume;
    private Volume bloodRedVolume;

    private VolumeProfile invertProfile;
    private VolumeProfile blackAndWhiteProfile;
    private VolumeProfile bloodRedProfile;

    private void Awake()
    {
        ResolveReferences();
        EnsureStreakText();
        SetupCameraPostProcessing();
        CreateEffectVolumes();
    }

    private void OnEnable()
    {
        SetupCameraPostProcessing();

        currentStreak = GameManager.Instance != null ? GameManager.Instance.Combo : 0;
        punchTimer = -1f;
        StopScreenEffectImmediate();
        UpdateStreakTextDisplay();

        GameEvents.BrickDestroyed += OnBrickDestroyed;
        GameEvents.BallHitPaddle += OnBallHitPaddle;
        GameEvents.BallLost += OnBallLost;
        GameEvents.GameOver += OnGameOver;
        GameEvents.GameRestarted += OnGameRestarted;
    }

    private void OnDisable()
    {
        GameEvents.BrickDestroyed -= OnBrickDestroyed;
        GameEvents.BallHitPaddle -= OnBallHitPaddle;
        GameEvents.BallLost -= OnBallLost;
        GameEvents.GameOver -= OnGameOver;
        GameEvents.GameRestarted -= OnGameRestarted;

        punchTimer = -1f;
        StopScreenEffectImmediate();

        if (streakRect != null)
            streakRect.localScale = baseStreakScale;
        if (streakText != null)
            streakText.color = baseStreakColor;
    }

    private void OnDestroy()
    {
        if (invertProfile != null)
            Destroy(invertProfile);
        if (blackAndWhiteProfile != null)
            Destroy(blackAndWhiteProfile);
        if (bloodRedProfile != null)
            Destroy(bloodRedProfile);
    }

    private void OnBrickDestroyed(ImpactInfo info)
    {
        int newStreak = info.Combo > 0
            ? info.Combo
            : (GameManager.Instance != null ? GameManager.Instance.Combo : currentStreak + 1);

        int previousStreak = currentStreak;
        currentStreak = newStreak;

        // Trigger UI counter update and pop animation
        punchTimer = 0f;
        UpdateStreakTextDisplay();

        // Trigger tiered visual effects for 3rd, 4th, and 5th streaks.
        // Higher streaks (up to 5) immediately stop and replace any previous visual effect.
        // Streaks > 5 do nothing to visual effects (only increase the streak counter).
        if (currentStreak == 3 || (previousStreak < 3 && currentStreak == 3))
        {
            StartScreenEffect(ScreenEffectType.Invert);
        }
        else if (currentStreak == 4 || (previousStreak < 4 && currentStreak == 4))
        {
            StartScreenEffect(ScreenEffectType.BlackAndWhite);
        }
        else if (currentStreak == 5 || (previousStreak < 5 && currentStreak >= 5))
        {
            StartScreenEffect(ScreenEffectType.BloodRed);
        }
    }

    private void OnBallHitPaddle(ImpactInfo info)
    {
        ResetStreakCounter();
    }

    private void OnBallLost()
    {
        ResetStreakCounter();
    }

    private void OnGameOver()
    {
        StopScreenEffectImmediate();
        ResetStreakCounter();
    }

    private void OnGameRestarted()
    {
        StopScreenEffectImmediate();
        ResetStreakCounter();
    }

    private void ResetStreakCounter()
    {
        currentStreak = 0;
        punchTimer = -1f;

        if (streakRect != null)
            streakRect.localScale = baseStreakScale;

        UpdateStreakTextDisplay();

        if (activeEffect == ScreenEffectType.None && streakText != null)
            streakText.color = baseStreakColor;
    }

    private void StartScreenEffect(ScreenEffectType effectType)
    {
        // Immediately stop any previous visual effect before activating the new one
        SetAllVolumesActive(false, false, false);

        activeEffect = effectType;
        effectTimer = 0f;

        switch (activeEffect)
        {
            case ScreenEffectType.Invert:
                SetAllVolumesActive(true, false, false);
                break;
            case ScreenEffectType.BlackAndWhite:
                SetAllVolumesActive(false, true, false);
                break;
            case ScreenEffectType.BloodRed:
                SetAllVolumesActive(false, false, true);
                break;
        }
    }

    private void StopScreenEffectImmediate()
    {
        activeEffect = ScreenEffectType.None;
        effectTimer = -1f;
        SetAllVolumesActive(false, false, false);
        RestoreHudTextColors();
    }

    private void SetAllVolumesActive(bool invert, bool blackAndWhite, bool bloodRed)
    {
        if (invertVolume != null)
        {
            invertVolume.weight = invert ? 1f : 0f;
            invertVolume.enabled = invert;
        }
        if (blackAndWhiteVolume != null)
        {
            blackAndWhiteVolume.weight = blackAndWhite ? 1f : 0f;
            blackAndWhiteVolume.enabled = blackAndWhite;
        }
        if (bloodRedVolume != null)
        {
            bloodRedVolume.weight = bloodRed ? 1f : 0f;
            bloodRedVolume.enabled = bloodRed;
        }
    }

    private void Update()
    {
        UpdateStreakPunchAnimation();
    }

    private void LateUpdate()
    {
        if (activeEffect == ScreenEffectType.None) return;

        effectTimer += Time.unscaledDeltaTime;
        if (effectTimer >= effectDuration)
        {
            StopScreenEffectImmediate();
            return;
        }

        ApplyEffectHudColors();
    }

    private void UpdateStreakPunchAnimation()
    {
        if (punchTimer < 0f || streakRect == null) return;

        punchTimer += Time.deltaTime;
        float t = punchTimer / Mathf.Max(0.01f, punchDuration);

        if (t >= 1f)
        {
            punchTimer = -1f;
            streakRect.localScale = baseStreakScale;
            if (activeEffect == ScreenEffectType.None && streakText != null)
                streakText.color = GetStreakTierColor(currentStreak);
            return;
        }

        Vector3 scale;
        if (t < 0.22f)
        {
            float p = t / 0.22f;
            scale = Vector3.Lerp(baseStreakScale, punchPeakScale, 1f - (1f - p) * (1f - p));
        }
        else
        {
            float p = (t - 0.22f) / 0.78f;
            scale = Vector3.Lerp(punchPeakScale, baseStreakScale, Mathf.SmoothStep(0f, 1f, p));
        }

        streakRect.localScale = scale;

        if (activeEffect == ScreenEffectType.None && streakText != null)
        {
            Color targetCol = GetStreakTierColor(currentStreak);
            streakText.color = Color.Lerp(streakHighlightColor, targetCol, Mathf.SmoothStep(0f, 1f, t));
        }
    }

    private Color GetStreakTierColor(int streak)
    {
        if (streak >= 5) return new Color(1f, 0.25f, 0.28f, 1f);
        if (streak == 4) return Color.white;
        if (streak == 3) return new Color(0.45f, 1f, 0.92f, 1f);
        return baseStreakColor;
    }

    private void ApplyEffectHudColors()
    {
        Color hudOverrideColor = Color.white;
        switch (activeEffect)
        {
            case ScreenEffectType.Invert:
                hudOverrideColor = invertHudTextColor;
                break;
            case ScreenEffectType.BlackAndWhite:
                hudOverrideColor = blackAndWhiteHudTextColor;
                break;
            case ScreenEffectType.BloodRed:
                hudOverrideColor = bloodRedHudTextColor;
                break;
            default:
                return;
        }

        if (scoreText != null)
            scoreText.color = hudOverrideColor;
        if (livesText != null)
            livesText.color = hudOverrideColor;
        if (streakText != null)
            streakText.color = hudOverrideColor;
    }

    private void RestoreHudTextColors()
    {
        if (scoreText != null)
            scoreText.color = baseScoreColor;
        if (livesText != null)
            livesText.color = baseLivesColor;
        if (streakText != null)
            streakText.color = GetStreakTierColor(currentStreak);
    }

    private void UpdateStreakTextDisplay()
    {
        if (streakText != null)
            streakText.text = $"STREAK {currentStreak}";
    }

    private void ResolveReferences()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (hudCanvas == null)
        {
            HUD hud = FindAnyObjectByType<HUD>();
            if (hud != null)
                hudCanvas = hud.GetComponent<Canvas>();
        }

        if (hudCanvas != null)
        {
            if (scoreText == null)
            {
                Transform scoreTr = hudCanvas.transform.Find("ScoreText");
                if (scoreTr != null)
                    scoreText = scoreTr.GetComponent<TMP_Text>();
            }
            if (livesText == null)
            {
                Transform livesTr = hudCanvas.transform.Find("LivesText");
                if (livesTr != null)
                    livesText = livesTr.GetComponent<TMP_Text>();
            }
            if (streakText == null)
            {
                Transform streakTr = hudCanvas.transform.Find("StreakText");
                if (streakTr != null)
                    streakText = streakTr.GetComponent<TMP_Text>();
            }
        }

        if (scoreText != null)
            baseScoreColor = scoreText.color;
        if (livesText != null)
            baseLivesColor = livesText.color;
    }

    private void EnsureStreakText()
    {
        if (streakText == null && hudCanvas != null)
        {
            GameObject streakObj = new GameObject("StreakText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            streakObj.layer = hudCanvas.gameObject.layer;
            RectTransform rt = streakObj.GetComponent<RectTransform>();
            rt.SetParent(hudCanvas.transform, false);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(120f, -40f);
            rt.sizeDelta = new Vector2(420f, 70f);

            TextMeshProUGUI tmp = streakObj.GetComponent<TextMeshProUGUI>();
            if (scoreText != null)
            {
                tmp.font = scoreText.font;
                tmp.fontSharedMaterial = scoreText.fontSharedMaterial;
                tmp.fontSize = scoreText.fontSize;
            }
            else
            {
                tmp.fontSize = 48f;
            }

            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = baseStreakColor;
            tmp.raycastTarget = false;
            tmp.text = "STREAK 0";
            streakText = tmp;
        }

        if (streakText != null)
        {
            streakRect = streakText.rectTransform;
            baseStreakScale = streakRect.localScale;
            baseStreakColor = streakText.color;
        }
    }

    private void SetupCameraPostProcessing()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            UniversalAdditionalCameraData camData = targetCamera.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null)
                camData.renderPostProcessing = true;
        }
    }

    private void CreateEffectVolumes()
    {
        // 1. 3rd Streak: Invert Colors Volume
        invertProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        invertProfile.name = "Streak3_InvertProfile";
        ColorCurves invertCurves = invertProfile.Add<ColorCurves>(true);
        Keyframe[] invertKeys = new Keyframe[]
        {
            new Keyframe(0f, 1f, -1f, -1f),
            new Keyframe(1f, 0f, -1f, -1f)
        };
        TextureCurve invertedMasterCurve = new TextureCurve(invertKeys, 1f, false, new Vector2(0f, 1f));
        invertedMasterCurve.SetDirty();
        invertCurves.master.Override(invertedMasterCurve);
        invertVolume = CreateChildVolume("Streak3_InvertVolume", invertProfile, 110f);

        // 2. 4th Streak: White & Black Volume
        blackAndWhiteProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        blackAndWhiteProfile.name = "Streak4_BlackAndWhiteProfile";
        ColorAdjustments bwAdjustments = blackAndWhiteProfile.Add<ColorAdjustments>(true);
        bwAdjustments.saturation.Override(-100f);
        bwAdjustments.contrast.Override(55f);
        bwAdjustments.postExposure.Override(0.25f);
        blackAndWhiteVolume = CreateChildVolume("Streak4_BlackAndWhiteVolume", blackAndWhiteProfile, 120f);

        // 3. 5th Streak: Blood-Like Red Volume
        bloodRedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        bloodRedProfile.name = "Streak5_BloodRedProfile";

        ChannelMixer bloodMixer = bloodRedProfile.Add<ChannelMixer>(true);
        bloodMixer.redOutRedIn.Override(130f);
        bloodMixer.redOutGreenIn.Override(110f);
        bloodMixer.redOutBlueIn.Override(110f);
        bloodMixer.greenOutRedIn.Override(0f);
        bloodMixer.greenOutGreenIn.Override(0f);
        bloodMixer.greenOutBlueIn.Override(0f);
        bloodMixer.blueOutRedIn.Override(0f);
        bloodMixer.blueOutGreenIn.Override(0f);
        bloodMixer.blueOutBlueIn.Override(0f);

        ColorCurves bloodCurves = bloodRedProfile.Add<ColorCurves>(true);
        Keyframe[] redLiftKeys = new Keyframe[]
        {
            new Keyframe(0f, 0.24f, 0.76f, 0.76f),
            new Keyframe(1f, 1f, 0.76f, 0.76f)
        };
        Keyframe[] zeroChannelKeys = new Keyframe[]
        {
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(1f, 0.02f, 0.02f, 0.02f)
        };
        TextureCurve bloodRedCurve = new TextureCurve(redLiftKeys, 0.24f, false, new Vector2(0f, 1f));
        bloodRedCurve.SetDirty();
        TextureCurve bloodZeroG = new TextureCurve(zeroChannelKeys, 0f, false, new Vector2(0f, 1f));
        bloodZeroG.SetDirty();
        TextureCurve bloodZeroB = new TextureCurve(zeroChannelKeys, 0f, false, new Vector2(0f, 1f));
        bloodZeroB.SetDirty();

        bloodCurves.red.Override(bloodRedCurve);
        bloodCurves.green.Override(bloodZeroG);
        bloodCurves.blue.Override(bloodZeroB);

        ColorAdjustments bloodAdjust = bloodRedProfile.Add<ColorAdjustments>(true);
        bloodAdjust.contrast.Override(32f);
        bloodAdjust.colorFilter.Override(new Color(1f, 0.08f, 0.08f, 1f));

        Vignette bloodVignette = bloodRedProfile.Add<Vignette>(true);
        bloodVignette.color.Override(new Color(0.18f, 0f, 0f, 1f));
        bloodVignette.intensity.Override(0.45f);
        bloodVignette.smoothness.Override(0.38f);

        bloodRedVolume = CreateChildVolume("Streak5_BloodRedVolume", bloodRedProfile, 130f);
    }

    private Volume CreateChildVolume(string childName, VolumeProfile profile, float priority)
    {
        GameObject go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        Volume vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = priority;
        vol.weight = 0f;
        vol.sharedProfile = profile;
        vol.enabled = false;
        return vol;
    }
}
