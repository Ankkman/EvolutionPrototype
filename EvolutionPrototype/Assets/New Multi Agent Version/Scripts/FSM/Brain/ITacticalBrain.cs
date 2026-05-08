public interface ITacticalBrain : IAgentBrain
{
    AgentBrainOutput Decide(AgentBrainInputSnapshot input);
}