using UnityEngine;
using System.Collections.Generic;

public class ObjectiveSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Cofres")]
    public GameObject chestPrefab;
    public float firstChestDelay = 12f;
    public float minChestInterval = 25f;
    public float maxChestInterval = 40f;
    public int maxActiveChests = 1;

    [Header("Zonas de recursos")]
    public GameObject resourceZonePrefab;
    public float firstResourceZoneDelay = 18f;
    public float minResourceZoneInterval = 35f;
    public float maxResourceZoneInterval = 55f;
    public int maxActiveResourceZones = 1;

    [Header("Distancia de aparición")]
    public float minSpawnDistance = 6f;
    public float maxSpawnDistance = 10f;

    private float chestTimer;
    private float resourceZoneTimer;

    private List<GameObject> activeChests = new List<GameObject>();
    private List<GameObject> activeResourceZones = new List<GameObject>();

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

        chestTimer = firstChestDelay;
        resourceZoneTimer = firstResourceZoneDelay;
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
        HandleResourceZoneSpawning();
    }

    void HandleChestSpawning()
    {
        if (chestPrefab == null) return;
        if (activeChests.Count >= maxActiveChests) return;

        chestTimer -= Time.deltaTime;

        if (chestTimer <= 0f)
        {
            GameObject newChest = SpawnObjective(chestPrefab);
            activeChests.Add(newChest);

            chestTimer = Random.Range(minChestInterval, maxChestInterval);

            Debug.Log("Ha aparecido un cofre.");
        }
    }

    void HandleResourceZoneSpawning()
    {
        if (resourceZonePrefab == null) return;
        if (activeResourceZones.Count >= maxActiveResourceZones) return;

        resourceZoneTimer -= Time.deltaTime;

        if (resourceZoneTimer <= 0f)
        {
            GameObject newResourceZone = SpawnObjective(resourceZonePrefab);
            activeResourceZones.Add(newResourceZone);

            resourceZoneTimer = Random.Range(minResourceZoneInterval, maxResourceZoneInterval);

            Debug.Log("Ha aparecido una zona de recursos.");
        }
    }

    GameObject SpawnObjective(GameObject prefab)
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        if (randomDirection == Vector2.zero)
        {
            randomDirection = Vector2.right;
        }

        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

        Vector3 spawnPosition = player.position + (Vector3)(randomDirection * distance);

        return Instantiate(prefab, spawnPosition, Quaternion.identity);
    }

    void CleanNullObjectives()
    {
        activeChests.RemoveAll(chest => chest == null);
        activeResourceZones.RemoveAll(zone => zone == null);
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, minSpawnDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, maxSpawnDistance);
    }
}