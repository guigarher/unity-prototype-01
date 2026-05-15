using UnityEngine;
using TMPro;

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
    public float secondsPerPulse = 1.5f;
    public int minResourcePerPulse = 1;
    public int maxResourcePerPulse = 2;

    [Header("Límite de la zona")]
    public bool hasLimitedPulses = false;
    public int maxPulses = 5;

    [Header("Zona infinita durante el día")]
    public bool infiniteZone = true;
    public float secondsIncreasePerPulse = 0.35f;
    public float maxSecondsPerPulse = 4.5f;
    public int resourceBonusPerPulse = 1;
    public int maxResourceBonus = 6;

    [Header("Si el jugador sale")]
    public bool loseProgressWhenOutside = true;
    public float progressDecaySpeed = 1.5f;

    [Header("Visual del pulso actual")]
    public Transform fillVisual;

    [Header("Texto opcional")]
    public TextMeshPro progressText;

    [Header("Popup")]
    public GameObject resourcePopupPrefab;
    public Vector3 popupOffset = new Vector3(0f, 1f, 0f);

    private bool playerInside = false;
    private float progress = 0f;
    private int completedPulses = 0;
    private int currentResourceBonus = 0;

    private Vector3 initialFillScale;
    private PlayerResources playerResources;

    void Start()
    {
        if (fillVisual != null)
        {
            initialFillScale = fillVisual.localScale;
            fillVisual.localScale = Vector3.zero;
            fillVisual.gameObject.SetActive(false);
        }

        UpdateVisual();
    }

    void Update()
    {
        UpdateProgress();
        UpdateVisual();
    }

    void UpdateProgress()
    {
        if (GamePhaseManager.Instance != null && GamePhaseManager.Instance.IsNight())
        {
            return;
        }

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
        amount += currentResourceBonus;

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

        Debug.Log(
            "Pulso completado en zona de " + resourceType +
            ". Pulso: " + completedPulses +
            " | Recurso ganado: " + amount +
            " | Siguiente pulso tarda: " + secondsPerPulse.ToString("0.00") + "s"
        );

        if (infiniteZone)
        {
            secondsPerPulse = Mathf.Min(
                maxSecondsPerPulse,
                secondsPerPulse + secondsIncreasePerPulse
            );

            currentResourceBonus = Mathf.Min(
                maxResourceBonus,
                currentResourceBonus + resourceBonusPerPulse
            );

            return;
        }

        if (hasLimitedPulses && completedPulses >= maxPulses)
        {
            Destroy(gameObject);
        }
    }

    void UpdateVisual()
    {
        float pulsePercent = Mathf.Clamp01(progress / secondsPerPulse);

        if (fillVisual != null)
        {
            if (pulsePercent < 0.03f)
            {
                fillVisual.gameObject.SetActive(false);
                fillVisual.localScale = Vector3.zero;
            }
            else
            {
                fillVisual.gameObject.SetActive(true);
                fillVisual.localScale = initialFillScale * pulsePercent;
            }
        }

        if (progressText != null)
        {
            if (infiniteZone)
            {
                int minNext = minResourcePerPulse + currentResourceBonus;
                int maxNext = maxResourcePerPulse + currentResourceBonus;

                progressText.text = minNext + "-" + maxNext;
            }
            else if (hasLimitedPulses)
            {
                progressText.text = completedPulses + " / " + maxPulses;
            }
            else
            {
                progressText.text = "";
            }
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