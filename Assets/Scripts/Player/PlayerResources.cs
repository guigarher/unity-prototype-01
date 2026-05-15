using UnityEngine;
using System;

public class PlayerResources : MonoBehaviour
{
    [Header("Recursos")]
    public int wood = 0;
    public int stone = 0;

    public static event Action<int, int> OnResourcesChanged;

    void Start()
    {
        NotifyResourcesChanged();
    }

    public void AddWood(int amount)
    {
        wood += amount;
        Debug.Log("Madera +" + amount + ". Total madera: " + wood);
        NotifyResourcesChanged();
    }

    public void AddStone(int amount)
    {
        stone += amount;
        Debug.Log("Piedra +" + amount + ". Total piedra: " + stone);
        NotifyResourcesChanged();
    }

    public bool SpendWood(int amount)
    {
        if (wood < amount) return false;

        wood -= amount;
        NotifyResourcesChanged();
        return true;
    }

    public bool SpendStone(int amount)
    {
        if (stone < amount) return false;

        stone -= amount;
        NotifyResourcesChanged();
        return true;
    }

    public bool CanSpendResources(int woodCost, int stoneCost)
    {
        return wood >= woodCost && stone >= stoneCost;
    }

    public bool SpendResources(int woodCost, int stoneCost)
    {
        if (!CanSpendResources(woodCost, stoneCost))
        {
            return false;
        }

        wood -= woodCost;
        stone -= stoneCost;

        NotifyResourcesChanged();
        return true;
    }

    void NotifyResourcesChanged()
    {
        OnResourcesChanged?.Invoke(wood, stone);
    }
}