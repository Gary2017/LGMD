using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonocularMLPController
{
    public int[] netInfo;
    public double[] weightList;
    public double[] bias;
    public double result;
    NeuralNetwork NN;


    public MonocularMLPController()
    {
        netInfo = new int[] { 3, 8, 1 };

        weightList = new double[]{0.3529, -0.6771, -0.4883, -0.6743,  0.5797, -0.4706,  0.5984, 1.1739,
        -0.02, -0.0761, 0.1956, -0.1136, 0.562, 0.0468, 0.5208, 0.1301,
        0.7294,  0.3551,  0.0677,  0.7391,  0.7244, -0.2633, -0.2624, 0.0765,
        0.4301,  0.012 , -0.2408,  0.4143,  0.3724, -0.0919, -0.6082, -0.8431};
        bias = new double[] { 0.7310, -0.4520, -0.4868, -0.2210, 0.1113, 0.1582, -0.7156, -0.6182, 0.4424 };  // 参数量等于layer2 + layer3
        NN = new NeuralNetwork(netInfo);
        NN.LoadWeight(weightList);
        NN.LoadBias(bias);
    }

    public double getOutput(double[] inputs)
    {
        return NN.Pushout(inputs)[0];
    }

}
