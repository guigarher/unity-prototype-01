using UnityEngine;
using System.Collections;

public class EnemyMelee : MonoBehaviour
{
    private EnemyKnockback knockback;

    [Header("Target")]
    public Transform target;
    private Transform playerTarget;

    [Header("Comportamiento de noche")]
    public bool targetsBaseAtNight = false;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Separación entre enemigos")]
    public float separationRadius = 0.7f;
    public float separationStrength = 1.2f;
    public LayerMask enemyLayer;

    [Header("Attack")]
    public float attackStartRange = 1.2f;
    public float attackHitRange = 1.1f;
    public float attackHitOffset = 0.9f;
    public float attackWindUp = 0.5f;
    public float attackCooldown = 1f;

    [Header("Aviso visual del ataque")]
    public Transform attackWarningVisual;
    public bool showAttackWarning = true;
    public float warningStartAlpha = 0.12f;
    public float warningEndAlpha = 0.65f;
    public float warningHitFlashTime = 0.08f;

    private SpriteRenderer attackWarningRenderer;
    private Color attackWarningBaseColor = Color.red;
    private float attackWarningProgress = 0f;

    [Header("Daño")]
    public int damage = 8;
    public int baseDamage = 1;

    private Rigidbody2D rb;
    private bool isAttacking = false;
    private float cooldownTimer = 0f;

    private Vector2 lockedAttackDirection = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        knockback = GetComponent<EnemyKnockback>();
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }

        CacheAttackWarningRenderer();
        HideAttackWarning();
        UpdateTargetByPhase();
    }

    void CacheAttackWarningRenderer()
    {
        if (attackWarningVisual == null) return;

        attackWarningRenderer = attackWarningVisual.GetComponentInChildren<SpriteRenderer>();

        if (attackWarningRenderer != null)
        {
            attackWarningBaseColor = attackWarningRenderer.color;
        }
    }

    void FixedUpdate()
    {
        if (knockback != null && knockback.IsBeingKnockedBack)
        {
            return;
        }

        if (target == null || rb == null) return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.fixedDeltaTime;
        }

        float distanceToTarget = GetEffectiveDistanceToTarget();

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAttackWarningVisual();
            return;
        }

        if (distanceToTarget > attackStartRange)
        {
            ChaseTargetWithSeparation();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;

            if (cooldownTimer <= 0f)
            {
                StartCoroutine(AttackRoutine());
            }
        }
    }

    void ChaseTargetWithSeparation()
    {
        Vector2 directionToTarget = ((Vector2)target.position - rb.position).normalized;
        Vector2 separation = CalculateSeparation();

        Vector2 finalDirection = directionToTarget + separation * separationStrength;

        if (finalDirection.sqrMagnitude > 1f)
        {
            finalDirection.Normalize();
        }

        float distanceToTarget = GetEffectiveDistanceToTarget();

        float slowStartDistance = 6f;
        float slowFullDistance = 16f;
        float minSpeedMultiplier = 0.45f;

        float t = Mathf.InverseLerp(slowStartDistance, slowFullDistance, distanceToTarget);
        float speedMultiplier = Mathf.Lerp(1f, minSpeedMultiplier, t);

        float currentMoveSpeed = moveSpeed * speedMultiplier;

        rb.linearVelocity = finalDirection * currentMoveSpeed;
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

    void OnEnable()
    {
        GamePhaseManager.OnPhaseChanged += OnPhaseChanged;
    }

    void OnDisable()
    {
        GamePhaseManager.OnPhaseChanged -= OnPhaseChanged;
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

        float distance = Vector2.Distance(transform.position, target.position);

        BaseCore baseCore = target.GetComponentInParent<BaseCore>();

        if (baseCore != null)
        {
            distance -= baseCore.enemyAttackRadius;
        }

        return Mathf.Max(0f, distance);
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        LockAttackDirection();

        attackWarningProgress = 0f;
        ShowAttackWarning();

        float timer = 0f;

        while (timer < attackWindUp)
        {
            timer += Time.deltaTime;
            attackWarningProgress = Mathf.Clamp01(timer / attackWindUp);

            UpdateAttackWarningVisual();

            yield return null;
        }

        attackWarningProgress = 1f;
        UpdateAttackWarningVisual();

        if (target != null && IsTargetInsideLockedAttackArea())
        {
            DamageTarget();
        }

        yield return new WaitForSeconds(warningHitFlashTime);

        HideAttackWarning();

        cooldownTimer = attackCooldown;
        isAttacking = false;
    }

    void LockAttackDirection()
    {
        if (target == null)
        {
            lockedAttackDirection = Vector2.right;
            return;
        }

        lockedAttackDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;

        if (lockedAttackDirection.sqrMagnitude < 0.01f)
        {
            lockedAttackDirection = Vector2.right;
        }
    }

    Vector2 GetAttackCenter()
    {
        return (Vector2)transform.position + lockedAttackDirection * attackHitOffset;
    }

    bool IsTargetInsideLockedAttackArea()
    {
        if (target == null) return false;

        Vector2 attackCenter = GetAttackCenter();

        BaseCore baseCore = target.GetComponentInParent<BaseCore>();

        if (baseCore != null)
        {
            float distanceToBase = Vector2.Distance(attackCenter, target.position);
            distanceToBase -= baseCore.enemyAttackRadius;

            return distanceToBase <= attackHitRange;
        }

        Collider2D targetCollider = target.GetComponentInChildren<Collider2D>();

        if (targetCollider != null)
        {
            Vector2 closestPoint = targetCollider.ClosestPoint(attackCenter);
            float distanceToCollider = Vector2.Distance(attackCenter, closestPoint);

            return distanceToCollider <= attackHitRange;
        }

        float distanceToTargetCenter = Vector2.Distance(attackCenter, target.position);
        return distanceToTargetCenter <= attackHitRange;
    }

    void DamageTarget()
    {
        PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            return;
        }

        BaseCore baseCore = target.GetComponentInParent<BaseCore>();

        if (baseCore != null)
        {
            baseCore.TakeDamage(baseDamage);
        }
    }

    void ShowAttackWarning()
    {
        if (!showAttackWarning) return;
        if (attackWarningVisual == null) return;

        attackWarningVisual.gameObject.SetActive(true);
        UpdateAttackWarningVisual();
    }

    void HideAttackWarning()
    {
        if (attackWarningVisual == null) return;

        if (attackWarningRenderer != null)
        {
            Color color = attackWarningBaseColor;
            color.a = 0f;
            attackWarningRenderer.color = color;
        }

        attackWarningVisual.gameObject.SetActive(false);
    }

    void UpdateAttackWarningVisual()
    {
        if (!showAttackWarning) return;
        if (attackWarningVisual == null) return;

        Vector2 attackCenter = GetAttackCenter();

        attackWarningVisual.position = attackCenter;
        attackWarningVisual.localScale = Vector3.one * (attackHitRange * 2f);

        if (attackWarningRenderer != null)
        {
            Color color = attackWarningBaseColor;
            color.a = Mathf.Lerp(warningStartAlpha, warningEndAlpha, attackWarningProgress);
            attackWarningRenderer.color = color;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackStartRange);

        Gizmos.color = Color.red;
        Vector2 previewDirection = lockedAttackDirection;

        if (previewDirection.sqrMagnitude < 0.01f)
        {
            previewDirection = Vector2.right;
        }

        Vector2 previewCenter = (Vector2)transform.position + previewDirection.normalized * attackHitOffset;
        Gizmos.DrawWireSphere(previewCenter, attackHitRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}