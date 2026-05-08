using System;
using System.Collections.Generic;
using UnityEngine;

public class FixedNeuralEvolutionManager : MonoBehaviour
{
    [Serializable]
    public class TrainingSlot
    {
        public AgentBrainRunner brainRunner;
        public NeuralFitnessProbe fitnessProbe;
        public TrainingAgentResetter resetter;
        public AgentIdentity identity; // optional, auto-filled if null
    }

    [Header("Training Slots (Evolving Team)")]
    public List<TrainingSlot> slots = new List<TrainingSlot>();

    [Header("Optional Opponent Team (for early episode end)")]
    public List<AgentIdentity> opponents = new List<AgentIdentity>();

    [Header("Opponent Resetters (optional)")]
    public List<TrainingAgentResetter> opponentResetters = new List<TrainingAgentResetter>();

    [Header("Evolution Settings")]
    public int populationSize = 24;
    public int eliteCount = 4;
    public float mutationRate = 0.12f;
    public float mutationStrength = 0.3f;
    public int randomSeed = 12345;

    [Header("Episode")]
    public float episodeDuration = 45f;
    public bool autoStart = true;
    public bool endEpisodeIfAnyTeamWiped = true;

    [Header("Verbose Logs")]
    public bool verboseLogs = true;
    public bool logEveryEpisode = true;

    [Header("Runtime Debug")]
    [SerializeField] private bool running;
    [SerializeField] private int generation = 0;
    [SerializeField] private int currentBatchStart = 0;
    [SerializeField] private float episodeTimer = 0f;
    [SerializeField] private float bestFitnessLastGeneration = 0f;
    [SerializeField] private int completedEpisodes = 0;
    [SerializeField] private string lastEpisodeEndReason = "None";

    private System.Random rng;
    private List<FixedNeuralGenome> population;
    private float[] fitnessScores;

    private const int InputCount = 12;
    private const int HiddenCount = 16;
    private const int OutputCount = 7;

    private void Awake()
    {
        AutoWireMissingReferences();
    }

    private void Start()
    {
        if (autoStart)
            StartCoroutine(BeginTrainingDelayed());
        else if (verboseLogs)
            Debug.Log("[EVOLVE] AutoStart disabled. Call BeginTraining() from inspector context menu.");
    }

    private System.Collections.IEnumerator BeginTrainingDelayed()
    {
        // Let AgentFSM/AgentBrainRunner Start() initialize first.
        yield return null;
        BeginTraining();
    }

    private void Update()
    {
        if (!running) return;

        episodeTimer += Time.deltaTime;

        bool timeout = episodeTimer >= episodeDuration;
        bool wiped = endEpisodeIfAnyTeamWiped && IsTeamWipeConditionMet();

        if (timeout || wiped)
        {
            lastEpisodeEndReason = timeout ? "Timeout" : "TeamWipe";
            FinishBatchAndAdvance(lastEpisodeEndReason);
        }
    }

