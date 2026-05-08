using UnityEngine;

[System.Serializable]
public class AgentBrainOutput
{
    public TacticalActionType primaryAction = TacticalActionType.None;
    public Vector3 desiredWorldPosition = Vector3.zero;

    [Range(0f, 1f)] public float attackWeight = 0f;
    [Range(0f, 1f)] public float retreatWeight = 0f;
    [Range(0f, 1f)] public float regroupWeight = 0f;
    [Range(0f, 1f)] public float aggression = 0.5f;
    [Range(0f, 1f)] public float allyCohesion = 0.5f;

    [Range(0f, 1f)] public float confidence = 1f;
    public bool obeyActiveCommand = true;
}