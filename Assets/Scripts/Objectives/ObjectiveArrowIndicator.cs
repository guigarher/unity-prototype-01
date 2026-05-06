using UnityEngine;

public class ObjectiveArrowIndicator : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public Transform arrowTransform;

    [Header("Configuración")]
    public float arrowDistanceFromPlayer = 1.6f;
    public float hideDistance = 1.2f;
    public float scanInterval = 0.25f;

    [Header("Rotación")]
    public float rotationOffset = -90f;

    private ObjectiveTarget currentTarget;
    private float scanTimer = 0f;

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

        if (arrowTransform != null)
        {
            arrowTransform.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (player == null || arrowTransform == null) return;

        scanTimer -= Time.deltaTime;

        if (scanTimer <= 0f)
        {
            FindBestTarget();
            scanTimer = scanInterval;
        }

        UpdateArrow();
    }

    void FindBestTarget()
    {
        ObjectiveTarget[] targets = Object.FindObjectsByType<ObjectiveTarget>(FindObjectsInactive.Exclude);

        ObjectiveTarget bestTarget = null;
        float bestScore = Mathf.Infinity;

        foreach (ObjectiveTarget target in targets)
        {
            if (target == null) continue;

            float distance = Vector2.Distance(player.position, target.transform.position);

            // Menor score = mejor objetivo.
            // La prioridad resta, así que objetivos importantes ganan.
            float score = distance - target.priority * 5f;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        currentTarget = bestTarget;
    }

    void UpdateArrow()
    {
        if (currentTarget == null)
        {
            arrowTransform.gameObject.SetActive(false);
            return;
        }

        Vector2 direction = currentTarget.transform.position - player.position;
        float distance = direction.magnitude;

        if (distance <= hideDistance)
        {
            arrowTransform.gameObject.SetActive(false);
            return;
        }

        direction.Normalize();

        arrowTransform.gameObject.SetActive(true);

        arrowTransform.position = player.position + (Vector3)(direction * arrowDistanceFromPlayer);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowTransform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
    }
}