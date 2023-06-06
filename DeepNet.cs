namespace DungeonSolver;

/// <summary>
/// uses ELU, Sigmoid
/// </summary>
internal class DeepNet
{
    private readonly float[][] nodes;
    private readonly float[][,] weights;
    private readonly float[][,] biases;



    //private const float alpha = 1;
    private const float gamma = .01f;
    private const float gammaBias = .005f;
    private const float momentumStepSize = .001f;
    private const float momentumSpeedDecay = .9f;
    private const float momentumAccelerationDecay = .999f;
    private const float momentumEpsilon = .00000001f;

    private float momentumSpeedDecayCorrection = momentumSpeedDecay;
    private float momentumAccelerationDecayCorrection = momentumAccelerationDecay;

    public DeepNet(int inputNodeCount, IList<int> hiddenNodeCounts, int outputNodeCount)
    {
        this.nodes = new float[hiddenNodeCounts.Count + 1][];
        for (var i = 0; i < hiddenNodeCounts.Count; i++)
            this.nodes[i] = new float[hiddenNodeCounts[i]];
        this.nodes[^1] = new float[outputNodeCount];

        this.weights = new float[hiddenNodeCounts.Count + 1][,];
        this.weights[0] = new float[inputNodeCount, hiddenNodeCounts[0]];
        this.weights[^1] = new float[hiddenNodeCounts[^1], outputNodeCount];
        for (var i = 1; i < hiddenNodeCounts.Count; i++)
            this.weights[i] = new float[hiddenNodeCounts[i - 1], hiddenNodeCounts[i]];

        this.biases = new float[hiddenNodeCounts.Count + 1][,];
        for (var i = 0; i < hiddenNodeCounts.Count; i++)
            this.biases[i] = new float[hiddenNodeCounts[i], 3];
        this.biases[^1] = new float[outputNodeCount, 3];

        Random r = new();
        for (var i = 0; i < hiddenNodeCounts.Count + 1; i++)
        {
            var matrix = weights[i];
            //var heSigma = MathF.Sqrt(2f / matrix.GetLength(0)); 
            var xavierSigma = MathF.Sqrt(6f / (matrix.GetLength(0) + matrix.GetLength(1)));
            NormalDistribution normal = new(r, xavierSigma);
            for (var j = 0; j < matrix.GetLength(0); j++)
                for (var k = 0; k < matrix.GetLength(1); k++)
                    matrix[j, k] = normal.Next();
        }
        //{
        //    var matrix = weights[^1];
        //    var xavierSigma = MathF.Sqrt(6f / (matrix.GetLength(0) + matrix.GetLength(1)));
        //    NormalDistribution normal = new(r, xavierSigma);
        //    for (var j = 0; j < matrix.GetLength(0); j++)
        //        for (var k = 0; k < matrix.GetLength(1); k++)
        //            matrix[j, k] = normal.Next();
        //}
    }

    public float[] FeedForward(float[] input)
    {
        float[,] biases;
        var fromNodes = input;
        for (var k = 0; k < this.nodes.Length; k++)
        {
            var matrix = this.weights[k];
            var toNodes = this.nodes[k];
            biases = this.biases[k];
            for (var j = 0; j < toNodes.Length; j++)
            {
                toNodes[j] = 0;
                for (var i = 0; i < fromNodes.Length; i++)
                    toNodes[j] += matrix[i, j] * fromNodes[i];
                toNodes[j] += biases[j, 0];

                toNodes[j] = MathF.Tanh(toNodes[j]);
                //if (toNodes[j] <= 0)
                //    toNodes[j] = alpha * (MathF.Exp(toNodes[j]) - 1);
            }
            fromNodes = toNodes;
        }
        //matrix = weights[^1];
        //toNodes = this.nodes[^1];
        //biases = this.biases[^1];

        //for (var j = 0; j < toNodes.Length; j++)
        //{
        //    toNodes[j] = 0;
        //    for (var i = 0; i < fromNodes.Length; i++)
        //        toNodes[j] += matrix[i, j] * fromNodes[i];
        //    toNodes[j] += biases[j];
        //    toNodes[j] = MathF.Tanh(toNodes[j]);
        //}

        return this.nodes[^1];
    }

