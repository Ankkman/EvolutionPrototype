public interface IAgentBrain
{
    string BrainName { get; }
    void Initialize(AgentBrainContext context);
    void Tick(float deltaTime);
    void Shutdown();
}