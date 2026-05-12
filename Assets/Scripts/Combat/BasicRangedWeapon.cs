using UnityEngine;
using System.Collections.Generic;

public class BasicRangedWeapon : WeaponBase
{
    private bool alternateSpreadSide = true;
    private float weaponDamageMultiplier = 1f;
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

    private float attackTimer = 0f;
    private float weaponProjectileSpeedMultiplier = 1f;

    void Update()
    {
        if (!isActiveWeapon) return;
        if (playerStats == null || bulletPrefab == null) return;

        attackTimer -= Time.deltaTime;

        GameObject nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null && attackTimer <= 0f)
        {
            Shoot(nearestEnemy);

            float cooldown = baseAttackCooldown / playerStats.attackSpeedMultiplier;
            attackTimer = cooldown;
        }
    }

    GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        GameObject nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < minDistance && distance <= attackRange)
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

        // Siempre disparamos una bala directa al centro del enemigo
        SpawnBullet(baseDirection, finalDamage);

        // Si solo hay una bala, terminamos aquí
        if (projectileCount <= 1)
        {
            return;
        }

        int extraProjectiles = projectileCount - 1;

        // Para que con 2 balas no siempre se vaya hacia el mismo lado
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
                "Esta arma a distancia gana daño.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "ranged_weapon_speed",
                "[Arma] Gatillo rápido",
                "Esta arma a distancia reduce su cooldown.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "ranged_weapon_range",
                "[Arma] Cañón largo",
                "Esta arma a distancia gana alcance.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "ranged_weapon_projectile_speed",
                "[Arma] Disparo veloz",
                "Los proyectiles de esta arma vuelan más rápido.",
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
            case "ranged_weapon_damage":
                weaponDamageMultiplier += GetWeaponDamageBonus(option.rarity);
                break;

            case "ranged_weapon_speed":
                baseAttackCooldown = Mathf.Max(
                    0.25f,
                    baseAttackCooldown - GetWeaponCooldownReduction(option.rarity)
                );
                break;

            case "ranged_weapon_range":
                attackRange += GetWeaponRangeBonus(option.rarity);
                break;

            case "ranged_weapon_projectile_speed":
                weaponProjectileSpeedMultiplier += GetWeaponProjectileSpeedBonus(option.rarity);
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

    float GetWeaponCooldownReduction(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.05f;
            case UpgradeRarity.Rare: return 0.08f;
            case UpgradeRarity.Epic: return 0.13f;
            case UpgradeRarity.Legendary: return 0.20f;
        }

        return 0.05f;
    }

    float GetWeaponRangeBonus(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.7f;
            case UpgradeRarity.Rare: return 1.2f;
            case UpgradeRarity.Epic: return 2f;
            case UpgradeRarity.Legendary: return 3f;
        }

        return 0.7f;
    }

    float GetWeaponProjectileSpeedBonus(UpgradeRarity rarity)
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
}