using UnityEngine;
using System.Collections.Generic;

public class BasicRangedWeapon : WeaponBase
{
    [Header("Proyectil")]
    public GameObject bulletPrefab;

    [Header("Ataque")]
    public float attackRange = 10f;
    public float baseAttackCooldown = 0.8f;
    public float projectileSpawnDistance = 0.6f;

    [Header("Daño del arma")]
    public int baseDamage = 1;

    [Header("Disparo múltiple")]
    public float spreadAngle = 12f;

    [Header("Mejoras específicas")]
    public float commonBonus = 0.10f;
    public float rareBonus = 0.15f;
    public float epicBonus = 0.20f;
    public float legendaryBonus = 0.25f;

    private bool alternateSpreadSide = true;

    private float weaponDamageMultiplier = 1f;
    private float weaponAttackSpeedMultiplier = 1f;
    private float weaponRangeMultiplier = 0f;
    private float weaponProjectileSpeedMultiplier = 1f;

    private float attackTimer = 0f;

    void Update()
    {
        if (!isActiveWeapon) return;
        if (playerStats == null || bulletPrefab == null) return;

        attackTimer -= Time.deltaTime;

        GameObject nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null && attackTimer <= 0f)
        {
            Shoot(nearestEnemy);
            attackTimer = GetCurrentCooldown();
        }
    }

    float GetCurrentCooldown()
    {
        return baseAttackCooldown / (playerStats.attackSpeedMultiplier * weaponAttackSpeedMultiplier);
    }

    float GetFinalAttackRange()
    {
        return attackRange * (1f + weaponRangeMultiplier);
    }

    GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        GameObject nearest = null;
        float minDistance = Mathf.Infinity;
        float finalAttackRange = GetFinalAttackRange();

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < minDistance && distance <= finalAttackRange)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    void Shoot(GameObject enemy)
    {
        Vector2 baseDirection = (enemy.transform.position - transform.position).normalized;

        int projectileCount = 1 + playerStats.projectileCountBonus;

        int finalDamage = Mathf.RoundToInt(
            baseDamage *
            weaponDamageMultiplier *
            playerStats.damageMultiplier *
            playerStats.rangedDamageMultiplier
        );

        SpawnBullet(baseDirection, finalDamage);

        if (projectileCount <= 1)
        {
            return;
        }

        int extraProjectiles = projectileCount - 1;
        int firstSide = alternateSpreadSide ? 1 : -1;

        for (int i = 0; i < extraProjectiles; i++)
        {
            int side = (i % 2 == 0) ? firstSide : -firstSide;
            int spreadStep = (i / 2) + 1;

            float currentAngle = spreadAngle * spreadStep * side;
            Vector2 newDirection = RotateVector(baseDirection, currentAngle);

            SpawnBullet(newDirection, finalDamage);
        }

        alternateSpreadSide = !alternateSpreadSide;
    }

    void SpawnBullet(Vector2 direction, int damage)
    {
        Vector3 spawnPosition = transform.position + (Vector3)(direction * projectileSpawnDistance);

        GameObject bulletObject = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

        Bullet bulletScript = bulletObject.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bulletScript.Initialize(
                direction,
                damage,
                playerStats.critChance,
                playerStats.critMultiplier,
                playerStats.projectileSpeedMultiplier * weaponProjectileSpeedMultiplier
            );
        }
    }

    Vector2 RotateVector(Vector2 vector, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;

        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        float x = vector.x * cos - vector.y * sin;
        float y = vector.x * sin + vector.y * cos;

        return new Vector2(x, y).normalized;
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
                "ranged_weapon_damage",
                "[Arma] Semillas reforzadas",
                "Daño directo.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "ranged_weapon_speed",
                "[Arma] Gatillo rápido",
                "Velocidad de ataque.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "ranged_weapon_range",
                "[Arma] Cañón largo",
                "Alcance.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "ranged_weapon_projectile_speed",
                "[Arma] Disparo veloz",
                "Velocidad de proyectil.",
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
            case "ranged_weapon_damage":
                option.title = prefix + " " + weaponName + ": semillas reforzadas";
                option.description = "Daño directo +" + percent + "%.";
                break;

            case "ranged_weapon_speed":
                option.title = prefix + " " + weaponName + ": gatillo rápido";
                option.description = "Velocidad de ataque +" + percent + "%.";
                break;

            case "ranged_weapon_range":
                option.title = prefix + " " + weaponName + ": cañón largo";
                option.description = "Alcance +" + percent + "%.";
                break;

            case "ranged_weapon_projectile_speed":
                option.title = prefix + " " + weaponName + ": disparo veloz";
                option.description = "Velocidad de proyectil +" + percent + "%.";
                break;
        }
    }

    public override void ApplySpecificUpgrade(UpgradeOption option)
    {
        float bonus = GetSpecificBonus(option.rarity);

        switch (option.id)
        {
            case "ranged_weapon_damage":
                weaponDamageMultiplier += bonus;
                break;

            case "ranged_weapon_speed":
                weaponAttackSpeedMultiplier += bonus;
                break;

            case "ranged_weapon_range":
                weaponRangeMultiplier += bonus;
                break;

            case "ranged_weapon_projectile_speed":
                weaponProjectileSpeedMultiplier += bonus;
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
