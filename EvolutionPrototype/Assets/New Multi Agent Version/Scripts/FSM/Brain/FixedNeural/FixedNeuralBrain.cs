using UnityEngine;

/// <summary>
/// Fixed-topology tactical brain prototype.
/// Produces tactical intent only; stable systems still execute movement/combat.
/// </summary>
public class FixedNeuralBrain : ITacticalBrain
{
    // Input layout size
    private const int InputCount = 12;
    private const int HiddenCount = 16;
    private const int OutputCount = 7;

    // Output indices
    // 0 attack, 1 retreat, 2 regroup, 3 moveX, 4 moveZ, 5 aggression, 6 cohesion
    private const int O_Attack = 0;
    private const int O_Retreat = 1;
    private const int O_Regroup = 2;
    private const int O_MoveX = 3;
    private const int O_MoveZ = 4;
    private const int O_Aggression = 5;
    private const int O_Cohesion = 6;

    private AgentBrainContext context;
    private FixedNeuralGenome genome;
    private FixedNeuralNetwork net;
    private float[] inputBuffer;

    public string BrainName => "Fixed Neural Prototype";

    public void Initialize(AgentBrainContext context)
    {
        this.context = context;
        inputBuffer = new float[InputCount];

        // 1. Generate the seed and RNG once
        int seed = context.Identity != null
            ? context.Identity.agentName.GetHashCode()
            : context.FSM.gameObject.name.GetHashCode();

        var rng = new System.Random(seed);

        // 2. Initialize genome if it doesn't exist
        if (genome == null)
        {
            genome = FixedNeuralGenome.CreateRandom(InputCount, HiddenCount, OutputCount, rng, 1f);
        }

        // 3. Initialize the network using that genome
        net = new FixedNeuralNetwork(genome);

        // Keep FSM enabled for safe fallback
        if (context.FSM != null)
            context.FSM.enabled = true;
    }

    public void Tick(float deltaTime)
    {
        // No direct actuation in prototype mode.
        // Decisions are exposed through Decide(...) and can be logged/evaluated.
    }

    public AgentBrainOutput Decide(AgentBrainInputSnapshot input)
    {
        BuildInputs(input, inputBuffer);
        float[] outv = net.Forward(inputBuffer);

        float attack = outv[O_Attack];
        float retreat = outv[O_Retreat];
        float regroup = outv[O_Regroup];

        Vector2 move = new Vector2((outv[O_MoveX] * 2f) - 1f, (outv[O_MoveZ] * 2f) - 1f);
        float aggression = outv[O_Aggression];
        float cohesion = outv[O_Cohesion];

        var result = new AgentBrainOutput
        {
            attackWeight = attack,
            retreatWeight = retreat,
            regroupWeight = regroup,
            aggression = aggression,
            allyCohesion = cohesion,
            confidence = Mathf.Clamp01(Mathf.Max(attack, retreat, regroup)),
            obeyActiveCommand = true
        };

        // Primary tactical label
        if (retreat > attack && retreat > regroup)
            result.primaryAction = TacticalActionType.Retreat;
        else if (regroup > attack && regroup > retreat)
            result.primaryAction = TacticalActionType.Regroup;
        else if (attack > 0.5f)
            result.primaryAction = TacticalActionType.Engage;
        else
            result.primaryAction = TacticalActionType.Reposition;

        // Desired reposition point in world (intent only)
        Vector3 moveLocal = new Vector3(move.x, 0f, move.y);
        float mag = Mathf.Clamp01(moveLocal.magnitude);
        Vector3 dirWorld = context.FSM.transform.TransformDirection(moveLocal.normalized);
        float desiredStep = 6f * mag;
        result.desiredWorldPosition = input.selfPosition + dirWorld * desiredStep;

        return result;
    }

    public void Shutdown()
    {
        // Keep FSM alive as safety baseline
        if (context?.FSM != null)
            context.FSM.enabled = true;
    }

    private void BuildInputs(AgentBrainInputSnapshot s, float[] dst)
    {
        // 0-1 normalized tactical context
        dst[0] = s.awareness01;
        dst[1] = s.hasVisibleTarget ? 1f : 0f;
        dst[2] = s.targetDistance < 0f ? 1f : Mathf.Clamp01(s.targetDistance / 25f);
        dst[3] = s.health01 < 0f ? 1f : s.health01;
        dst[4] = s.ammo01 < 0f ? 1f : s.ammo01;
        dst[5] = s.isReloading ? 1f : 0f;
        dst[6] = s.isHealing ? 1f : 0f;
        dst[7] = s.isUnderCommand ? 1f : 0f;

        // Command one-hot (Move/Regroup/Hold/None folded into 3 bits + fallback)
        dst[8] = s.commandType == CommandReceiver.CommandType.MoveTo ? 1f : 0f;
        dst[9] = s.commandType == CommandReceiver.CommandType.Regroup ? 1f : 0f;
        dst[10] = s.commandType == CommandReceiver.CommandType.Hold ? 1f : 0f;

        // Bias input
        dst[11] = 1f;
    }

    public void SetGenome(FixedNeuralGenome newGenome)
    {
        if (newGenome == null) return;
        genome = newGenome.Clone();
        net = new FixedNeuralNetwork(genome);
    }

    public FixedNeuralGenome GetGenomeCopy()
    {
        return genome != null ? genome.Clone() : null;
    }

}