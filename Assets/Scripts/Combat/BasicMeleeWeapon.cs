using UnityEngine;
using System.Collections.Generic;

public class BasicMeleeWeapon : WeaponBase
{
    private float weaponDamageMultiplier = 1f;
    [Header("Daño del arma")]
    public int baseDamage = 3;

    [Header("Knockback")]
    public float knockbackForce = 4f;
    public float knockbackDuration = 0.12f;

    [Header("Tamaño del ataque")]
    public float baseAttackRange = 1f;

    [Header("Ataque")]
    public Transform attackPoint;
    public float attackPointDistance = 1.2f;
    public float baseAttackCooldown = 2f;

    [Header("Visual")]
    public GameObject meleeVisualPrefab;

    [Header("Detección")]
    public LayerMask enemyLayer;

    private float attackTimer = 0f;
    private PlayerMovement movement;
    private Rigidbody2D rb;
    private Vector2 lastAttackDirection = Vector2.right;

    [Header("Memoria de dirección")]
    public float directionChangeGraceTime = 0.12f;

    private Vector2 candidateAttackDirection = Vector2.right;
    private float candidateDirectionTimer = 0f;

    private float bonusRange = 0f;

    protected override void Awake()
    {
        base.Awake();
        movement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!isActiveWeapon) return;
        if (playerStats == null || movement == null || attackPoint == null) return;

        UpdateAttackPointPosition();

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            PerformAttack();

            float cooldown = baseAttackCooldown / playerStats.attackSpeedMultiplier;
            attackTimer = cooldown;
        }
    }

    float GetFinalRange()
    {
        return baseAttackRange + bonusRange + playerStats.meleeRange + playerStats.areaRangeBonus;
    }

    void UpdateAttackPointPosition()
    {
        Vector2 dir = GetAttackDirection();

        attackPoint.localPosition = dir * attackPointDistance;
    }

    Vector2 GetAttackDirection()
    {
        if (rb == null || rb.linearVelocity.sqrMagnitude <= 0.05f)
        {
            return lastAttackDirection;
        }

        Vector2 currentDirection = rb.linearVelocity.normalized;

        // Si la dirección actual es diagonal, la aceptamos al instante.
        if (IsDiagonalDirection(currentDirection))
        {
            lastAttackDirection = currentDirection;
            candidateAttackDirection = currentDirection;
            candidateDirectionTimer = 0f;

            return lastAttackDirection;
        }

        // Si la última dirección buena era diagonal y ahora aparece una recta,
        // esperamos un poco antes de machacar la diagonal.
        if (IsDiagonalDirection(lastAttackDirection))
        {
            if (Vector2.Dot(candidateAttackDirection, currentDirection) < 0.98f)
            {
                candidateAttackDirection = currentDirection;
                candidateDirectionTimer = 0f;
            }
            else
            {
                candidateDirectionTimer += Time.deltaTime;
            }

            if (candidateDirectionTimer >= directionChangeGraceTime)
            {
                lastAttackDirection = candidateAttackDirection;
            }

            return lastAttackDirection;
        }

        // Si no veníamos de una diagonal, actualizamos normal.
        lastAttackDirection = currentDirection;
        return lastAttackDirection;
    }

    bool IsDiagonalDirection(Vector2 direction)
    {
        direction.Normalize();

        return Mathf.Abs(direction.x) > 0.35f && Mathf.Abs(direction.y) > 0.35f;
    }

    void PerformAttack()
    {
        Vector2 attackCenter = attackPoint.position;
        float finalRange = GetFinalRange();

        if (meleeVisualPrefab != null)
        {
            GameObject slash = Instantiate(meleeVisualPrefab, attackCenter, Quaternion.identity);
            slash.transform.localScale = Vector3.one * (finalRange * 2f);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackCenter,
            finalRange,
            enemyLayer
        );

        int finalDamage = Mathf.RoundToInt(
            baseDamage *
            weaponDamageMultiplier *
            playerStats.damageMultiplier *
            playerStats.meleeDamageMultiplier
        );

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth == null) continue;

            int damageToApply = finalDamage;

            bool isCrit = Random.value < playerStats.critChance;

            if (isCrit)
            {
                damageToApply = Mathf.RoundToInt(finalDamage * playerStats.critMultiplier);
                Debug.Log("CRÍTICO melee!");
            }

            enemyHealth.TakeDamage(damageToApply, isCrit);

            EnemyKnockback enemyKnockback = hit.GetComponent<EnemyKnockback>();
            if (enemyKnockback != null)
            {
                Vector2 knockDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;

                enemyKnockback.ApplyKnockback(
                    knockDir,
                    knockbackForce * playerStats.meleeKnockbackMultiplier,
                    knockbackDuration
                );
            }
        }
    }

    public override void ActivateWeapon()
    {
        base.ActivateWeapon();
        attackTimer = 0f;
    }

    public override void DeactivateWeapon()
    {
        base.DeactivateWeapon();
    }

    public override List<UpgradeOption> GetSpecificUpgradeOptions()
    {
        return new List<UpgradeOption>
        {
            new UpgradeOption(
                "melee_weapon_damage",
                "[Arma] Espada afilada",
                "Esta arma melee gana +10% de daño.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "melee_weapon_range",
                "[Arma] Golpe amplio",
                "Esta arma melee gana +0.2 de alcance.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "melee_weapon_speed",
                "[Arma] Golpe rápido",
                "Esta arma melee reduce su cooldown en 0.15 segundos.",
                UpgradeRarity.Common,
                true,
                weaponId
            )
        };
    }

    public override void ApplySpecificUpgrade(UpgradeOption option)
    {
        switch (option.id)
        {
            case "melee_weapon_damage":
                weaponDamageMultiplier += GetWeaponDamageBonus(option.rarity);
                break;

            case "melee_weapon_range":
                bonusRange += GetWeaponRangeBonus(option.rarity);
                break;

            case "melee_weapon_speed":
                baseAttackCooldown = Mathf.Max(
                    0.4f,
                    baseAttackCooldown - GetWeaponCooldownReduction(option.rarity)
                );
                break;
        }

        LevelUp();

        Debug.Log("Mejora aplicada a " + weaponName + ": " + option.title);
    }

    float GetWeaponDamageBonus(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.10f;
            case UpgradeRarity.Rare: return 0.18f;
            case UpgradeRarity.Epic: return 0.30f;
            case UpgradeRarity.Legendary: return 0.45f;
        }

        return 0.10f;
    }

    float GetWeaponRangeBonus(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.15f;
            case UpgradeRarity.Rare: return 0.25f;
            case UpgradeRarity.Epic: return 0.40f;
            case UpgradeRarity.Legendary: return 0.65f;
        }

        return 0.15f;
    }

    float GetWeaponCooldownReduction(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.10f;
            case UpgradeRarity.Rare: return 0.16f;
            case UpgradeRarity.Epic: return 0.25f;
            case UpgradeRarity.Legendary: return 0.40f;
        }

        return 0.10f;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        // Dirección por defecto (derecha) SOLO para visualizar
        Vector2 dir = Vector2.right;

        // Calculamos donde estaría el attackPoint
        Vector3 previewPosition = transform.position + (Vector3)(dir * attackPointDistance);

        float radius = baseAttackRange + bonusRange;

        PlayerStats ps = GetComponent<PlayerStats>();
        if (ps != null)
        {
            radius += ps.meleeRange + ps.areaRangeBonus;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(previewPosition, radius);
    }
}