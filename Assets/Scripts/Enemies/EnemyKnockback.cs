using UnityEngine;
using System.Collections;

public class EnemyKnockback : MonoBehaviour
{
    private Rigidbody2D rb;
    private Coroutine knockbackRoutine;

    public bool IsBeingKnockedBack { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
        }

        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction, force, duration));
    }

    IEnumerator KnockbackRoutine(Vector2 direction, float force, float duration)
    {
        IsBeingKnockedBack = true;

        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration; // 0 → 1
            float currentForce = Mathf.Lerp(force, 0f, t);

            rb.linearVelocity = direction.normalized * currentForce;

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        IsBeingKnockedBack = false;
        knockbackRoutine = null;
    }
}