using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
/// <summary>
/// 神经网络
/// </summary>
public class NeuralNetwork
{
    public List<NeuralLayer> neuralLayerList = new List<NeuralLayer>();
    private List<double> weightList = new List<double>();//所有神经元的权重的集合【将作为个体的基因】
    public List<double> WeightList
    {
        get
        {
            return weightList;
        }
        set
        {
            LoadWeight(value);
        }
    }
    private List<double> outlist = new List<double>();//计算好的返回值
    public int bodyinfo;

    /// <summary>
    /// 初始化神经网络的时候就要指定好层数和对应神经元个数
    /// </summary>
    public NeuralNetwork(int[] bodyinfo)  // bodyinfo:每层的神经元个数
    {
        int layerCount = bodyinfo.Length;
        for (int i = 0; i < bodyinfo.Length; i++)  // bodyinfo.Length代表神经网络的层数
        {
            NeuralLayer neuralLayer = new NeuralLayer(i, bodyinfo);
            neuralLayerList.Add(neuralLayer);
        }
        //将所有神经元的权重集合加入到列表//
        foreach (var layer in neuralLayerList)
        {
            foreach (var neural in layer.neuralList)
            {
                if (neural.weights == null) continue;
                foreach (var weight in neural.weights)
                {
                    weightList.Add(weight);
                }
            }
        }
    }
    public void Foresh(double[] d)
    {
        string str = "";
        foreach (var item in d)
        {
            str += item.ToString() + ",";

        }
        Debug.LogError("权重s:" + str);//

    }
    //给所有神经细胞按照保存好权重load赋值
    public void LoadWeight(double[] weightList)  // weightList为一维列表
    {
        double[] temp = (double[])weightList.Clone();
        this.weightList.Clear();
        int index = 0;
        for (int i = 0; i < neuralLayerList.Count - 1; i++)  // 输出层 没有权重 故减1
        {
            foreach (var neural in neuralLayerList[i].neuralList)
            {
                for (int j = 0; j < neural.weights.Length; j++)
                {
                    neural.weights[j] = temp[index];
                    this.weightList.Add(temp[index]);
                    index++;
                }
            }
        }
    }

    //给所有隐藏层神经细胞按照保存好偏置load赋值
    public void LoadBias(double[] biasList)
    {
        int index = 0;
        for (int i = 1; i < neuralLayerList.Count; i++)  // 隐藏层 没有权重 故减1
        {
            foreach (var neural in neuralLayerList[i].neuralList)
            {
                neural.bias = biasList[index];
                index++;
            }
        }
    }


    //给所有神经细胞按照保存好权重load赋值
    public void LoadWeight(List<double> weightList)
    {

        double[] temp = (double[])weightList.ToArray().Clone();
        this.weightList.Clear();
        int index = 0;
        for (int i = 0; i < neuralLayerList.Count - 1; i++)//输出层 没有权重
        {
            foreach (var neural in neuralLayerList[i].neuralList)
            {
                for (int j = 0; j < neural.weights.Length; j++)
                {
                    neural.weights[j] = temp[index];
                    this.weightList.Add(temp[index]);
                    index++;
                }
            }
        }
    }

    /// <summary>
    /// 输入，然后计算返回值出去
    /// </summary>
    public double[] Pushout(double[] inputs)  // 出口函数
    {
        return Calculate(inputs).ToArray();
    }
    //重置每个神经元自己的值
    private void ResetNeuralValue()
    {
        foreach (var layer in neuralLayerList)
        {
            foreach (var neural in layer.neuralList)
            {
                neural.value = 0;
            }
        }
    }

    //各种加权求和
    private List<double> Calculate(double[] inputs)
    {
        // ResetNeuralValue();//每个神经元自己的值都重置为0
        outlist.Clear();
        ///输入层
        //foreach (var input in inputs)
        //{
        //    foreach (var neural in neuralLayerList[0].neuralList)//第一层[输入层]神经元赋值
        //    {
        //        Debug.Log(input);
        //        neural.value = input;
        //    }
        //}
        for (int i = 0; i < inputs.Length; i++)
        {
            neuralLayerList[0].neuralList[i].value = inputs[i];
        }
        ///隐藏层
        for (int i = 1; i < neuralLayerList.Count - 1; i++)//一层里面 有一竖行神经元 
        {

            NeuralLayer layer = neuralLayerList[i];//本神经元层
            for (int k = 0; k < layer.neuralList.Count; k++)//索引k代表是本层第一个神经元
            {
                Neural neural = layer.neuralList[k];
                neural.value = 0;
                for (int j = 0; j < neuralLayerList[i - 1].neuralList.Count; j++)//本层上一次
                {
                    Neural frontneural = neuralLayerList[i - 1].neuralList[j];
                    neural.value += frontneural.value * frontneural.weights[k];
                }
                neural.value += neural.bias;
                neural.value = ActivationFunc(neural.value);
            }
        }

        ///输出层
        for (int i = 0; i < neuralLayerList[neuralLayerList.Count - 1].neuralList.Count; i++)//输出层
        {
            Neural outneural = neuralLayerList[neuralLayerList.Count - 1].neuralList[i];
            outneural.value = 0;
            foreach (var hideneural in neuralLayerList[neuralLayerList.Count - 2].neuralList)//隐藏层最后一层
            {
                outneural.value += ActivationFunc(hideneural.value) * hideneural.weights[i];
            }
            outneural.value += outneural.bias;
            double value = outneural.value;
            // double value = ActivationFunc(outneural.value);//输入和输出层 通常不需要激活函数
            outlist.Add(value);
        }
        return outlist;
    }
    //激活函数
    private double ActivationFunc(double x)
    {
        return ReLuFunction(x);
    }
    //y=1/(1+e^-x)//值->0-1
    private double ReLuFunction(double x)
    {
        return x > 0 ? x : 0;
    }
    //激活函数[返回值在-1，1]
    //y=sinh(x)/cosh(x)=(e^x - e^-x)/(e^x + e^-x)tanh函数
    private double TanhFunction(double x)
    {
        return (double)Math.Tanh(x);
    }

}
/// <summary>
/// 神经层
/// </summary>
public class NeuralLayer
{
    public NeuralLayer(int i, int[] bodyinfo)  // i: 层数编号  bodyinfo:每层的神经元个数
    {
        for (int j = 0; j < bodyinfo[i]; j++)
        {
            Neural neural = new Neural(i, bodyinfo);
            neuralList.Add(neural);
        }
    }
    public List<Neural> neuralList = new List<Neural>();
}
/// <summary>
/// 神经元
/// </summary>
public class Neural
{
    public Neural(int i, int[] bodyinfo)
    {
        //最后一层[输出层]是没有染色体的
        if (i + 1 >= bodyinfo.Length) return;
        this.weights = new double[bodyinfo[i + 1]];  //一个神经元的权重数=后一层的神经元数量
        RandomWeights();
    }
    public double value;
    public double bias;
    public double[] weights;//权重
    private void RandomWeights()//随机一个权重|有数据的话 外面再重新赋值
    {
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = UnityEngine.Random.Range(-1f, 1f);
        }
    }
}