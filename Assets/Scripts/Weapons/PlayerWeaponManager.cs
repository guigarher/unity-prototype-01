using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{
    private WeaponBase[] allWeapons;

    private void Awake()
    {
        RefreshWeaponList();
    }

    private void Start()
    {
        DeactivateAllWeapons();
    }

    public void RefreshWeaponList()
    {
        allWeapons = GetComponents<WeaponBase>();
    }

    public void DeactivateAllWeapons()
    {
        if (allWeapons == null) RefreshWeaponList();

        foreach (WeaponBase weapon in allWeapons)
        {
            if (weapon != null)
            {
                weapon.DeactivateWeapon();
            }
        }
    }

    public void ActivateWeaponById(string weaponId)
    {
        if (allWeapons == null) RefreshWeaponList();

        foreach (WeaponBase weapon in allWeapons)
        {
            if (weapon != null && weapon.weaponId == weaponId)
            {
                weapon.ActivateWeapon();
                Debug.Log("Arma activada: " + weapon.weaponName);
                return;
            }
        }

        Debug.LogWarning("No se encontró arma con weaponId: " + weaponId);
    }

    public WeaponBase GetWeaponById(string weaponId)
    {
        if (allWeapons == null) RefreshWeaponList();

        foreach (WeaponBase weapon in allWeapons)
        {
            if (weapon != null && weapon.weaponId == weaponId)
            {
                return weapon;
            }
        }

        return null;
    }

    public bool HasAnyActiveWeapon()
    {
        if (allWeapons == null) RefreshWeaponList();

        foreach (WeaponBase weapon in allWeapons)
        {
            if (weapon != null && weapon.isActiveWeapon)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasActiveWeapon(string weaponId)
    {
        if (allWeapons == null) RefreshWeaponList();

        foreach (WeaponBase weapon in allWeapons)
        {
            if (weapon != null && weapon.isActiveWeapon && weapon.weaponId == weaponId)
            {
                return true;
            }
        }

        return false;
    }

    public List<WeaponBase> GetActiveWeapons()
    {
        if (allWeapons == null) RefreshWeaponList();

        List<WeaponBase> activeWeapons = new List<WeaponBase>();

        foreach (WeaponBase weapon in allWeapons)
        {
            if (weapon != null && weapon.isActiveWeapon)
            {
                activeWeapons.Add(weapon);
            }
        }

        return activeWeapons;
    }

    public List<WeaponBase> GetInactiveWeapons()
    {
        if (allWeapons == null) RefreshWeaponList();

        List<WeaponBase> inactiveWeapons = new List<WeaponBase>();

        foreach (WeaponBase weapon in allWeapons)
        {
            if (weapon != null && !weapon.isActiveWeapon)
            {
                inactiveWeapons.Add(weapon);
            }
        }

        return inactiveWeapons;
    }

    public List<WeaponBase> GetRandomInactiveWeapons(int count)
    {
        List<WeaponBase> availableWeapons = GetInactiveWeapons();
        List<WeaponBase> selectedWeapons = new List<WeaponBase>();

        while (selectedWeapons.Count < count && availableWeapons.Count > 0)
        {
            int index = Random.Range(0, availableWeapons.Count);

            selectedWeapons.Add(availableWeapons[index]);
            availableWeapons.RemoveAt(index);
        }

        return selectedWeapons;
    }

    public bool CanOfferMoreWeapons()
    {
        return GetInactiveWeaponCount() > 0;
    }

    public int GetActiveWeaponCount()
    {
        return GetActiveWeapons().Count;
    }

    public int GetInactiveWeaponCount()
    {
        return GetInactiveWeapons().Count;
    }
}