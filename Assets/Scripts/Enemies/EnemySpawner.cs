using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject enemyPrefab;
    public Transform player;

    [Header("Distancia de spawn")]
    public float spawnDistance = 8f;

    [Header("Ritmo de aparición")]
    public float startSpawnInterval = 2f;
    public float minSpawnInterval = 0.35f;
    public float intervalReductionPerMinute = 0.35f;

    [Header("Cantidad máxima en pantalla")]
    public int startMaxEnemies = 20;
    public int maxEnemiesCap = 120;
    public int extraEnemiesPerMinute = 12;

    [Header("Spawns múltiples")]
    public int maxSpawnPerWave = 4;
    public float waveGrowthPerMinute = 0.8f;

    private float spawnTimer;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        spawnTimer = startSpawnInterval;
    }

    void Update()
    {
        if (player == null || enemyPrefab == null) return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            TrySpawnWave();
            spawnTimer = GetCurrentSpawnInterval();
        }
    }

    void TrySpawnWave()
    {
        int currentEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        int currentMaxEnemies = GetCurrentMaxEnemies();

        if (currentEnemies >= currentMaxEnemies)
            return;

        int enemiesToSpawn = GetCurrentSpawnCountPerWave();
        int availableSlots = currentMaxEnemies - currentEnemies;

        enemiesToSpawn = Mathf.Min(enemiesToSpawn, availableSlots);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        if (randomDirection == Vector2.zero)
            randomDirection = Vector2.right;

        Vector3 spawnPosition = player.position + (Vector3)(randomDirection * spawnDistance);

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        EnemyMelee enemyMelee = newEnemy.GetComponent<EnemyMelee>();
        if (enemyMelee != null)
        {
            enemyMelee.target = player;
        }
    }

    float GetCurrentSpawnInterval()
    {
        float minutes = GetElapsedMinutes();
        float currentInterval = startSpawnInterval - (minutes * intervalReductionPerMinute);

        return Mathf.Max(minSpawnInterval, currentInterval);
    }

    int GetCurrentMaxEnemies()
    {
        float minutes = GetElapsedMinutes();
        int currentMax = startMaxEnemies + Mathf.FloorToInt(minutes * extraEnemiesPerMinute);

        return Mathf.Min(currentMax, maxEnemiesCap);
    }

    int GetCurrentSpawnCountPerWave()
    {
        float minutes = GetElapsedMinutes();
        int spawnCount = 1 + Mathf.FloorToInt(minutes * waveGrowthPerMinute);

        return Mathf.Clamp(spawnCount, 1, maxSpawnPerWave);
    }

    float GetElapsedMinutes()
    {
        if (GameManager.Instance == null)
            return 0f;

        return GameManager.Instance.gameTime / 60f;
    }
}