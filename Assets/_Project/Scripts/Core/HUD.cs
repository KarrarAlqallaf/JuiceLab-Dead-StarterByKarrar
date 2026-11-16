using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Skor, can göstergesi ve Game Over menüsü.
/// Juice katmanı devredeyken skor yazısının sahipliğini ona devrediyor —
/// iki sistemin aynı alana yazması juice mimarilerinde klasik bir çakışma kaynağı.
/// </summary>
public class HUD : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;

    [Header("Game Over UI (Boş bırakılırsa otomatik oluşturulur)")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    /// <summary>Bir juice modülü skoru devraldıysa HUD ona karışmaz.</summary>
    public bool ScoreHandledExternally { get; set; }

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != "Before")
        {
            EnsureGameOverMenu();
        }
    }

    private void OnEnable()
    {
        GameEvents.ScoreChanged += OnScoreChanged;
        GameEvents.LivesChanged += OnLivesChanged;
        GameEvents.GameOver += OnGameOver;
        GameEvents.GameRestarted += OnGameRestarted;
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= OnScoreChanged;
        GameEvents.LivesChanged -= OnLivesChanged;
        GameEvents.GameOver -= OnGameOver;
        GameEvents.GameRestarted -= OnGameRestarted;
    }

    private void Start()
    {
        RefreshScore();
        if (GameManager.Instance != null)
            OnLivesChanged(GameManager.Instance.Lives);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(GameManager.Instance != null && GameManager.Instance.IsGameOver);
    }

    /// <summary>Sahiplik geri alındığında güncel değeri anında yazmak için.</summary>
    public void RefreshScore()
    {
        if (GameManager.Instance != null && scoreText != null)
            scoreText.text = $"SCORE {GameManager.Instance.Score}";
    }

    private void OnScoreChanged(int score)
    {
        if (ScoreHandledExternally) return;
        if (scoreText != null)
            scoreText.text = $"SCORE {score}";
    }

    private void OnLivesChanged(int lives)
    {
        if (livesText != null)
            livesText.text = $"LIVES {lives}";
    }

    private void OnGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    private void OnGameRestarted()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnRestartButtonClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();
    }

    private void EnsureGameOverMenu()
    {
        if (gameOverPanel != null)
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartButtonClicked);
                restartButton.onClick.AddListener(OnRestartButtonClicked);
            }
            gameOverPanel.SetActive(false);
            return;
        }

        TMP_FontAsset fontAsset = scoreText != null ? scoreText.font : TMP_Settings.defaultFontAsset;

        // Tam ekran yarı saydam arka plan paneli
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.layer = gameObject.layer;
        RectTransform panelRect = gameOverPanel.AddComponent<RectTransform>();
        panelRect.SetParent(transform, false);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRect.localScale = Vector3.one;

        Image panelImage = gameOverPanel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.04f, 0.08f, 0.85f);
        panelImage.raycastTarget = true;

        // "GAME OVER" başlığı
        GameObject titleObj = new GameObject("GameOverTitle");
        titleObj.layer = gameObject.layer;
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.SetParent(panelRect, false);
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 80f);
        titleRect.sizeDelta = new Vector2(900f, 140f);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null)
            titleText.font = fontAsset;
        titleText.text = "GAME OVER";
        titleText.fontSize = 92f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.30f, 0.43f, 1f);
        titleText.raycastTarget = false;

        // "RESTART" butonu
        GameObject buttonObj = new GameObject("RestartButton");
        buttonObj.layer = gameObject.layer;
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.SetParent(panelRect, false);
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -55f);
        buttonRect.sizeDelta = new Vector2(320f, 84f);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = Color.white;
        buttonImage.raycastTarget = true;

        restartButton = buttonObj.AddComponent<Button>();
        restartButton.targetGraphic = buttonImage;

        ColorBlock colors = restartButton.colors;
        colors.normalColor = new Color(0.22f, 0.78f, 0.75f, 1f);
        colors.highlightedColor = new Color(0.32f, 0.90f, 0.86f, 1f);
        colors.pressedColor = new Color(0.16f, 0.58f, 0.56f, 1f);
        colors.selectedColor = new Color(0.32f, 0.90f, 0.86f, 1f);
        colors.colorMultiplier = 1f;
        restartButton.colors = colors;

        restartButton.onClick.AddListener(OnRestartButtonClicked);

        // Buton üzerindeki "RESTART" yazısı
        GameObject buttonTextObj = new GameObject("RestartText");
        buttonTextObj.layer = gameObject.layer;
        RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.SetParent(buttonRect, false);
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null)
            buttonText.font = fontAsset;
        buttonText.text = "RESTART";
        buttonText.fontSize = 42f;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        buttonText.raycastTarget = false;

        gameOverPanel.SetActive(false);
    }
}