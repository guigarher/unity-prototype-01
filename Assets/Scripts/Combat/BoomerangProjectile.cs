using UnityEngine;
using System.Collections.Generic;

public class BoomerangProjectile : MonoBehaviour
{
    private enum BoomerangState
    {
        GoingOut,
        Returning
    }

    [Header("Visual ligado al hitbox")]
    public Transform visualRoot;
    public float visualReferenceHitRadius = 0.45f;
    public bool scaleVisualWithHitRadius = true;

    private Vector3 baseVisualScale = Vector3.one;

    [Header("Movimiento")]
    public float outwardSpeed = 12f;
    public float returnSpeed = 12f;
    public float speedChangeRate = 25f;

    [Header("Curva")]
    public float outwardCurveStrength = 0.65f;
    public float outwardTurnSpeedDegrees = 720f;
    public float returnTurnSpeedDegrees = 520f;
    public bool curveToLeft = true;

    [Header("Retorno")]
    public float returnDistance = 0.65f;
    public float safetyLifetime = 8f;

    private BoomerangState state = BoomerangState.GoingOut;

    private float knockbackForce;
    private float knockbackDuration;
    private BoomerangWeapon ownerWeapon;
    private Transform ownerTransform;

    private Vector2 forwardDirection;
    private Vector2 sideDirection;
    private Vector2 currentMoveDirection;

    private int damage;
    private bool isCrit;

    private float forwardDistance;
    private float hitRadius;
    private LayerMask enemyLayer;

    private int bleedDamagePerTick;
    private float bleedDuration;
    private float bleedTickInterval;
    private float bleedChance;

    private float travelledDistance = 0f;
    private float currentSpeed;
    private float lifeTimer = 0f;

    private bool hasNotifiedOwner = false;

    private Dictionary<EnemyHealth, float> hitCooldowns = new Dictionary<EnemyHealth, float>();
    private float sameEnemyHitCooldown = 0.20f;

    void Awake()
    {
        if (visualRoot == null)
        {
            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                visualRoot = spriteRenderer.transform;
            }
            else
            {
                visualRoot = transform;
            }
        }

