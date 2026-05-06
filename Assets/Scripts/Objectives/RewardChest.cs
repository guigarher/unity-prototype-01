using UnityEngine;

public class RewardChest : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject xpOrbPrefab;
    public GameObject coinPrefab;

    [Header("Cantidad aleatoria")]
    public int minXPOrbCount = 5;
    public int maxXPOrbCount = 9;

    public int minCoinCount = 3;
    public int maxCoinCount = 6;

    [Header("Forma del drop")]
    public float dropCircleRadius = 1.2f;
    public float positionRandomness = 0.2f;

    [Header("Comportamiento")]
    public bool destroyOnCollect = true;

    private bool collected = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        Collect();
    }

    void Collect()
    {
        collected = true;

        int xpCount = Random.Range(minXPOrbCount, maxXPOrbCount + 1);
        int coinCount = Random.Range(minCoinCount, maxCoinCount + 1);

        SpawnDropsInCircle(xpOrbPrefab, xpCount, 0f);
        SpawnDropsInCircle(coinPrefab, coinCount, 180f / Mathf.Max(coinCount, 1));

        Debug.Log("Cofre abierto: soltó " + xpCount + " orbes de XP y " + coinCount + " monedas.");

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
    }

    void SpawnDropsInCircle(GameObject prefab, int count, float angleOffset)
    {
        if (prefab == null) return;
        if (count <= 0) return;

        float angleStep = 360f / count;
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleOffset + angleStep * i;
            float radians = angle * Mathf.Deg2Rad;

            Vector2 direction = new Vector2(
                Mathf.Cos(radians),
                Mathf.Sin(radians)
            );

            Vector2 randomOffset = Random.insideUnitCircle * positionRandomness;

            Vector3 spawnPosition = transform.position +
                                    (Vector3)(direction * dropCircleRadius + randomOffset);

            Instantiate(prefab, spawnPosition, Quaternion.identity);
        }
    }
}