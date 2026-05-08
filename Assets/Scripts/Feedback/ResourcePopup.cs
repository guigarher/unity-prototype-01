using UnityEngine;
using TMPro;

public class ResourcePopup : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 1.2f;
    public float lifetime = 1f;

    [Header("Visual")]
    public int sortingOrder = 120;

    private TextMeshPro textMesh;
    private float timer;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingOrder = sortingOrder;
        }

        timer = lifetime;
    }

    void Start()
    {
        Destroy(gameObject, lifetime + 0.1f);
    }

    public void Initialize(string text)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }

        if (textMesh == null) return;

        textMesh.text = text;
        textMesh.fontSize = 3.2f;
        textMesh.color = new Color(0.5f, 1f, 0.4f, 1f);

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