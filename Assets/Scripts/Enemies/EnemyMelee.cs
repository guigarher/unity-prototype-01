using UnityEngine;
using System.Collections;

public class EnemyMelee : MonoBehaviour
{
    private EnemyKnockback knockback;

    [Header("Target")]
    public Transform target;

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
    public int damage = 8;

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
        if (target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                target = playerObject.transform;
            }
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

        float distanceToPlayer = Vector2.Distance(transform.position, target.position);

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (distanceToPlayer > attackStartRange)
        {
            ChasePlayerWithSeparation();
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

    void ChasePlayerWithSeparation()
    {
        Vector2 directionToPlayer = ((Vector2)target.position - rb.position).normalized;
        Vector2 separation = CalculateSeparation();

        Vector2 finalDirection = directionToPlayer + separation * separationStrength;

        if (finalDirection.sqrMagnitude > 1f)
        {
            finalDirection.Normalize();
        }

        float distanceToPlayer = Vector2.Distance(transform.position, target.position);

        float slowStartDistance = 6f;
        float slowFullDistance = 16f;
        float minSpeedMultiplier = 0.45f;

        float t = Mathf.InverseLerp(slowStartDistance, slowFullDistance, distanceToPlayer);
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

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(attackWindUp);

        if (target != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, target.position);

            if (distanceToPlayer <= attackHitRange)
            {
                PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
            }
        }

        cooldownTimer = attackCooldown;
        isAttacking = false;
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