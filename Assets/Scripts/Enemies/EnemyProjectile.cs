using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Datos")]
    public float speed = 6f;
    public int damage = 6;
    public float lifeTime = 4f;

    private Vector2 direction = Vector2.right;

    public void Initialize(Vector2 newDirection, float newSpeed, int newDamage)
    {
        direction = newDirection.normalized;
        speed = newSpeed;
        damage = newDamage;
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        lifeTime -= Time.deltaTime;

        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        BaseCore baseCore = other.GetComponentInParent<BaseCore>();

        if (baseCore != null)
        {
            baseCore.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }
    }
}