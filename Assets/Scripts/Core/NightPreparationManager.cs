using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class NightPreparationManager : MonoBehaviour
{
    public static NightPreparationManager Instance;

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
    public int baseRerollCost = 10;
    public int rerollCostIncrease = 5;

    [Header("Pulso defensivo de base")]
    public float basePulseRadius = 5f;
    public float basePulseInterval = 3f;

    private int currentRerollCost;
    private bool isPreparationOpen = false;
    private bool pendingNightPreparation = false;

    private List<NightPreparationOption> currentOptions = new List<NightPreparationOption>();

    private float temporaryMoveSpeedBonus = 0f;
    private float temporaryDamageBonus = 0f;
    private float temporaryAttackSpeedBonus = 0f;
    private float temporaryCritChanceBonus = 0f;
    private float temporaryCritMultiplierBonus = 0f;
    private float temporaryDodgeChanceBonus = 0f;
    private float temporaryAreaRangeBonus = 0f;
    private float temporaryProjectileSpeedBonus = 0f;
    private float temporaryHealthRegenBonus = 0f;
    private float temporaryDamageReductionBonus = 0f;

    private int temporaryMaxHealthBonus = 0;
    private int temporaryProjectileCountBonus = 0;

    private int temporaryBaseShieldBonus = 0;
    private int temporaryBasePulseDamage = 0;
    private Coroutine basePulseRoutine;

    void Awake()
    {
        Instance = this;
    }

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
            RequestNightPreparation();
        }
        else if (phase == GamePhase.Day)
        {
            pendingNightPreparation = false;
            RemoveTemporaryNightBuffs();
        }
    }

    void RequestNightPreparation()
    {
        pendingNightPreparation = true;
        TryOpenPendingNightPreparation();
    }

    public bool TryOpenPendingNightPreparation()
    {
        if (!pendingNightPreparation) return false;
        if (isPreparationOpen) return true;

        if (!CanOpenNightPreparationNow())
        {
            return false;
        }

        pendingNightPreparation = false;
        OpenNightPreparation();

        return true;
    }

    bool CanOpenNightPreparationNow()
    {
        if (LevelUpManager.Instance != null && LevelUpManager.Instance.IsRewardMenuBusy())
        {
            return false;
        }

        return true;
    }

    public bool IsPreparationOpen()
    {
        return isPreparationOpen;
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
    }

    NightPreparationOption CreateRandomWoodOption()
    {
        int roll = Random.Range(0, 6);

        switch (roll)
        {
            case 0:
                return new NightPreparationOption(
                    "wood_damage",
                    "Combustión agresiva",
                    "NOCHE: +40% daño general.\nPERMANENTE: +4% daño general.",
                    10,
                    0,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.Damage, 0.40f),
                        NightEffect.Permanent(NightEffectType.Damage, 0.04f)
                    }
                );

            case 1:
                return new NightPreparationOption(
                    "wood_attack_speed",
                    "Vapor a presión",
                    "NOCHE: +40% velocidad de ataque.\nPERMANENTE: +4% velocidad de ataque.",
                    10,
                    0,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.AttackSpeed, 0.40f),
                        NightEffect.Permanent(NightEffectType.AttackSpeed, 0.04f)
                    }
                );

            case 2:
                return new NightPreparationOption(
                    "wood_crit_chance",
                    "Mira de latón",
                    "NOCHE: +40% probabilidad crítica.\nPERMANENTE: +4% probabilidad crítica.",
                    10,
                    0,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.CritChance, 0.40f),
                        NightEffect.Permanent(NightEffectType.CritChance, 0.04f)
                    }
                );

            case 3:
                return new NightPreparationOption(
                    "wood_crit_damage",
                    "Engranajes afilados",
                    "NOCHE: +40% daño crítico.\nPERMANENTE: +4% daño crítico.",
                    10,
                    0,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.CritMultiplier, 0.40f),
                        NightEffect.Permanent(NightEffectType.CritMultiplier, 0.04f)
                    }
                );

            case 4:
                return new NightPreparationOption(
                    "wood_area",
                    "Guadaña expansiva",
                    "NOCHE: +40% radio de área.\nPERMANENTE: +4% radio de área.",
                    10,
                    0,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.AreaRange, 0.40f),
                        NightEffect.Permanent(NightEffectType.AreaRange, 0.04f)
                    }
                );

            default:
                return new NightPreparationOption(
                    "wood_projectile",
                    "Munición de vapor",
                    "NOCHE: +1 proyectil extra.\nPERMANENTE: +8% velocidad de proyectil.",
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
        int roll = Random.Range(0, 3);

        switch (roll)
        {
            case 0:
                return new NightPreparationOption(
                    "stone_dodge",
                    "Reflejos minerales",
                    "NOCHE: +20% esquiva.\nPERMANENTE: +2% esquiva.",
                    0,
                    10,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.DodgeChance, 0.20f),
                        NightEffect.Permanent(NightEffectType.DodgeChance, 0.02f)
                    }
                );

            case 1:
                return new NightPreparationOption(
                    "stone_damage_reduction",
                    "Blindaje temporal",
                    "NOCHE: -40% daño recibido.\nPERMANENTE: +4% vida máxima.",
                    0,
                    10,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.DamageReduction, 0.40f),
                        NightEffect.Permanent(NightEffectType.MaxHealthPercent, 0.04f)
                    }
                );

            default:
                return new NightPreparationOption(
                    "stone_health",
                    "Corazón reforzado",
                    "NOCHE: +40% vida máxima.\nPERMANENTE: +4% vida máxima.",
                    0,
                    10,
                    0,
                    new List<NightEffect>
                    {
                        NightEffect.Permanent(NightEffectType.MaxHealthPercent, 0.04f),
                        NightEffect.Night(NightEffectType.MaxHealthPercent, 0.40f)
                    }
                );
        }
    }

    NightPreparationOption CreateRandomMixedOption()
    {
        List<NightEffect> effects = new List<NightEffect>();

        string title = "Motor de vapor inestable";
        string description =
            "NOCHE: +30% velocidad de movimiento.\n" +
            "PERMANENTE: +3% velocidad de movimiento.";

        effects.Add(NightEffect.Permanent(NightEffectType.MoveSpeedPercent, 0.03f));
        effects.Add(NightEffect.Night(NightEffectType.MoveSpeedPercent, 0.30f));

        NightEffect offensiveNight;
        NightEffect offensivePermanent;
        string offensiveText;

        CreateReducedOffensiveEffect(
            out offensiveNight,
            out offensivePermanent,
            out offensiveText
        );

        NightEffect defensiveNight;
        NightEffect defensivePermanent;
        string defensiveText;

        CreateReducedDefensiveEffect(
            out defensiveNight,
            out defensivePermanent,
            out defensiveText
        );

        effects.Add(offensivePermanent);
        effects.Add(offensiveNight);
        effects.Add(defensivePermanent);
        effects.Add(defensiveNight);

        description += "\n" + offensiveText;
        description += "\n" + defensiveText;

        return new NightPreparationOption(
            "mixed_generated",
            title,
            description,
            15,
            15,
            0,
            effects
        );
    }

    void CreateReducedOffensiveEffect(
        out NightEffect night,
        out NightEffect permanent,
        out string text
    )
    {
        int roll = Random.Range(0, 5);

        switch (roll)
        {
            case 0:
                night = NightEffect.Night(NightEffectType.Damage, 0.20f);
                permanent = NightEffect.Permanent(NightEffectType.Damage, 0.02f);
                text = "EXTRA OFENSIVO: +20% daño esta noche y +2% daño permanente.";
                return;

            case 1:
                night = NightEffect.Night(NightEffectType.AttackSpeed, 0.20f);
                permanent = NightEffect.Permanent(NightEffectType.AttackSpeed, 0.02f);
                text = "EXTRA OFENSIVO: +20% velocidad de ataque esta noche y +2% permanente.";
                return;

            case 2:
                night = NightEffect.Night(NightEffectType.CritChance, 0.20f);
                permanent = NightEffect.Permanent(NightEffectType.CritChance, 0.02f);
                text = "EXTRA OFENSIVO: +20% probabilidad crítica esta noche y +2% permanente.";
                return;

            case 3:
                night = NightEffect.Night(NightEffectType.CritMultiplier, 0.20f);
                permanent = NightEffect.Permanent(NightEffectType.CritMultiplier, 0.02f);
                text = "EXTRA OFENSIVO: +20% daño crítico esta noche y +2% permanente.";
                return;

            default:
                night = NightEffect.Night(NightEffectType.AreaRange, 0.20f);
                permanent = NightEffect.Permanent(NightEffectType.AreaRange, 0.02f);
                text = "EXTRA OFENSIVO: +20% radio de área esta noche y +2% permanente.";
                return;
        }
    }

    void CreateReducedDefensiveEffect(
        out NightEffect night,
        out NightEffect permanent,
        out string text
    )
    {
        int roll = Random.Range(0, 3);

        switch (roll)
        {
            case 0:
                night = NightEffect.Night(NightEffectType.DodgeChance, 0.10f);
                permanent = NightEffect.Permanent(NightEffectType.DodgeChance, 0.01f);
                text = "EXTRA DEFENSIVO: +10% esquiva esta noche y +1% esquiva permanente.";
                return;

            case 1:
                night = NightEffect.Night(NightEffectType.DamageReduction, 0.20f);
                permanent = NightEffect.Permanent(NightEffectType.MaxHealthPercent, 0.02f);
                text = "EXTRA DEFENSIVO: -20% daño recibido esta noche y +2% vida máxima permanente.";
                return;

            default:
                night = NightEffect.Night(NightEffectType.MaxHealthPercent, 0.20f);
                permanent = NightEffect.Permanent(NightEffectType.MaxHealthPercent, 0.02f);
                text = "EXTRA DEFENSIVO: +20% vida máxima esta noche y +2% vida máxima permanente.";
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
                    "NOCHE: la base gana +80 de escudo.\nPERMANENTE: +25 vida máxima de base.",
                    6,
                    8,
                    3,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.BaseShield, 80f),
                        NightEffect.Permanent(NightEffectType.BaseMaxHealth, 25f)
                    }
                );

            case 1:
                return new NightPreparationOption(
                    "base_pulse",
                    "Pulso de caldera",
                    "NOCHE: la base hace 6 de daño en área cada pocos segundos.\nPERMANENTE: +20 vida máxima de base.",
                    8,
                    8,
                    5,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.BasePulseDamage, 6f),
                        NightEffect.Permanent(NightEffectType.BaseMaxHealth, 20f)
                    }
                );

            case 2:
                return new NightPreparationOption(
                    "base_big_repair",
                    "Reparación mayor",
                    "NOCHE: repara +100 vida de base.\nPERMANENTE: +15 vida máxima de base.",
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
                    "NOCHE: +50 escudo de base.\nPERMANENTE: +50 vida máxima de base.",
                    2,
                    10,
                    4,
                    new List<NightEffect>
                    {
                        NightEffect.Night(NightEffectType.BaseShield, 50f),
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

            string purchasedText = option.purchased ? " [COMPRADA]" : "";

            string descriptionForConsole = option.description.Replace("\n", " | ");

            Debug.Log(
                (i + 1) + ": " + option.title + purchasedText +
                " | Coste: " + option.GetCostText() +
                " | " + descriptionForConsole
            );
        }

        Debug.Log("F: Reroll opciones | Coste actual: " + currentRerollCost + " oro");
        Debug.Log("ENTER: Empezar la noche");
    }

    void TryBuyOption(int index)
    {
        if (index < 0 || index >= currentOptions.Count) return;
        if (playerResources == null) return;

        NightPreparationOption option = currentOptions[index];

        if (option.purchased)
        {
            Debug.Log("Ya compraste esta mejora: " + option.title);
            return;
        }

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

        if (!playerResources.SpendResources(option.woodCost, option.stoneCost))
        {
            Debug.Log("No se pudieron gastar los recursos de: " + option.title);
            return;
        }

        ApplyNightPreparationOption(option);

        Debug.Log("Comprado: " + option.title);

        option.purchased = true;

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

            case NightEffectType.CritMultiplier:
                playerStats.critMultiplier += effect.value;
                if (!effect.isPermanent) temporaryCritMultiplierBonus += effect.value;
                break;

            case NightEffectType.DodgeChance:
                playerStats.dodgeChance = Mathf.Min(0.75f, playerStats.dodgeChance + effect.value);
                if (!effect.isPermanent) temporaryDodgeChanceBonus += effect.value;
                break;

            case NightEffectType.AreaRange:
            case NightEffectType.MeleeRange:
                playerStats.areaRangeBonus += effect.value;
                if (!effect.isPermanent) temporaryAreaRangeBonus += effect.value;
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

            case NightEffectType.HealthRegenPercent:
                float regenBonus = playerStats.maxHealth * effect.value;
                playerStats.healthRegen += regenBonus;
                if (!effect.isPermanent) temporaryHealthRegenBonus += regenBonus;
                break;

            case NightEffectType.DamageReduction:
                if (playerHealth != null)
                {
                    playerHealth.damageReduction = Mathf.Clamp(playerHealth.damageReduction + effect.value, 0f, 0.90f);
                    if (!effect.isPermanent) temporaryDamageReductionBonus += effect.value;
                }
                break;

            case NightEffectType.Armor:
                int armorBonus = Mathf.RoundToInt(effect.value);
                playerStats.armor += armorBonus;
                break;

            case NightEffectType.MaxHealth:
                int flatHealthBonus = Mathf.RoundToInt(effect.value);
                ApplyMaxHealthBonus(flatHealthBonus, effect.isPermanent);
                break;

            case NightEffectType.MaxHealthPercent:
                int percentHealthBonus = Mathf.Max(1, Mathf.RoundToInt(playerStats.maxHealth * effect.value));
                ApplyMaxHealthBonus(percentHealthBonus, effect.isPermanent);
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

    void ApplyMaxHealthBonus(int healthBonus, bool isPermanent)
    {
        if (healthBonus <= 0) return;

        playerStats.maxHealth += healthBonus;

        if (playerHealth != null)
        {
            playerHealth.currentHealth += healthBonus;
            playerHealth.currentHealth = Mathf.Min(playerHealth.currentHealth, playerStats.maxHealth);
        }

        if (!isPermanent)
        {
            temporaryMaxHealthBonus += healthBonus;
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
                    EnemyHealth enemyHealth = enemy.GetComponentInParent<EnemyHealth>();

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

        if (repairAmount <= 0)
        {
            Debug.Log("La reparación no tiene cantidad válida.");
            return;
        }

        if (baseCore.currentHealth >= baseCore.maxHealth)
        {
            Debug.Log("La base ya está al máximo de vida. No se gastan recursos.");
            return;
        }

        if (!playerResources.CanSpendResources(repairWoodCost, repairStoneCost))
        {
            Debug.Log("No tienes recursos suficientes para reparar.");
            return;
        }

        if (!playerResources.SpendResources(repairWoodCost, repairStoneCost))
        {
            Debug.Log("No se pudieron gastar los recursos de reparación.");
            return;
        }

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

        for (int i = 0; i < currentOptions.Count; i++)
        {
            if (currentOptions[i].purchased)
            {
                continue;
            }

            if (i == 0)
            {
                currentOptions[i] = CreateRandomWoodOption();
            }
            else if (i == 1)
            {
                currentOptions[i] = CreateRandomStoneOption();
            }
            else if (i == 2)
            {
                currentOptions[i] = CreateRandomMixedOption();
            }
        }

        currentRerollCost += rerollCostIncrease;

        Debug.Log("Reroll nocturno realizado. Las compras ya realizadas no han cambiado. Nuevo coste: " + currentRerollCost);
        ShowNightPreparationInConsole();
    }

    void StartNight()
    {
        pendingNightPreparation = false;
        isPreparationOpen = false;
        Time.timeScale = 1f;

        Debug.Log("La noche comienza.");
    }

    void RemoveTemporaryNightBuffs()
    {
        FindReferencesIfNeeded();

        if (playerStats == null) return;

        playerStats.moveSpeed -= temporaryMoveSpeedBonus;
        playerStats.damageMultiplier -= temporaryDamageBonus;
        playerStats.attackSpeedMultiplier -= temporaryAttackSpeedBonus;
        playerStats.critChance -= temporaryCritChanceBonus;
        playerStats.critMultiplier -= temporaryCritMultiplierBonus;
        playerStats.dodgeChance -= temporaryDodgeChanceBonus;
        playerStats.areaRangeBonus -= temporaryAreaRangeBonus;
        playerStats.projectileSpeedMultiplier -= temporaryProjectileSpeedBonus;
        playerStats.healthRegen -= temporaryHealthRegenBonus;
        playerStats.healthRegen = Mathf.Max(0f, playerStats.healthRegen);
        playerStats.projectileCountBonus -= temporaryProjectileCountBonus;

        if (playerHealth != null)
        {
            playerHealth.damageReduction = Mathf.Max(0f, playerHealth.damageReduction - temporaryDamageReductionBonus);
        }

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
        playerStats.projectileCountBonus = Mathf.Max(0, playerStats.projectileCountBonus);

        temporaryMoveSpeedBonus = 0f;
        temporaryDamageBonus = 0f;
        temporaryAttackSpeedBonus = 0f;
        temporaryCritChanceBonus = 0f;
        temporaryCritMultiplierBonus = 0f;
        temporaryDodgeChanceBonus = 0f;
        temporaryAreaRangeBonus = 0f;
        temporaryProjectileSpeedBonus = 0f;
        temporaryHealthRegenBonus = 0f;
        temporaryDamageReductionBonus = 0f;

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
    CritMultiplier,
    DodgeChance,
    AreaRange,
    MeleeRange,
    ProjectileSpeed,
    ProjectileCount,
    MoveSpeedPercent,
    MoveSpeedFlat,
    HealthRegen,
    HealthRegenPercent,
    DamageReduction,
    Armor,
    MaxHealth,
    MaxHealthPercent,
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

    public bool purchased = false;

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
