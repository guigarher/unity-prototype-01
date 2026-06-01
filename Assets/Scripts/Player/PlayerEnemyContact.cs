using System.Collections.Generic;
using UnityEngine;

public class PlayerEnemyContact : MonoBehaviour
{
    [Header("Detección")]
    public LayerMask enemyLayer;
    public float contactRadius = 0.55f;

    [Header("Daño por contacto")]
    public int damagePerEnemy = 1;
    public float damageInterval = 1f;

    [Header("Ralentización")]
    [Range(0.1f, 1f)]
    public float oneEnemySlowMultiplier = 0.75f;

    [Range(0.1f, 1f)]
    public float manyEnemiesSlowMultiplier = 0.55f;

    public int manyEnemiesThreshold = 3;

    [Header("Popup opcional")]
    public GameObject damagePopupPrefab;
    public Vector3 popupOffset = new Vector3(0f, 0.6f, 0f);

    [Header("Debug")]
    public bool logContactDamage = false;
    public bool drawDebugGizmo = true;

    private PlayerHealth playerHealth;
    private PlayerStats playerStats;

    private float damageTimer = 0f;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerStats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        int touchingEnemies = CountTouchingEnemies();

        UpdateSlow(touchingEnemies);
        UpdateContactDamage(touchingEnemies);
    }

    int CountTouchingEnemies()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            contactRadius,
            enemyLayer
        );

        HashSet<EnemyHealth> uniqueEnemies = new HashSet<EnemyHealth>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                uniqueEnemies.Add(enemyHealth);
            }
        }

        return uniqueEnemies.Count;
    }

    void UpdateSlow(int touchingEnemies)
    {
        if (playerStats == null) return;

        if (touchingEnemies <= 0)
        {
            playerStats.contactSlowMultiplier = 1f;
            return;
        }

        if (touchingEnemies >= manyEnemiesThreshold)
        {
            playerStats.contactSlowMultiplier = manyEnemiesSlowMultiplier;
        }
        else
        {
            playerStats.contactSlowMultiplier = oneEnemySlowMultiplier;
        }
    }

    void UpdateContactDamage(int touchingEnemies)
    {
        if (touchingEnemies <= 0)
        {
            damageTimer = 0f;
            return;
        }

        damageTimer -= Time.deltaTime;

        if (damageTimer > 0f) return;

        damageTimer = damageInterval;

        int totalDamage = damagePerEnemy * touchingEnemies;

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(totalDamage);
        }

        ShowDamagePopup(totalDamage);

        if (logContactDamage)
        {
            Debug.Log(
                "CONTACTO ENEMIGO | enemigos=" + touchingEnemies +
                " | daño=" + totalDamage +
                " | slow=" + (playerStats != null ? playerStats.contactSlowMultiplier : -1f)
            );
        }
    }

    void ShowDamagePopup(int damage)
    {
        if (damagePopupPrefab == null) return;

        GameObject popupObject = Instantiate(
            damagePopupPrefab,
            transform.position + popupOffset,
            Quaternion.identity
        );

        DamagePopup popup = popupObject.GetComponent<DamagePopup>();

        if (popup != null)
        {
            popup.Setup(damage, false);
        }
    }

    void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.contactSlowMultiplier = 1f;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmo) return;

        Gizmos.DrawWireSphere(transform.position, contactRadius);
    }
}