using UnityEngine;
using System.Collections.Generic;

public class BoomerangWeapon : WeaponBase
{
    [Header("Knockback")]
    public float knockbackForce = 4f;
    public float knockbackDuration = 0.12f;

    [Header("Prefab")]
    public GameObject boomerangProjectilePrefab;

    [Header("Ataque")]
    public float baseAttackCooldown = 2.2f;
    public float spawnDistance = 0.7f;

    [Header("Daño")]
    public int baseDamage = 2;

    [Header("Trayectoria")]
    public float flightDuration = 1.15f;
    public float forwardDistance = 4.5f;
    public float sideDistance = 2.2f;
    public float behindDistance = 1.2f;

    [Header("Impacto")]
    public float hitRadius = 0.45f;
    public LayerMask enemyLayer;

    [Header("Sangrado")]
    public int bleedDamagePerTick = 1;
    public float bleedDuration = 4f;
    public float bleedTickInterval = 1f;
    public float bleedChance = 0.7f;

    [Header("Control")]
    public bool allowOnlyOneBoomerang = true;
    public bool useProjectileCountBonus = false;

    [Header("Memoria de dirección")]
    public float directionChangeGraceTime = 0.02f;

    [Header("Mejoras específicas")]
    public float commonBonus = 0.10f;
    public float rareBonus = 0.15f;
    public float epicBonus = 0.20f;
    public float legendaryBonus = 0.25f;

    private float weaponDamageMultiplier = 1f;
    private float weaponAttackSpeedMultiplier = 1f;
    private float weaponSizeMultiplier = 0f;
    private float weaponBleedDamageMultiplier = 1f;

    private float attackTimer = 0f;
    private int activeBoomerangs = 0;

    private Rigidbody2D rb;

