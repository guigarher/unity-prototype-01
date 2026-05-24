using TMPro;
using UnityEngine;

public enum DamagePopupType
{
    Normal,
    Crit,
    Poison,
    Bleed
}

public class DamagePopup : MonoBehaviour
{
    public float moveSpeed = 0.6f;
    public float lifetime = 0.35f;

    private TextMeshPro textMesh;
    private float timer;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingOrder = 100;
        }

        timer = lifetime;
    }

    void Start()
    {
        Destroy(gameObject, lifetime + 0.1f);
    }

    public void Setup(int damage, bool isCrit)
    {
        Setup(damage, isCrit ? DamagePopupType.Crit : DamagePopupType.Normal);
    }

    public void Setup(int damage, DamagePopupType popupType)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }

        if (textMesh == null) return;

        switch (popupType)
        {
            case DamagePopupType.Crit:
                textMesh.text = damage + "!";
                textMesh.fontSize = 4.5f;
                textMesh.color = Color.red;
                break;

            case DamagePopupType.Poison:
                textMesh.text = damage.ToString();
                textMesh.fontSize = 3f;
                textMesh.color = Color.green;
                break;

            case DamagePopupType.Bleed:
                textMesh.text = damage.ToString();
                textMesh.fontSize = 3.2f;
                textMesh.color = new Color(0.8f, 0f, 0f, 1f);
                break;

            default:
                textMesh.text = damage.ToString();
                textMesh.fontSize = 3f;
                textMesh.color = Color.yellow;
                break;
        }

        timer = lifetime;
    }

    public void SetupText(string text, Color color, float fontSize)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }

        if (textMesh == null) return;

        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.color = color;

        timer = lifetime;
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.unscaledDeltaTime;

        timer -= Time.unscaledDeltaTime;

        if (textMesh != null)
        {
            Color color = textMesh.color;
            color.a = Mathf.Clamp01(timer / lifetime);
            textMesh.color = color;
        }

        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }
}