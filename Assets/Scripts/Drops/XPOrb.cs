using UnityEngine;

public class XPOrb : MonoBehaviour
{
    public bool applyXPMultiplier = true;
    public int xpValue = 1;

    [Header("Movimiento")]
    public float moveSpeed = 8f;
    public float maxMoveSpeed = 18f;
    public float acceleration = 28f;

    [Header("Recogida")]
    public float pickupDelay = 0.35f;

    [Header("Visual")]
    public Transform visualRoot;
    public SpriteRenderer spriteRenderer;
    public bool overrideColor = true;
    public Color orbColor = Color.cyan;

    [Header("Animación visual suave")]
    public bool pulse = true;
    public float pulseAmount = 0.03f;
    public float pulseSpeed = 4f;
    public float rotateSpeed = 45f;
    public float floatAmplitude = 0.015f;
    public float floatFrequency = 3f;

    [Header("VFX opcional")]
    public GameObject collectVfxPrefab;

    private float pickupDelayTimer;
    private float currentMoveSpeed;

    private Transform player;
    private PlayerXP playerXP;
    private PlayerStats stats;

    private Vector3 initialVisualScale;
    private Vector3 initialVisualLocalPosition;

    private bool collected = false;
    private bool hasSeparateVisualRoot = false;

    void Start()
    {
        pickupDelayTimer = pickupDelay;
        currentMoveSpeed = moveSpeed;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogWarning("No se encontró Player para el orbe de XP.");
            return;
        }

        player = playerObject.transform;
        playerXP = playerObject.GetComponent<PlayerXP>();
        stats = playerObject.GetComponent<PlayerStats>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // Si no has asignado visualRoot, intentamos usar el SpriteRenderer,
        // pero solo si está en un hijo, NO en el objeto raíz.
        if (visualRoot == null && spriteRenderer != null && spriteRenderer.transform != transform)
        {
            visualRoot = spriteRenderer.transform;
        }

        hasSeparateVisualRoot = visualRoot != null && visualRoot != transform;

        if (hasSeparateVisualRoot)
        {
            initialVisualScale = visualRoot.localScale;
            initialVisualLocalPosition = visualRoot.localPosition;
        }

        if (spriteRenderer != null && overrideColor)
        {
            spriteRenderer.color = orbColor;
        }
    }

    void Update()
    {
        UpdateVisual();

        if (pickupDelayTimer > 0f)
        {
            pickupDelayTimer -= Time.deltaTime;
            return;
        }

        if (player == null || stats == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > stats.pickupRange) return;

        currentMoveSpeed = Mathf.MoveTowards(
            currentMoveSpeed,
            maxMoveSpeed,
            acceleration * Time.deltaTime
        );

        Vector2 direction = (player.position - transform.position).normalized;

        transform.position += (Vector3)(direction * currentMoveSpeed * Time.deltaTime);
    }

    void UpdateVisual()
    {
        // Importante: si no hay un hijo visual separado, NO tocamos posición/escala/rotación.
        // Así no rompemos el movimiento del orbe.
        if (!hasSeparateVisualRoot) return;

        float pulseScale = 1f;

        if (pulse)
        {
            pulseScale += Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        }

        visualRoot.localScale = initialVisualScale * Mathf.Max(0.01f, pulseScale);

        float floatOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        visualRoot.localPosition = initialVisualLocalPosition + Vector3.up * floatOffset;

        visualRoot.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        TryCollect(collision);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        TryCollect(collision);
    }

    void TryCollect(Collider2D collision)
    {
        if (collected) return;
        if (pickupDelayTimer > 0f) return;
        if (!collision.CompareTag("Player")) return;

        Collect();
    }

    void Collect()
    {
        if (collected) return;

        collected = true;

        if (playerXP != null)
        {
            playerXP.AddXP(xpValue, applyXPMultiplier);
        }

        if (collectVfxPrefab != null)
        {
            Instantiate(collectVfxPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}