    [ContextMenu("Begin Training")]
    public void BeginTraining()
    {
        AutoWireMissingReferences();

        if (slots.Count == 0)
        {
            Debug.LogError("[EVOLVE] No training slots assigned.");
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].brainRunner == null || slots[i].fitnessProbe == null || slots[i].resetter == null)
            {
                Debug.LogError($"[EVOLVE] Slot {i} missing references (brainRunner/fitnessProbe/resetter).");
                return;
            }
        }

        rng = new System.Random(randomSeed);
        population = new List<FixedNeuralGenome>(populationSize);
        fitnessScores = new float[populationSize];

        for (int i = 0; i < populationSize; i++)
        {
            population.Add(FixedNeuralGenome.CreateRandom(InputCount, HiddenCount, OutputCount, rng, 1f));
            fitnessScores[i] = float.MinValue;
        }

        generation = 1;
        currentBatchStart = 0;
        completedEpisodes = 0;
        episodeTimer = 0f;
        bestFitnessLastGeneration = 0f;
        lastEpisodeEndReason = "Started";

        AssignBatch();
        ResetEpisodeForAllSlots();

        running = true;

        Debug.Log($"[EVOLVE] Training started. Pop={populationSize}, Slots={slots.Count}, Episode={episodeDuration}s");
    }

    [ContextMenu("Stop Training")]
    public void StopTraining()
    {
        running = false;
        Debug.Log("[EVOLVE] Training stopped.");
    }

    private void AssignBatch()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            int genomeIndex = currentBatchStart + i;
            if (genomeIndex >= population.Count) break;

            var slot = slots[i];

            slot.brainRunner.EnsureBrain(AgentBrainType.FixedNeural);
            bool assigned = slot.brainRunner.TrySetFixedNeuralGenome(population[genomeIndex]);

            if (!assigned)
                Debug.LogWarning($"[EVOLVE] Slot {i} failed genome assignment.");
        }

        if (verboseLogs)
        {
            int end = Mathf.Min(currentBatchStart + slots.Count - 1, population.Count - 1);
            Debug.Log($"[EVOLVE] Assigned genomes [{currentBatchStart}..{end}] in Gen {generation}");
        }
    }

    private void ResetEpisodeForAllSlots()
    {
        // Reset evolving team
        for (int i = 0; i < slots.Count; i++)
        {
            try
            {
                slots[i].resetter.ResetForEpisode();
                slots[i].fitnessProbe.ResetEpisode();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EVOLVE] Reset failed in slot {i}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Reset fixed opponents
        for (int i = 0; i < opponentResetters.Count; i++)
        {
            if (opponentResetters[i] == null) continue;
            opponentResetters[i].ResetForEpisode();
        }
    }

    private void FinishBatchAndAdvance(string reason)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            int genomeIndex = currentBatchStart + i;
            if (genomeIndex >= population.Count) break;

            float score = slots[i].fitnessProbe.GetFitness(Mathf.Max(episodeTimer, 0.01f));
            fitnessScores[genomeIndex] = score;
        }

        completedEpisodes++;

        if (logEveryEpisode)
            Debug.Log($"[EVOLVE] Episode #{completedEpisodes} ended ({reason}) at {episodeTimer:F1}s");

        currentBatchStart += slots.Count;

        if (currentBatchStart >= population.Count)
        {
            EvolveNextGeneration();
            currentBatchStart = 0;
        }

        AssignBatch();
        ResetEpisodeForAllSlots();
        episodeTimer = 0f;
    }

    private void EvolveNextGeneration()
    {
        var ranked = new List<(FixedNeuralGenome genome, float fitness)>(population.Count);
        for (int i = 0; i < population.Count; i++)
            ranked.Add((population[i], fitnessScores[i]));

        ranked.Sort((a, b) => b.fitness.CompareTo(a.fitness));
        bestFitnessLastGeneration = ranked[0].fitness;

        int safeElite = Mathf.Clamp(eliteCount, 1, populationSize);
        int tournamentPool = Mathf.Max(2, populationSize / 2);

        var next = new List<FixedNeuralGenome>(populationSize);

        for (int i = 0; i < safeElite; i++)
            next.Add(ranked[i].genome.Clone());

        while (next.Count < populationSize)
        {
            FixedNeuralGenome parent = TournamentSelect(ranked, tournamentPool, 3);
            FixedNeuralGenome child = parent.Clone();
            child.MutateInPlace(rng, mutationRate, mutationStrength);
            next.Add(child);
        }

        population = next;
        generation++;

        Debug.Log($"[EVOLVE] Generation {generation} created. BestFitness={bestFitnessLastGeneration:F2}");
    }

    private FixedNeuralGenome TournamentSelect(List<(FixedNeuralGenome genome, float fitness)> ranked, int poolSize, int k)
    {
        int maxIndex = Mathf.Min(poolSize, ranked.Count);

        int bestIndex = rng.Next(maxIndex);
        float bestFit = ranked[bestIndex].fitness;

        for (int i = 1; i < k; i++)
        {
            int idx = rng.Next(maxIndex);
            if (ranked[idx].fitness > bestFit)
            {
                bestFit = ranked[idx].fitness;
                bestIndex = idx;
            }
        }

        return ranked[bestIndex].genome;
    }

    private bool IsTeamWipeConditionMet()
    {
        bool evolvingDead = true;
        for (int i = 0; i < slots.Count; i++)
        {
            AgentIdentity id = slots[i].identity;
            if (id == null && slots[i].brainRunner != null)
                id = slots[i].brainRunner.GetComponent<AgentIdentity>();

            if (id != null && id.IsAlive)
            {
                evolvingDead = false;
                break;
            }
        }

        bool opponentDead = false;
        if (opponents != null && opponents.Count > 0)
        {
            opponentDead = true;
            for (int i = 0; i < opponents.Count; i++)
            {
                if (opponents[i] != null && opponents[i].IsAlive)
                {
                    opponentDead = false;
                    break;
                }
            }
        }

        return evolvingDead || opponentDead;
    }

    private void AutoWireMissingReferences()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].brainRunner == null) continue;

            if (slots[i].fitnessProbe == null)
                slots[i].fitnessProbe = slots[i].brainRunner.GetComponent<NeuralFitnessProbe>();

            if (slots[i].resetter == null)
                slots[i].resetter = slots[i].brainRunner.GetComponent<TrainingAgentResetter>();

            if (slots[i].identity == null)
                slots[i].identity = slots[i].brainRunner.GetComponent<AgentIdentity>();
        }
    }
}