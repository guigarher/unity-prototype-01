using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 100;
    public float healthRegen = 0f;

    [Header("Defensa")]
    public int armor = 0;

    [Range(0f, 0.75f)]
    public float dodgeChance = 0f;

    [Header("Movimiento")]
    public float moveSpeed = 5f;

    [Header("Ataque global")]
    public float attackSpeedMultiplier = 1f;

    // Afecta a TODO el daño: melee, ranged, magia, estados, etc.
    public float damageMultiplier = 1f;

    [Range(0f, 0.80f)]
    public float cooldownReduction = 0f;

    [Header("Crítico")]
    [Range(0f, 1f)]
    public float critChance = 0.05f;
    public float critMultiplier = 2f;

    [Header("Recogida y progreso")]
    public float pickupRange = 1.5f;
    public float xpMultiplier = 1f;
    public float luck = 0f;

    [Header("Multiplicadores de daño por tipo")]
    public float meleeDamageMultiplier = 1f;
    public float rangedDamageMultiplier = 1f;
    public float magicDamageMultiplier = 1f;

    [Header("Daño prolongado / estados")]
    public float fireDamageMultiplier = 1f;
    public float poisonDamageMultiplier = 1f;
    public float bleedDamageMultiplier = 1f;

    [Header("Melee")]
    public float meleeRange = 1.5f;
    public float meleeKnockbackMultiplier = 1f;
    [Header("Área / rango cercano")]
    public float areaRangeBonus = 0f;
    [Header("Ranged")]
    public float projectileSpeedMultiplier = 1f;
    public int projectileCountBonus = 0;

    [Header("Utilidad futura")]
    public float statusEffectChance = 0f;
}