using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AgentFSM))]
[RequireComponent(typeof(AgentIdentity))]
[RequireComponent(typeof(AwarenessModel))]
[RequireComponent(typeof(NavigationController))]
public class AgentBrainRunner : MonoBehaviour
{
    [Header("Brain Selection")]
    public AgentBrainType initialBrain = AgentBrainType.FSMBaseline;

    [Header("Debug")]
    [SerializeField] private string activeBrainName = "None";
    [SerializeField] private AgentBrainInputSnapshot lastInput;
    [SerializeField] private AgentBrainOutput lastOutput;

    private IAgentBrain activeBrain;
    private ITacticalBrain tacticalBrain;
    private AgentBrainContext context;

    public AgentBrainInputSnapshot LastInput => lastInput;
    public AgentBrainOutput LastOutput => lastOutput;

    private void Awake()
    {
        context = new AgentBrainContext(
            GetComponent<AgentFSM>(),
            GetComponent<AgentIdentity>(),
            GetComponent<AwarenessModel>(),
            GetComponent<NavigationController>(),
            GetComponent<CombatController>(),
            GetComponent<CommandReceiver>()
        );
    }

    private void Start()
    {
        SwitchBrain(initialBrain);
    }

    private void Update()
    {
        if (activeBrain == null) return;

        activeBrain.Tick(Time.deltaTime);

        // Phase 0.5: capture standardized input/output contract per frame.
        lastInput = AgentBrainInputSnapshot.Build(context);

        if (tacticalBrain != null)
            lastOutput = tacticalBrain.Decide(lastInput);
    }

    public void SwitchBrain(AgentBrainType brainType)
    {
        activeBrain?.Shutdown();

        activeBrain = CreateBrain(brainType);
        tacticalBrain = activeBrain as ITacticalBrain;

        activeBrain.Initialize(context);
        activeBrainName = activeBrain.BrainName;

        Debug.Log($"[BRAIN] {gameObject.name} using brain: {activeBrainName}");
    }

    private IAgentBrain CreateBrain(AgentBrainType brainType)
    {
        switch (brainType)
        {
            case AgentBrainType.FSMBaseline:
                return new FSMBrain();

            case AgentBrainType.FixedNeural:
                return new FixedNeuralBrain();
            case AgentBrainType.NEAT:
                Debug.LogWarning($"[BRAIN] {brainType} not implemented yet on {gameObject.name}. Falling back to FSMBaseline.");
                return new FSMBrain();

            default:
                return new FSMBrain();
        }
    }

    public string GetActiveBrainName()
    {
        return activeBrainName;
    }


    public bool EnsureBrain(AgentBrainType brainType)
    {
        if (brainType == AgentBrainType.FixedNeural && !(activeBrain is FixedNeuralBrain))
        {
            SwitchBrain(AgentBrainType.FixedNeural);
            return true;
        }

        if (brainType == AgentBrainType.FSMBaseline && !(activeBrain is FSMBrain))
        {
            SwitchBrain(AgentBrainType.FSMBaseline);
            return true;
        }

        return false;
    }

    public bool TrySetFixedNeuralGenome(FixedNeuralGenome genome)
    {
        if (activeBrain is FixedNeuralBrain fixedBrain)
        {
            fixedBrain.SetGenome(genome);
            return true;
        }
        return false;
    }

}