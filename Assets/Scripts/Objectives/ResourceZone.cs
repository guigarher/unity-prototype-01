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
    public bool hasLimitedPulses = true;
    public int maxPulses = 5;

    [Header("Si el jugador sale")]
    public bool loseProgressWhenOutside = true;
    public float progressDecaySpeed = 1.5f;

    [Header("Visual del pulso actual")]
    public Transform fillVisual;

    [Header("Circunferencia de progreso total")]
    public LineRenderer totalProgressRing;
    public float totalRingRadius = 2.65f;
    public float totalRingWidth = 0.12f;
    public int totalRingSegments = 80;
    public Color totalRingColor = new Color(1f, 0.75f, 0.1f, 1f);

    [Header("Texto opcional")]
    public TextMeshPro progressText;

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

        SetupTotalProgressRing();
        UpdateVisual();
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

    void UpdateVisual()
    {
        float pulsePercent = Mathf.Clamp01(progress / secondsPerPulse);

        if (fillVisual != null)
        {
            fillVisual.localScale = initialFillScale * pulsePercent;
        }

        if (hasLimitedPulses && maxPulses > 0)
        {
            float totalPercent = (completedPulses + pulsePercent) / maxPulses;
            totalPercent = Mathf.Clamp01(totalPercent);

            DrawProgressRing(totalProgressRing, totalPercent);
        }

        if (progressText != null && hasLimitedPulses)
        {
            progressText.text = completedPulses + " / " + maxPulses;
        }
    }

    void SetupTotalProgressRing()
    {
        if (totalProgressRing == null) return;

        totalProgressRing.useWorldSpace = false;
        totalProgressRing.loop = false;
        totalProgressRing.startWidth = totalRingWidth;
        totalProgressRing.endWidth = totalRingWidth;

        totalProgressRing.sortingLayerName = "Default";
        totalProgressRing.sortingOrder = 20;

        totalProgressRing.startColor = totalRingColor;
        totalProgressRing.endColor = totalRingColor;

        Material material = new Material(Shader.Find("Sprites/Default"));
        totalProgressRing.material = material;

        DrawProgressRing(totalProgressRing, 0f);
    }

    void DrawProgressRing(LineRenderer lineRenderer, float percent)
    {
        if (lineRenderer == null) return;

        percent = Mathf.Clamp01(percent);

        // Evita que se vea el trocito inicial del anillo cuando aún casi no hay progreso.
        if (percent < 0.03f)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        int visibleSegments = Mathf.Max(2, Mathf.CeilToInt(totalRingSegments * percent));
        lineRenderer.positionCount = visibleSegments + 1;

        float maxAngle = 360f * percent;

        for (int i = 0; i <= visibleSegments; i++)
        {
            float t = i / (float)visibleSegments;
            float angle = maxAngle * t;

            // Empieza arriba y gira en sentido horario.
            float radians = (angle + 90f) * Mathf.Deg2Rad;

            float x = Mathf.Cos(radians) * totalRingRadius;
            float y = Mathf.Sin(radians) * totalRingRadius;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
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