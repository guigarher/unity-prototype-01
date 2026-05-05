using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Tiempo")]
    public float gameTime = 0f;

    [Header("Dificultad")]
    public float difficultyMultiplier = 1f;
    public float difficultyScaleTime = 120f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Update()
    {
        gameTime += Time.deltaTime;

        difficultyMultiplier = 1f + (gameTime / difficultyScaleTime);
    }
}