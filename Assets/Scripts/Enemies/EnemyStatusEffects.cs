using UnityEngine;
using System.Collections;

public class EnemyStatusEffects : MonoBehaviour
{
    [Header("Sangrado")]
    public int maxBleedStacks = 5;

    private int currentBleedStacks = 0;
    private int totalBleedDamagePerTick = 0;
    private float bleedTimeRemaining = 0f;
    private Coroutine bleedRoutine;

    [Header("Veneno")]
    public int maxPoisonStacks = 5;

    private int currentPoisonStacks = 0;
    private int totalPoisonDamagePerTick = 0;
    private float poisonTimeRemaining = 0f;
    private Coroutine poisonRoutine;

    public void ApplyBleed(int damagePerTick, float duration, float tickInterval)
    {
        if (damagePerTick <= 0) return;
        if (duration <= 0f) return;
        if (tickInterval <= 0f) return;

        if (currentBleedStacks < maxBleedStacks)
        {
            currentBleedStacks++;
            totalBleedDamagePerTick += damagePerTick;
        }

        bleedTimeRemaining = duration;

        if (bleedRoutine == null)
        {
            bleedRoutine = StartCoroutine(BleedRoutine(tickInterval));
        }
    }

    IEnumerator BleedRoutine(float tickInterval)
    {
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();

        while (bleedTimeRemaining > 0f)
        {
            yield return new WaitForSeconds(tickInterval);

            bleedTimeRemaining -= tickInterval;

            if (enemyHealth == null)
            {
                break;
            }

            enemyHealth.TakeDamage(totalBleedDamagePerTick, false);
        }

        currentBleedStacks = 0;
        totalBleedDamagePerTick = 0;
        bleedTimeRemaining = 0f;
        bleedRoutine = null;
    }

    public void ApplyPoison(int damagePerTick, float duration, float tickInterval, int maxStacksForThisPoison)
    {
        if (damagePerTick <= 0) return;
        if (duration <= 0f) return;
        if (tickInterval <= 0f) return;

        int allowedStacks = Mathf.Clamp(maxStacksForThisPoison, 1, maxPoisonStacks);

        if (currentPoisonStacks < allowedStacks)
        {
            currentPoisonStacks++;
            totalPoisonDamagePerTick += damagePerTick;
        }

        poisonTimeRemaining = duration;

        if (poisonRoutine == null)
        {
            poisonRoutine = StartCoroutine(PoisonRoutine(tickInterval));
        }
    }

    IEnumerator PoisonRoutine(float tickInterval)
    {
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();

        while (poisonTimeRemaining > 0f)
        {
            if (enemyHealth == null)
            {
                break;
            }

            // El veneno hace daño inmediatamente al empezar.
            enemyHealth.TakeDamage(totalPoisonDamagePerTick, false);

            yield return new WaitForSeconds(tickInterval);

            poisonTimeRemaining -= tickInterval;
        }

        currentPoisonStacks = 0;
        totalPoisonDamagePerTick = 0;
        poisonTimeRemaining = 0f;
        poisonRoutine = null;
    }
}