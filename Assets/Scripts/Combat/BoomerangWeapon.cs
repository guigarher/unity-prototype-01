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
    private float weaponDamageMultiplier = 1f;
    private Rigidbody2D rb;

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

    private float attackTimer = 0f;
    private int activeBoomerangs = 0;
    private PlayerMovement movement;

    [Header("Memoria de dirección")]
    public float directionChangeGraceTime = 0.02f;

    private Vector2 lastThrowDirection = Vector2.right;
    private Vector2 candidateThrowDirection = Vector2.right;
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

        return baseAttackCooldown / playerStats.attackSpeedMultiplier;
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
            hitRadius,
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
                "El boomerang gana daño.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "boomerang_cooldown",
                "[Arma] Lanzamiento rápido",
                "El boomerang tarda menos en volver a lanzarse.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "boomerang_size",
                "[Arma] Hoja más grande",
                "El boomerang golpea en un área mayor.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "boomerang_bleed",
                "[Arma] Corte profundo",
                "El sangrado del boomerang mejora.",
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
            case "boomerang_damage":
                weaponDamageMultiplier += GetDamageBonus(option.rarity);
                break;

            case "boomerang_cooldown":
                baseAttackCooldown = Mathf.Max(
                    0.65f,
                    baseAttackCooldown - GetCooldownReduction(option.rarity)
                );
                break;

            case "boomerang_size":
                float sizeBonus = GetSizeBonus(option.rarity);

                hitRadius += sizeBonus;
                sideDistance += sizeBonus * 1.5f;
                break;

            case "boomerang_bleed":
                bleedDamagePerTick += GetBleedDamageBonus(option.rarity);
                bleedDuration += GetBleedDurationBonus(option.rarity);
                bleedChance = Mathf.Min(1f, bleedChance + GetBleedChanceBonus(option.rarity));
                break;
        }

        LevelUp();

        Debug.Log("Mejora aplicada a " + weaponName + ": " + option.title);
    }

    float GetDamageBonus(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.12f;
            case UpgradeRarity.Rare: return 0.20f;
            case UpgradeRarity.Epic: return 0.35f;
            case UpgradeRarity.Legendary: return 0.55f;
        }

        return 0.12f;
    }

    float GetCooldownReduction(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.12f;
            case UpgradeRarity.Rare: return 0.20f;
            case UpgradeRarity.Epic: return 0.32f;
            case UpgradeRarity.Legendary: return 0.50f;
        }

        return 0.12f;
    }

    float GetSizeBonus(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.08f;
            case UpgradeRarity.Rare: return 0.14f;
            case UpgradeRarity.Epic: return 0.22f;
            case UpgradeRarity.Legendary: return 0.35f;
        }

        return 0.08f;
    }

    int GetBleedDamageBonus(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 1;
            case UpgradeRarity.Rare: return 1;
            case UpgradeRarity.Epic: return 2;
            case UpgradeRarity.Legendary: return 3;
        }

        return 1;
    }

    float GetBleedDurationBonus(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.5f;
            case UpgradeRarity.Rare: return 1f;
            case UpgradeRarity.Epic: return 1.5f;
            case UpgradeRarity.Legendary: return 2.5f;
        }

        return 0.5f;
    }

    float GetBleedChanceBonus(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.05f;
            case UpgradeRarity.Rare: return 0.10f;
            case UpgradeRarity.Epic: return 0.15f;
            case UpgradeRarity.Legendary: return 0.25f;
        }

        return 0.05f;
    }
}