    private Vector2 lastThrowDirection = Vector2.right;
    private Vector2 candidateThrowDirection = Vector2.right;
    private float candidateDirectionTimer = 0f;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!isActiveWeapon) return;
        if (playerStats == null || boomerangProjectilePrefab == null) return;

        UpdateThrowDirectionMemory();

        int maxBoomerangs = GetMaxActiveBoomerangs();

        if (activeBoomerangs >= maxBoomerangs)
        {
            return;
        }

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            bool thrown = TryThrowBoomerang();

            if (thrown)
            {
                attackTimer = GetCurrentCooldown();
            }
        }
    }

    int GetMaxActiveBoomerangs()
    {
        int maxBoomerangs = 1;

        if (!allowOnlyOneBoomerang && useProjectileCountBonus && playerStats != null)
        {
            maxBoomerangs += playerStats.projectileCountBonus;
        }

        return Mathf.Max(1, maxBoomerangs);
    }

    float GetCurrentCooldown()
    {
        if (playerStats == null)
        {
            return baseAttackCooldown;
        }

        return baseAttackCooldown / (playerStats.attackSpeedMultiplier * weaponAttackSpeedMultiplier);
    }

    float GetFinalHitRadius()
    {
        return hitRadius * (1f + weaponSizeMultiplier);
    }

    bool TryThrowBoomerang()
    {
        int maxBoomerangs = GetMaxActiveBoomerangs();

        if (activeBoomerangs >= maxBoomerangs)
        {
            return false;
        }

        Vector2 direction = GetThrowDirection();

        int finalDamage = Mathf.RoundToInt(
            baseDamage *
            weaponDamageMultiplier *
            playerStats.damageMultiplier *
            playerStats.rangedDamageMultiplier
        );

        bool isCrit = Random.value < playerStats.critChance;

        if (isCrit)
        {
            finalDamage = Mathf.RoundToInt(finalDamage * playerStats.critMultiplier);
        }

        Vector3 spawnPosition = transform.position + (Vector3)(direction * spawnDistance);

        GameObject boomerangObject = Instantiate(
            boomerangProjectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        BoomerangProjectile boomerang = boomerangObject.GetComponent<BoomerangProjectile>();

        if (boomerang == null)
        {
            Debug.LogWarning("El prefab del boomerang no tiene el componente BoomerangProjectile.");
            Destroy(boomerangObject);
            return false;
        }

        activeBoomerangs++;

        int finalBleedDamagePerTick = Mathf.RoundToInt(
            bleedDamagePerTick *
            weaponBleedDamageMultiplier *
            playerStats.damageMultiplier *
            playerStats.bleedDamageMultiplier
        );

        finalBleedDamagePerTick = Mathf.Max(1, finalBleedDamagePerTick);

        float finalBleedChance = Mathf.Clamp01(bleedChance);

        boomerang.Initialize(
            this,
            transform,
            direction,
            finalDamage,
            isCrit,
            flightDuration,
            forwardDistance,
            sideDistance,
            behindDistance,
            GetFinalHitRadius(),
            enemyLayer,
            finalBleedDamagePerTick,
            bleedDuration,
            bleedTickInterval,
            finalBleedChance,
            knockbackForce,
            knockbackDuration
        );

        return true;
    }

    Vector2 GetThrowDirection()
    {
        if (lastThrowDirection.sqrMagnitude <= 0.01f)
        {
            return Vector2.right;
        }

        return lastThrowDirection.normalized;
    }

    void UpdateThrowDirectionMemory()
    {
        if (rb == null || rb.linearVelocity.sqrMagnitude <= 0.05f)
        {
            return;
        }

        Vector2 currentDirection = rb.linearVelocity.normalized;

        if (IsDiagonalDirection(currentDirection))
        {
            lastThrowDirection = currentDirection;
            candidateThrowDirection = currentDirection;
            candidateDirectionTimer = 0f;
            return;
        }

        if (IsDiagonalDirection(lastThrowDirection))
        {
            if (Vector2.Dot(candidateThrowDirection, currentDirection) < 0.98f)
            {
                candidateThrowDirection = currentDirection;
                candidateDirectionTimer = 0f;
            }
            else
            {
                candidateDirectionTimer += Time.deltaTime;
            }

            if (candidateDirectionTimer >= directionChangeGraceTime)
            {
                lastThrowDirection = candidateThrowDirection;
            }

            return;
        }

        lastThrowDirection = currentDirection;
    }

    bool IsDiagonalDirection(Vector2 direction)
    {
        direction.Normalize();

        return Mathf.Abs(direction.x) > 0.35f && Mathf.Abs(direction.y) > 0.35f;
    }

    public void NotifyBoomerangDestroyed()
    {
        activeBoomerangs--;
        activeBoomerangs = Mathf.Max(0, activeBoomerangs);
    }

    public override void ActivateWeapon()
    {
        base.ActivateWeapon();
        attackTimer = 0f;
        activeBoomerangs = 0;
    }

    public override void DeactivateWeapon()
    {
        base.DeactivateWeapon();
        activeBoomerangs = 0;
    }

    public override List<UpgradeOption> GetSpecificUpgradeOptions()
    {
        return new List<UpgradeOption>
        {
            new UpgradeOption(
                "boomerang_damage",
                "[Arma] Filo dentado",
                "Daño directo.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "boomerang_speed",
                "[Arma] Lanzamiento rápido",
                "Velocidad de ataque.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "boomerang_size",
                "[Arma] Hoja más grande",
                "Radio de impacto.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "boomerang_bleed",
                "[Arma] Corte profundo",
                "Daño de sangrado.",
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
            case "boomerang_damage":
                option.title = prefix + " " + weaponName + ": filo dentado";
                option.description = "Daño directo +" + percent + "%.";
                break;

            case "boomerang_speed":
                option.title = prefix + " " + weaponName + ": lanzamiento rápido";
                option.description = "Velocidad de ataque +" + percent + "%.";
                break;

            case "boomerang_size":
                option.title = prefix + " " + weaponName + ": hoja más grande";
                option.description = "Radio de impacto +" + percent + "%.";
                break;

            case "boomerang_bleed":
                option.title = prefix + " " + weaponName + ": corte profundo";
                option.description = "Sangrado +" + percent + "% de daño.";
                break;
        }
    }

    public override void ApplySpecificUpgrade(UpgradeOption option)
    {
        float bonus = GetSpecificBonus(option.rarity);

        switch (option.id)
        {
            case "boomerang_damage":
                weaponDamageMultiplier += bonus;
                break;

            case "boomerang_speed":
                weaponAttackSpeedMultiplier += bonus;
                break;

            case "boomerang_size":
                weaponSizeMultiplier += bonus;
                break;

            case "boomerang_bleed":
                weaponBleedDamageMultiplier += bonus;
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
}
