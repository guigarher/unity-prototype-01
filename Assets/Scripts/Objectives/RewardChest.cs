using UnityEngine;

public class RewardChest : MonoBehaviour
{
    [Header("Apertura")]
    public float secondsToOpen = 1.75f;
    public bool loseProgressWhenAway = true;
    public float openProgressDecaySpeed = 2f;

    [Header("Circunferencia de apertura")]
    public LineRenderer openProgressRing;
    public float ringRadius = 1.1f;
    public float ringWidth = 0.08f;
    public int ringSegments = 80;
    public Color ringColor = new Color(1f, 0.85f, 0.15f, 1f);
    public int ringSortingOrder = 30;

    [Header("Recompensa de XP")]
    public GameObject xpOrbPrefab;
    public int minXPOrbCount = 5;
    public int maxXPOrbCount = 8;

    [Header("Monedas")]
    public GameObject coinPrefab;
    public int minCoinCount = 3;
    public int maxCoinCount = 6;

    [Header("Forma del drop")]
    public float dropCircleRadius = 1.4f;
    public float positionRandomness = 0.15f;

    [Header("Popup")]
    public GameObject resourcePopupPrefab;
    public Vector3 popupOffset = new Vector3(0f, 1f, 0f);

    [Header("Comportamiento")]
    public bool destroyOnOpen = true;

    private bool playerInside = false;
    private bool opened = false;
    private float openProgress = 0f;

    private GameObject playerObject;

    void Awake()
    {
        if (openProgressRing == null)
        {
            Transform ringTransform = transform.Find("OpenProgressRing");

            if (ringTransform != null)
            {
                openProgressRing = ringTransform.GetComponent<LineRenderer>();
            }
        }
    }

    void Start()
    {
        SetupOpenProgressRing();
        DrawOpenRing(0f);
    }

    void Update()
    {
        if (opened) return;

        UpdateOpenProgress();
        DrawOpenRing(openProgress / secondsToOpen);
    }

    void UpdateOpenProgress()
    {
        if (playerInside)
        {
            openProgress += Time.deltaTime;

            if (openProgress >= secondsToOpen)
            {
                OpenChest();
            }
        }
        else if (loseProgressWhenAway)
        {
            openProgress -= openProgressDecaySpeed * Time.deltaTime;
            openProgress = Mathf.Max(0f, openProgress);
        }

        openProgress = Mathf.Clamp(openProgress, 0f, secondsToOpen);
    }

    void OpenChest()
    {
        if (opened) return;

        opened = true;
        DrawOpenRing(1f);

        if (playerObject != null)
        {
            PlayerXP playerXP = playerObject.GetComponent<PlayerXP>();

            if (playerXP != null)
            {
                int totalXP = Mathf.CeilToInt(playerXP.GetFullLevelXPReward());
                int xpOrbCount = Random.Range(minXPOrbCount, maxXPOrbCount + 1);

                SpawnXPOrbsInCircle(totalXP, xpOrbCount);

                ShowPopup("¡XP de nivel!");
            }
        }

        int coinCount = Random.Range(minCoinCount, maxCoinCount + 1);
        SpawnCoinsInCircle(coinCount);

        Debug.Log("Cofre abierto: soltó XP visual y " + coinCount + " monedas.");

        if (destroyOnOpen)
        {
            Destroy(gameObject);
        }
    }

    void SpawnXPOrbsInCircle(int totalXP, int count)
    {
        if (xpOrbPrefab == null) return;
        if (count <= 0) return;

        int baseValue = totalXP / count;
        int remainder = totalXP % count;

        float angleStep = 360f / count;
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 spawnPosition = GetCirclePosition(angle);

            GameObject orbObject = Instantiate(xpOrbPrefab, spawnPosition, Quaternion.identity);

            XPOrb xpOrb = orbObject.GetComponent<XPOrb>();

            if (xpOrb != null)
            {
                xpOrb.xpValue = baseValue + (i < remainder ? 1 : 0);
                xpOrb.applyXPMultiplier = false;
                xpOrb.pickupDelay = 0.7f;
            }
        }
    }

    void SpawnCoinsInCircle(int count)
    {
        if (coinPrefab == null) return;
        if (count <= 0) return;

        float angleStep = 360f / count;
        float startAngle = Random.Range(0f, 360f) + 180f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 spawnPosition = GetCirclePosition(angle);

            GameObject coinObject = Instantiate(coinPrefab, spawnPosition, Quaternion.identity);

            Coin coin = coinObject.GetComponent<Coin>();

            if (coin != null)
            {
                coin.value = 1;
                coin.pickupDelay = 0.7f;
            }
        }
    }

    Vector3 GetCirclePosition(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;

        Vector2 direction = new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        );

        Vector2 randomOffset = Random.insideUnitCircle * positionRandomness;

        return transform.position +
               (Vector3)(direction * dropCircleRadius + randomOffset);
    }

    void ShowPopup(string text)
    {
        if (resourcePopupPrefab == null) return;

        GameObject popupObject = Instantiate(
            resourcePopupPrefab,
            transform.position + popupOffset,
            Quaternion.identity
        );

        ResourcePopup popup = popupObject.GetComponent<ResourcePopup>();

        if (popup != null)
        {
            popup.Initialize(text);
        }
    }

    void SetupOpenProgressRing()
    {
        if (openProgressRing == null) return;

        openProgressRing.useWorldSpace = false;
        openProgressRing.loop = false;

        openProgressRing.startWidth = ringWidth;
        openProgressRing.endWidth = ringWidth;

        openProgressRing.startColor = ringColor;
        openProgressRing.endColor = ringColor;

        openProgressRing.sortingLayerName = "Default";
        openProgressRing.sortingOrder = ringSortingOrder;

        if (openProgressRing.material == null)
        {
            openProgressRing.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    void DrawOpenRing(float percent)
    {
        if (openProgressRing == null) return;

        percent = Mathf.Clamp01(percent);

        // Si el progreso es casi cero, no dibujamos nada.
        if (percent < 0.03f)
        {
            openProgressRing.positionCount = 0;
            return;
        }

        int visibleSegments = Mathf.Max(2, Mathf.CeilToInt(ringSegments * percent));

        openProgressRing.positionCount = visibleSegments + 1;

        float maxAngle = 360f * percent;

        for (int i = 0; i <= visibleSegments; i++)
        {
            float t = i / (float)visibleSegments;
            float angle = maxAngle * t;

            float radians = (angle + 90f) * Mathf.Deg2Rad;

            float x = Mathf.Cos(radians) * ringRadius;
            float y = Mathf.Sin(radians) * ringRadius;

            openProgressRing.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (opened) return;
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        playerObject = other.gameObject;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
    }
}