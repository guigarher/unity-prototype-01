using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI coinsText;

    [Header("Referencias Player")]
    public PlayerHealth playerHealth;
    public PlayerStats playerStats;
    public PlayerXP playerXP;
    public PlayerCurrency playerCurrency;

    void Start()
    {
        FindPlayerReferences();
    }

    void Update()
    {
        UpdateHUD();
    }

    void FindPlayerReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("HUDManager: No se encontró Player.");
            return;
        }

        if (playerHealth == null)
            playerHealth = player.GetComponent<PlayerHealth>();

        if (playerStats == null)
            playerStats = player.GetComponent<PlayerStats>();

        if (playerXP == null)
            playerXP = player.GetComponent<PlayerXP>();

        if (playerCurrency == null)
            playerCurrency = player.GetComponent<PlayerCurrency>();
    }

    void UpdateHUD()
    {
        if (playerHealth != null && playerStats != null && healthText != null)
        {
            healthText.text = "HP: " + playerHealth.currentHealth + " / " + playerStats.maxHealth;
        }

        if (playerXP != null && levelText != null)
        {
            levelText.text = "Nivel: " + playerXP.currentLevel;
        }

        if (playerXP != null && xpText != null)
        {
            xpText.text = "XP: " + Mathf.FloorToInt(playerXP.currentXP) + " / " + Mathf.FloorToInt(playerXP.xpToNextLevel);
        }

        if (playerCurrency != null && coinsText != null)
        {
            coinsText.text = "Monedas: " + playerCurrency.coins;
        }
    }
}