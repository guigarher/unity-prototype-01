using UnityEngine;

public class EnemyRepositioner : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Distancias")]
    public float maxDistanceFromPlayer = 24f;
    public float respawnDistance = 14f;

    [Header("Control")]
    public float checkInterval = 1.5f;

    private float checkTimer = 0f;

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
    }

    void Update()
    {
        if (player == null) return;

        checkTimer -= Time.deltaTime;

        if (checkTimer <= 0f)
        {
            checkTimer = checkInterval;
            CheckReposition();
        }
    }

    void CheckReposition()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > maxDistanceFromPlayer)
        {
            RepositionAroundPlayer();
        }
    }

    void RepositionAroundPlayer()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        if (randomDirection == Vector2.zero)
        {
            randomDirection = Vector2.right;
        }

        Vector3 newPosition = player.position + (Vector3)(randomDirection * respawnDistance);

        transform.position = newPosition;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}