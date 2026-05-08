using UnityEngine;

/// <summary>
/// Shared references passed to any brain implementation.
/// Keeps brain logic decoupled from GetComponent calls.
/// </summary>
public class AgentBrainContext
{
    public AgentFSM FSM { get; }
    public AgentIdentity Identity { get; }
    public AwarenessModel Awareness { get; }
    public NavigationController Navigation { get; }
    public CombatController Combat { get; }
    public CommandReceiver CommandReceiver { get; }

    public AgentBrainContext(
        AgentFSM fsm,
        AgentIdentity identity,
        AwarenessModel awareness,
        NavigationController navigation,
        CombatController combat,
        CommandReceiver commandReceiver
    )
    {
        FSM = fsm;
        Identity = identity;
        Awareness = awareness;
        Navigation = navigation;
        Combat = combat;
        CommandReceiver = commandReceiver;
    }
}