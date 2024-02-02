using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
/// <summary>
/// ������
/// </summary>
public class NeuralNetwork
{
    public List<NeuralLayer> neuralLayerList = new List<NeuralLayer>();
    private List<double> weightList = new List<double>();//������Ԫ��Ȩ�صļ��ϡ�����Ϊ����Ļ���
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
    private List<double> outlist = new List<double>();//����õķ���ֵ
    public int bodyinfo;

    /// <summary>
    /// ��ʼ���������ʱ���Ҫָ���ò����Ͷ�Ӧ��Ԫ����
    /// </summary>
    public NeuralNetwork(int[] bodyinfo)  // bodyinfo:ÿ�����Ԫ����
    {
        int layerCount = bodyinfo.Length;
        for (int i = 0; i < bodyinfo.Length; i++)  // bodyinfo.Length����������Ĳ���
        {
            NeuralLayer neuralLayer = new NeuralLayer(i, bodyinfo);
            neuralLayerList.Add(neuralLayer);
        }
        //��������Ԫ��Ȩ�ؼ��ϼ��뵽�б�//
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
        Debug.LogError("Ȩ��s:" + str);//

    }
    //��������ϸ�����ձ����Ȩ��load��ֵ
    public void LoadWeight(double[] weightList)  // weightListΪһά�б�
    {
        double[] temp = (double[])weightList.Clone();
        this.weightList.Clear();
        int index = 0;
        for (int i = 0; i < neuralLayerList.Count - 1; i++)  // ����� û��Ȩ�� �ʼ�1
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

    //���������ز���ϸ�����ձ����ƫ��load��ֵ
    public void LoadBias(double[] biasList)
    {
        int index = 0;
        for (int i = 1; i < neuralLayerList.Count; i++)  // ���ز� û��Ȩ�� �ʼ�1
        {
            foreach (var neural in neuralLayerList[i].neuralList)
            {
                neural.bias = biasList[index];
                index++;
            }
        }
    }


    //��������ϸ�����ձ����Ȩ��load��ֵ
    public void LoadWeight(List<double> weightList)
    {

        double[] temp = (double[])weightList.ToArray().Clone();
        this.weightList.Clear();
        int index = 0;
        for (int i = 0; i < neuralLayerList.Count - 1; i++)//����� û��Ȩ��
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
    /// ���룬Ȼ����㷵��ֵ��ȥ
    /// </summary>
    public double[] Pushout(double[] inputs)  // ���ں���
    {
        return Calculate(inputs).ToArray();
    }
    //����ÿ����Ԫ�Լ���ֵ
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

    //���ּ�Ȩ���
    private List<double> Calculate(double[] inputs)
    {
        // ResetNeuralValue();//ÿ����Ԫ�Լ���ֵ������Ϊ0
        outlist.Clear();
        ///�����
        //foreach (var input in inputs)
        //{
        //    foreach (var neural in neuralLayerList[0].neuralList)//��һ��[�����]��Ԫ��ֵ
        //    {
        //        Debug.Log(input);
        //        neural.value = input;
        //    }
        //}
        for (int i = 0; i < inputs.Length; i++)
        {
            neuralLayerList[0].neuralList[i].value = inputs[i];
        }
        ///���ز�
        for (int i = 1; i < neuralLayerList.Count - 1; i++)//һ������ ��һ������Ԫ 
        {

            NeuralLayer layer = neuralLayerList[i];//����Ԫ��
            for (int k = 0; k < layer.neuralList.Count; k++)//����k�����Ǳ����һ����Ԫ
            {
                Neural neural = layer.neuralList[k];
                neural.value = 0;
                for (int j = 0; j < neuralLayerList[i - 1].neuralList.Count; j++)//������һ��
                {
                    Neural frontneural = neuralLayerList[i - 1].neuralList[j];
                    neural.value += frontneural.value * frontneural.weights[k];
                }
                neural.value += neural.bias;
                neural.value = ActivationFunc(neural.value);
            }
        }

        ///�����
        for (int i = 0; i < neuralLayerList[neuralLayerList.Count - 1].neuralList.Count; i++)//�����
        {
            Neural outneural = neuralLayerList[neuralLayerList.Count - 1].neuralList[i];
            outneural.value = 0;
            foreach (var hideneural in neuralLayerList[neuralLayerList.Count - 2].neuralList)//���ز����һ��
            {
                outneural.value += ActivationFunc(hideneural.value) * hideneural.weights[i];
            }
            outneural.value += outneural.bias;
            double value = outneural.value;
            // double value = ActivationFunc(outneural.value);//���������� ͨ������Ҫ�����
            outlist.Add(value);
        }
        return outlist;
    }
    //�����
    private double ActivationFunc(double x)
    {
        return ReLuFunction(x);
    }
    //y=1/(1+e^-x)//ֵ->0-1
    private double ReLuFunction(double x)
    {
        return x > 0 ? x : 0;
    }
    //�����[����ֵ��-1��1]
    //y=sinh(x)/cosh(x)=(e^x - e^-x)/(e^x + e^-x)tanh����
    private double TanhFunction(double x)
    {
        return (double)Math.Tanh(x);
    }

}
/// <summary>
/// �񾭲�
/// </summary>
public class NeuralLayer
{
    public NeuralLayer(int i, int[] bodyinfo)  // i: �������  bodyinfo:ÿ�����Ԫ����
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
/// ��Ԫ
/// </summary>
public class Neural
{
    public Neural(int i, int[] bodyinfo)
    {
        //���һ��[�����]��û��Ⱦɫ���
        if (i + 1 >= bodyinfo.Length) return;
        this.weights = new double[bodyinfo[i + 1]];  //һ����Ԫ��Ȩ����=��һ�����Ԫ����
        RandomWeights();
    }
    public double value;
    public double bias;
    public double[] weights;//Ȩ��
    private void RandomWeights()//���һ��Ȩ��|�����ݵĻ� ���������¸�ֵ
    {
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = UnityEngine.Random.Range(-1f, 1f);
        }
    }
}