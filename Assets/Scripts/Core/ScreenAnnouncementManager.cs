using UnityEngine;
using TMPro;
using System.Collections;

public class ScreenAnnouncementManager : MonoBehaviour
{
    public static ScreenAnnouncementManager Instance;

    [Header("Referencias UI")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;

    [Header("Timing")]
    public float fadeInTime = 0.15f;
    public float fadeOutTime = 0.35f;

    [Header("Debug")]
    public bool testOnStart = false;

    private Coroutine currentRoutine;

    void Awake()
    {
        Instance = this;
        HideInstant();
    }

    void Start()
    {
        if (testOnStart)
        {
            ShowMessage("¡SUBIDA DE NIVEL!", "Nivel 2 conseguido", 2f);
        }
    }

    public void ShowMessage(string title, string subtitle, float holdTime)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(ShowRoutine(title, subtitle, holdTime));
    }

    public void ShowPersistent(string title, string subtitle)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        if (titleText != null) titleText.text = title;
        if (subtitleText != null) subtitleText.text = subtitle;

        ShowInstant();
    }

    IEnumerator ShowRoutine(string title, string subtitle, float holdTime)
    {
        if (titleText != null) titleText.text = title;
        if (subtitleText != null) subtitleText.text = subtitle;

        if (canvasGroup == null) yield break;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float timer = 0f;
        canvasGroup.alpha = 0f;

        while (timer < fadeInTime)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(timer / fadeInTime);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        timer = 0f;

        while (timer < holdTime)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        timer = 0f;

        while (timer < fadeOutTime)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeOutTime);
            yield return null;
        }

        HideInstant();
        currentRoutine = null;
    }

    void ShowInstant()
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void HideInstant()
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}