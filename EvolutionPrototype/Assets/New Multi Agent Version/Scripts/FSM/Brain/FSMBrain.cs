using UnityEngine;

/// <summary>
/// Baseline adapter: keeps existing FSM active, and exposes tactical decisions
/// derived from current FSM state for logging/comparison.
/// </summary>
public class FSMBrain : ITacticalBrain
{
    private AgentBrainContext context;

    public string BrainName => "FSM Baseline";

    public void Initialize(AgentBrainContext context)
    {
        this.context = context;
        if (this.context?.FSM != null)
            this.context.FSM.enabled = true;
    }

    public void Tick(float deltaTime)
    {
        // No-op: AgentFSM runs its own Update loop.
    }

    public AgentBrainOutput Decide(AgentBrainInputSnapshot input)
    {
        var output = new AgentBrainOutput();

        switch (input.currentFSMState)
        {
            case AgentState.Patrol:
                output.primaryAction = TacticalActionType.Reposition;
                output.attackWeight = 0.1f;
                output.retreatWeight = 0.0f;
                output.regroupWeight = input.isUnderCommand ? 0.6f : 0.2f;
                output.aggression = 0.2f;
                output.allyCohesion = 0.5f;
                output.confidence = 0.95f;
                break;

            case AgentState.Alert:
                output.primaryAction = TacticalActionType.Investigate;
                output.attackWeight = 0.4f;
                output.retreatWeight = 0.1f;
                output.regroupWeight = 0.3f;
                output.aggression = 0.4f;
                output.allyCohesion = 0.7f;
                output.confidence = 0.9f;
                break;

            case AgentState.Engage:
                output.primaryAction = TacticalActionType.Engage;
                output.attackWeight = 0.9f;
                output.retreatWeight = input.health01 >= 0f && input.health01 < 0.3f ? 0.8f : 0.2f;
                output.regroupWeight = 0.2f;
                output.aggression = 0.85f;
                output.allyCohesion = 0.6f;
                output.confidence = 0.95f;
                break;

            case AgentState.Dead:
                output.primaryAction = TacticalActionType.Hold;
                output.attackWeight = 0f;
                output.retreatWeight = 0f;
                output.regroupWeight = 0f;
                output.aggression = 0f;
                output.allyCohesion = 0f;
                output.confidence = 1f;
                break;
        }

        output.obeyActiveCommand = true;
        output.desiredWorldPosition = input.selfPosition;
        return output;
    }

    public void Shutdown()
    {
        if (context?.FSM != null)
            context.FSM.enabled = false;
    }
}