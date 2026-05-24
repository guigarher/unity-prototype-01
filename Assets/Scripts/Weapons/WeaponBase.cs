using UnityEngine;
using System.Collections.Generic;

public enum WeaponTag
{
    Melee,
    Ranged,
    Projectile,
    Area,
    Poison,
    Bleed,
    Magic
}

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Info arma")]
    public string weaponId;
    public string weaponName;

    [Header("Etiquetas")]
    public List<WeaponTag> weaponTags = new List<WeaponTag>();

    public bool HasTag(WeaponTag tag)
    {
        return weaponTags.Contains(tag);
    }
    public bool isActiveWeapon = false;

    [Header("Progresión")]
    public int weaponLevel = 0;
    public int maxLevel = 15;

    protected PlayerStats playerStats;

    protected virtual void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();
    }

    public virtual void ActivateWeapon()
    {
        isActiveWeapon = true;

        if (weaponLevel <= 0)
        {
            weaponLevel = 1;
        }

        enabled = true;
    }

    public virtual void DeactivateWeapon()
    {
        isActiveWeapon = false;
        enabled = false;
    }

    public virtual bool CanLevelUp()
    {
        return isActiveWeapon && weaponLevel < maxLevel;
    }

    public virtual void LevelUp()
    {
        if (!CanLevelUp()) return;

        weaponLevel++;
        OnLevelUp();

        Debug.Log(weaponName + " sube a nivel " + weaponLevel);
    }

    protected virtual void OnLevelUp()
    {
    }

    public virtual List<UpgradeOption> GetSpecificUpgradeOptions()
    {
        return new List<UpgradeOption>();
    }

    public virtual void ApplySpecificUpgrade(UpgradeOption option)
    {
    }
}