        if (visualRoot != null)
        {
            baseVisualScale = visualRoot.localScale;
        }
    }

    public void Initialize(
        BoomerangWeapon ownerWeapon,
        Transform ownerTransform,
        Vector2 direction,
        int damage,
        bool isCrit,
        float flightDuration,
        float forwardDistance,
        float sideDistance,
        float behindDistance,
        float hitRadius,
        LayerMask enemyLayer,
        int bleedDamagePerTick,
        float bleedDuration,
        float bleedTickInterval,
        float bleedChance,
        float knockbackForce,
        float knockbackDuration
    )
    {
        this.ownerWeapon = ownerWeapon;
        this.ownerTransform = ownerTransform;

        this.damage = damage;
        this.isCrit = isCrit;

        this.forwardDistance = forwardDistance;
        this.hitRadius = hitRadius;
        UpdateVisualScaleFromHitRadius();
        this.enemyLayer = enemyLayer;

        this.bleedDamagePerTick = bleedDamagePerTick;
        this.bleedDuration = bleedDuration;
        this.bleedTickInterval = bleedTickInterval;
        this.bleedChance = Mathf.Clamp01(bleedChance);
        this.knockbackForce = knockbackForce;
        this.knockbackDuration = knockbackDuration;

        forwardDirection = direction.normalized;

        if (forwardDirection == Vector2.zero)
        {
            forwardDirection = Vector2.right;
        }

        sideDirection = curveToLeft
            ? new Vector2(-forwardDirection.y, forwardDirection.x)
            : new Vector2(forwardDirection.y, -forwardDirection.x);

        currentMoveDirection = forwardDirection;
        currentSpeed = outwardSpeed;

        travelledDistance = 0f;
        lifeTimer = 0f;
        state = BoomerangState.GoingOut;
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;

        if (state == BoomerangState.GoingOut)
        {
            MoveOutwardWithCurve();
        }
        else
        {
            ReturnSmoothlyToPlayer();
        }

        transform.Rotate(0f, 0f, 900f * Time.deltaTime);

        UpdateHitCooldowns();
        DamageEnemiesInRange();

        if (lifeTimer >= safetyLifetime)
        {
            DestroyBoomerang();
        }
    }

    void UpdateVisualScaleFromHitRadius()
    {
        if (!scaleVisualWithHitRadius) return;
        if (visualRoot == null) return;
        if (visualReferenceHitRadius <= 0f) return;

        float scaleMultiplier = hitRadius / visualReferenceHitRadius;

        visualRoot.localScale = baseVisualScale * scaleMultiplier;
    }

    void MoveOutwardWithCurve()
    {
        float progress = Mathf.Clamp01(travelledDistance / forwardDistance);

        // Curva suave: empieza casi recto, se abre en mitad de la ida y luego estabiliza.
        float curveAmount = Mathf.Sin(progress * Mathf.PI) * outwardCurveStrength;

        Vector2 desiredDirection = (forwardDirection + sideDirection * curveAmount).normalized;

        currentMoveDirection = RotateDirectionTowards(
            currentMoveDirection,
            desiredDirection,
            outwardTurnSpeedDegrees
        );

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            outwardSpeed,
            speedChangeRate * Time.deltaTime
        );

        Vector3 movement = (Vector3)(currentMoveDirection * currentSpeed * Time.deltaTime);

        transform.position += movement;
        travelledDistance += movement.magnitude;

        if (travelledDistance >= forwardDistance)
        {
            state = BoomerangState.Returning;
        }
    }

    void ReturnSmoothlyToPlayer()
    {
        if (ownerTransform == null)
        {
            return;
        }

        Vector2 toPlayer = ownerTransform.position - transform.position;

        if (toPlayer.sqrMagnitude <= returnDistance * returnDistance)
        {
            DestroyBoomerang();
            return;
        }

        Vector2 desiredDirection = toPlayer.normalized;

        // La clave: no gira de golpe. Va corrigiendo la dirección poco a poco.
        currentMoveDirection = RotateDirectionTowards(
            currentMoveDirection,
            desiredDirection,
            returnTurnSpeedDegrees
        );

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            returnSpeed,
            speedChangeRate * Time.deltaTime
        );

        transform.position += (Vector3)(currentMoveDirection * currentSpeed * Time.deltaTime);
    }

    Vector2 RotateDirectionTowards(Vector2 currentDirection, Vector2 desiredDirection, float turnSpeedDegrees)
    {
        float maxRadiansDelta = turnSpeedDegrees * Mathf.Deg2Rad * Time.deltaTime;

        Vector3 rotated = Vector3.RotateTowards(
            currentDirection,
            desiredDirection,
            maxRadiansDelta,
            0f
        );

        Vector2 result = rotated;

        if (result.sqrMagnitude <= 0.001f)
        {
            return desiredDirection;
        }

        return result.normalized;
    }

    void DamageEnemiesInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            hitRadius,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();

            if (enemyHealth == null)
            {
                enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth == null) continue;

            if (hitCooldowns.ContainsKey(enemyHealth))
            {
                continue;
            }

            enemyHealth.TakeDamage(damage, isCrit);
            ApplyKnockbackToEnemy(hit, enemyHealth);
            TryApplyBleed(enemyHealth);

            hitCooldowns.Add(enemyHealth, sameEnemyHitCooldown);
        }
    }

    void ApplyKnockbackToEnemy(Collider2D hit, EnemyHealth enemyHealth)
    {
        EnemyKnockback enemyKnockback = hit.GetComponent<EnemyKnockback>();

        if (enemyKnockback == null)
        {
            enemyKnockback = hit.GetComponentInParent<EnemyKnockback>();
        }

        if (enemyKnockback == null) return;

        Vector2 knockDirection =
            ((Vector2)enemyHealth.transform.position - (Vector2)transform.position).normalized;

        if (knockDirection.sqrMagnitude < 0.01f)
        {
            knockDirection = Vector2.right;
        }

        enemyKnockback.ApplyKnockback(
            knockDirection,
            knockbackForce,
            knockbackDuration
        );
    }

    void TryApplyBleed(EnemyHealth enemyHealth)
    {
        if (Random.value > bleedChance) return;

        EnemyStatusEffects statusEffects = enemyHealth.GetComponent<EnemyStatusEffects>();

        if (statusEffects == null)
        {
            statusEffects = enemyHealth.gameObject.AddComponent<EnemyStatusEffects>();
        }

        statusEffects.ApplyBleed(
            bleedDamagePerTick,
            bleedDuration,
            bleedTickInterval
        );
    }

    void UpdateHitCooldowns()
    {
        if (hitCooldowns.Count == 0) return;

        List<EnemyHealth> keys = new List<EnemyHealth>(hitCooldowns.Keys);

        foreach (EnemyHealth enemyHealth in keys)
        {
            if (enemyHealth == null)
            {
                hitCooldowns.Remove(enemyHealth);
                continue;
            }

            hitCooldowns[enemyHealth] -= Time.deltaTime;

            if (hitCooldowns[enemyHealth] <= 0f)
            {
                hitCooldowns.Remove(enemyHealth);
            }
        }
    }

    void DestroyBoomerang()
    {
        NotifyOwnerOnce();
        Destroy(gameObject);
    }

    void NotifyOwnerOnce()
    {
        if (hasNotifiedOwner) return;

        hasNotifiedOwner = true;

        if (ownerWeapon != null)
        {
            ownerWeapon.NotifyBoomerangDestroyed();
        }
    }

    void OnDestroy()
    {
        NotifyOwnerOnce();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}