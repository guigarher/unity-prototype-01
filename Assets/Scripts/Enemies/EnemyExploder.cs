using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyExploder : MonoBehaviour
{
    private EnemyKnockback knockback;
    private EnemyHealth enemyHealth;

    [Header("Target")]
    public Transform target;
    private Transform playerTarget;

    [Header("Comportamiento de noche")]
    public bool targetsBaseAtNight = false;

    [Header("Movimiento")]
    public float moveSpeed = 2.8f;

    [Header("Separación entre enemigos")]
    public float separationRadius = 0.8f;
    public float separationStrength = 1.2f;
    public LayerMask enemyLayer;

    [Header("Explosión")]
    public float explosionTriggerRange = 0.9f;
    public float explosionRadius = 2.4f;
    public float explosionWindUp = 0.55f;

    [Header("Daño de explosión")]
    public int damageToPlayer = 20;
    public int damageToBase = 0;
    public int damageToEnemies = 60;
    public bool damageEnemies = true;

    [Header("Feedback de carga")]
    public bool useChargeFeedback = true;
    public Transform visualRoot;
    public Color chargeColor = new Color(1f, 0.35f, 0.05f, 1f);
    public float blinkSpeed = 12f;
    public float shakeAmount = 0.06f;

    [Header("Visual al explotar opcional")]
    public GameObject explosionVisualPrefab;
    public float explosionVisualLifetime = 0.35f;

    [Header("Debug")]
    public bool logExplosion = true;

    private Rigidbody2D rb;

    private bool isChargingExplosion = false;
    private bool hasExploded = false;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Vector3 originalVisualLocalPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        knockback = GetComponent<EnemyKnockback>();
        enemyHealth = GetComponent<EnemyHealth>();

        CacheVisuals();
    }

    void OnEnable()
    {
        GamePhaseManager.OnPhaseChanged += OnPhaseChanged;

        if (enemyHealth != null)
        {
            enemyHealth.OnDeath += OnEnemyDeath;
        }
    }

    void OnDisable()
    {
        GamePhaseManager.OnPhaseChanged -= OnPhaseChanged;

        if (enemyHealth != null)
        {
            enemyHealth.OnDeath -= OnEnemyDeath;
        }
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }

        UpdateTargetByPhase();
    }

    void FixedUpdate()
    {
        if (hasExploded) return;

        if (knockback != null && knockback.IsBeingKnockedBack)
        {
            return;
        }

        if (rb == null || target == null) return;

        if (isChargingExplosion)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distanceToTarget = GetEffectiveDistanceToTarget();

        if (distanceToTarget <= explosionTriggerRange)
        {
            StartCoroutine(ExplosionChargeRoutine());
            return;
        }

        ChaseTargetWithSeparation();
    }

    void CacheVisuals()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }

        if (visualRoot == null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null && spriteRenderers[i].transform != transform)
                {
                    visualRoot = spriteRenderers[i].transform;
                    break;
                }
            }
        }

        if (visualRoot != null)
        {
            originalVisualLocalPosition = visualRoot.localPosition;
        }
    }

    void ChaseTargetWithSeparation()
    {
        if (target == null || rb == null) return;

        Vector2 directionToTarget = ((Vector2)target.position - rb.position).normalized;
        Vector2 separation = CalculateSeparation();

        Vector2 finalDirection = directionToTarget + separation * separationStrength;

        if (finalDirection.sqrMagnitude > 1f)
        {
            finalDirection.Normalize();
        }

        rb.linearVelocity = finalDirection * moveSpeed;
    }

    Vector2 CalculateSeparation()
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(
            transform.position,
            separationRadius,
            enemyLayer
        );

        Vector2 separation = Vector2.zero;

        foreach (Collider2D enemy in nearbyEnemies)
        {
            if (enemy.gameObject == gameObject) continue;

            Vector2 awayFromEnemy = (Vector2)transform.position - (Vector2)enemy.transform.position;
            float distance = awayFromEnemy.magnitude;

            if (distance > 0f)
            {
                separation += awayFromEnemy.normalized / distance;
            }
        }

        return separation;
    }

    void OnPhaseChanged(GamePhase phase)
    {
        UpdateTargetByPhase();
    }

    void UpdateTargetByPhase()
    {
        if (targetsBaseAtNight &&
            GamePhaseManager.Instance != null &&
            GamePhaseManager.Instance.IsNight() &&
            BaseCore.Instance != null)
        {
            target = BaseCore.Instance.transform;
        }
        else
        {
            target = playerTarget;
        }
    }

    float GetEffectiveDistanceToTarget()
    {
        if (target == null) return Mathf.Infinity;

        BaseCore baseCore = target.GetComponentInParent<BaseCore>();

        if (baseCore != null)
        {
            float distanceToBase = Vector2.Distance(transform.position, target.position);
            distanceToBase -= baseCore.enemyAttackRadius;

            return Mathf.Max(0f, distanceToBase);
        }

        Collider2D targetCollider = target.GetComponentInChildren<Collider2D>();

        if (targetCollider != null)
        {
            Vector2 closestPoint = targetCollider.ClosestPoint(transform.position);
            float distanceToCollider = Vector2.Distance(transform.position, closestPoint);

            return Mathf.Max(0f, distanceToCollider);
        }

        return Vector2.Distance(transform.position, target.position);
    }

    IEnumerator ExplosionChargeRoutine()
    {
        if (isChargingExplosion || hasExploded) yield break;

        isChargingExplosion = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        float timer = 0f;

        while (timer < explosionWindUp)
        {
            if (hasExploded) yield break;

            timer += Time.deltaTime;

            UpdateChargeFeedback(timer / explosionWindUp);

            yield return null;
        }

        RestoreVisuals();
        Explode();

        yield return new WaitForSeconds(0.02f);

        Destroy(gameObject);
    }

    void UpdateChargeFeedback(float progress)
    {
        if (!useChargeFeedback) return;

        float blink = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
        Color currentColor = Color.Lerp(Color.white, chargeColor, blink);

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = Color.Lerp(
                    originalColors[i],
                    currentColor,
                    Mathf.Clamp01(progress)
                );
            }
        }

        if (visualRoot != null)
        {
            Vector2 shake = Random.insideUnitCircle * shakeAmount * progress;
            visualRoot.localPosition = originalVisualLocalPosition + (Vector3)shake;
        }
    }

    void RestoreVisuals()
    {
        if (spriteRenderers != null && originalColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null && i < originalColors.Length)
                {
                    spriteRenderers[i].color = originalColors[i];
                }
            }
        }

        if (visualRoot != null)
        {
            visualRoot.localPosition = originalVisualLocalPosition;
        }
    }

    void OnEnemyDeath()
    {
        if (hasExploded) return;
        if (isChargingExplosion) return;

        StartCoroutine(ExplosionChargeRoutine());
    }

    void Explode()
    {
        if (hasExploded) return;

        hasExploded = true;
        isChargingExplosion = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        RestoreVisuals();
        SpawnExplosionVisual();
        DamageThingsInExplosion();

        if (logExplosion)
        {
            Debug.Log("Enemigo explosivo explotó en " + transform.position);
        }
    }

    void DamageThingsInExplosion()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            explosionRadius
        );

        HashSet<PlayerHealth> damagedPlayers = new HashSet<PlayerHealth>();
        HashSet<BaseCore> damagedBases = new HashSet<BaseCore>();
        HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null && !damagedPlayers.Contains(playerHealth))
            {
                damagedPlayers.Add(playerHealth);
                playerHealth.TakeDamage(damageToPlayer);
                continue;
            }

            BaseCore baseCore = hit.GetComponentInParent<BaseCore>();

            if (baseCore != null && damageToBase > 0 && !damagedBases.Contains(baseCore))
            {
                damagedBases.Add(baseCore);
                baseCore.TakeDamage(damageToBase);
                continue;
            }

            if (!damageEnemies) continue;

            EnemyHealth otherEnemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (otherEnemyHealth == null) continue;
            if (otherEnemyHealth == enemyHealth) continue;
            if (damagedEnemies.Contains(otherEnemyHealth)) continue;

            damagedEnemies.Add(otherEnemyHealth);
            otherEnemyHealth.TakeDamage(damageToEnemies, false);
        }
    }

    void SpawnExplosionVisual()
    {
        if (explosionVisualPrefab == null) return;

        GameObject visual = Instantiate(
            explosionVisualPrefab,
            transform.position,
            Quaternion.identity
        );

        visual.transform.localScale = Vector3.one * (explosionRadius * 2f);

        Destroy(visual, explosionVisualLifetime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionTriggerRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}