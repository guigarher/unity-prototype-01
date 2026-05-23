using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DamageFlash : MonoBehaviour
{
    [Header("Flash")]
    public Color flashColor = Color.red;
    public float flashDuration = 0.08f;

    [Header("Reinicio visual")]
    public bool forceRestartFlash = true;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Coroutine flashRoutine;

    void Awake()
    {
        CacheRenderers();
    }

    void CacheRenderers()
    {
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        List<SpriteRenderer> validRenderers = new List<SpriteRenderer>();

        foreach (SpriteRenderer spriteRenderer in allRenderers)
        {
            if (spriteRenderer == null) continue;

            if (spriteRenderer.GetComponentInParent<IgnoreDamageFlash>() != null)
            {
                continue;
            }

            validRenderers.Add(spriteRenderer);
        }

        spriteRenderers = validRenderers.ToArray();
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
    }

    public void PlayFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreOriginalColors();
            flashRoutine = null;
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        if (forceRestartFlash)
        {
            RestoreOriginalColors();

            // Espera un frame real para que el nuevo flash se note,
            // aunque el golpe llegue mientras el sprite estaba rojo.
            yield return null;
        }

        SetFlashColor();

        yield return new WaitForSecondsRealtime(flashDuration);

        RestoreOriginalColors();

        flashRoutine = null;
    }

    void SetFlashColor()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = flashColor;
            }
        }
    }

    void RestoreOriginalColors()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = originalColors[i];
            }
        }
    }
}