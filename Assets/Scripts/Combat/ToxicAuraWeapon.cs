using UnityEngine;
using System.Collections.Generic;

public class ToxicAuraWeapon : WeaponBase
{
    [Header("Control anti-abuso")]
    public float perEnemyPoisonApplyCooldown = 1f;

    private Dictionary<EnemyHealth, float> poisonCooldowns = new Dictionary<EnemyHealth, float>();
    [Header("Aura")]
    public float auraRadius = 2.4f;
    public float applyInterval = 0.5f;
    public LayerMask enemyLayer;

    [Header("Veneno")]
    public int poisonDamagePerTick = 20;
    public float poisonDuration = 3.5f;
    public float poisonTickInterval = 1f;
    public int maxPoisonStacks = 3;
    public float poisonApplyChance = 1f;

    [Header("Daño directo opcional")]
    public int contactDamagePerPulse = 0;

    [Header("Visual opcional")]
    public Transform auraVisual;
    public bool showAuraOnlyWhenActive = true;

    private float applyTimer = 0f;
    private float weaponPoisonMultiplier = 1f;

    protected override void Awake()
    {
        base.Awake();

        if (auraVisual != null)
        {
            auraVisual.localScale = Vector3.one * (auraRadius * 2f);

            if (showAuraOnlyWhenActive)
            {
                auraVisual.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (!isActiveWeapon) return;
        if (playerStats == null) return;

        UpdateAuraVisual();
        UpdatePoisonCooldowns();

        applyTimer -= Time.deltaTime;

        if (applyTimer <= 0f)
        {
            ApplyAura();
            applyTimer = GetCurrentApplyInterval();
        }
    }

    void ApplyAura()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            GetFinalAuraRadius(),
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

            ApplyContactDamage(enemyHealth);
            ApplyPoison(enemyHealth);
        }
    }

    float GetFinalAuraRadius()
    {
        if (playerStats == null)
        {
            return auraRadius;
        }

        return auraRadius + playerStats.areaRangeBonus;
    }
    void ApplyContactDamage(EnemyHealth enemyHealth)
    {
        if (contactDamagePerPulse <= 0) return;

        int finalContactDamage = Mathf.RoundToInt(
            contactDamagePerPulse *
            playerStats.damageMultiplier *
            playerStats.magicDamageMultiplier *
            playerStats.poisonDamageMultiplier
        );

        finalContactDamage = Mathf.Max(1, finalContactDamage);

        enemyHealth.TakeDamage(finalContactDamage, false);
    }

    void ApplyPoison(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null) return;

        if (poisonCooldowns.ContainsKey(enemyHealth))
        {
            return;
        }

        float finalPoisonChance = Mathf.Clamp01(
            poisonApplyChance + playerStats.statusEffectChance
        );

        if (Random.value > finalPoisonChance) return;

        int finalPoisonDamagePerTick = Mathf.RoundToInt(
            poisonDamagePerTick *
            weaponPoisonMultiplier *
            playerStats.damageMultiplier *
            playerStats.magicDamageMultiplier *
            playerStats.poisonDamageMultiplier
        );

        finalPoisonDamagePerTick = Mathf.Max(1, finalPoisonDamagePerTick);

        EnemyStatusEffects statusEffects = enemyHealth.GetComponent<EnemyStatusEffects>();

        if (statusEffects == null)
        {
            statusEffects = enemyHealth.gameObject.AddComponent<EnemyStatusEffects>();
        }

        statusEffects.ApplyPoison(
            finalPoisonDamagePerTick,
            poisonDuration,
            poisonTickInterval,
            maxPoisonStacks
        );

