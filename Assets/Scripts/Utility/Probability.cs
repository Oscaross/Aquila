using UnityEngine;

/**
 * Central class for all probabilistic events and sampling code within the game. 
*/
public static class Probability
{

    /// <summary>
    /// A discrete probability mass function which returns the probability of some event happening a certain number of times within a given interval of space.
    /// </summary>
    /// <param name="k">The number of times the event should happen.</param>
    /// <param name="lambda">The mean (expected) number of events.</param>
    /// <returns>The probability that the event with lambda expected occurrences occurs exactly k times.</returns>
    public static float PoissonPMF(int k, float lambda)
    {
        if (k < 0 || lambda <= 0f) return 0f;

        // Working in log space avoids numerical underflow/overflow.
        double logP = -lambda + k * Mathf.Log(lambda) - LogFactorial(k);
        return (float)System.Math.Exp(logP); // simply pass through the definition of Poisson
    }

    /// <summary>
    /// Uses Knuth's method to draw a random count from the Poisson distribution, Poisson(lambda).
    /// </summary>
    /// <param name="lambda">The expected number of occurrences over some range.</param>
    /// <returns>The number of actual ocurrences, to the nearest integer.</returns>
    public static int SamplePoisson(float lambda)
    {
        if (lambda <= 0f) return 0;

        // It is computationally too expensive past 30 samples to use this method so we approximate with a simple Gaussian.
        if (lambda > 30f) return Mathf.Max(0, Mathf.RoundToInt(SampleGaussian(lambda, Mathf.Sqrt(lambda))));

        double L = System.Math.Exp(-lambda);
        int k = 0;
        double p = 1.0;
        do
        {
            k++;
            p *= Random.value;
        } while (p > L);

        return k - 1;
    }

    /// <summary>
    /// Returns a random value from a normally distributed curve over some mean and stdev.
    /// </summary>
    /// <param name="mean"></param>
    /// <param name="stDev"></param>
    /// <returns>The random value according to the normal distribution.</returns>
    public static float SampleGaussian(float mean, float stDev)
    {
        // Uses the Box-Muller transform which generates pairs of independent normally distributed random numbers.
        float u1 = 1f - Random.value;
        float u2 = Random.value;
        float z = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);

        // The random value is the expected value plus the independent random value scaling the standard deviation.
        return mean + stDev * z; 
    }

    private static double LogFactorial(int n)
    {
        double sum = 0.0;
        for (int i = 2; i <= n; i++)
        {
            sum += System.Math.Log(i);
        }

        return sum;
    }
    
}
