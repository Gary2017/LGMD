using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MLPController
{
    public int[] netInfo;
    public double[] weightList;
    public double[] bias;
    public double result;
    NeuralNetwork NN;


    public MLPController()
    {
        netInfo = new int[] { 5, 8, 1 };
        weightList = new double[]{0.8932, -0.3025,  0.0069, -0.1315,  0.0916, -0.3288,  0.0148, -0.0468,
           -0.2978, 0.3513, -0.5355, 0.169, -0.299, -0.1911, 0.2644, -1.0085,
           0.4856, -0.3342,  0.2763, -0.4433, -0.1638,  0.0826, -0.3014, 0.3657,
           -1.0272,  0.2492, -0.0056,  0.0681, -0.3725, -0.177 ,  0.0886, -0.541,
           0.2233, -0.1405,  0.777 ,  0.7158,  0.3077, -0.1043, -0.1023, 0.2551,
           -1.1663, -0.1664,  0.4082,  0.5053,  0.0397,  0.0581, -0.1985,  0.8237};
        bias = new double[]{ 0.0750, 0.0179, 0.4094, 0.5470, -0.4273, 0.1266, -0.2918, 1.2731, 0.1949 };
        NN = new NeuralNetwork(netInfo);
        NN.LoadWeight(weightList);
        NN.LoadBias(bias);
    }

    public double getOutput(double[] inputs)
    {
        return NN.Pushout(inputs)[0];
    }

}
