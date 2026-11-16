using UnityEngine;

/// <summary>
/// Tuğla ızgarasını kodla üretir. Elle 28 tuğla dizmek yerine
/// satır/sütun sayısını Inspector'dan değiştirip anında deneyebilirsin.
/// </summary>
public class LevelBuilder : MonoBehaviour
{
    [Header("Izgara")]
    [SerializeField] private Brick brickPrefab;
    [SerializeField] private int columns = 7;
    [SerializeField] private int rows = 4;
    [SerializeField] private Vector2 brickSize = new Vector2(2f, 0.7f);
    [SerializeField] private Vector2 spacing = new Vector2(0.15f, 0.15f);
    [SerializeField] private float topY = 4f;

    [Header("Renkler")]
    [SerializeField] private Color[] rowColors;

    public int RemainingBricks { get; private set; }

    private void Start()
    {
        Build();
    }

    public void Build()
    {
        ClearBricks();

        float stepX = brickSize.x + spacing.x;
        float stepY = brickSize.y + spacing.y;

        // Izgarayı ekranda ortalamak için toplam genişliğin yarısı kadar sola kaydır.
        float totalWidth = columns * stepX - spacing.x;
        float startX = -totalWidth * 0.5f + brickSize.x * 0.5f;

        RemainingBricks = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 pos = new Vector3(
                    startX + col * stepX,
                    topY - row * stepY,
                    0f);

                Brick brick = Instantiate(brickPrefab, pos, Quaternion.identity, transform);
                brick.name = $"Brick_r{row}_c{col}";
                brick.transform.localScale = new Vector3(brickSize.x, brickSize.y, 1f);
                RemainingBricks++;

                if (rowColors.Length > 0 && brick.TryGetComponent(out SpriteRenderer sr))
                {
                    Color c = rowColors[row % rowColors.Length];

                    // Unity'de Color dizisine yeni eklenen eleman (0,0,0,0) olarak doğar
                    // ve hex alanına RGB yazmak alpha'yı değiştirmez. Görünmez tuğla tuzağı.
                    if (c.a <= 0.01f) c.a = 1f;

                    sr.color = c;
                }
            }
        }
    }

    public void ClearBricks()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
        RemainingBricks = 0;
    }

    public int RegisterBrickDestroyed()
    {
        RemainingBricks = Mathf.Max(0, RemainingBricks - 1);
        return RemainingBricks;
    }

    /// <summary>
    /// Topun tuğla (engel) bölgesinden tamamen çıkıp çıkmadığını kontrol eder.
    /// Top en alt tuğla sırasının altına indiğinde yeni tuğlalar güvenle oluşturulabilir.
    /// </summary>
    public bool IsBallOutsideObstacleZone(Vector2 ballPosition, float safetyMargin = 0.6f)
    {
        float stepY = brickSize.y + spacing.y;
        float bottomEdgeY = topY - (rows - 1) * stepY - brickSize.y * 0.5f;
        return ballPosition.y < (bottomEdgeY - safetyMargin);
    }
}