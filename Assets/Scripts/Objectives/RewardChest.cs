using UnityEngine;

public class RewardChest : MonoBehaviour
{
    [Header("Recompensa principal")]
    public bool giveFullLevel = true;

    [Header("Monedas")]
    public GameObject coinPrefab;
    public int minCoinCount = 3;
    public int maxCoinCount = 6;

    [Header("Forma del drop")]
    public float dropCircleRadius = 1.2f;
    public float positionRandomness = 0.2f;

    [Header("Popup")]
    public GameObject resourcePopupPrefab;
    public Vector3 popupOffset = new Vector3(0f, 1f, 0f);

    [Header("Comportamiento")]
    public bool destroyOnCollect = true;

    private bool collected = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        Collect(other.gameObject);
    }

    void Collect(GameObject player)
    {
        collected = true;

        if (giveFullLevel)
        {
            PlayerXP playerXP = player.GetComponent<PlayerXP>();

            if (playerXP != null)
            {
                playerXP.AddExactXPForOneLevel();
                ShowPopup("+1 nivel");
            }
        }

        int coinCount = Random.Range(minCoinCount, maxCoinCount + 1);
        SpawnCoinsInCircle(coinCount);

        Debug.Log("Cofre abierto: +1 nivel y " + coinCount + " monedas.");

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
    }

    void SpawnCoinsInCircle(int count)
    {
        if (coinPrefab == null) return;
        if (count <= 0) return;

        float angleStep = 360f / count;
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            float radians = angle * Mathf.Deg2Rad;

            Vector2 direction = new Vector2(
                Mathf.Cos(radians),
                Mathf.Sin(radians)
            );

            Vector2 randomOffset = Random.insideUnitCircle * positionRandomness;

            Vector3 spawnPosition = transform.position +
                                    (Vector3)(direction * dropCircleRadius + randomOffset);

            Instantiate(coinPrefab, spawnPosition, Quaternion.identity);
        }
    }

    void ShowPopup(string text)
    {
        if (resourcePopupPrefab == null) return;

        GameObject popupObject = Instantiate(
            resourcePopupPrefab,
            transform.position + popupOffset,
            Quaternion.identity
        );

        ResourcePopup popup = popupObject.GetComponent<ResourcePopup>();

        if (popup != null)
        {
            popup.Initialize(text);
        }
    }
}