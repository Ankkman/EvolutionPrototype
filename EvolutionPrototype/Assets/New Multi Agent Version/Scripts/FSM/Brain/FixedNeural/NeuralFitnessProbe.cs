using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AgentFSM))]
[RequireComponent(typeof(AgentIdentity))]
[RequireComponent(typeof(AwarenessModel))]
public class NeuralFitnessProbe : MonoBehaviour
{
    [Header("Weights")]
    public float aliveTimeWeight = 1.0f;
    public float engageTimeWeight = 0.6f;
    public float awarenessWeight = 15f;
    public float healthWeight = 20f;
    public float deathPenalty = -25f;
    public float survivalBonus = 15f;

    private AgentFSM fsm;
    private AgentIdentity identity;
    private AwarenessModel awareness;
    private IAgentCombat combat;

    private float aliveTime;
    private float engageTime;
    private float awarenessIntegral;
    private bool wasInitialized;

    private void Awake()
    {
        fsm = GetComponent<AgentFSM>();
        identity = GetComponent<AgentIdentity>();
        awareness = GetComponent<AwarenessModel>();
        combat = identity != null ? identity.Combat : null;
        wasInitialized = true;
    }

    public void ResetEpisode()
    {
        aliveTime = 0f;
        engageTime = 0f;
        awarenessIntegral = 0f;
    }

    private void Update()
    {
        if (!wasInitialized || combat == null) return;

        if (!combat.IsDead())
        {
            aliveTime += Time.deltaTime;
        }

        if (fsm.CurrentStateKey == AgentState.Engage)
        {
            engageTime += Time.deltaTime;
        }

        float awareness01 = Mathf.Clamp01(awareness.awareness / Mathf.Max(1f, awareness.maxAwareness));
        awarenessIntegral += awareness01 * Time.deltaTime;
    }

    public float GetFitness(float episodeDuration)
    {
        if (!wasInitialized || combat == null) return 0f;

        float avgAwareness = episodeDuration > 0.01f ? awarenessIntegral / episodeDuration : 0f;
        float health01 = Mathf.Clamp01(combat.GetCurrentHealth() / Mathf.Max(1f, combat.GetMaxHealth()));

        float score = 0f;
        score += aliveTime * aliveTimeWeight;
        score += engageTime * engageTimeWeight;
        score += avgAwareness * awarenessWeight;
        score += health01 * healthWeight;
        score += combat.IsDead() ? deathPenalty : survivalBonus;

        return score;
    }
}