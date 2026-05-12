using UnityEngine;
using TMPro;

public class BaseHUD : MonoBehaviour
{
    [Header("Texto UI")]
    public TextMeshProUGUI baseHealthText;

    void OnEnable()
    {
        BaseCore.OnBaseHealthChanged += UpdateBaseHealthUI;
        BaseCore.OnBaseDestroyed += ShowBaseDestroyed;
    }

    void OnDisable()
    {
        BaseCore.OnBaseHealthChanged -= UpdateBaseHealthUI;
        BaseCore.OnBaseDestroyed -= ShowBaseDestroyed;
    }

    void Start()
    {
        if (BaseCore.Instance != null)
        {
            UpdateBaseHealthUI(BaseCore.Instance.currentHealth, BaseCore.Instance.maxHealth);
        }
    }

    void UpdateBaseHealthUI(int currentHealth, int maxHealth)
    {
        if (baseHealthText == null) return;

        baseHealthText.text = "Base: " + currentHealth + " / " + maxHealth;
    }

    void ShowBaseDestroyed()
    {
        if (baseHealthText == null) return;

        baseHealthText.text = "BASE DESTRUIDA";
    }
}