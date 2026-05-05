using UnityEngine;

public class XPOrb : MonoBehaviour
{
    public int xpValue = 1;
    public float moveSpeed = 6f;

    private Transform player;
    private PlayerXP playerXP;
    private PlayerStats stats;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogWarning("No se encontró Player para el orbe de XP.");
            return;
        }

        player = playerObject.transform;
        playerXP = playerObject.GetComponent<PlayerXP>();
        stats = playerObject.GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (player == null || stats == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > stats.pickupRange) return;

        Vector2 direction = (player.position - transform.position).normalized;
        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (playerXP != null)
        {
            playerXP.AddXP(xpValue);
        }

        Destroy(gameObject);
    }
}