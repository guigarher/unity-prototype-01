using UnityEngine;
using System;

public class BaseCore : MonoBehaviour
{
    public static BaseCore Instance;
    [Header("Escudo temporal")]
    public int currentShield = 0;
    private DamageFlash damageFlash;

    [Header("Reglas de daño")]
    public bool onlyTakeDamageAtNight = true;

    [Header("Detección para enemigos")]
    public float enemyAttackRadius = 2.5f;

    [Header("Vida de la base")]
    public int maxHealth = 300;
    public int currentHealth;

    [Header("Debug")]
    public bool logDamage = true;

    public static event Action<int, int> OnBaseHealthChanged;
    public static event Action OnBaseDestroyed;

    private bool isDestroyed = false;

    void Awake()
    {
        Instance = this;

        damageFlash = GetComponent<DamageFlash>();

        currentHealth = maxHealth;

        NotifyHealthChanged();
    }

    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        if (onlyTakeDamageAtNight &&
            GamePhaseManager.Instance != null &&
            !GamePhaseManager.Instance.IsNight())
        {
            return;
        }

        int remainingDamage = damage;

        if (currentShield > 0)
        {
            int absorbed = Mathf.Min(currentShield, remainingDamage);
            currentShield -= absorbed;
            remainingDamage -= absorbed;

            if (logDamage)
            {
                Debug.Log("Escudo de base absorbió " + absorbed + ". Escudo restante: " + currentShield);
            }
        }

        if (remainingDamage <= 0)
        {
            NotifyHealthChanged();
            return;
        }

        currentHealth -= remainingDamage;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (damageFlash != null)
        {
            damageFlash.PlayFlash();
        }

        if (logDamage)
        {
            Debug.Log("Base recibió " + remainingDamage + " daño. Vida: " + currentHealth + "/" + maxHealth);
        }

        NotifyHealthChanged();

        if (currentHealth <= 0)
        {
            DestroyBase();
        }
    }

    public void Repair(int amount)
    {
        if (isDestroyed) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        Debug.Log("Base reparada +" + amount + ". Vida: " + currentHealth + "/" + maxHealth);

        NotifyHealthChanged();
    }

    public void AddShield(int amount)
    {
        if (isDestroyed) return;

        currentShield += amount;

        Debug.Log("La base gana +" + amount + " de escudo. Escudo actual: " + currentShield);

        NotifyHealthChanged();
    }

    public void RemoveShield(int amount)
    {
        if (amount <= 0) return;

        currentShield -= amount;
        currentShield = Mathf.Max(currentShield, 0);

        Debug.Log("Escudo temporal eliminado. Escudo actual: " + currentShield);

        NotifyHealthChanged();
    }

    public void IncreaseMaxHealth(int amount, bool alsoHeal = true)
    {
        if (isDestroyed) return;

        maxHealth += amount;

        if (alsoHeal)
        {
            currentHealth += amount;
        }

        currentHealth = Mathf.Min(currentHealth, maxHealth);

        Debug.Log("Vida máxima de la base aumentada +" + amount + ". Vida: " + currentHealth + "/" + maxHealth);

        NotifyHealthChanged();
    }

    void DestroyBase()
    {
        if (isDestroyed) return;

        isDestroyed = true;

        Debug.Log("La base ha sido destruida. Game Over.");

        OnBaseDestroyed?.Invoke();

        Time.timeScale = 0f;
    }

    void NotifyHealthChanged()
    {
        OnBaseHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}