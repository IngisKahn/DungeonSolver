namespace DungeonSolver;

internal class NormalDistribution
{
    private readonly Random random;
    private readonly float sigma;
    private float? nextValue;

    public NormalDistribution(Random random, float sigma)
    {
        this.random = random;
        this.sigma = sigma;
    }

    public float Next()
    {
        if (this.nextValue.HasValue)
        {
            var result = this.nextValue.Value;
            this.nextValue = null;
            return result;
        }

        for (; ; )
        {
            var v1 = 2 * this.random.NextDouble() - 1;
            var v2 = 2 * this.random.NextDouble() - 1;
            var w = v1 * v1 + v2 * v2;

            if (w > 1)
                continue;
            var y = Math.Sqrt(-2.0 * Math.Log(w) / w) * this.sigma;
            this.nextValue = (float)(v2 * y);
            return (float)(v1 * y);
        }
    }
}
