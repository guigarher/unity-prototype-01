using UnityEngine;
using System.Collections;

public class EnemyRanged : MonoBehaviour
{
    private EnemyKnockback knockback;

    [Header("Target")]
    public Transform target;
    private Transform playerTarget;

    [Header("Comportamiento de noche")]
    public bool targetsBaseAtNight = true;

    [Header("Movement")]
    public float moveSpeed = 2.4f;

    [Header("Distancias")]
    public float preferredDistance = 5f;
    public float tooCloseDistance = 3f;
    public float attackRange = 7f;

    [Header("Separación entre enemigos")]
    public float separationRadius = 0.8f;
    public float separationStrength = 1.2f;
    public LayerMask enemyLayer;

    [Header("Ataque ranged")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 6f;
    public float attackWindUp = 0.45f;
    public float attackCooldown = 2.2f;
    public float projectileSpawnDistance = 0.6f;

    [Header("Daño")]
    public int damage = 6;
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

        // Si está demasiado cerca, primero se aleja.
        if (distanceToTarget < tooCloseDistance)
        {
            MoveAroundTarget(distanceToTarget);
            return;
        }

        // Si está a rango, dispara.
        if (distanceToTarget <= attackRange && cooldownTimer <= 0f)
        {
            StartCoroutine(AttackRoutine());
            return;
        }

        MoveAroundTarget(distanceToTarget);
    }

    void MoveAroundTarget(float distanceToTarget)
    {
        Vector2 directionToTarget = ((Vector2)target.position - rb.position).normalized;
        Vector2 movementDirection;

        if (distanceToTarget > preferredDistance)
        {
            movementDirection = directionToTarget;
        }
        else if (distanceToTarget < tooCloseDistance)
        {
            movementDirection = -directionToTarget;
        }
        else
        {
            movementDirection = new Vector2(-directionToTarget.y, directionToTarget.x);
        }

        Vector2 separation = CalculateSeparation();
        Vector2 finalDirection = movementDirection + separation * separationStrength;

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

        ShootProjectile();

        cooldownTimer = attackCooldown;
        isAttacking = false;
    }

    void ShootProjectile()
    {
        if (projectilePrefab == null || target == null) return;

        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;

        Vector3 spawnPosition = transform.position + (Vector3)(direction * projectileSpawnDistance);

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        EnemyProjectile projectile = projectileObject.GetComponent<EnemyProjectile>();

        if (projectile != null)
        {
            int projectileDamage = damage;

            if (target.GetComponentInParent<BaseCore>() != null)
            {
                projectileDamage = baseDamage;
            }

            projectile.Initialize(direction, projectileSpeed, projectileDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}