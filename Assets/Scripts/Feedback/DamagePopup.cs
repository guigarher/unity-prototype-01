using TMPro;
using UnityEngine;

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
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }

        if (textMesh == null) return;

        textMesh.text = isCrit ? damage + "!" : damage.ToString();
        textMesh.fontSize = isCrit ? 4.5f : 3f;
        textMesh.color = isCrit ? Color.red : Color.yellow;

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