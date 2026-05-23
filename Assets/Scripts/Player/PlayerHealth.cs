using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int currentHealth;

    private bool canTakeDamage = true;
    public float damageCooldown = 0.2f;
    private bool isDead = false;

    private PlayerStats stats;
    private DamageFlash damageFlash;

    [Header("Game Over")]
    public float gameOverPauseDelay = 1f;

    [Header("Popup de esquiva")]
    public GameObject dodgePopupPrefab;
    public Vector3 dodgePopupOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Popup de daño recibido")]
    public GameObject damageTakenPopupPrefab;
    public Vector3 damageTakenPopupOffset = new Vector3(0f, 0.85f, 0f);

    private float regenAccumulator = 0f;

    void Awake()
    {
        damageFlash = GetComponent<DamageFlash>();
    }

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
                ShowDodgePopup();

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
        ShowDamageTakenPopup(damage);

        if (damageFlash != null)
        {
            damageFlash.PlayFlash();
        }

        Debug.Log("El jugador recibió daño. Vida actual: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(DamageCooldown());
    }

    void ShowDodgePopup()
    {
        if (dodgePopupPrefab == null) return;

        Vector3 randomOffset = new Vector3(
            Random.Range(-0.25f, 0.25f),
            Random.Range(0f, 0.25f),
            0f
        );

        GameObject popupObject = Instantiate(
            dodgePopupPrefab,
            transform.position + dodgePopupOffset + randomOffset,
            Quaternion.identity
        );

        DamagePopup popup = popupObject.GetComponent<DamagePopup>();

        if (popup != null)
        {
            popup.SetupText(
                "¡ESQUIVADO!",
                new Color(0.5f, 0.9f, 1f, 1f),
                2.6f
            );
        }
    }

    void ShowDamageTakenPopup(int damage)
    {
        if (damageTakenPopupPrefab == null) return;

        Vector3 randomOffset = new Vector3(
            Random.Range(-0.15f, 0.15f),
            Random.Range(0f, 0.15f),
            0f
        );

        GameObject popupObject = Instantiate(
            damageTakenPopupPrefab,
            transform.position + damageTakenPopupOffset + randomOffset,
            Quaternion.identity
        );

        DamagePopup popup = popupObject.GetComponent<DamagePopup>();

        if (popup != null)
        {
            popup.SetupText(
                "-" + damage,
                new Color(1f, 0.35f, 0.35f, 1f),
                2.8f
            );
        }
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

        if (ScreenAnnouncementManager.Instance != null)
        {
            ScreenAnnouncementManager.Instance.ShowPersistent(
                "TE HAN DERROTADO",
                "La noche se ha cobrado su precio."
            );
        }

        StartCoroutine(PauseGameAfterDelay());
    }

    IEnumerator PauseGameAfterDelay()
    {
        yield return new WaitForSecondsRealtime(gameOverPauseDelay);
        Time.timeScale = 0f;
    }

    IEnumerator DamageCooldown()
    {
        canTakeDamage = false;
        yield return new WaitForSecondsRealtime(damageCooldown);
        canTakeDamage = true;
    }
}