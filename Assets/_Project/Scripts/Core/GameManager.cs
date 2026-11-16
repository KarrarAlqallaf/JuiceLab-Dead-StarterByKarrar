using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Skor, can ve combo sayacı. Tek sahnelik bir proje olduğu için basit singleton yeterli.
/// Durum değiştiğinde olay yayınlar; kimin dinlediğini bilmez.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Ball ball;
    [SerializeField] private LevelBuilder levelBuilder;
    [SerializeField] private int startingLives = 3;
    [SerializeField] private float speedIncreasePerRound = 1.2f;

    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int Round { get; private set; } = 1;
    public bool IsGameOver { get; private set; }

    /// <summary>Rakete değmeden arka arkaya kırılan tuğla sayısı.</summary>
    public int Combo { get; private set; }

    private bool waitingForNextRound;
    private bool isBaselineBeforeScene;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        isBaselineBeforeScene = SceneManager.GetActiveScene().name == "Before";

        if (ball == null)
            ball = FindAnyObjectByType<Ball>();
        if (levelBuilder == null)
            levelBuilder = FindAnyObjectByType<LevelBuilder>();

        Lives = startingLives;
        Round = 1;
        IsGameOver = false;
    }

    private void OnEnable()
    {
        GameEvents.BrickDestroyed += OnBrickDestroyed;
    }

    private void OnDisable()
    {
        GameEvents.BrickDestroyed -= OnBrickDestroyed;
    }

    private void Update()
    {
        if (isBaselineBeforeScene || IsGameOver || !waitingForNextRound) return;
        if (ball == null || levelBuilder == null) return;

        // Top engel (tuğla) bölgesinden tamamen çıktıktan sonra yeni turu başlat
        if (levelBuilder.IsBallOutsideObstacleZone(ball.transform.position))
        {
            StartNextRound();
        }
    }

    private void OnBrickDestroyed(ImpactInfo info)
    {
        if (isBaselineBeforeScene || levelBuilder == null) return;

        int remaining = levelBuilder.RegisterBrickDestroyed();
        if (remaining <= 0)
        {
            waitingForNextRound = true;
        }
    }

    private void StartNextRound()
    {
        waitingForNextRound = false;
        Round++;
        levelBuilder.Build();
        ball.IncreaseSpeed(speedIncreasePerRound);
    }

    public void AddScore(int amount)
    {
        Score += amount;
        GameEvents.RaiseScoreChanged(Score);
    }

    /// <summary>Combo'yu bir artırır ve yeni değeri döndürür.</summary>
    public int RegisterBrickCombo() => ++Combo;

    public void ResetCombo() => Combo = 0;

    public void OnBallLost()
    {
        if (IsGameOver) return;

        Lives--;
        ResetCombo();

        if (Lives <= 0)
        {
            if (isBaselineBeforeScene)
            {
                Lives = startingLives;
                Score = 0;
                GameEvents.RaiseScoreChanged(Score);
                GameEvents.RaiseLivesChanged(Lives);
                GameEvents.RaiseBallLost();
                if (ball != null)
                    ball.Launch();
                return;
            }

            Lives = 0;
            IsGameOver = true;
            waitingForNextRound = false;

            GameEvents.RaiseLivesChanged(Lives);
            GameEvents.RaiseBallLost();

            if (ball != null)
                ball.Stop();

            GameEvents.RaiseGameOver();
            return;
        }

        GameEvents.RaiseLivesChanged(Lives);
        GameEvents.RaiseBallLost();

        if (ball != null)
            ball.Launch();
    }

    public void RestartGame()
    {
        IsGameOver = false;
        waitingForNextRound = false;
        Lives = startingLives;
        Score = 0;
        Combo = 0;
        Round = 1;

        if (levelBuilder != null)
            levelBuilder.Build();

        if (ball != null)
        {
            ball.ResetSpeed();
            ball.Launch();
        }

        GameEvents.RaiseScoreChanged(Score);
        GameEvents.RaiseLivesChanged(Lives);
        GameEvents.RaiseGameRestarted();
    }
}