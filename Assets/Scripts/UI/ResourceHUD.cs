using UnityEngine;
using TMPro;

public class ResourceHUD : MonoBehaviour
{
    [Header("Texto UI")]
    public TextMeshProUGUI resourcesText;

    void OnEnable()
    {
        PlayerResources.OnResourcesChanged += UpdateResourcesUI;
    }

    void OnDisable()
    {
        PlayerResources.OnResourcesChanged -= UpdateResourcesUI;
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            PlayerResources resources = playerObject.GetComponent<PlayerResources>();

            if (resources != null)
            {
                UpdateResourcesUI(resources.wood, resources.stone);
            }
        }
    }

    void UpdateResourcesUI(int wood, int stone)
    {
        if (resourcesText == null) return;

        resourcesText.text =
            "Madera: " + wood +
            "\nPiedra: " + stone;
    }
}