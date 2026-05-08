using UnityEngine;
using TMPro;

public class PhaseHUD : MonoBehaviour
{
    [Header("Texto")]
    public TextMeshProUGUI phaseText;

    void Update()
    {
        if (phaseText == null) return;
        if (GamePhaseManager.Instance == null) return;

        GamePhase phase = GamePhaseManager.Instance.currentPhase;
        float timer = GamePhaseManager.Instance.GetPhaseTimer();

        string phaseName = phase == GamePhase.Day ? "DÍA" : "NOCHE";

        phaseText.text = phaseName + " - " + Mathf.CeilToInt(timer) + "s";
    }
}