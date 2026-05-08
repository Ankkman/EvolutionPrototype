using UnityEngine;

[DisallowMultipleComponent]
public class TrainingAgentResetter : MonoBehaviour
{
    private Vector3 spawnPos;
    private Quaternion spawnRot;

    private AgentFSM fsm;
    private CombatStub combatStub;

    private void Awake()
    {
        spawnPos = transform.position;
        spawnRot = transform.rotation;

        fsm = GetComponent<AgentFSM>();
        combatStub = GetComponent<CombatStub>();
    }

    public void ResetForEpisode()
    {
        transform.position = spawnPos;
        transform.rotation = spawnRot;

        if (combatStub != null)
        {
            combatStub.ResetStubState();
        }

        if (fsm != null)
        {
            fsm.ResetFSM();
        }
    }
}