using UnityEngine;

public class ObjectiveArrowIndicator : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Flecha para cofres")]
    public Transform chestArrowTransform;

    [Header("Flecha para recursos")]
    public Transform resourceArrowTransform;

    [Header("Configuración")]
    public float chestArrowDistanceFromPlayer = 1.6f;
    public float resourceArrowDistanceFromPlayer = 2.1f;
    public float hideDistance = 1.2f;
    public float scanInterval = 0.25f;

    [Header("Rotación")]
    public float rotationOffset = -90f;

    private ObjectiveTarget currentChestTarget;
    private ObjectiveTarget currentResourceTarget;

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

        HideArrow(chestArrowTransform);
        HideArrow(resourceArrowTransform);
    }

    void Update()
    {
        if (player == null) return;

        scanTimer -= Time.deltaTime;

        if (scanTimer <= 0f)
        {
            currentChestTarget = FindNearestTargetOfType(ObjectiveType.Chest);
            currentResourceTarget = FindNearestTargetOfType(ObjectiveType.FarmZone);

            scanTimer = scanInterval;
        }

        UpdateArrow(
            chestArrowTransform,
            currentChestTarget,
            chestArrowDistanceFromPlayer
        );

        UpdateArrow(
            resourceArrowTransform,
            currentResourceTarget,
            resourceArrowDistanceFromPlayer
        );
    }

    ObjectiveTarget FindNearestTargetOfType(ObjectiveType objectiveType)
    {
        ObjectiveTarget[] targets = Object.FindObjectsByType<ObjectiveTarget>(
            FindObjectsInactive.Exclude
        );

        ObjectiveTarget nearestTarget = null;
        float nearestDistance = Mathf.Infinity;

        foreach (ObjectiveTarget target in targets)
        {
            if (target == null) continue;
            if (target.objectiveType != objectiveType) continue;

            float distance = Vector2.Distance(player.position, target.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = target;
            }
        }

        return nearestTarget;
    }

    void UpdateArrow(Transform arrowTransform, ObjectiveTarget target, float distanceFromPlayer)
    {
        if (arrowTransform == null) return;

        if (target == null)
        {
            HideArrow(arrowTransform);
            return;
        }

        Vector2 direction = target.transform.position - player.position;
        float distance = direction.magnitude;

        if (distance <= hideDistance)
        {
            HideArrow(arrowTransform);
            return;
        }

        direction.Normalize();

        arrowTransform.gameObject.SetActive(true);

        arrowTransform.position = player.position + (Vector3)(direction * distanceFromPlayer);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        arrowTransform.rotation = Quaternion.Euler(
            0f,
            0f,
            angle + rotationOffset
        );
    }

    void HideArrow(Transform arrowTransform)
    {
        if (arrowTransform == null) return;

        arrowTransform.gameObject.SetActive(false);
    }
}