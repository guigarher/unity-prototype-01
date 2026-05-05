using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    public float lifetime = 0.15f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}