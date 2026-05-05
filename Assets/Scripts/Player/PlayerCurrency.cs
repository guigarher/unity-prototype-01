using UnityEngine;

public class PlayerCurrency : MonoBehaviour
{
    public int coins = 0;

    public void AddCoins(int amount)
    {
        coins += amount;
        Debug.Log("Monedas: " + coins);
    }

    public bool CanAfford(int amount)
    {
        return coins >= amount;
    }

    public bool SpendCoins(int amount)
    {
        if (coins < amount)
        {
            Debug.Log("No tienes suficientes monedas");
            return false;
        }

        coins -= amount;
        Debug.Log("Gastas " + amount + " monedas. Quedan: " + coins);
        return true;
    }
}