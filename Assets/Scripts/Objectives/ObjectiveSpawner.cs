using UnityEngine;
using System.Collections.Generic;

public class ObjectiveSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public GameObject chestPrefab;

    [Header("Tiempo")]
    public float firstChestDelay = 12f;
    public float minChestInterval = 25f;
    public float maxChestInterval = 40f;

    [Header("Distancia de aparición")]
    public float minSpawnDistance = 6f;
    public float maxSpawnDistance = 10f;

    [Header("Límite")]
    public int maxActiveChests = 1;

    private float chestTimer;
    private List<GameObject> activeChests = new List<GameObject>();

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
    }

    void Update()
    {
        if (player == null || chestPrefab == null) return;

        CleanNullChests();

        if (activeChests.Count >= maxActiveChests)
        {
            return;
        }

        chestTimer -= Time.deltaTime;

        if (chestTimer <= 0f)
        {
            SpawnChest();
            chestTimer = Random.Range(minChestInterval, maxChestInterval);
        }
    }

    void SpawnChest()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        if (randomDirection == Vector2.zero)
        {
            randomDirection = Vector2.right;
        }

        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

        Vector3 spawnPosition = player.position + (Vector3)(randomDirection * distance);

        GameObject newChest = Instantiate(chestPrefab, spawnPosition, Quaternion.identity);

        activeChests.Add(newChest);

        Debug.Log("Ha aparecido un cofre cerca. Ve a por él.");
    }

    void CleanNullChests()
    {
        activeChests.RemoveAll(chest => chest == null);
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