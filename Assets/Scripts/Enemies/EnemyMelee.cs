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
    public float attackHitRange = 1.6f;
    public float attackWindUp = 0.5f;
    public float attackCooldown = 1f;

    [Header("Daño")]
    public int damage = 8;
    public int baseDamage = 1;

    private Rigidbody2D rb;
    private bool isAttacking = false;
    private float cooldownTimer = 0f;

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

        UpdateTargetByPhase();
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

        yield return new WaitForSeconds(attackWindUp);

        if (target != null)
        {
            float distanceToTarget = GetEffectiveDistanceToTarget();

            if (distanceToTarget <= attackHitRange)
            {
                DamageTarget();
            }
        }

        cooldownTimer = attackCooldown;
        isAttacking = false;
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackStartRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackHitRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}