using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject enemyPrefab;
    public GameObject rangedEnemyPrefab;
    public Transform player;

    [Header("Distancia de spawn")]
    public float spawnDistance = 8f;

    [Header("Ritmo de aparición melee")]
    public float startSpawnInterval = 2f;
    public float minSpawnInterval = 0.35f;
    public float intervalReductionPerMinute = 0.35f;

    [Header("Cantidad máxima en pantalla")]
    public int startMaxEnemies = 20;
    public int maxEnemiesCap = 120;
    public int extraEnemiesPerMinute = 12;

    [Header("Spawns múltiples melee")]
    public int maxSpawnPerWave = 4;
    public float waveGrowthPerMinute = 0.8f;

    [Header("Ranged enemies")]
    public bool enableRangedEnemies = true;
    public float rangedStartDelay = 18f;
    public float rangedMinInterval = 13f;
    public float rangedMaxInterval = 20f;
    public int rangedMinPackSize = 3;
    public int rangedMaxPackSize = 5;
    public float rangedSpawnDistance = 10f;
    public float rangedPackSpread = 1.2f;

    private float spawnTimer;
    private float rangedSpawnTimer;

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
        rangedSpawnTimer = rangedStartDelay;
    }

    void Update()
    {
        if (player == null) return;

        HandleMeleeSpawning();
        HandleRangedSpawning();
    }

    void HandleMeleeSpawning()
    {
        if (enemyPrefab == null) return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            TrySpawnMeleeWave();
            spawnTimer = GetCurrentSpawnInterval();
        }
    }

    void HandleRangedSpawning()
    {
        if (!enableRangedEnemies) return;
        if (rangedEnemyPrefab == null) return;

        rangedSpawnTimer -= Time.deltaTime;

        if (rangedSpawnTimer <= 0f)
        {
            TrySpawnRangedPack();
            rangedSpawnTimer = Random.Range(rangedMinInterval, rangedMaxInterval);
        }
    }

    void TrySpawnMeleeWave()
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
            SpawnEnemy(enemyPrefab, spawnDistance);
        }
    }

    void TrySpawnRangedPack()
    {
        int currentEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        int currentMaxEnemies = GetCurrentMaxEnemies();

        if (currentEnemies >= currentMaxEnemies)
            return;

        int packSize = Random.Range(rangedMinPackSize, rangedMaxPackSize + 1);
        int availableSlots = currentMaxEnemies - currentEnemies;

        packSize = Mathf.Min(packSize, availableSlots);

        Vector2 baseDirection = Random.insideUnitCircle.normalized;

        if (baseDirection == Vector2.zero)
        {
            baseDirection = Vector2.right;
        }

        Vector3 packCenter = player.position + (Vector3)(baseDirection * rangedSpawnDistance);

        for (int i = 0; i < packSize; i++)
        {
            Vector2 offset = Random.insideUnitCircle * rangedPackSpread;
            Vector3 spawnPosition = packCenter + (Vector3)offset;

            SpawnEnemyAtPosition(rangedEnemyPrefab, spawnPosition);
        }
    }

    void SpawnEnemy(GameObject prefab, float distance)
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        if (randomDirection == Vector2.zero)
            randomDirection = Vector2.right;

        Vector3 spawnPosition = player.position + (Vector3)(randomDirection * distance);

        SpawnEnemyAtPosition(prefab, spawnPosition);
    }

    void SpawnEnemyAtPosition(GameObject prefab, Vector3 spawnPosition)
    {
        GameObject newEnemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

        EnemyMelee enemyMelee = newEnemy.GetComponent<EnemyMelee>();
        if (enemyMelee != null)
        {
            enemyMelee.target = player;
        }

        EnemyRanged enemyRanged = newEnemy.GetComponent<EnemyRanged>();
        if (enemyRanged != null)
        {
            enemyRanged.target = player;
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