using UnityEngine;
using System.Collections.Generic;

public class BasicMeleeWeapon : WeaponBase
{
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

    [Header("Memoria de dirección")]
    public float directionChangeGraceTime = 0.12f;

    [Header("Mejoras específicas")]
    public float commonBonus = 0.10f;
    public float rareBonus = 0.15f;
    public float epicBonus = 0.20f;
    public float legendaryBonus = 0.25f;

    private float weaponDamageMultiplier = 1f;
    private float weaponAttackSpeedMultiplier = 1f;
    private float weaponRangeMultiplier = 0f;

    private float attackTimer = 0f;
    private PlayerMovement movement;
    private Rigidbody2D rb;

    private Vector2 lastAttackDirection = Vector2.right;
    private Vector2 candidateAttackDirection = Vector2.right;
    private float candidateDirectionTimer = 0f;

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
            attackTimer = GetCurrentCooldown();
        }
    }

    float GetCurrentCooldown()
    {
        return baseAttackCooldown / (playerStats.attackSpeedMultiplier * weaponAttackSpeedMultiplier);
    }

    float GetFinalRange()
    {
        float baseRange = baseAttackRange + playerStats.meleeRange;
        float rangeMultiplier = 1f + playerStats.areaRangeBonus + weaponRangeMultiplier;

        return baseRange * rangeMultiplier;
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

        if (IsDiagonalDirection(currentDirection))
        {
            lastAttackDirection = currentDirection;
            candidateAttackDirection = currentDirection;
            candidateDirectionTimer = 0f;

            return lastAttackDirection;
        }

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
            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null) continue;

            int damageToApply = finalDamage;

            bool isCrit = Random.value < playerStats.critChance;

            if (isCrit)
            {
                damageToApply = Mathf.RoundToInt(finalDamage * playerStats.critMultiplier);
                Debug.Log("CRÍTICO melee!");
            }

            enemyHealth.TakeDamage(damageToApply, isCrit);

            EnemyKnockback enemyKnockback = hit.GetComponentInParent<EnemyKnockback>();
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
                "Daño directo.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "melee_weapon_range",
                "[Arma] Golpe amplio",
                "Radio de golpe.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "melee_weapon_speed",
                "[Arma] Golpe rápido",
                "Velocidad de ataque.",
                UpgradeRarity.Common,
                true,
                weaponId
            )
        };
    }

    public override void BuildSpecificUpgradeOptionText(UpgradeOption option)
    {
        string prefix = GetRarityPrefix(option.rarity);
        int percent = GetSpecificPercent(option.rarity);

        switch (option.id)
        {
            case "melee_weapon_damage":
                option.title = prefix + " " + weaponName + ": espada afilada";
                option.description = "Daño directo +" + percent + "%.";
                break;

            case "melee_weapon_range":
                option.title = prefix + " " + weaponName + ": golpe amplio";
                option.description = "Radio de golpe +" + percent + "%.";
                break;

            case "melee_weapon_speed":
                option.title = prefix + " " + weaponName + ": golpe rápido";
                option.description = "Velocidad de ataque +" + percent + "%.";
                break;
        }
    }

    public override void ApplySpecificUpgrade(UpgradeOption option)
    {
        float bonus = GetSpecificBonus(option.rarity);

        switch (option.id)
        {
            case "melee_weapon_damage":
                weaponDamageMultiplier += bonus;
                break;

            case "melee_weapon_range":
                weaponRangeMultiplier += bonus;
                break;

            case "melee_weapon_speed":
                weaponAttackSpeedMultiplier += bonus;
                break;
        }

        LevelUp();

        Debug.Log("Mejora aplicada a " + weaponName + ": " + option.title);
    }

    float GetSpecificBonus(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return commonBonus;
            case UpgradeRarity.Rare: return rareBonus;
            case UpgradeRarity.Epic: return epicBonus;
            case UpgradeRarity.Legendary: return legendaryBonus;
        }

        return commonBonus;
    }

    int GetSpecificPercent(UpgradeRarity rarity)
    {
        return Mathf.RoundToInt(GetSpecificBonus(rarity) * 100f);
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Vector2 dir = Vector2.right;
        Vector3 previewPosition = transform.position + (Vector3)(dir * attackPointDistance);

        float radius = baseAttackRange;

        PlayerStats ps = GetComponent<PlayerStats>();
        if (ps != null)
        {
            radius = (baseAttackRange + ps.meleeRange) * (1f + ps.areaRangeBonus);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(previewPosition, radius);
    }
}
