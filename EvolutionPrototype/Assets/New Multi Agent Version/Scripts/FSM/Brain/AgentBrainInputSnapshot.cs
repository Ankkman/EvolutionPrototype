using UnityEngine;

[System.Serializable]
public class AgentBrainInputSnapshot
{
    public float timestamp;
    public AgentState currentFSMState;

    public Vector3 selfPosition;
    public Vector3 forward;

    public float awareness01;
    public bool hasVisibleTarget;
    public float targetDistance;

    public float health01;
    public float ammo01;

    public bool isReloading;
    public bool isHealing;
    public bool isUnderCommand;
    public CommandReceiver.CommandType commandType;

    public static AgentBrainInputSnapshot Build(AgentBrainContext context)
    {
        var snapshot = new AgentBrainInputSnapshot
        {
            timestamp = Time.time,
            currentFSMState = context.FSM != null ? context.FSM.CurrentStateKey : AgentState.Patrol,
            selfPosition = context.FSM != null ? context.FSM.transform.position : Vector3.zero,
            forward = context.FSM != null ? context.FSM.transform.forward : Vector3.forward,
            awareness01 = 0f,
            hasVisibleTarget = false,
            targetDistance = -1f,
            health01 = -1f,
            ammo01 = -1f,
            isReloading = false,
            isHealing = false,
            isUnderCommand = false,
            commandType = CommandReceiver.CommandType.None
        };

        if (context.Awareness != null)
        {
            snapshot.awareness01 = Mathf.Clamp01(context.Awareness.awareness / Mathf.Max(1f, context.Awareness.maxAwareness));
            snapshot.hasVisibleTarget = context.Awareness.currentTarget != null;

            if (snapshot.hasVisibleTarget && context.FSM != null)
            {
                snapshot.targetDistance = Vector3.Distance(
                    context.FSM.transform.position,
                    context.Awareness.currentTarget.position
                );
            }
        }

        if (context.Combat != null)
        {
            float maxHealth = Mathf.Max(1f, context.Combat.GetMaxHealth());
            snapshot.health01 = Mathf.Clamp01(context.Combat.GetCurrentHealth() / maxHealth);

            int maxAmmo = Mathf.Max(1, context.Combat.GetMaxAmmo());
            int currentAmmo = Mathf.Max(0, context.Combat.GetCurrentAmmo());
            snapshot.ammo01 = Mathf.Clamp01((float)currentAmmo / maxAmmo);

            snapshot.isReloading = context.Combat.IsReloading();
            snapshot.isHealing = context.Combat.IsHealing();
        }

        if (context.CommandReceiver != null)
        {
            snapshot.isUnderCommand = context.CommandReceiver.IsUnderCommand();
            snapshot.commandType = context.CommandReceiver.CurrentCommand;
        }

        return snapshot;
    }
}