        poisonCooldowns[enemyHealth] = perEnemyPoisonApplyCooldown;
    }

    void UpdatePoisonCooldowns()
    {
        if (poisonCooldowns.Count == 0) return;

        List<EnemyHealth> keys = new List<EnemyHealth>(poisonCooldowns.Keys);

        foreach (EnemyHealth enemyHealth in keys)
        {
            if (enemyHealth == null)
            {
                poisonCooldowns.Remove(enemyHealth);
                continue;
            }

            poisonCooldowns[enemyHealth] -= Time.deltaTime;

            if (poisonCooldowns[enemyHealth] <= 0f)
            {
                poisonCooldowns.Remove(enemyHealth);
            }
        }
    }

    float GetCurrentApplyInterval()
    {
        float finalInterval = applyInterval / playerStats.attackSpeedMultiplier;

        return Mathf.Max(0.15f, finalInterval);
    }

    void UpdateAuraVisual()
    {
        if (auraVisual == null) return;

        auraVisual.localScale = Vector3.one * (GetFinalAuraRadius() * 2f);
    }

    public override void ActivateWeapon()
    {
        base.ActivateWeapon();
        applyTimer = 0f;

        if (auraVisual != null && showAuraOnlyWhenActive)
        {
            auraVisual.gameObject.SetActive(true);
        }
    }

    public override void DeactivateWeapon()
    {
        base.DeactivateWeapon();

        if (auraVisual != null && showAuraOnlyWhenActive)
        {
            auraVisual.gameObject.SetActive(false);
        }
    }

    public override List<UpgradeOption> GetSpecificUpgradeOptions()
    {
        return new List<UpgradeOption>
        {
            new UpgradeOption(
                "toxic_aura_damage",
                "[Arma] Humo corrosivo",
                "El veneno del aura hace más daño.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "toxic_aura_radius",
                "[Arma] Nube expansiva",
                "El aura tóxica aumenta su radio.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "toxic_aura_frequency",
                "[Arma] Fuga constante",
                "El aura aplica veneno más a menudo.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "toxic_aura_stacks",
                "[Arma] Veneno acumulativo",
                "El veneno puede acumular más cargas.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "toxic_aura_duration",
                "[Arma] Toxina persistente",
                "El veneno dura más tiempo.",
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
            case "toxic_aura_damage":
                weaponPoisonMultiplier += GetPoisonDamageBonus(option.rarity);
                break;

            case "toxic_aura_radius":
                auraRadius += GetRadiusBonus(option.rarity);
                break;

            case "toxic_aura_frequency":
                applyInterval = Mathf.Max(
                    0.20f,
                    applyInterval - GetIntervalReduction(option.rarity)
                );
                break;

            case "toxic_aura_stacks":
                maxPoisonStacks += GetStackBonus(option.rarity);
                break;

            case "toxic_aura_duration":
                poisonDuration += GetDurationBonus(option.rarity);
                break;
        }

        LevelUp();

        Debug.Log("Mejora aplicada a " + weaponName + ": " + option.title);
    }

    float GetPoisonDamageBonus(UpgradeRarity rarity)
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

    float GetRadiusBonus(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.25f;
            case UpgradeRarity.Rare: return 0.40f;
            case UpgradeRarity.Epic: return 0.65f;
            case UpgradeRarity.Legendary: return 1.0f;
        }

        return 0.25f;
    }

    float GetIntervalReduction(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.04f;
            case UpgradeRarity.Rare: return 0.07f;
            case UpgradeRarity.Epic: return 0.11f;
            case UpgradeRarity.Legendary: return 0.16f;
        }

        return 0.04f;
    }

    int GetStackBonus(UpgradeRarity rarity)
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

    float GetDurationBonus(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.5f;
            case UpgradeRarity.Rare: return 1.0f;
            case UpgradeRarity.Epic: return 1.5f;
            case UpgradeRarity.Legendary: return 2.5f;
        }

        return 0.5f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        float radius = auraRadius;

        PlayerStats ps = GetComponent<PlayerStats>();
        if (ps != null)
        {
            radius += ps.areaRangeBonus;
        }

        Gizmos.DrawWireSphere(transform.position, radius);
    }
}