using UnityEngine;
using System.Collections.Generic;

public class ToxicAuraWeapon : WeaponBase
{
    [Header("Control anti-abuso")]
    public float perEnemyPoisonApplyCooldown = 1f;

    [Header("Aura")]
    public float auraRadius = 2.4f;
    public float applyInterval = 0.5f;
    public LayerMask enemyLayer;

    [Header("Escalado de área")]
    public float globalAreaBonusMultiplier = 0.35f;
    public float maxFinalAuraRadius = 4.5f;

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

    [Header("Mejoras específicas")]
    public float commonBonus = 0.10f;
    public float rareBonus = 0.15f;
    public float epicBonus = 0.20f;
    public float legendaryBonus = 0.25f;

    private Dictionary<EnemyHealth, float> poisonCooldowns = new Dictionary<EnemyHealth, float>();

    private float applyTimer = 0f;

    private float weaponPoisonDamageMultiplier = 1f;
    private float weaponAuraRadiusMultiplier = 0f;
    private float weaponPoisonSpeedMultiplier = 0f;
    private float weaponPoisonDurationMultiplier = 0f;

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
            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth == null) continue;

            ApplyContactDamage(enemyHealth);
            ApplyPoison(enemyHealth);
        }
    }

    float GetFinalAuraRadius()
    {
        float globalAreaBonus = 0f;

        if (playerStats != null)
        {
            globalAreaBonus = playerStats.areaRangeBonus * globalAreaBonusMultiplier;
        }

        float finalRadius = auraRadius * (1f + weaponAuraRadiusMultiplier + globalAreaBonus);

        return Mathf.Min(finalRadius, maxFinalAuraRadius);
    }

    float GetCurrentApplyInterval()
    {
        float speedMultiplier = 1f + weaponPoisonSpeedMultiplier;

        return Mathf.Max(0.15f, applyInterval / speedMultiplier);
    }

    float GetCurrentPoisonCooldown()
    {
        float speedMultiplier = 1f + weaponPoisonSpeedMultiplier;

        return Mathf.Max(0.15f, perEnemyPoisonApplyCooldown / speedMultiplier);
    }

    float GetFinalPoisonDuration()
    {
        return poisonDuration * (1f + weaponPoisonDurationMultiplier);
    }

    void ApplyContactDamage(EnemyHealth enemyHealth)
    {
        if (contactDamagePerPulse <= 0) return;

        int finalContactDamage = Mathf.RoundToInt(
            contactDamagePerPulse *
            playerStats.damageMultiplier *
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

        float finalPoisonChance = Mathf.Clamp01(poisonApplyChance);

        if (Random.value > finalPoisonChance) return;

        int finalPoisonDamagePerTick = Mathf.RoundToInt(
            poisonDamagePerTick *
            weaponPoisonDamageMultiplier *
            playerStats.damageMultiplier *
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
            GetFinalPoisonDuration(),
            poisonTickInterval,
            maxPoisonStacks
        );

        poisonCooldowns[enemyHealth] = GetCurrentPoisonCooldown();
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
                "Daño de veneno.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "toxic_aura_radius",
                "[Arma] Nube expansiva",
                "Radio.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "toxic_aura_frequency",
                "[Arma] Fuga constante",
                "Velocidad de veneno.",
                UpgradeRarity.Common,
                true,
                weaponId
            ),

            new UpgradeOption(
                "toxic_aura_duration",
                "[Arma] Toxina persistente",
                "Duración del veneno.",
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
            case "toxic_aura_damage":
                option.title = prefix + " " + weaponName + ": humo corrosivo";
                option.description = "Veneno +" + percent + "% de daño.";
                break;

            case "toxic_aura_radius":
                option.title = prefix + " " + weaponName + ": nube expansiva";
                option.description = "Radio +" + percent + "%.";
                break;

            case "toxic_aura_frequency":
                option.title = prefix + " " + weaponName + ": fuga constante";
                option.description = "Veneno +" + percent + "% más rápido.";
                break;

            case "toxic_aura_duration":
                option.title = prefix + " " + weaponName + ": toxina persistente";
                option.description = "Duración del veneno +" + percent + "%.";
                break;
        }
    }

    public override void ApplySpecificUpgrade(UpgradeOption option)
    {
        float bonus = GetSpecificBonus(option.rarity);

        switch (option.id)
        {
            case "toxic_aura_damage":
                weaponPoisonDamageMultiplier += bonus;
                break;

            case "toxic_aura_radius":
                weaponAuraRadiusMultiplier += bonus;
                break;

            case "toxic_aura_frequency":
                weaponPoisonSpeedMultiplier += bonus;
                break;

            case "toxic_aura_duration":
                weaponPoisonDurationMultiplier += bonus;
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
        Gizmos.color = Color.green;

        float radius = auraRadius;

        PlayerStats ps = GetComponent<PlayerStats>();
        if (ps != null)
        {
            radius *= 1f + (ps.areaRangeBonus * globalAreaBonusMultiplier);
        }

        radius = Mathf.Min(radius, maxFinalAuraRadius);

        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