    public float[] Train(float[] input, float[] expectedOutput)
    {
        this.FeedForward(input);

        var output = new float[expectedOutput.Length];
        Array.Copy(this.nodes[^1], output, output.Length);

        var maxWeightWidth = 0;
        var maxWeightHeight = 0;
        for (var i = 0; i < this.weights.Length; i++)
        {
            maxWeightWidth = Math.Max(maxWeightWidth, this.weights[i].GetLength(0));
            maxWeightHeight = Math.Max(maxWeightHeight, this.weights[i].GetLength(1));
        }
        var lastWeights1 = new float[maxWeightWidth, maxWeightHeight];
        var lastWeights2 = new float[maxWeightWidth, maxWeightHeight];

        var layerNodes = this.nodes[^1];
        var layerBiases = this.biases[^1];
        var layerInputWeights = this.weights[^1];
        var previousLayerNodes = this.nodes[^2];

        var momentumStep = momentumStepSize * MathF.Sqrt(1 - momentumAccelerationDecayCorrection) / (1 - momentumSpeedDecayCorrection);

        (float Adjustment, float Speed, float Acceletation) ComputeMomentumAdjustment(float lastSpeed, float lastAcceleration, float delta)
        {
            var momentumSpeed = momentumSpeedDecay * lastSpeed + (1 - momentumSpeedDecay) * delta;
            var momentumAcceleration = momentumAccelerationDecay * lastAcceleration + (1 - momentumAccelerationDecay) * delta * delta;

            var momentumAdjustment = momentumStep * momentumSpeed / (MathF.Sqrt(momentumAcceleration) + momentumEpsilon);
            return (momentumAdjustment, momentumSpeed, momentumAcceleration);
        }
        
        for (var outputNodeIndex = 0; outputNodeIndex < layerNodes.Length; outputNodeIndex++) 
        {
            var rateOfCostInOutput = layerNodes[outputNodeIndex] - expectedOutput[outputNodeIndex];
            var tanH = MathF.Tanh(layerNodes[outputNodeIndex]);
            var rateOfOutputInInput = 1 - tanH * tanH;
            var delta = rateOfCostInOutput * rateOfOutputInInput;

            var momentum = ComputeMomentumAdjustment(layerBiases[outputNodeIndex, 1], layerBiases[outputNodeIndex, 2], delta);
            
            for (var lastHiddenLayerNodeIndex = 0; lastHiddenLayerNodeIndex < layerInputWeights.GetLength(0); lastHiddenLayerNodeIndex++)
            {
                var weight = layerInputWeights[lastHiddenLayerNodeIndex, outputNodeIndex];
                lastWeights1[lastHiddenLayerNodeIndex, outputNodeIndex] = weight;
                layerInputWeights[lastHiddenLayerNodeIndex, outputNodeIndex] = weight - (previousLayerNodes[lastHiddenLayerNodeIndex] * delta * gamma - momentum.Adjustment);
            }
            layerNodes[outputNodeIndex] = delta; // used for backprop
            layerBiases[outputNodeIndex, 0] -= delta * gammaBias - momentum.Adjustment;
            layerBiases[outputNodeIndex, 1] = momentum.Speed;
            layerBiases[outputNodeIndex, 2] = momentum.Acceletation;
        }

        for (var layerIndex = this.nodes.GetLength(0) - 2; layerIndex >= 0; layerIndex--)
        {
            var nextLayerNodes = layerNodes;
            layerNodes = previousLayerNodes;
            layerBiases = this.biases[layerIndex];
            layerInputWeights = this.weights[layerIndex];
            previousLayerNodes = layerIndex > 0 ? this.nodes[layerIndex - 1] : input;
            for (var hiddenNodeIndex = 0; hiddenNodeIndex < layerNodes.Length; hiddenNodeIndex++) 
            {
                var weightedSumOfNextNodeDeltas = 0f;
                for (var nextLayerIndex = 0; nextLayerIndex < nextLayerNodes.Length; nextLayerIndex++)
                    weightedSumOfNextNodeDeltas += nextLayerNodes[nextLayerIndex] * lastWeights1[hiddenNodeIndex, nextLayerIndex];
                var nodeInput = layerNodes[hiddenNodeIndex];
                //var derivativeOfActivation = nodeInput >= 0 ? 1 : alpha * MathF.Exp(nodeInput);
                var tanH = MathF.Tanh(nodeInput);
                var derivativeOfActivation = 1 - tanH * tanH;
                var delta = weightedSumOfNextNodeDeltas * derivativeOfActivation;

                var momentum = ComputeMomentumAdjustment(layerBiases[hiddenNodeIndex, 1], layerBiases[hiddenNodeIndex, 2], delta);


                for (var lastHiddenLayerNodeIndex = 0; lastHiddenLayerNodeIndex < layerInputWeights.GetLength(0); lastHiddenLayerNodeIndex++)
                {
                    var weight = layerInputWeights[lastHiddenLayerNodeIndex, hiddenNodeIndex];
                    lastWeights2[lastHiddenLayerNodeIndex, hiddenNodeIndex] = weight;
                    layerInputWeights[lastHiddenLayerNodeIndex, hiddenNodeIndex] = weight - (previousLayerNodes[lastHiddenLayerNodeIndex] * delta * gamma - momentum.Adjustment);
                }
                layerNodes[hiddenNodeIndex] = delta; // used for backprop
                layerBiases[hiddenNodeIndex, 0] -= delta * gammaBias - momentum.Adjustment;
                layerBiases[hiddenNodeIndex, 1] = momentum.Speed;
                layerBiases[hiddenNodeIndex, 2] = momentum.Acceletation;
            }
            (lastWeights1 , lastWeights2) = (lastWeights2, lastWeights1);           
        }
        momentumSpeedDecayCorrection *= momentumSpeedDecay;
        momentumAccelerationDecayCorrection *= momentumAccelerationDecay;
        return output;
    }
}