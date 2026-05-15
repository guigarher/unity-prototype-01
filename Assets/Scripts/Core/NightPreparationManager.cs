using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class NightPreparationManager : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerStats playerStats;
    public PlayerHealth playerHealth;
    public PlayerResources playerResources;
    public PlayerCurrency playerCurrency;
    public BaseCore baseCore;

    [Header("Reparación manual")]
    public int repairWoodCost = 5;
    public int repairStoneCost = 5;
    public int repairAmount = 20;

    [Header("Reroll")]
    public int baseRerollCost = 5;
    public int rerollCostIncrease = 3;

    [Header("Pulso defensivo de base")]
    public float basePulseRadius = 5f;
    public float basePulseInterval = 3f;

    private int currentRerollCost;
    private bool isPreparationOpen = false;

    private List<NightPreparationOption> currentOptions = new List<NightPreparationOption>();

    // Buffs temporales del jugador
    private float temporaryMoveSpeedBonus = 0f;
    private float temporaryDamageBonus = 0f;
    private float temporaryAttackSpeedBonus = 0f;
    private float temporaryCritChanceBonus = 0f;
    private float temporaryDodgeChanceBonus = 0f;
    private float temporaryMeleeRangeBonus = 0f;
    private float temporaryProjectileSpeedBonus = 0f;
    private float temporaryHealthRegenBonus = 0f;

    private int temporaryArmorBonus = 0;
    private int temporaryMaxHealthBonus = 0;
    private int temporaryProjectileCountBonus = 0;

    // Buffs temporales de base
    private int temporaryBaseShieldBonus = 0;
    private int temporaryBasePulseDamage = 0;
    private Coroutine basePulseRoutine;

    void Start()
    {
        FindReferencesIfNeeded();
        currentRerollCost = baseRerollCost;
    }

    void OnEnable()
    {
        GamePhaseManager.OnPhaseChanged += OnPhaseChanged;
    }

    void OnDisable()
    {
        GamePhaseManager.OnPhaseChanged -= OnPhaseChanged;
    }

    void Update()
    {
        if (!isPreparationOpen) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) TryBuyOption(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) TryBuyOption(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) TryBuyOption(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) TryBuyOption(3);

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            TryRepairBase();
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryRerollOptions();
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            StartNight();
        }
    }

    void FindReferencesIfNeeded()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            if (playerStats == null) playerStats = player.GetComponent<PlayerStats>();
            if (playerHealth == null) playerHealth = player.GetComponent<PlayerHealth>();
            if (playerResources == null) playerResources = player.GetComponent<PlayerResources>();
            if (playerCurrency == null) playerCurrency = player.GetComponent<PlayerCurrency>();
        }

        if (baseCore == null && BaseCore.Instance != null)
        {
            baseCore = BaseCore.Instance;
        }
    }

    void OnPhaseChanged(GamePhase phase)
    {
        FindReferencesIfNeeded();

        if (phase == GamePhase.Night)
        {
            OpenNightPreparation();
        }
        else if (phase == GamePhase.Day)
        {
            RemoveTemporaryNightBuffs();
        }
    }

    void OpenNightPreparation()
    {
        if (isPreparationOpen) return;

        isPreparationOpen = true;
        currentRerollCost = baseRerollCost;

        Time.timeScale = 0f;

        GenerateNightOptions();
        ShowNightPreparationInConsole();
    }

    void GenerateNightOptions()
    {
        currentOptions.Clear();

        currentOptions.Add(CreateRandomWoodOption());
        currentOptions.Add(CreateRandomStoneOption());
        currentOptions.Add(CreateRandomMixedOption());
        currentOptions.Add(CreateRandomBaseOption());
    }

    NightPreparationOption CreateRandomWoodOption()
    {
        int roll = Random.Range(0, 5);

        switch (roll)
        {
            case 0:
                return new NightPreparationOption(
                    "wood_damage",
                    "Combustión agresiva",
                    "Esta noche: +25% daño general.\nPermanente: +3% daño general.",
                    8,
                    0,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.Damage, 0.25f),
                        NightEffect.Permanent(NightEffectType.Damage, 0.03f)
                    }
                );

            case 1:
                return new NightPreparationOption(
                    "wood_attack_speed",
                    "Vapor a presión",
                    "Esta noche: +25% velocidad de ataque.\nPermanente: +3% velocidad de ataque.",
                    8,
                    0,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.AttackSpeed, 0.25f),
                        NightEffect.Permanent(NightEffectType.AttackSpeed, 0.03f)
                    }
                );

            case 2:
                return new NightPreparationOption(
                    "wood_crit",
                    "Mira de latón",
                    "Esta noche: +15% crítico.\nPermanente: +2% crítico.",
                    7,
                    0,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.CritChance, 0.15f),
                        NightEffect.Permanent(NightEffectType.CritChance, 0.02f)
                    }
                );

            case 3:
                return new NightPreparationOption(
                    "wood_melee_range",
                    "Guadaña expansiva",
                    "Esta noche: +0.50 rango melee.\nPermanente: +0.12 rango melee.",
                    7,
                    0,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.MeleeRange, 0.50f),
                        NightEffect.Permanent(NightEffectType.MeleeRange, 0.12f)
                    }
                );

            default:
                return new NightPreparationOption(
                    "wood_projectile",
                    "Munición de vapor",
                    "Esta noche: +1 proyectil extra.\nPermanente: +8% velocidad de proyectil.",
                    10,
                    0,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.ProjectileCount, 1f),
                        NightEffect.Permanent(NightEffectType.ProjectileSpeed, 0.08f)
                    }
                );
        }
    }

    NightPreparationOption CreateRandomStoneOption()
    {
        int roll = Random.Range(0, 4);

        switch (roll)
        {
            case 0:
                return new NightPreparationOption(
                    "stone_dodge",
                    "Reflejos minerales",
                    "Esta noche: +12% esquiva.\nPermanente: +2% esquiva.",
                    0,
                    7,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.DodgeChance, 0.12f),
                        NightEffect.Permanent(NightEffectType.DodgeChance, 0.02f)
                    }
                );

            case 1:
                return new NightPreparationOption(
                    "stone_armor",
                    "Plumas blindadas",
                    "Esta noche: +2 armadura.\nPermanente: +5 vida máxima.",
                    0,
                    8,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.Armor, 2f),
                        NightEffect.Permanent(NightEffectType.MaxHealth, 5f)
                    }
                );

            case 2:
                return new NightPreparationOption(
                    "stone_health",
                    "Corazón reforzado",
                    "Esta noche: +20 vida máxima.\nPermanente: +8 vida máxima.",
                    0,
                    8,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.MaxHealth, 20f),
                        NightEffect.Permanent(NightEffectType.MaxHealth, 8f)
                    }
                );

            default:
                return new NightPreparationOption(
                    "stone_defense_mix",
                    "Instinto defensivo",
                    "Esta noche: +6% esquiva y +1 armadura.\nPermanente: +4 vida máxima.",
                    0,
                    9,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.DodgeChance, 0.06f),
                        NightEffect.Night(NightEffectType.Armor, 1f),
                        NightEffect.Permanent(NightEffectType.MaxHealth, 4f)
                    }
                );
        }
    }

    NightPreparationOption CreateRandomMixedOption()
    {
        List<NightEffect> effects = new List<NightEffect>();

        string title;
        string description;

        bool useRegenBase = Random.value < 0.5f;

        if (useRegenBase)
        {
            title = "Sistema hidráulico inestable";
            description = "Esta noche: +0.8 regeneración de vida.\nPermanente: +25 vida máxima de base.";

            effects.Add(NightEffect.Night(NightEffectType.HealthRegen, 0.8f));
            effects.Add(NightEffect.Permanent(NightEffectType.BaseMaxHealth, 25f));
        }
        else
        {
            title = "Motor de vapor ligero";
            description = "Esta noche: +15% velocidad de movimiento.\nPermanente: +0.10 velocidad de movimiento.";

            effects.Add(NightEffect.Night(NightEffectType.MoveSpeedPercent, 0.15f));
            effects.Add(NightEffect.Permanent(NightEffectType.MoveSpeedFlat, 0.10f));
        }

        NightEffect offensiveNight;
        NightEffect offensivePermanent;
        string offensiveText;
        int extraWoodCost;
        int extraStoneCost;

        CreateReducedOffensiveEffect(
            out offensiveNight,
            out offensivePermanent,
            out offensiveText,
            out extraWoodCost,
            out extraStoneCost
        );

        NightEffect defensiveNight;
        NightEffect defensivePermanent;
        string defensiveText;

        CreateReducedDefensiveEffect(
            out defensiveNight,
            out defensivePermanent,
            out defensiveText
        );

        effects.Add(offensiveNight);
        effects.Add(offensivePermanent);
        effects.Add(defensiveNight);
        effects.Add(defensivePermanent);

        description += "\n" + offensiveText;
        description += "\n" + defensiveText;

        return new NightPreparationOption(
            "mixed_generated",
            title,
            description,
            5 + extraWoodCost,
            5 + extraStoneCost,
            0,
            effects
        );
    }

    void CreateReducedOffensiveEffect(
        out NightEffect night,
        out NightEffect permanent,
        out string text,
        out int extraWoodCost,
        out int extraStoneCost
    )
    {
        extraWoodCost = 0;
        extraStoneCost = 0;

        int roll = Random.Range(0, 5);

        switch (roll)
        {
            case 0:
                night = NightEffect.Night(NightEffectType.Damage, 0.10f);
                permanent = NightEffect.Permanent(NightEffectType.Damage, 0.01f);
                text = "Extra: +10% daño esta noche y +1% daño permanente.";
                return;

            case 1:
                night = NightEffect.Night(NightEffectType.AttackSpeed, 0.10f);
                permanent = NightEffect.Permanent(NightEffectType.AttackSpeed, 0.01f);
                text = "Extra: +10% velocidad de ataque esta noche y +1% permanente.";
                return;

            case 2:
                night = NightEffect.Night(NightEffectType.CritChance, 0.06f);
                permanent = NightEffect.Permanent(NightEffectType.CritChance, 0.01f);
                text = "Extra: +6% crítico esta noche y +1% crítico permanente.";
                return;

            case 3:
                night = NightEffect.Night(NightEffectType.MeleeRange, 0.20f);
                permanent = NightEffect.Permanent(NightEffectType.MeleeRange, 0.05f);
                text = "Extra: +0.20 rango melee esta noche y +0.05 permanente.";
                return;

            default:
                night = NightEffect.Night(NightEffectType.ProjectileCount, 1f);
                permanent = NightEffect.Permanent(NightEffectType.ProjectileSpeed, 0.04f);
                text = "Extra: +1 proyectil esta noche y +4% velocidad de proyectil permanente.";
                extraWoodCost = 2;
                extraStoneCost = 1;
                return;
        }
    }

    void CreateReducedDefensiveEffect(
        out NightEffect night,
        out NightEffect permanent,
        out string text
    )
    {
        int roll = Random.Range(0, 4);

        switch (roll)
        {
            case 0:
                night = NightEffect.Night(NightEffectType.DodgeChance, 0.05f);
                permanent = NightEffect.Permanent(NightEffectType.DodgeChance, 0.01f);
                text = "Extra: +5% esquiva esta noche y +1% esquiva permanente.";
                return;

            case 1:
                night = NightEffect.Night(NightEffectType.Armor, 1f);
                permanent = NightEffect.Permanent(NightEffectType.MaxHealth, 2f);
                text = "Extra: +1 armadura esta noche y +2 vida máxima permanente.";
                return;

            case 2:
                night = NightEffect.Night(NightEffectType.MaxHealth, 10f);
                permanent = NightEffect.Permanent(NightEffectType.MaxHealth, 2f);
                text = "Extra: +10 vida máxima esta noche y +2 permanente.";
                return;

            default:
                night = NightEffect.Night(NightEffectType.BaseRepair, 25f);
                permanent = NightEffect.Permanent(NightEffectType.BaseMaxHealth, 15f);
                text = "Extra: repara +25 base esta noche y +15 vida máxima de base permanente.";
                return;
        }
    }

    NightPreparationOption CreateRandomBaseOption()
    {
        int roll = Random.Range(0, 4);

        switch (roll)
        {
            case 0:
                return new NightPreparationOption(
                    "base_shield",
                    "Escudo del núcleo",
                    "Esta noche: la base gana +50 de escudo.\nPermanente: +25 vida máxima de base.",
                    6,
                    8,
                    3,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.BaseShield, 50f),
                        NightEffect.Permanent(NightEffectType.BaseMaxHealth, 25f)
                    }
                );

            case 1:
                return new NightPreparationOption(
                    "base_pulse",
                    "Pulso de caldera",
                    "Esta noche: la base hace daño en área cada pocos segundos.\nPermanente: +20 vida máxima de base.",
                    8,
                    8,
                    5,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.BasePulseDamage, 4f),
                        NightEffect.Permanent(NightEffectType.BaseMaxHealth, 20f)
                    }
                );

            case 2:
                return new NightPreparationOption(
                    "base_big_repair",
                    "Reparación mayor",
                    "Esta noche: repara +100 vida de base.\nPermanente: +15 vida máxima de base.",
                    5,
                    8,
                    2,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.BaseRepair, 100f),
                        NightEffect.Permanent(NightEffectType.BaseMaxHealth, 15f)
                    }
                );

            default:
                return new NightPreparationOption(
                    "base_foundation",
                    "Muro de chatarra",
                    "Esta noche: +30 escudo de base.\nPermanente: +50 vida máxima de base.",
                    2,
                    10,
                    4,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.BaseShield, 30f),
                        NightEffect.Permanent(NightEffectType.BaseMaxHealth, 50f)
                    }
                );
        }
    }

    void ShowNightPreparationInConsole()
    {
        Debug.Log("=== PREPARACIÓN NOCTURNA ===");
        Debug.Log("Recursos: " + GetResourceDebugText());

        for (int i = 0; i < currentOptions.Count; i++)
        {
            NightPreparationOption option = currentOptions[i];

            Debug.Log(
                (i + 1) + ": " + option.title +
                " | Coste: " + option.GetCostText() +
                "\n" + option.description
            );
        }

        Debug.Log("R: Reparar base | Coste: " + repairWoodCost + " madera + " + repairStoneCost + " piedra | +" + repairAmount + " vida");
        Debug.Log("F: Reroll opciones | Coste actual: " + currentRerollCost + " oro");
        Debug.Log("ENTER: Empezar la noche");
    }

    void TryBuyOption(int index)
    {
        if (index < 0 || index >= currentOptions.Count) return;
        if (playerResources == null) return;

        NightPreparationOption option = currentOptions[index];

        if (!playerResources.CanSpendResources(option.woodCost, option.stoneCost))
        {
            Debug.Log("No tienes madera/piedra suficiente para: " + option.title);
            return;
        }

        if (option.coinCost > 0)
        {
            if (playerCurrency == null)
            {
                Debug.Log("No se encontró PlayerCurrency para gastar oro.");
                return;
            }

            if (!playerCurrency.SpendCoins(option.coinCost))
            {
                Debug.Log("No tienes oro suficiente para: " + option.title);
                return;
            }
        }

        playerResources.SpendResources(option.woodCost, option.stoneCost);

        ApplyNightPreparationOption(option);

        Debug.Log("Comprado: " + option.title);

        currentOptions.RemoveAt(index);

        ShowNightPreparationInConsole();
    }

    void ApplyNightPreparationOption(NightPreparationOption option)
    {
        foreach (NightEffect effect in option.effects)
        {
            ApplyEffect(effect);
        }
    }

    void ApplyEffect(NightEffect effect)
    {
        if (playerStats == null) return;

        switch (effect.type)
        {
            case NightEffectType.Damage:
                playerStats.damageMultiplier += effect.value;
                if (!effect.isPermanent) temporaryDamageBonus += effect.value;
                break;

            case NightEffectType.AttackSpeed:
                playerStats.attackSpeedMultiplier += effect.value;
                if (!effect.isPermanent) temporaryAttackSpeedBonus += effect.value;
                break;

            case NightEffectType.CritChance:
                playerStats.critChance = Mathf.Min(1f, playerStats.critChance + effect.value);
                if (!effect.isPermanent) temporaryCritChanceBonus += effect.value;
                break;

            case NightEffectType.DodgeChance:
                playerStats.dodgeChance = Mathf.Min(0.75f, playerStats.dodgeChance + effect.value);
                if (!effect.isPermanent) temporaryDodgeChanceBonus += effect.value;
                break;

            case NightEffectType.MeleeRange:
                playerStats.meleeRange += effect.value;
                if (!effect.isPermanent) temporaryMeleeRangeBonus += effect.value;
                break;

            case NightEffectType.ProjectileSpeed:
                playerStats.projectileSpeedMultiplier += effect.value;
                if (!effect.isPermanent) temporaryProjectileSpeedBonus += effect.value;
                break;

            case NightEffectType.ProjectileCount:
                int projectileBonus = Mathf.RoundToInt(effect.value);
                playerStats.projectileCountBonus += projectileBonus;
                if (!effect.isPermanent) temporaryProjectileCountBonus += projectileBonus;
                break;

            case NightEffectType.MoveSpeedPercent:
                float moveBonus = playerStats.moveSpeed * effect.value;
                playerStats.moveSpeed += moveBonus;
                if (!effect.isPermanent) temporaryMoveSpeedBonus += moveBonus;
                break;

            case NightEffectType.MoveSpeedFlat:
                playerStats.moveSpeed += effect.value;
                if (!effect.isPermanent) temporaryMoveSpeedBonus += effect.value;
                break;

            case NightEffectType.HealthRegen:
                playerStats.healthRegen += effect.value;
                if (!effect.isPermanent) temporaryHealthRegenBonus += effect.value;
                break;

            case NightEffectType.Armor:
                int armorBonus = Mathf.RoundToInt(effect.value);
                playerStats.armor += armorBonus;
                if (!effect.isPermanent) temporaryArmorBonus += armorBonus;
                break;

            case NightEffectType.MaxHealth:
                int healthBonus = Mathf.RoundToInt(effect.value);
                playerStats.maxHealth += healthBonus;

                if (playerHealth != null)
                {
                    playerHealth.currentHealth += healthBonus;
                    playerHealth.currentHealth = Mathf.Min(playerHealth.currentHealth, playerStats.maxHealth);
                }

                if (!effect.isPermanent) temporaryMaxHealthBonus += healthBonus;
                break;

            case NightEffectType.BaseMaxHealth:
                if (baseCore != null)
                {
                    baseCore.IncreaseMaxHealth(Mathf.RoundToInt(effect.value), true);
                }
                break;

            case NightEffectType.BaseRepair:
                if (baseCore != null)
                {
                    baseCore.Repair(Mathf.RoundToInt(effect.value));
                }
                break;

            case NightEffectType.BaseShield:
                if (baseCore != null)
                {
                    int shieldAmount = Mathf.RoundToInt(effect.value);
                    baseCore.AddShield(shieldAmount);

                    if (!effect.isPermanent)
                    {
                        temporaryBaseShieldBonus += shieldAmount;
                    }
                }
                break;

            case NightEffectType.BasePulseDamage:
                int pulseDamage = Mathf.RoundToInt(effect.value);
                temporaryBasePulseDamage += pulseDamage;

                if (basePulseRoutine == null)
                {
                    basePulseRoutine = StartCoroutine(BasePulseRoutine());
                }
                break;
        }
    }

    IEnumerator BasePulseRoutine()
    {
        while (temporaryBasePulseDamage > 0)
        {
            yield return new WaitForSeconds(basePulseInterval);

            if (baseCore == null) continue;

            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            foreach (GameObject enemy in enemies)
            {
                if (enemy == null) continue;

                float distance = Vector2.Distance(baseCore.transform.position, enemy.transform.position);

                if (distance <= basePulseRadius)
                {
                    EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(temporaryBasePulseDamage, false);
                    }
                }
            }

            Debug.Log("Pulso de base hizo " + temporaryBasePulseDamage + " daño en área.");
        }

        basePulseRoutine = null;
    }

    void TryRepairBase()
    {
        if (playerResources == null || baseCore == null) return;

        if (baseCore.currentHealth >= baseCore.maxHealth)
        {
            Debug.Log("La base ya está al máximo de vida.");
            return;
        }

        if (!playerResources.CanSpendResources(repairWoodCost, repairStoneCost))
        {
            Debug.Log("No tienes recursos suficientes para reparar.");
            return;
        }

        playerResources.SpendResources(repairWoodCost, repairStoneCost);
        baseCore.Repair(repairAmount);

        Debug.Log("Base reparada +" + repairAmount + ". Recursos restantes: " + GetResourceDebugText());
        ShowNightPreparationInConsole();
    }

    void TryRerollOptions()
    {
        if (playerCurrency == null)
        {
            Debug.Log("No se encontró PlayerCurrency para hacer reroll.");
            return;
        }

        if (!playerCurrency.SpendCoins(currentRerollCost))
        {
            Debug.Log("No tienes oro suficiente para reroll. Coste: " + currentRerollCost);
            return;
        }

        GenerateNightOptions();

        currentRerollCost += rerollCostIncrease;

        Debug.Log("Reroll nocturno realizado. Nuevo coste: " + currentRerollCost);
        ShowNightPreparationInConsole();
    }

    void StartNight()
    {
        isPreparationOpen = false;
        Time.timeScale = 1f;

        Debug.Log("La noche comienza.");
    }

    void RemoveTemporaryNightBuffs()
    {
        if (playerStats == null) return;

        playerStats.moveSpeed -= temporaryMoveSpeedBonus;
        playerStats.damageMultiplier -= temporaryDamageBonus;
        playerStats.attackSpeedMultiplier -= temporaryAttackSpeedBonus;
        playerStats.critChance -= temporaryCritChanceBonus;
        playerStats.dodgeChance -= temporaryDodgeChanceBonus;
        playerStats.meleeRange -= temporaryMeleeRangeBonus;
        playerStats.projectileSpeedMultiplier -= temporaryProjectileSpeedBonus;
        playerStats.healthRegen -= temporaryHealthRegenBonus;

        playerStats.armor -= temporaryArmorBonus;
        playerStats.projectileCountBonus -= temporaryProjectileCountBonus;

        if (temporaryMaxHealthBonus > 0)
        {
            playerStats.maxHealth -= temporaryMaxHealthBonus;

            if (playerHealth != null)
            {
                playerHealth.currentHealth = Mathf.Min(playerHealth.currentHealth, playerStats.maxHealth);
            }
        }

        if (baseCore != null && temporaryBaseShieldBonus > 0)
        {
            baseCore.RemoveShield(temporaryBaseShieldBonus);
        }

        temporaryBasePulseDamage = 0;

        playerStats.critChance = Mathf.Max(0f, playerStats.critChance);
        playerStats.dodgeChance = Mathf.Max(0f, playerStats.dodgeChance);
        playerStats.armor = Mathf.Max(0, playerStats.armor);
        playerStats.projectileCountBonus = Mathf.Max(0, playerStats.projectileCountBonus);

        temporaryMoveSpeedBonus = 0f;
        temporaryDamageBonus = 0f;
        temporaryAttackSpeedBonus = 0f;
        temporaryCritChanceBonus = 0f;
        temporaryDodgeChanceBonus = 0f;
        temporaryMeleeRangeBonus = 0f;
        temporaryProjectileSpeedBonus = 0f;
        temporaryHealthRegenBonus = 0f;

        temporaryArmorBonus = 0;
        temporaryMaxHealthBonus = 0;
        temporaryProjectileCountBonus = 0;
        temporaryBaseShieldBonus = 0;

        Debug.Log("Los buffs nocturnos temporales han terminado.");
    }

    string GetResourceDebugText()
    {
        if (playerResources == null) return "sin PlayerResources";

        return "Madera: " + playerResources.wood +
               " | Piedra: " + playerResources.stone;
    }
}

