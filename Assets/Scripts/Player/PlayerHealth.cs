using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int currentHealth;

    private bool canTakeDamage = true;
    public float damageCooldown = 0.2f;
    private bool isDead = false;

    private PlayerStats stats;

    private float regenAccumulator = 0f;

    void Start()
    {
        stats = GetComponent<PlayerStats>();

        if (stats != null)
        {
            currentHealth = stats.maxHealth;
        }
        else
        {
            currentHealth = 5;
        }

        Debug.Log("Vida inicial: " + currentHealth);
    }

    void Update()
    {
        RegenerateHealth();
    }

    void RegenerateHealth()
    {
        if (isDead) return;
        if (stats == null) return;
        if (stats.healthRegen <= 0f) return;
        if (currentHealth >= stats.maxHealth) return;

        regenAccumulator += stats.healthRegen * Time.deltaTime;

        if (regenAccumulator >= 1f)
        {
            int healAmount = Mathf.FloorToInt(regenAccumulator);
            currentHealth = Mathf.Min(currentHealth + healAmount, stats.maxHealth);
            regenAccumulator -= healAmount;

            Debug.Log("Regeneras vida. Vida actual: " + currentHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        if (!canTakeDamage || isDead) return;

        if (stats != null)
        {
            if (Random.value < stats.dodgeChance)
            {
                Debug.Log("¡Esquiva!");
                StartCoroutine(DamageCooldown());
                return;
            }

            damage = Mathf.Max(1, damage - stats.armor);
        }

        currentHealth -= damage;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        Debug.Log("El jugador recibió daño. Vida actual: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(DamageCooldown());
    }

    void Die()
    {
        isDead = true;
        Debug.Log("El jugador ha muerto");

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        BasicRangedWeapon rangedWeapon = GetComponent<BasicRangedWeapon>();
        if (rangedWeapon != null)
        {
            rangedWeapon.enabled = false;
        }

        BasicMeleeWeapon meleeWeapon = GetComponent<BasicMeleeWeapon>();
        if (meleeWeapon != null)
        {
            meleeWeapon.enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Time.timeScale = 0f;
    }

    IEnumerator DamageCooldown()
    {
        canTakeDamage = false;
        yield return new WaitForSecondsRealtime(damageCooldown);
        canTakeDamage = true;
    }
}