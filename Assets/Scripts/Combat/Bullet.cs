using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Movimiento")]
    public float baseSpeed = 8f;
    public float lifeTime = 3f;

    private float finalSpeed;
    private int damage = 1;

    private float critChance = 0f;
    private float critMultiplier = 2f;

    private Vector2 direction = Vector2.right;
    private bool initialized = false;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(
        Vector2 newDirection,
        int newDamage,
        float newCritChance,
        float newCritMultiplier,
        float speedMultiplier
    )
    {
        direction = newDirection.normalized;

        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        damage = Mathf.Max(0, newDamage);
        critChance = Mathf.Clamp01(newCritChance);
        critMultiplier = Mathf.Max(1f, newCritMultiplier);

        finalSpeed = baseSpeed * Mathf.Max(0.1f, speedMultiplier);

        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        transform.position += (Vector3)(direction * finalSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();

        if (enemyHealth == null) return;

        int finalDamage = damage;
        bool isCrit = Random.value < critChance;

        if (isCrit)
        {
            finalDamage = Mathf.RoundToInt(damage * critMultiplier);
            Debug.Log("CRÍTICO ranged!");
        }

        enemyHealth.TakeDamage(finalDamage, isCrit);
        Destroy(gameObject);
    }

    int CalculateFinalDamage()
    {
        int finalDamage = damage;

        bool isCrit = Random.value < critChance;

        if (isCrit)
        {
            finalDamage = Mathf.RoundToInt(damage * critMultiplier);
            Debug.Log("CRÍTICO ranged!");
        }

        return finalDamage;
    }
}