using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class EnemyHealth : MonoBehaviour
{
    [Header("Muerte")]
    public bool destroyAutomaticallyOnDeath = true;

    [Header("Vida")]
    public int maxHealth = 4;
    public int currentHealth;

    [Header("Escalado opcional")]
    public bool scaleHealthWithDifficulty = false;

    [Range(0f, 1f)]
    public float healthScalingStrength = 0.15f;

    [Header("Drops XP")]
    public int xpValue = 1;

    public GameObject blueXpOrbPrefab;
    public int blueXpOrbValue = 1;

    public GameObject purpleXpOrbPrefab;
    public int purpleXpOrbValue = 3;

    public float xpDropSpread = 0.5f;

    [Header("Drops")]
    public GameObject coinPrefab;

    [Range(0f, 1f)]
    public float coinDropChance = 0.1f;

    public int coinDropCount = 1;

    [Header("Feedback")]
    public GameObject damagePopupPrefab;
    public Vector3 popupOffset = Vector3.zero;

    private bool isDead = false;
    private EnemyFlash enemyFlash;

    public event Action OnDeath;

    void Awake()
    {
        enemyFlash = GetComponent<EnemyFlash>();
    }

    void Start()
    {
        currentHealth = CalculateStartingHealth();
    }

    int CalculateStartingHealth()
    {
        float finalMultiplier = 1f;

        if (scaleHealthWithDifficulty && GameManager.Instance != null)
        {
            float difficulty = GameManager.Instance.difficultyMultiplier;
            finalMultiplier = 1f + ((difficulty - 1f) * healthScalingStrength);
        }

        return Mathf.Max(1, Mathf.RoundToInt(maxHealth * finalMultiplier));
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, false);
    }

    public void TakeDamage(int damage, bool isCrit)
    {
        DamagePopupType popupType = isCrit ? DamagePopupType.Crit : DamagePopupType.Normal;

        TakeDamage(damage, popupType);
    }

    public void TakeDamage(int damage, DamagePopupType popupType)
    {
        if (isDead) return;

        damage = Mathf.Max(0, damage);

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        ShowDamagePopup(damage, popupType);

        if (enemyFlash != null)
        {
            enemyFlash.Flash();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void ShowDamagePopup(int damage, DamagePopupType popupType)
    {
        if (damagePopupPrefab == null) return;

        Vector3 randomOffset = new Vector3(
            Random.Range(-0.05f, 0.05f),
            Random.Range(0f, 0.05f),
            0f
        );

        Vector3 spawnPosition = transform.position + Vector3.up * 0.4f + popupOffset + randomOffset;

        GameObject popupObject = Instantiate(
            damagePopupPrefab,
            spawnPosition,
            Quaternion.identity
        );

        DamagePopup popup = popupObject.GetComponent<DamagePopup>();

        if (popup != null)
        {
            popup.Setup(damage, popupType);
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        OnDeath?.Invoke();

        DropXP();
        DropCoins();

        if (destroyAutomaticallyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    void DropXP()
    {
        if (xpValue <= 0) return;

        int remainingXP = xpValue;

        while (purpleXpOrbPrefab != null &&
            purpleXpOrbValue > 0 &&
            remainingXP >= purpleXpOrbValue)
        {
            SpawnXPOrb(purpleXpOrbPrefab, purpleXpOrbValue);
            remainingXP -= purpleXpOrbValue;
        }

        while (blueXpOrbPrefab != null &&
            blueXpOrbValue > 0 &&
            remainingXP > 0)
        {
            int valueToDrop = Mathf.Min(blueXpOrbValue, remainingXP);
            SpawnXPOrb(blueXpOrbPrefab, valueToDrop);
            remainingXP -= valueToDrop;
        }
    }

    void SpawnXPOrb(GameObject prefab, int value)
    {
        Vector2 offset = Random.insideUnitCircle * xpDropSpread;

        GameObject orbObject = Instantiate(
            prefab,
            transform.position + (Vector3)offset,
            Quaternion.identity
        );

        XPOrb orb = orbObject.GetComponent<XPOrb>();

        if (orb != null)
        {
            orb.xpValue = value;
        }
    }

    void DropCoins()
    {
        if (coinPrefab == null) return;
        if (Random.value > coinDropChance) return;

        for (int i = 0; i < coinDropCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.5f;
            Instantiate(coinPrefab, transform.position + (Vector3)offset, Quaternion.identity);
        }
    }
}