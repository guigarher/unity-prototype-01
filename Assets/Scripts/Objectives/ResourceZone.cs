using UnityEngine;

public enum ResourceType
{
    Wood,
    Stone
}

public class ResourceZone : MonoBehaviour
{
    [Header("Tipo de recurso")]
    public ResourceType resourceType = ResourceType.Wood;

    [Header("Carga por pulso")]
    public float secondsPerPulse = 3f;
    public int minResourcePerPulse = 1;
    public int maxResourcePerPulse = 2;

    [Header("Límite de la zona")]
    public bool hasLimitedPulses = true;
    public int maxPulses = 5;

    [Header("Si el jugador sale")]
    public bool loseProgressWhenOutside = true;
    public float progressDecaySpeed = 1.5f;

    [Header("Visual")]
    public Transform fillVisual;

    [Header("Popup")]
    public GameObject resourcePopupPrefab;
    public Vector3 popupOffset = new Vector3(0f, 1f, 0f);

    private bool playerInside = false;
    private float progress = 0f;
    private int completedPulses = 0;
    private Vector3 initialFillScale;

    private PlayerResources playerResources;

    void Start()
    {
        if (fillVisual != null)
        {
            initialFillScale = fillVisual.localScale;
            fillVisual.localScale = Vector3.zero;
        }
    }

    void Update()
    {
        UpdateProgress();
        UpdateVisual();
    }

    void UpdateProgress()
    {
        if (playerInside)
        {
            progress += Time.deltaTime;

            if (progress >= secondsPerPulse)
            {
                CompletePulse();
            }
        }
        else if (loseProgressWhenOutside)
        {
            progress -= progressDecaySpeed * Time.deltaTime;
            progress = Mathf.Max(0f, progress);
        }
    }

    void CompletePulse()
    {
        progress = 0f;
        completedPulses++;

        int amount = Random.Range(minResourcePerPulse, maxResourcePerPulse + 1);

        if (playerResources != null)
        {
            switch (resourceType)
            {
                case ResourceType.Wood:
                    playerResources.AddWood(amount);
                    ShowResourcePopup("+" + amount + " madera");
                    break;

                case ResourceType.Stone:
                    playerResources.AddStone(amount);
                    ShowResourcePopup("+" + amount + " piedra");
                    break;
            }
        }

        Debug.Log("Pulso completado en zona de " + resourceType + ". Pulsos: " + completedPulses + "/" + maxPulses);

        if (hasLimitedPulses && completedPulses >= maxPulses)
        {
            Destroy(gameObject);
        }
    }

    void ShowResourcePopup(string text)
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

    void UpdateVisual()
    {
        if (fillVisual == null) return;

        float percent = progress / secondsPerPulse;
        fillVisual.localScale = initialFillScale * percent;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        playerResources = other.GetComponent<PlayerResources>();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
    }
}