public enum NightEffectType
{
    Damage,
    AttackSpeed,
    CritChance,
    DodgeChance,
    MeleeRange,
    ProjectileSpeed,
    ProjectileCount,
    MoveSpeedPercent,
    MoveSpeedFlat,
    HealthRegen,
    Armor,
    MaxHealth,
    BaseMaxHealth,
    BaseRepair,
    BaseShield,
    BasePulseDamage
}

[System.Serializable]
public class NightEffect
{
    public NightEffectType type;
    public float value;
    public bool isPermanent;

    public NightEffect(NightEffectType type, float value, bool isPermanent)
    {
        this.type = type;
        this.value = value;
        this.isPermanent = isPermanent;
    }

    public static NightEffect Night(NightEffectType type, float value)
    {
        return new NightEffect(type, value, false);
    }

    public static NightEffect Permanent(NightEffectType type, float value)
    {
        return new NightEffect(type, value, true);
    }
}

public class NightPreparationOption
{
    public string id;
    public string title;
    public string description;

    public int woodCost;
    public int stoneCost;
    public int coinCost;

    public List<NightEffect> effects;

    public NightPreparationOption(
        string id,
        string title,
        string description,
        int woodCost,
        int stoneCost,
        int coinCost,
        List<NightEffect> effects
    )
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.woodCost = woodCost;
        this.stoneCost = stoneCost;
        this.coinCost = coinCost;
        this.effects = effects;
    }

    public string GetCostText()
    {
        string text = "";

        if (woodCost > 0) text += woodCost + " madera ";
        if (stoneCost > 0) text += stoneCost + " piedra ";
        if (coinCost > 0) text += coinCost + " oro ";

        if (text == "") text = "gratis";

        return text;
    }
}