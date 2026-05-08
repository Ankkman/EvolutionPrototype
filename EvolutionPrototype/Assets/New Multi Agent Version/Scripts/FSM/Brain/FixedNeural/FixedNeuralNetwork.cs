using UnityEngine;

public class FixedNeuralNetwork
{
    private readonly FixedNeuralGenome genome;
    private readonly float[] hiddenBuffer;
    private readonly float[] outputBuffer;

    public FixedNeuralNetwork(FixedNeuralGenome genome)
    {
        this.genome = genome;
        hiddenBuffer = new float[genome.hiddenCount];
        outputBuffer = new float[genome.outputCount];
    }

    public float[] Forward(float[] input)
    {
        // Hidden layer: tanh
        for (int h = 0; h < genome.hiddenCount; h++)
        {
            float sum = genome.bHidden[h];
            int rowOffset = h * genome.inputCount;

            for (int i = 0; i < genome.inputCount; i++)
                sum += genome.wInputHidden[rowOffset + i] * input[i];

            hiddenBuffer[h] = (float)System.Math.Tanh(sum);
        }

        // Output layer: sigmoid in [0,1]
        for (int o = 0; o < genome.outputCount; o++)
        {
            float sum = genome.bOutput[o];
            int rowOffset = o * genome.hiddenCount;

            for (int h = 0; h < genome.hiddenCount; h++)
                sum += genome.wHiddenOutput[rowOffset + h] * hiddenBuffer[h];

            outputBuffer[o] = Sigmoid(sum);
        }

        return outputBuffer;
    }

    private static float Sigmoid(float x)
    {
        return 1f / (1f + Mathf.Exp(-x));
    }
}