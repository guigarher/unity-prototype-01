using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance;

    private const int OPTION_COUNT = 4;

    [Header("Anuncio de subida de nivel")]
    public float levelUpAnnouncementDelay = 1.2f;
    private bool openingRewardMenu = false;

    [Header("Reroll con oro")]
    public int baseRerollCost = 5;
    public int rerollCostIncrease = 3;
    private int currentRerollCost;

    [Header("Control de opciones")]
    public bool guaranteeWeaponUpgradeOption = true;

    [Header("Mejoras especiales legendarias")]
    [Range(0f, 1f)]
    public float legendarySpecialChance = 0.75f;

    public int projectileCountMinLevelsBetweenOffers = 6;

    private int currentRewardLevel = 1;
    private int lastProjectileCountOfferedLevel = -999;

    private PlayerStats stats;
    private PlayerHealth health;
    private PlayerCurrency currency;
    private PlayerWeaponManager weaponManager;

    private bool waitingForChoice = false;
    private bool choosingWeapon = false;

    private List<UpgradeOption> currentOptions = new List<UpgradeOption>();
    private List<WeaponBase> currentWeaponOptions = new List<WeaponBase>();

    void Awake()
    {
        Instance = this;
        InitializeReferences();
    }

    void Start()
    {
        InitializeReferences();
        currentRerollCost = baseRerollCost;
    }

    void InitializeReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("No se encontró un objeto con tag Player.");
            return;
        }

        if (stats == null) stats = player.GetComponent<PlayerStats>();
        if (health == null) health = player.GetComponent<PlayerHealth>();
        if (currency == null) currency = player.GetComponent<PlayerCurrency>();
        if (weaponManager == null) weaponManager = player.GetComponent<PlayerWeaponManager>();
    }

    void Update()
    {
        if (!waitingForChoice) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) ChooseOption(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) ChooseOption(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) ChooseOption(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) ChooseOption(3);

        if (!choosingWeapon && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RerollUpgrades();
        }
    }

    public void CheckInitialWeaponChoice()
    {
        InitializeReferences();

        if (weaponManager == null) return;

        if (!weaponManager.HasAnyActiveWeapon())
        {
            OpenWeaponChoiceMenu();
        }
    }

    public void OpenRewardMenuForLevel(int level)
    {
        InitializeReferences();

        if (openingRewardMenu) return;

        StartCoroutine(OpenRewardMenuRoutine(level));
    }

    IEnumerator OpenRewardMenuRoutine(int level)
    {
        openingRewardMenu = true;
        currentRewardLevel = level;

        if (ScreenAnnouncementManager.Instance != null)
        {
            ScreenAnnouncementManager.Instance.ShowMessage(
                "¡SUBIDA DE NIVEL!",
                "Nivel " + level + " conseguido",
                levelUpAnnouncementDelay
            );
        }

        yield return new WaitForSecondsRealtime(levelUpAnnouncementDelay);

        if (weaponManager != null && IsWeaponMilestoneLevel(level) && weaponManager.CanOfferMoreWeapons())
        {
            OpenWeaponChoiceMenu();
        }
        else
        {
            OpenLevelUpMenu();
        }

        openingRewardMenu = false;
    }

    public bool IsWeaponMilestoneLevel(int level)
    {
        return level == 5 || level == 10 || level == 15;
    }

    public void OpenLevelUpMenu()
    {
        InitializeReferences();

        if (stats == null) return;

        choosingWeapon = false;
        waitingForChoice = true;
        Time.timeScale = 0f;

        currentRerollCost = baseRerollCost;

        GenerateUpgradeOptions();
        ShowUpgradeOptionsInConsole();
    }

    public void OpenWeaponChoiceMenu()
    {
        InitializeReferences();

        if (weaponManager == null) return;

        choosingWeapon = true;
        waitingForChoice = true;
        Time.timeScale = 0f;

        GenerateWeaponOptions();
        ShowWeaponOptionsInConsole();
    }

    void ChooseOption(int index)
    {
        if (choosingWeapon)
            ChooseWeapon(index);
        else
            ChooseUpgrade(index);
    }

    void GenerateUpgradeOptions()
    {
        currentOptions.Clear();

        List<string> normalGeneralPool = CreateUpgradePool();
        List<string> legendaryOnlyPool = CreateLegendaryOnlyUpgradePool();

        List<UpgradeOption> weaponOptions = CreateWeaponUpgradeOptions();

        // 1 opción de arma garantizada, si hay mejoras de arma disponibles.
        if (guaranteeWeaponUpgradeOption && weaponOptions.Count > 0)
        {
            AddRandomOptionFromPool(weaponOptions, currentOptions);
        }

        // El resto son mejoras generales.
        while (currentOptions.Count < OPTION_COUNT && normalGeneralPool.Count > 0)
        {
            UpgradeOption option = CreateRolledGeneralUpgrade(normalGeneralPool, legendaryOnlyPool);

            if (option != null)
            {
                currentOptions.Add(option);
            }
        }

        // Si por cualquier motivo faltan opciones, rellenamos con mejoras de arma.
        while (currentOptions.Count < OPTION_COUNT && weaponOptions.Count > 0)
        {
            AddRandomOptionFromPool(weaponOptions, currentOptions);
        }
    }

    List<UpgradeOption> CreateWeaponUpgradeOptions()
    {
        List<UpgradeOption> weaponOptions = new List<UpgradeOption>();

        if (weaponManager == null) return weaponOptions;

        List<WeaponBase> activeWeapons = weaponManager.GetActiveWeapons();

        foreach (WeaponBase weapon in activeWeapons)
        {
            List<UpgradeOption> specificOptions = weapon.GetSpecificUpgradeOptions();

            foreach (UpgradeOption option in specificOptions)
            {
                UpgradeRarity rarity = RollRarity();

                option.rarity = rarity;
                option.title = GetRarityPrefix(rarity) + " " + option.title;
                option.description = GetWeaponUpgradeDescription(option.id, rarity, option.description);

                weaponOptions.Add(option);
            }
        }

        return weaponOptions;
    }

    string GetWeaponUpgradeDescription(string id, UpgradeRarity rarity, string fallbackDescription)
    {
        switch (id)
        {
            case "melee_weapon_damage":
                return "Esta arma melee gana +" + Mathf.RoundToInt(GetWeaponDamageValue(rarity) * 100f) + "% de daño.";

            case "melee_weapon_range":
                return "Esta arma melee gana +" + GetWeaponRangeValue(rarity).ToString("0.##") + " de alcance.";

            case "melee_weapon_speed":
                return "Esta arma melee reduce su cooldown en " + GetWeaponCooldownReductionValue(rarity).ToString("0.##") + " segundos.";

            case "ranged_weapon_damage":
                return "Esta arma a distancia gana +" + Mathf.RoundToInt(GetWeaponDamageValue(rarity) * 100f) + "% de daño.";

            case "ranged_weapon_speed":
                return "Esta arma a distancia reduce su cooldown en " + GetWeaponRangedCooldownReductionValue(rarity).ToString("0.##") + " segundos.";

            case "ranged_weapon_range":
                return "Esta arma a distancia gana +" + GetWeaponRangedRangeValue(rarity).ToString("0.##") + " de alcance.";

            case "ranged_weapon_projectile_speed":
                return "Los proyectiles de esta arma vuelan +" + Mathf.RoundToInt(GetWeaponProjectileSpeedValue(rarity) * 100f) + "% más rápido.";
        }

        return fallbackDescription;
    }

    UpgradeOption CreateRolledGeneralUpgrade(List<string> normalGeneralPool, List<string> legendaryOnlyPool)
    {
        UpgradeRarity rolledRarity = RollRarity();

        bool canUseLegendarySpecial =
            rolledRarity == UpgradeRarity.Legendary &&
            legendaryOnlyPool.Count > 0 &&
            Random.value < legendarySpecialChance;

        if (canUseLegendarySpecial)
        {
            int specialIndex = Random.Range(0, legendaryOnlyPool.Count);
            string specialId = legendaryOnlyPool[specialIndex];

            legendaryOnlyPool.RemoveAt(specialIndex);

            if (specialId == "projectilecount")
            {
                lastProjectileCountOfferedLevel = currentRewardLevel;
            }

            return CreateUpgrade(specialId, UpgradeRarity.Legendary);
        }

        if (normalGeneralPool.Count <= 0) return null;

        int index = Random.Range(0, normalGeneralPool.Count);
        string id = normalGeneralPool[index];

        normalGeneralPool.RemoveAt(index);

        return CreateUpgrade(id, rolledRarity);
    }

    List<string> CreateLegendaryOnlyUpgradePool()
    {
        List<string> pool = new List<string>();

        // Regeneración ahora sí es una mejora especial legendaria real.
        pool.Add("regen");

        if (weaponManager != null && weaponManager.HasActiveWeaponWithTag(WeaponTag.Projectile))
        {
            bool projectileCountCanAppear =
                currentRewardLevel - lastProjectileCountOfferedLevel >= projectileCountMinLevelsBetweenOffers;

            if (projectileCountCanAppear)
            {
                pool.Add("projectilecount");
            }
        }

        return pool;
    }

    void AddRandomOptionFromPool(List<UpgradeOption> sourcePool, List<UpgradeOption> destination)
    {
        if (sourcePool == null || sourcePool.Count == 0) return;

        int index = Random.Range(0, sourcePool.Count);

        destination.Add(sourcePool[index]);
        sourcePool.RemoveAt(index);
    }

    void GenerateWeaponOptions()
    {
        currentWeaponOptions.Clear();

        if (weaponManager == null) return;

        currentWeaponOptions = weaponManager.GetRandomInactiveWeapons(OPTION_COUNT);
    }

    void ShowUpgradeOptionsInConsole()
    {
        Debug.Log("=== SUBIDA DE NIVEL ===");

        for (int i = 0; i < currentOptions.Count; i++)
        {
            Debug.Log((i + 1) + ": " + currentOptions[i].title + " - " + currentOptions[i].description);
        }

        Debug.Log("Pulsa 1, 2, 3 o 4 para elegir. Pulsa R para reroll. Coste actual: " + currentRerollCost + " oro");
    }

    void ShowWeaponOptionsInConsole()
    {
        Debug.Log("=== ELIGE UN ARMA ===");

        for (int i = 0; i < currentWeaponOptions.Count; i++)
        {
            Debug.Log((i + 1) + ": " + currentWeaponOptions[i].weaponName + " (" + currentWeaponOptions[i].weaponId + ")");
        }

        Debug.Log("Pulsa 1, 2, 3 o 4 para elegir tu arma.");
    }

    void ChooseUpgrade(int index)
    {
        if (index < 0 || index >= currentOptions.Count) return;

        UpgradeOption chosenOption = currentOptions[index];
        ApplyUpgrade(chosenOption);

        Debug.Log("Mejora elegida: " + chosenOption.title);

        CloseMenu();
    }

    void ChooseWeapon(int index)
    {
        if (index < 0 || index >= currentWeaponOptions.Count) return;

        WeaponBase chosenWeapon = currentWeaponOptions[index];

        if (weaponManager != null && chosenWeapon != null)
        {
            weaponManager.ActivateWeaponById(chosenWeapon.weaponId);
            Debug.Log("Arma elegida: " + chosenWeapon.weaponName);
        }

        CloseMenu();
    }

    void CloseMenu()
    {
        waitingForChoice = false;
        choosingWeapon = false;
        Time.timeScale = 1f;

        currentOptions.Clear();
        currentWeaponOptions.Clear();
    }

    void RerollUpgrades()
    {
        if (currency == null)
        {
            Debug.Log("No se encontró PlayerCurrency para hacer reroll.");
            return;
        }

        if (!currency.SpendCoins(currentRerollCost))
        {
            Debug.Log("No tienes oro suficiente para reroll. Coste actual: " + currentRerollCost);
            return;
        }

        GenerateUpgradeOptions();

        currentRerollCost += rerollCostIncrease;

        Debug.Log("Reroll realizado. Nuevo coste: " + currentRerollCost + " oro");

        ShowUpgradeOptionsInConsole();
    }

    void ApplyUpgrade(UpgradeOption option)
    {
        if (option.isWeaponUpgrade)
        {
            WeaponBase weapon = weaponManager.GetWeaponById(option.weaponId);

            if (weapon != null)
            {
                weapon.ApplySpecificUpgrade(option);
            }

            return;
        }

        switch (option.id)
        {
            case "damage":
                stats.damageMultiplier += GetDamageValue(option.rarity);
                break;

            case "melee":
                stats.meleeDamageMultiplier += GetTypeDamageValue(option.rarity);
                break;

            case "ranged":
                stats.rangedDamageMultiplier += GetTypeDamageValue(option.rarity);
                break;

            case "magic":
                stats.magicDamageMultiplier += GetTypeDamageValue(option.rarity);
                break;

            case "attackspeed":
                stats.attackSpeedMultiplier += GetAttackSpeedValue(option.rarity);
                break;

            case "movespeed":
                stats.moveSpeed += GetMoveSpeedValue(option.rarity);
                break;

            case "critchance":
                stats.critChance = Mathf.Min(1f, stats.critChance + GetCritChanceValue(option.rarity));
                break;

            case "critmulti":
                stats.critMultiplier += GetCritMultiValue(option.rarity);
                break;

            case "pickup":
                stats.pickupRange += GetPickupValue(option.rarity);
                break;

            case "maxhealth":
                int hpGain = GetHealthValue(option.rarity);
                stats.maxHealth += hpGain;

                if (health != null)
                {
                    health.currentHealth = Mathf.Min(health.currentHealth + hpGain, stats.maxHealth);
                }
                break;

            case "regen":
                stats.healthRegen += GetHealthRegenValue(option.rarity);
                break;

            case "armor":
                stats.armor += GetArmorValue(option.rarity);
                break;

            case "dodge":
                stats.dodgeChance = Mathf.Min(0.75f, stats.dodgeChance + GetDodgeChanceValue(option.rarity));
                break;

            case "xpboost":
                stats.xpMultiplier += GetXPBoostValue(option.rarity);
                break;

            case "luck":
                stats.luck += GetLuckValue(option.rarity);
                break;

            case "fire":
                stats.fireDamageMultiplier += GetElementDamageValue(option.rarity);
                break;

            case "poison":
                stats.poisonDamageMultiplier += GetElementDamageValue(option.rarity);
                break;

            case "bleed":
                stats.bleedDamageMultiplier += GetElementDamageValue(option.rarity);
                break;

            case "meleerange":
                stats.areaRangeBonus += GetMeleeRangeValue(option.rarity);
                break;

            case "projectilespeed":
                stats.projectileSpeedMultiplier += GetProjectileSpeedValue(option.rarity);
                break;

            case "projectilecount":
                stats.projectileCountBonus += GetProjectileCountValue(option.rarity);
                break;

            case "statuseffect":
                stats.statusEffectChance = Mathf.Min(1f, stats.statusEffectChance + GetStatusEffectChanceValue(option.rarity));
                break;
        }
    }

    List<string> CreateUpgradePool()
    {
        List<string> pool = new List<string>
        {
            "damage",
            "attackspeed",
            "movespeed",
            "critchance",
            "critmulti",
            "pickup",
            "maxhealth",
            "armor",
            "dodge",
            "xpboost",
            "luck"
        };

        if (weaponManager != null)
        {
            if (weaponManager.HasActiveWeaponWithTag(WeaponTag.Melee))
            {
                pool.Add("melee");
            }

            if (weaponManager.HasActiveWeaponWithTag(WeaponTag.Ranged))
            {
                pool.Add("ranged");
            }

            if (weaponManager.HasActiveWeaponWithTag(WeaponTag.Area))
            {
                pool.Add("meleerange");
            }

            if (weaponManager.HasActiveWeaponWithTag(WeaponTag.Projectile))
            {
                pool.Add("projectilespeed");
            }

            if (weaponManager.HasActiveWeaponWithTag(WeaponTag.Poison))
            {
                pool.Add("poison");
            }

            if (weaponManager.HasActiveWeaponWithTag(WeaponTag.Bleed))
            {
                pool.Add("bleed");
            }

            if (weaponManager.HasActiveWeaponWithTag(WeaponTag.Magic))
            {
                pool.Add("magic");
            }
        }

        return pool;
    }

    UpgradeOption CreateUpgrade(string id)
    {
        return CreateUpgrade(id, RollRarity());
    }

    UpgradeOption CreateUpgrade(string id, UpgradeRarity rarity)
    {
        switch (id)
        {
            case "damage":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Daño general",
                    "Aumenta todo tu daño en +" + Mathf.RoundToInt(GetDamageValue(rarity) * 100f) + "%.",
                    rarity
                );

            case "melee":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Daño melee",
                    "Tus armas melee hacen +" + Mathf.RoundToInt(GetTypeDamageValue(rarity) * 100f) + "% de daño.",
                    rarity
                );

            case "ranged":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Daño a distancia",
                    "Tus armas a distancia hacen +" + Mathf.RoundToInt(GetTypeDamageValue(rarity) * 100f) + "% de daño.",
                    rarity
                );

            case "magic":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Daño mágico",
                    "Tus armas mágicas hacen +" + Mathf.RoundToInt(GetTypeDamageValue(rarity) * 100f) + "% de daño.",
                    rarity
                );

            case "attackspeed":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Velocidad de ataque",
                    "Tus armas atacan +" + Mathf.RoundToInt(GetAttackSpeedValue(rarity) * 100f) + "% más rápido.",
                    rarity
                );

            case "movespeed":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Velocidad",
                    "Te mueves +" + GetMoveSpeedValue(rarity).ToString("0.##") + " más rápido.",
                    rarity
                );

            case "critchance":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Crítico",
                    "Aumenta tu probabilidad de crítico en +" + Mathf.RoundToInt(GetCritChanceValue(rarity) * 100f) + "%.",
                    rarity
                );

            case "critmulti":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Daño crítico",
                    "Aumenta el multiplicador crítico en +" + GetCritMultiValue(rarity).ToString("0.##") + ".",
                    rarity
                );

            case "pickup":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Recogida",
                    "Aumenta el rango de recogida en +" + GetPickupValue(rarity).ToString("0.##") + ".",
                    rarity
                );

            case "maxhealth":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Vida máxima",
                    "Aumenta tu vida máxima en +" + GetHealthValue(rarity) + " y te cura esa cantidad.",
                    rarity
                );

            case "regen":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Regeneración",
                    "Regeneras +" + GetHealthRegenValue(rarity).ToString("0.##") + " vida por segundo.",
                    rarity
                );

            case "armor":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Armadura",
                    "Ganas +" + GetArmorValue(rarity) + " armadura.",
                    rarity
                );

            case "dodge":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Esquiva",
                    "Aumenta la esquiva en +" + Mathf.RoundToInt(GetDodgeChanceValue(rarity) * 100f) + "%.",
                    rarity
                );

            case "xpboost":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Experiencia",
                    "Ganas +" + Mathf.RoundToInt(GetXPBoostValue(rarity) * 100f) + "% experiencia.",
                    rarity
                );

            case "luck":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Suerte",
                    "Ganas +" + GetLuckValue(rarity).ToString("0.##") + " suerte.",
                    rarity
                );

            case "fire":
                return new UpgradeOption(id, GetRarityPrefix(rarity) + " Fuego", "Tus efectos de fuego ganan +" + Mathf.RoundToInt(GetElementDamageValue(rarity) * 100f) + "% daño.", rarity);

            case "poison":
                return new UpgradeOption(id, GetRarityPrefix(rarity) + " Veneno", "Tus efectos de veneno ganan +" + Mathf.RoundToInt(GetElementDamageValue(rarity) * 100f) + "% daño.", rarity);

            case "bleed":
                return new UpgradeOption(id, GetRarityPrefix(rarity) + " Sangrado", "Tus efectos de sangrado ganan +" + Mathf.RoundToInt(GetElementDamageValue(rarity) * 100f) + "% daño.", rarity);

            case "meleerange":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Área de ataque",
                    "Aumenta el rango de tus ataques cercanos y auras en +" + GetMeleeRangeValue(rarity).ToString("0.##") + ".",
                    rarity
                );

            case "projectilespeed":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Velocidad de proyectil",
                    "Tus proyectiles ganan +" + Mathf.RoundToInt(GetProjectileSpeedValue(rarity) * 100f) + "% velocidad.",
                    rarity
                );

            case "projectilecount":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Proyectil extra",
                    "Tus armas a distancia ganan +1 proyectil extra.",
                    rarity
                );

            case "statuseffect":
                return new UpgradeOption(
                    id,
                    GetRarityPrefix(rarity) + " Estados alterados",
                    "Aumenta la probabilidad de aplicar estados en +" + Mathf.RoundToInt(GetStatusEffectChanceValue(rarity) * 100f) + "%.",
                    rarity
                );
        }

        return new UpgradeOption(id, "Upgrade", "Upgrade genérico", rarity);
    }

    UpgradeRarity RollRarity()
    {
        float commonChance = 70f;
        float rareChance = 20f;
        float epicChance = 9f;
        float legendaryChance = 1f;

        if (stats != null)
        {
            commonChance -= stats.luck * 3f;
            rareChance += stats.luck * 1.5f;
            epicChance += stats.luck * 1f;
            legendaryChance += stats.luck * 0.5f;
        }

        commonChance = Mathf.Max(10f, commonChance);

        float total = commonChance + rareChance + epicChance + legendaryChance;
        float roll = Random.Range(0f, total);

        if (roll < commonChance) return UpgradeRarity.Common;

        roll -= commonChance;
        if (roll < rareChance) return UpgradeRarity.Rare;

        roll -= rareChance;
        if (roll < epicChance) return UpgradeRarity.Epic;

        return UpgradeRarity.Legendary;
    }

    UpgradeRarity RollRarityForUpgrade(string id)
    {
        return RollRarity();
    }

    string GetRarityPrefix(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return "[Común]";
            case UpgradeRarity.Rare: return "[Rara]";
            case UpgradeRarity.Epic: return "[Épica]";
            case UpgradeRarity.Legendary: return "[Legendaria]";
        }

        return "";
    }

    float GetDamageValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.04f;
            case UpgradeRarity.Rare: return 0.07f;
            case UpgradeRarity.Epic: return 0.10f;
            case UpgradeRarity.Legendary: return 0.15f;
        }

        return 0.05f;
    }

    float GetTypeDamageValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.08f;
            case UpgradeRarity.Rare: return 0.15f;
            case UpgradeRarity.Epic: return 0.25f;
            case UpgradeRarity.Legendary: return 0.40f;
        }

        return 0.08f;
    }

    float GetAttackSpeedValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.07f;
            case UpgradeRarity.Rare: return 0.12f;
            case UpgradeRarity.Epic: return 0.20f;
            case UpgradeRarity.Legendary: return 0.30f;
        }

        return 0.10f;
    }

    float GetMoveSpeedValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.3f;
            case UpgradeRarity.Rare: return 0.5f;
            case UpgradeRarity.Epic: return 0.75f;
            case UpgradeRarity.Legendary: return 1f;
        }

        return 0.3f;
    }

    float GetCritChanceValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.03f;
            case UpgradeRarity.Rare: return 0.05f;
            case UpgradeRarity.Epic: return 0.07f;
            case UpgradeRarity.Legendary: return 0.10f;
        }

        return 0.02f;
    }

    float GetCritMultiValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.15f;
            case UpgradeRarity.Rare: return 0.30f;
            case UpgradeRarity.Epic: return 0.50f;
            case UpgradeRarity.Legendary: return 0.75f;
        }

        return 0.15f;
    }

    float GetPickupValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.4f;
            case UpgradeRarity.Rare: return 0.8f;
            case UpgradeRarity.Epic: return 1.2f;
            case UpgradeRarity.Legendary: return 1.8f;
        }

        return 0.4f;
    }

    int GetHealthValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 10;
            case UpgradeRarity.Rare: return 20;
            case UpgradeRarity.Epic: return 35;
            case UpgradeRarity.Legendary: return 50;
        }

        return 10;
    }

    float GetHealthRegenValue(UpgradeRarity rarity)
    {
        return 0.75f;
    }

    int GetArmorValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 1;
            case UpgradeRarity.Rare: return 2;
            case UpgradeRarity.Epic: return 3;
            case UpgradeRarity.Legendary: return 4;
        }

        return 1;
    }

    float GetDodgeChanceValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.02f;
            case UpgradeRarity.Rare: return 0.04f;
            case UpgradeRarity.Epic: return 0.07f;
            case UpgradeRarity.Legendary: return 0.10f;
        }

        return 0.02f;
    }

    float GetXPBoostValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.15f;
            case UpgradeRarity.Rare: return 0.30f;
            case UpgradeRarity.Epic: return 0.50f;
            case UpgradeRarity.Legendary: return 0.75f;
        }

        return 0.15f;
    }

    float GetLuckValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 1f;
            case UpgradeRarity.Rare: return 2f;
            case UpgradeRarity.Epic: return 3f;
            case UpgradeRarity.Legendary: return 5f;
        }

        return 1f;
    }

    float GetElementDamageValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.08f;
            case UpgradeRarity.Rare: return 0.15f;
            case UpgradeRarity.Epic: return 0.25f;
            case UpgradeRarity.Legendary: return 0.40f;
        }

        return 0.08f;
    }

    float GetMeleeRangeValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.15f;
            case UpgradeRarity.Rare: return 0.30f;
            case UpgradeRarity.Epic: return 0.45f;
            case UpgradeRarity.Legendary: return 0.70f;
        }

        return 0.15f;
    }

    float GetProjectileSpeedValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.10f;
            case UpgradeRarity.Rare: return 0.20f;
            case UpgradeRarity.Epic: return 0.35f;
            case UpgradeRarity.Legendary: return 0.50f;
        }

        return 0.10f;
    }

    int GetProjectileCountValue(UpgradeRarity rarity)
    {
        return 1;
    }

    float GetStatusEffectChanceValue(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return 0.03f;
            case UpgradeRarity.Rare: return 0.06f;
            case UpgradeRarity.Epic: return 0.10f;
            case UpgradeRarity.Legendary: return 0.15f;
        }

        return 0.03f;
    }

    float GetWeaponDamageValue(UpgradeRarity rarity)
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

    float GetWeaponRangeValue(UpgradeRarity rarity)
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

    float GetWeaponCooldownReductionValue(UpgradeRarity rarity)
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

    float GetWeaponRangedCooldownReductionValue(UpgradeRarity rarity)
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

    float GetWeaponRangedRangeValue(UpgradeRarity rarity)
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

    float GetWeaponProjectileSpeedValue(UpgradeRarity rarity)
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

public enum UpgradeRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}


[System.Serializable]
public class UpgradeOption
{
    public string id;
    public string title;
    public string description;
    public UpgradeRarity rarity;

    public bool isWeaponUpgrade;
    public string weaponId;

    public UpgradeOption(
        string id,
        string title,
        string description,
        UpgradeRarity rarity,
        bool isWeaponUpgrade = false,
        string weaponId = ""
    )
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.rarity = rarity;
        this.isWeaponUpgrade = isWeaponUpgrade;
        this.weaponId = weaponId;
    }

}