using UnityEngine;
using System;

public enum GamePhase
{
    Day,
    Night
}

public class GamePhaseManager : MonoBehaviour
{
    public static GamePhaseManager Instance;

    [Header("Fase actual")]
    public GamePhase currentPhase = GamePhase.Day;

    [Header("Duración de fases")]
    public float dayDuration = 90f;
    public float nightDuration = 45f;

    [Header("Debug")]
    public bool logPhaseChanges = true;

    private float phaseTimer;

    public static event Action<GamePhase> OnPhaseChanged;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartPhase(GamePhase.Day);
    }

    void Update()
    {
        phaseTimer -= Time.deltaTime;

        if (phaseTimer <= 0f)
        {
            SwitchPhase();
        }
    }

    void StartPhase(GamePhase newPhase)
    {
        currentPhase = newPhase;

        if (currentPhase == GamePhase.Day)
        {
            phaseTimer = dayDuration;
        }
        else
        {
            phaseTimer = nightDuration;
        }

        if (logPhaseChanges)
        {
            Debug.Log("Nueva fase: " + currentPhase);
        }

        OnPhaseChanged?.Invoke(currentPhase);
    }

    void SwitchPhase()
    {
        if (currentPhase == GamePhase.Day)
        {
            StartPhase(GamePhase.Night);
        }
        else
        {
            StartPhase(GamePhase.Day);
        }
    }

    public bool IsDay()
    {
        return currentPhase == GamePhase.Day;
    }

    public bool IsNight()
    {
        return currentPhase == GamePhase.Night;
    }

    public float GetPhaseTimer()
    {
        return phaseTimer;
    }

    public float GetCurrentPhaseDuration()
    {
        if (currentPhase == GamePhase.Day)
        {
            return dayDuration;
        }

        return nightDuration;
    }
}