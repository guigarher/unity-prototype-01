using UnityEngine;
using System.Collections.Generic;

public class ObjectiveSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public Transform baseCore;

    [Header("Cofres")]
    public GameObject chestPrefab;
    public float firstChestDelay = 12f;
    public float minChestInterval = 25f;
    public float maxChestInterval = 40f;
    public int maxActiveChests = 1;

    [Header("Zonas de recursos diarias")]
    public GameObject woodResourceZonePrefab;
    public GameObject stoneResourceZonePrefab;
    public bool spawnResourcesAtDayStart = true;
    public bool replaceResourcesEachDay = true;

    [Header("Distancia de cofres respecto al jugador")]
    public float minChestSpawnDistance = 8f;
    public float maxChestSpawnDistance = 14f;

    [Header("Distancia de cofres respecto a la base")]
    public float minChestDistanceFromBase = 10f;
    public float maxChestDistanceFromBase = 50f;

    [Header("Distancia de cofres respecto a recursos")]
    public float minChestDistanceFromResourceZones = 7f;
    public int maxChestSpawnAttempts = 40;

    [Header("Distancia de recursos respecto a la base")]
    public float minResourceSpawnDistanceFromBase = 10f;
    public float maxResourceSpawnDistanceFromBase = 20f;
    public float minDistanceBetweenResourceZones = 7f;
    public float minResourceDistanceFromPlayer = 6f;
    public int maxResourceSpawnAttempts = 40;

    private float chestTimer;

    private GameObject currentWoodZone;
    private GameObject currentStoneZone;

    private List<GameObject> activeChests = new List<GameObject>();

    void Start()
    {
        FindReferencesIfNeeded();

        chestTimer = firstChestDelay;

        if (spawnResourcesAtDayStart)
        {
            SpawnDailyResourceZones();
        }
    }

    void OnEnable()
    {
        GamePhaseManager.OnPhaseChanged += OnPhaseChanged;
    }

    void OnDisable()
    {
        GamePhaseManager.OnPhaseChanged -= OnPhaseChanged;
    }

    void Update()
    {
        if (player == null) return;

        CleanNullObjectives();

        if (GamePhaseManager.Instance != null && GamePhaseManager.Instance.IsNight())
        {
            return;
        }

        HandleChestSpawning();
    }

    void FindReferencesIfNeeded()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (baseCore == null && BaseCore.Instance != null)
        {
            baseCore = BaseCore.Instance.transform;
        }
    }

    void OnPhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Day && spawnResourcesAtDayStart)
        {
            SpawnDailyResourceZones();
        }
    }

    void HandleChestSpawning()
    {
        if (chestPrefab == null) return;
        if (activeChests.Count >= maxActiveChests) return;

        chestTimer -= Time.deltaTime;

        if (chestTimer <= 0f)
        {
            GameObject newChest = SpawnChestAroundPlayer();

            if (newChest != null)
            {
                activeChests.Add(newChest);
                Debug.Log("Ha aparecido un cofre.");
            }
            else
            {
                Debug.LogWarning("No se encontró posición válida para el cofre.");
            }

            chestTimer = Random.Range(minChestInterval, maxChestInterval);
        }
    }

    GameObject SpawnChestAroundPlayer()
    {
        for (int i = 0; i < maxChestSpawnAttempts; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;

            if (randomDirection == Vector2.zero)
            {
                randomDirection = Vector2.right;
            }

            float distance = Random.Range(minChestSpawnDistance, maxChestSpawnDistance);

            Vector3 spawnPosition = player.position + (Vector3)(randomDirection * distance);

            if (IsValidChestPosition(spawnPosition))
            {
                return Instantiate(chestPrefab, spawnPosition, Quaternion.identity);
            }
        }

        return null;
    }

    bool IsValidChestPosition(Vector3 position)
    {
        if (baseCore != null)
        {
            float distanceToBase = Vector2.Distance(position, baseCore.position);

            if (distanceToBase < minChestDistanceFromBase)
            {
                return false;
            }

            if (distanceToBase > maxChestDistanceFromBase)
            {
                return false;
            }
        }

        if (currentWoodZone != null)
        {
            float distanceToWood = Vector2.Distance(position, currentWoodZone.transform.position);

            if (distanceToWood < minChestDistanceFromResourceZones)
            {
                return false;
            }
        }

        if (currentStoneZone != null)
        {
            float distanceToStone = Vector2.Distance(position, currentStoneZone.transform.position);

            if (distanceToStone < minChestDistanceFromResourceZones)
            {
                return false;
            }
        }

        return true;
    }

    void SpawnDailyResourceZones()
    {
        FindReferencesIfNeeded();

        if (baseCore == null)
        {
            Debug.LogWarning("ObjectiveSpawner no encontró BaseCore para spawnear recursos.");
            return;
        }

        if (replaceResourcesEachDay)
        {
            ClearResourceZones();
        }

        if (currentWoodZone == null && woodResourceZonePrefab != null)
        {
            Vector3 woodPosition;

            if (TryGetValidResourceSpawnPosition(out woodPosition, false, Vector3.zero))
            {
                currentWoodZone = Instantiate(woodResourceZonePrefab, woodPosition, Quaternion.identity);
            }
        }

        if (currentStoneZone == null && stoneResourceZonePrefab != null)
        {
            Vector3 stonePosition;

            Vector3 avoidPosition = currentWoodZone != null
                ? currentWoodZone.transform.position
                : Vector3.zero;

            bool hasAvoidPosition = currentWoodZone != null;

            if (TryGetValidResourceSpawnPosition(out stonePosition, hasAvoidPosition, avoidPosition))
            {
                currentStoneZone = Instantiate(stoneResourceZonePrefab, stonePosition, Quaternion.identity);
            }
        }

        Debug.Log("Zonas de madera y piedra generadas para el día.");
    }

    void ClearResourceZones()
    {
        if (currentWoodZone != null)
        {
            Destroy(currentWoodZone);
            currentWoodZone = null;
        }

        if (currentStoneZone != null)
        {
            Destroy(currentStoneZone);
            currentStoneZone = null;
        }
    }

    bool TryGetValidResourceSpawnPosition(
        out Vector3 spawnPosition,
        bool hasAvoidPosition,
        Vector3 avoidPosition
    )
    {
        for (int i = 0; i < maxResourceSpawnAttempts; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;

            if (randomDirection == Vector2.zero)
            {
                randomDirection = Vector2.right;
            }

            float distance = Random.Range(
                minResourceSpawnDistanceFromBase,
                maxResourceSpawnDistanceFromBase
            );

            Vector3 candidate = baseCore.position + (Vector3)(randomDirection * distance);

            if (player != null)
            {
                float distanceToPlayer = Vector2.Distance(candidate, player.position);

                if (distanceToPlayer < minResourceDistanceFromPlayer)
                {
                    continue;
                }
            }

            if (hasAvoidPosition)
            {
                float distanceToOtherZone = Vector2.Distance(candidate, avoidPosition);

                if (distanceToOtherZone < minDistanceBetweenResourceZones)
                {
                    continue;
                }
            }

            spawnPosition = candidate;
            return true;
        }

        spawnPosition = baseCore.position + Vector3.right * maxResourceSpawnDistanceFromBase;
        return false;
    }

    void CleanNullObjectives()
    {
        activeChests.RemoveAll(chest => chest == null);

        if (currentWoodZone == null)
        {
            currentWoodZone = null;
        }

        if (currentStoneZone == null)
        {
            currentStoneZone = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, minChestSpawnDistance);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.position, maxChestSpawnDistance);
        }

        if (baseCore != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(baseCore.position, minChestDistanceFromBase);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(baseCore.position, maxChestDistanceFromBase);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(baseCore.position, minResourceSpawnDistanceFromBase);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(baseCore.position, maxResourceSpawnDistanceFromBase);
        }
    }
}