using UnityEngine;

public enum ObjectiveType
{
    Chest,
    FarmZone,
    Base,
    Boss,
    Resource
}

public class ObjectiveTarget : MonoBehaviour
{
    [Header("Tipo de objetivo")]
    public ObjectiveType objectiveType = ObjectiveType.Chest;

    [Header("Solo si es recurso")]
    public ResourceType resourceType = ResourceType.Wood;

    [Header("Texto")]
    public string displayName = "Objetivo";

    [Header("Prioridad")]
    public int priority = 0;
}