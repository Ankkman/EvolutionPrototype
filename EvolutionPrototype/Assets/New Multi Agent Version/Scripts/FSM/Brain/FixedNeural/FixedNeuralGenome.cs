using System;
using UnityEngine;

[Serializable]
public class FixedNeuralGenome
{
    public int inputCount;
    public int hiddenCount;
    public int outputCount;

    // Flattened matrices:
    // input->hidden: hiddenCount * inputCount
    // hidden->output: outputCount * hiddenCount
    public float[] wInputHidden;
    public float[] bHidden;
    public float[] wHiddenOutput;
    public float[] bOutput;

    public static FixedNeuralGenome CreateRandom(int input, int hidden, int output, System.Random rng, float initScale = 1f)
    {
        var g = new FixedNeuralGenome
        {
            inputCount = input,
            hiddenCount = hidden,
            outputCount = output,
            wInputHidden = new float[hidden * input],
            bHidden = new float[hidden],
            wHiddenOutput = new float[output * hidden],
            bOutput = new float[output]
        };

        FillRandom(g.wInputHidden, rng, initScale);
        FillRandom(g.bHidden, rng, initScale);
        FillRandom(g.wHiddenOutput, rng, initScale);
        FillRandom(g.bOutput, rng, initScale);

        return g;
    }

    public FixedNeuralGenome Clone()
    {
        return new FixedNeuralGenome
        {
            inputCount = inputCount,
            hiddenCount = hiddenCount,
            outputCount = outputCount,
            wInputHidden = (float[])wInputHidden.Clone(),
            bHidden = (float[])bHidden.Clone(),
            wHiddenOutput = (float[])wHiddenOutput.Clone(),
            bOutput = (float[])bOutput.Clone()
        };
    }

    public void MutateInPlace(System.Random rng, float mutationRate = 0.1f, float mutationStrength = 0.25f)
    {
        MutateArray(wInputHidden, rng, mutationRate, mutationStrength);
        MutateArray(bHidden, rng, mutationRate, mutationStrength);
        MutateArray(wHiddenOutput, rng, mutationRate, mutationStrength);
        MutateArray(bOutput, rng, mutationRate, mutationStrength);
    }

    private static void FillRandom(float[] arr, System.Random rng, float scale)
    {
        for (int i = 0; i < arr.Length; i++)
            arr[i] = (float)((rng.NextDouble() * 2.0 - 1.0) * scale);
    }

    private static void MutateArray(float[] arr, System.Random rng, float rate, float strength)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            if (rng.NextDouble() < rate)
                arr[i] += (float)((rng.NextDouble() * 2.0 - 1.0) * strength);
        }
    }
}