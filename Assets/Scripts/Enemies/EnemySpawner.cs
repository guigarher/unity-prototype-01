using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject enemyPrefab;
    public GameObject rangedEnemyPrefab;
    public Transform player;
    public Camera gameplayCamera;

    [Header("Distancia de spawn melee")]
    public float spawnDistance = 18f;
    public float spawnDistanceExtraRange = 6f;

    [Header("Reglas anti-spawn injusto")]
    public bool avoidSpawningInsideCamera = true;
    public float cameraSafePadding = 3f;
    public float minDistanceFromPlayer = 10f;
    public float minDistanceFromBase = 10f;
    public bool addBaseAttackRadiusToSafeDistance = true;
    public int maxSpawnPositionAttempts = 60;

    [Header("Ritmo de aparición melee")]
    public float startSpawnInterval = 2f;
    public float minSpawnInterval = 0.35f;
    public float intervalReductionPerMinute = 0.35f;

    [Header("Cantidad máxima en pantalla")]
    public int startMaxEnemies = 20;
    public int maxEnemiesCap = 120;
    public int extraEnemiesPerMinute = 12;

    [Header("Reserva para ranged")]
    public int rangedReservedSlots = 8;

    [Header("Spawns múltiples melee")]
    public int maxSpawnPerWave = 4;
    public float waveGrowthPerMinute = 0.8f;

    [Header("Ranged enemies")]
    public bool enableRangedEnemies = true;
    public float rangedStartDelay = 12f;

    [Header("Ranged de día")]
    public float dayRangedMinInterval = 12f;
    public float dayRangedMaxInterval = 18f;
    public int dayRangedMinPackSize = 1;
    public int dayRangedMaxPackSize = 2;

    [Header("Ranged de noche")]
    public float nightRangedMinInterval = 4f;
    public float nightRangedMaxInterval = 7f;
    public int nightRangedMinPackSize = 2;
    public int nightRangedMaxPackSize = 4;

    [Header("Límite ranged")]
    public int maxActiveRangedEnemies = 14;

    [Header("Spawn ranged")]
    public float rangedSpawnDistance = 20f;
    public float rangedSpawnDistanceExtraRange = 6f;
    public float rangedPackSpread = 2f;
    public bool spawnRangedAroundBaseAtNight = true;

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

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
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
            rangedSpawnTimer = GetCurrentRangedInterval();
        }
    }

    void TrySpawnMeleeWave()
    {
        int totalEnemies = CountAllEnemies();
        int meleeEnemies = CountMeleeEnemies();
        int currentMaxEnemies = GetCurrentMaxEnemies();

        int maxMeleeAllowed = Mathf.Max(0, currentMaxEnemies - rangedReservedSlots);

        if (totalEnemies >= currentMaxEnemies) return;
        if (meleeEnemies >= maxMeleeAllowed) return;

        int enemiesToSpawn = GetCurrentSpawnCountPerWave();

        int availableTotalSlots = currentMaxEnemies - totalEnemies;
        int availableMeleeSlots = maxMeleeAllowed - meleeEnemies;

        enemiesToSpawn = Mathf.Min(enemiesToSpawn, availableTotalSlots, availableMeleeSlots);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Vector3 spawnPosition;

            if (TryGetValidSpawnPositionAroundOrigin(
                player.position,
                spawnDistance,
                spawnDistanceExtraRange,
                out spawnPosition
            ))
            {
                SpawnEnemyAtPosition(enemyPrefab, spawnPosition);
            }
        }
    }

    void TrySpawnRangedPack()
    {
        int totalEnemies = CountAllEnemies();
        int rangedEnemies = CountRangedEnemies();
        int currentMaxEnemies = GetCurrentMaxEnemies();

        if (totalEnemies >= currentMaxEnemies) return;
        if (rangedEnemies >= maxActiveRangedEnemies) return;

        int packSize = GetCurrentRangedPackSize();

        int availableTotalSlots = currentMaxEnemies - totalEnemies;
        int availableRangedSlots = maxActiveRangedEnemies - rangedEnemies;

        packSize = Mathf.Min(packSize, availableTotalSlots, availableRangedSlots);

        if (packSize <= 0) return;

        Vector3 spawnOrigin = GetRangedSpawnOrigin();

        Vector3 packCenter;

        if (!TryGetValidSpawnPositionAroundOrigin(
            spawnOrigin,
            rangedSpawnDistance,
            rangedSpawnDistanceExtraRange,
            out packCenter
        ))
        {
            return;
        }

        for (int i = 0; i < packSize; i++)
        {
            Vector3 spawnPosition;

            if (TryGetValidSpawnPositionNearPoint(packCenter, rangedPackSpread, out spawnPosition))
            {
                SpawnEnemyAtPosition(rangedEnemyPrefab, spawnPosition);
            }
        }
    }

    bool TryGetValidSpawnPositionAroundOrigin(
        Vector3 origin,
        float baseDistance,
        float extraRange,
        out Vector3 spawnPosition
    )
    {
        for (int i = 0; i < maxSpawnPositionAttempts; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;

            if (randomDirection == Vector2.zero)
            {
                randomDirection = Vector2.right;
            }

            float finalDistance = Random.Range(baseDistance, baseDistance + extraRange);

            Vector3 candidatePosition = origin + (Vector3)(randomDirection * finalDistance);

            if (IsValidSpawnPosition(candidatePosition))
            {
                spawnPosition = candidatePosition;
                return true;
            }
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    bool TryGetValidSpawnPositionNearPoint(Vector3 center, float spread, out Vector3 spawnPosition)
    {
        for (int i = 0; i < maxSpawnPositionAttempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * spread;
            Vector3 candidatePosition = center + (Vector3)offset;

            if (IsValidSpawnPosition(candidatePosition))
            {
                spawnPosition = candidatePosition;
                return true;
            }
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    bool IsValidSpawnPosition(Vector3 position)
    {
        if (avoidSpawningInsideCamera && IsPositionInsideCameraArea(position))
        {
            return false;
        }

        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(position, player.position);

            if (distanceToPlayer < minDistanceFromPlayer)
            {
                return false;
            }
        }

        if (BaseCore.Instance != null)
        {
            float requiredDistanceFromBase = minDistanceFromBase;

            if (addBaseAttackRadiusToSafeDistance)
            {
                requiredDistanceFromBase += BaseCore.Instance.enemyAttackRadius;
            }

            float distanceToBase = Vector2.Distance(
                position,
                BaseCore.Instance.transform.position
            );

            if (distanceToBase < requiredDistanceFromBase)
            {
                return false;
            }
        }

        return true;
    }

    bool IsPositionInsideCameraArea(Vector3 worldPosition)
    {
        if (gameplayCamera == null) return false;

        if (gameplayCamera.orthographic)
        {
            Vector3 cameraPosition = gameplayCamera.transform.position;

            float cameraHalfHeight = gameplayCamera.orthographicSize;
            float cameraHalfWidth = cameraHalfHeight * gameplayCamera.aspect;

            float minX = cameraPosition.x - cameraHalfWidth - cameraSafePadding;
            float maxX = cameraPosition.x + cameraHalfWidth + cameraSafePadding;
            float minY = cameraPosition.y - cameraHalfHeight - cameraSafePadding;
            float maxY = cameraPosition.y + cameraHalfHeight + cameraSafePadding;

            return worldPosition.x > minX &&
                   worldPosition.x < maxX &&
                   worldPosition.y > minY &&
                   worldPosition.y < maxY;
        }

        Vector3 viewportPoint = gameplayCamera.WorldToViewportPoint(worldPosition);

        if (viewportPoint.z < 0f)
        {
            return false;
        }

        return viewportPoint.x > 0f &&
               viewportPoint.x < 1f &&
               viewportPoint.y > 0f &&
               viewportPoint.y < 1f;
    }

    void SpawnEnemyAtPosition(GameObject prefab, Vector3 spawnPosition)
    {
        Instantiate(prefab, spawnPosition, Quaternion.identity);
    }

    Vector3 GetRangedSpawnOrigin()
    {
        if (spawnRangedAroundBaseAtNight &&
            GamePhaseManager.Instance != null &&
            GamePhaseManager.Instance.IsNight() &&
            BaseCore.Instance != null)
        {
            return BaseCore.Instance.transform.position;
        }

        return player.position;
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

    float GetCurrentRangedInterval()
    {
        bool isNight = GamePhaseManager.Instance != null && GamePhaseManager.Instance.IsNight();

        if (isNight)
        {
            return Random.Range(nightRangedMinInterval, nightRangedMaxInterval);
        }

        return Random.Range(dayRangedMinInterval, dayRangedMaxInterval);
    }

    int GetCurrentRangedPackSize()
    {
        bool isNight = GamePhaseManager.Instance != null && GamePhaseManager.Instance.IsNight();

        if (isNight)
        {
            return Random.Range(nightRangedMinPackSize, nightRangedMaxPackSize + 1);
        }

        return Random.Range(dayRangedMinPackSize, dayRangedMaxPackSize + 1);
    }

    int CountAllEnemies()
    {
        return GameObject.FindGameObjectsWithTag("Enemy").Length;
    }

    int CountMeleeEnemies()
    {
        EnemyMelee[] meleeEnemies = Object.FindObjectsByType<EnemyMelee>(
            FindObjectsInactive.Exclude
        );

        return meleeEnemies.Length;
    }

    int CountRangedEnemies()
    {
        EnemyRanged[] rangedEnemies = Object.FindObjectsByType<EnemyRanged>(
            FindObjectsInactive.Exclude
        );

        return rangedEnemies.Length;
    }

    float GetElapsedMinutes()
    {
        if (GameManager.Instance == null)
            return 0f;

        return GameManager.Instance.gameTime / 60f;
    }

    void OnDrawGizmosSelected()
    {
        if (gameplayCamera != null && gameplayCamera.orthographic)
        {
            Vector3 cameraPosition = gameplayCamera.transform.position;

            float cameraHalfHeight = gameplayCamera.orthographicSize;
            float cameraHalfWidth = cameraHalfHeight * gameplayCamera.aspect;

            Vector3 cameraSafeSize = new Vector3(
                (cameraHalfWidth + cameraSafePadding) * 2f,
                (cameraHalfHeight + cameraSafePadding) * 2f,
                0f
            );

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(
                new Vector3(cameraPosition.x, cameraPosition.y, 0f),
                cameraSafeSize
            );
        }

        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, minDistanceFromPlayer);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, spawnDistance);
        }

        if (BaseCore.Instance != null)
        {
            float requiredDistanceFromBase = minDistanceFromBase;

            if (addBaseAttackRadiusToSafeDistance)
            {
                requiredDistanceFromBase += BaseCore.Instance.enemyAttackRadius;
            }

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(BaseCore.Instance.transform.position, requiredDistanceFromBase);
        }
    }
}