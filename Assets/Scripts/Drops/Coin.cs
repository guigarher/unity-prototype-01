using UnityEngine;

public class Coin : MonoBehaviour
{
    public int value = 1;
    public float moveSpeed = 6f;

    [Header("Recogida")]
    public float pickupDelay = 0.5f;

    private float pickupDelayTimer;

    private Transform player;
    private PlayerCurrency playerCurrency;
    private PlayerStats stats;

    void Start()
    {
        pickupDelayTimer = pickupDelay;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogWarning("No se encontró Player para la moneda.");
            return;
        }

        player = playerObject.transform;
        playerCurrency = playerObject.GetComponent<PlayerCurrency>();
        stats = playerObject.GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (pickupDelayTimer > 0f)
        {
            pickupDelayTimer -= Time.deltaTime;
            return;
        }

        if (player == null || stats == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > stats.pickupRange) return;

        Vector2 direction = (player.position - transform.position).normalized;
        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryCollect(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryCollect(collision);
    }

    void TryCollect(Collider2D collision)
    {
        if (pickupDelayTimer > 0f) return;
        if (!collision.CompareTag("Player")) return;

        if (playerCurrency != null)
        {
            playerCurrency.AddCoins(value);
        }

        Destroy(gameObject);
    }
}