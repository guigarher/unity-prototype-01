using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public int currentLevel = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 6f;

    private PlayerStats stats;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    void Start()
    {
        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.CheckInitialWeaponChoice();
        }
    }

    public void AddXP(float amount)
    {
        float multiplier = 1f;

        if (stats != null)
        {
            multiplier = stats.xpMultiplier;
        }

        float finalXP = amount * multiplier;
        currentXP += finalXP;

        Debug.Log("XP ganada: " + finalXP);

        ProcessLevelUps();
    }

    public void AddExactXPForOneLevel()
    {
        float xpReward = xpToNextLevel;

        currentXP += xpReward;

        Debug.Log("Cofre: XP equivalente a 1 nivel completo: " + xpReward);

        ProcessLevelUps();
    }

    void ProcessLevelUps()
    {
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            LevelUp();
        }
    }

    void LevelUp()
    {
        currentLevel++;

        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.35f);

        Debug.Log("¡Subiste a nivel " + currentLevel + "!");

        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.OpenRewardMenuForLevel(currentLevel);
        }
    }
}