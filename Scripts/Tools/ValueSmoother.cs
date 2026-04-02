using Godot;

namespace GodotGameAIbyExample.Scripts.Tools;

/// <summary>
/// <p>This class offers some methods to smooth values along a moving window of values. This
/// way you can avoid discontinuous changes of values.</p>
/// <p> Based on the article:
/// https://jasonfantl.com/posts/Collision-Avoidance/#simulations</p>
/// </summary>
public static class ValueSmoother
{
    /// <summary>
    /// Averages the values in the given array.
    /// </summary>
    /// <param name="values">Array of values to average.</param>
    /// <returns>Average value.</returns>
    public static float Average(float[] values)
    {
        if (values.Length == 0) return 0;
        int valueCount = values.Length;
        float sum = 0;
        
        foreach (var value in values)
        {
            sum += value;
        }
        
        return sum / valueCount;
    }

    /// <summary>
    /// <p>Sums the values in the given array, weighted by the given curve.</p>
    /// <p>Usually we use a Gaussian curve, this way we assign small weights to the most
    /// recent and oldest points, and weight more heavily the middle points. This would
    /// give us a much smoother signal, and we can even control how quickly it
    /// smooths.</p>
    /// </summary>
    /// <param name="valuesWindow">Array of values to smooth.</param>
    /// <param name="weights">Weights for the points of the sequence.
    /// Use <see cref="SampleWeights"/> to get a valid array of samples.</param>
    /// <remarks> valuesWindow and weights must have the same length, or you will get
    /// a crash.</remarks>
    /// <returns></returns>
    public static float WeightedMovingAverage(MovingWindow valuesWindow, float[] weights)
    {
        float[] values = valuesWindow.Values;
        float sum = 0;
        for (int i = 1; i <= values.Length; i++)
        {
            sum += weights[^i] * values[^i];
        }
        return sum;
    }

    /// <summary>
    /// Smooths the given value just taking in count the previous output value in the
    /// timeline.
    /// </summary>
    /// <param name="lastOutput">Previous output value.</param>
    /// <param name="currentInput">Current input value.</param>
    /// <param name="convergenceRate">Between 0 and 1. The higher value here, the less
    /// smoothing effect. If 1, the smoothing output is just the current observation.
    /// Whereas, if 0, the output is just a constant with the first input in the
    /// series.</param>
    /// <returns>New smoothed output.</returns>
    public static float Exponential(
        float lastOutput, 
        float currentInput, 
        float convergenceRate)
    {
        // Be aware that https://jasonfantl.com/posts/Collision-Avoidance/#simulations
        // uses (convergenceRate - 1), but I think it's a typo. I'm using wikipedia's
        // version: (1 - convergenceRate).
        return (1 - convergenceRate) * lastOutput + convergenceRate * currentInput;
    }

    /// <summary>
    /// Samples the given curve and returns an array of values.
    /// </summary>
    /// <param name="amount">Number of samples.</param>
    /// <param name="weightCurve">Curve to sample.</param>
    /// <param name="normalize">Whether the sum of samples values should sum one.</param>
    /// <returns>A sequence of values ranging from0 to 1.</returns>
    public static float[] SampleWeights(
        int amount, 
        Curve weightCurve, 
        bool normalize = true)
    {
        float[] weightValues = new float[amount];
        float sum = 0;
        
        // First pass. Get the weight values.
        for (int i = 0; i < amount; i++)
        {
            float offset = (float)i / amount;
            weightValues[i] = weightCurve.Sample(offset);
            sum += weightValues[i];
        }
        
        if (!normalize) return weightValues;
        // Second pass. Normalize the weighted values.
        for (int i = 0; i < amount; i++)
        {
            weightValues[i] /= sum;
        }
        return weightValues;
    }
}