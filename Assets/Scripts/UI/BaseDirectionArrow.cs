using UnityEngine;

public class BaseDirectionArrow : MonoBehaviour
{
    [Header("Referencias")]
    public Transform baseCore;
    public Camera gameplayCamera;
    public RectTransform arrowUI;

    [Header("Configuración")]
    public float screenMargin = 80f;
    public float hideDistanceFromScreenEdge = 60f;
    public float rotationOffset = -90f;

    void Update()
    {
        if (baseCore == null || gameplayCamera == null || arrowUI == null)
            return;

        Vector3 baseScreenPos = gameplayCamera.WorldToScreenPoint(baseCore.position);

        bool baseIsInFront = baseScreenPos.z > 0;

        bool baseIsInsideScreen =
            baseIsInFront &&
            baseScreenPos.x > hideDistanceFromScreenEdge &&
            baseScreenPos.x < Screen.width - hideDistanceFromScreenEdge &&
            baseScreenPos.y > hideDistanceFromScreenEdge &&
            baseScreenPos.y < Screen.height - hideDistanceFromScreenEdge;

        if (baseIsInsideScreen)
        {
            arrowUI.gameObject.SetActive(false);
            return;
        }

        arrowUI.gameObject.SetActive(true);

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        Vector3 direction = baseScreenPos - screenCenter;

        if (baseScreenPos.z < 0)
            direction *= -1f;

        direction.Normalize();

        Vector3 arrowPos = screenCenter + direction * 1000f;

        arrowPos.x = Mathf.Clamp(arrowPos.x, screenMargin, Screen.width - screenMargin);
        arrowPos.y = Mathf.Clamp(arrowPos.y, screenMargin, Screen.height - screenMargin);
        arrowPos.z = 0f;

        arrowUI.position = arrowPos;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowUI.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
    }
}