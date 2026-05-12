using UnityEngine;
using System;

public class BaseCore : MonoBehaviour
{
    public static BaseCore Instance;

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

        // Si está activado, la base solo puede recibir daño durante la noche.
        if (onlyTakeDamageAtNight &&
            GamePhaseManager.Instance != null &&
            !GamePhaseManager.Instance.IsNight())
        {
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (damageFlash != null)
        {
            damageFlash.PlayFlash();
        }

        if (logDamage)
        {
            Debug.Log("Base recibió " + damage + " daño. Vida: " + currentHealth + "/" + maxHealth);
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