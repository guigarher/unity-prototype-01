using UnityEngine;
using System.Collections.Generic;

public class BasicRangedWeapon : WeaponBase
{
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

        if (projectileCount <= 1)
        {
            SpawnBullet(baseDirection, finalDamage);
            return;
        }

        float totalSpread = spreadAngle * (projectileCount - 1);
        float startAngle = -totalSpread / 2f;

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = startAngle + (spreadAngle * i);
            Vector2 newDirection = RotateVector(baseDirection, currentAngle);
            SpawnBullet(newDirection, finalDamage);
        }
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
                "Esta arma a distancia gana +10% de daño.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "ranged_weapon_speed",
                "[Arma] Gatillo rápido",
                "Esta arma a distancia reduce su cooldown en 0.08 segundos.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "ranged_weapon_range",
                "[Arma] Cañón largo",
                "Esta arma a distancia gana +1 de alcance.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "ranged_weapon_projectile_speed",
                "[Arma] Disparo veloz",
                "Los proyectiles de esta arma vuelan un 15% más rápido.",
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
                weaponDamageMultiplier += 0.10f;
                break;

            case "ranged_weapon_speed":
                baseAttackCooldown = Mathf.Max(0.25f, baseAttackCooldown - 0.08f);
                break;

            case "ranged_weapon_range":
                attackRange += 1f;
                break;

            case "ranged_weapon_projectile_speed":
                weaponProjectileSpeedMultiplier += 0.15f;
                break;
        }

        LevelUp();

        Debug.Log("Mejora aplicada a " + weaponName + ": " + option.title);
    }
}