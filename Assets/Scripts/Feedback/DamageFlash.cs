using UnityEngine;
using System.Collections;

public class DamageFlash : MonoBehaviour
{
    [Header("Flash")]
    public Color flashColor = Color.red;
    public float flashDuration = 0.08f;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Coroutine flashRoutine;

    void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

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
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = flashColor;
            }
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = originalColors[i];
            }
        }

        flashRoutine = null;
    }
}