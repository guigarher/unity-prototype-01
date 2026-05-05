using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 4;
    public int currentHealth;

    [Header("Escalado opcional")]
    public bool scaleHealthWithDifficulty = false;

    [Range(0f, 1f)]
    public float healthScalingStrength = 0.15f;

    [Header("Drops")]
    public GameObject xpOrbPrefab;
    public int xpOrbCount = 1;

    public GameObject coinPrefab;

    [Range(0f, 1f)]
    public float coinDropChance = 0.1f;

    public int coinDropCount = 1;

    [Header("Feedback")]
    public GameObject damagePopupPrefab;
    public Vector3 popupOffset = Vector3.zero;

    private bool isDead = false;
    private EnemyFlash enemyFlash;

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
        if (isDead) return;

        damage = Mathf.Max(0, damage);

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        ShowDamagePopup(damage, isCrit);

        if (enemyFlash != null)
        {
            enemyFlash.Flash();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void ShowDamagePopup(int damage, bool isCrit)
    {
        if (damagePopupPrefab == null) return;

        Vector3 randomOffset = new Vector3(
            Random.Range(-0.05f, 0.05f),
            Random.Range(0f, 0.05f),
            0f
        );

        Vector3 spawnPosition = transform.position + Vector3.up * 0.4f + randomOffset;

        GameObject popupObject = Instantiate(
            damagePopupPrefab,
            spawnPosition,
            Quaternion.identity
        );

        DamagePopup popup = popupObject.GetComponent<DamagePopup>();

        if (popup != null)
        {
            popup.Setup(damage, isCrit);
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        DropXP();
        DropCoins();

        Destroy(gameObject);
    }

    void DropXP()
    {
        if (xpOrbPrefab == null) return;

        for (int i = 0; i < xpOrbCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.5f;
            Instantiate(xpOrbPrefab, transform.position + (Vector3)offset, Quaternion.identity);
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