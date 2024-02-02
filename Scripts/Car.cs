using UnityEngine;
using System;

public class Car : MonoBehaviour
{
    public AxleInfo[] axleInfos;//������Ϣ

    [Header("����")]
    public Vector3 massOfCenter;//����

    [Header("����һЩ����")]
    public float maxSteer = 30;//ת�����
    public float maxMotor = 300;//������ǰ��������
    public float maxBrake = 500000000;//ɲ������

    private void Awake()
    {
        GetComponent<Rigidbody>().centerOfMass = massOfCenter;
    }

    private void Update()
    {
        //ɲ��
        Brake();

        //�������ӵ�λ��
        UpdateWheelTrans();
    }

    private void FixedUpdate()
    {
        Move();
    }

    /// <summary>
    /// �ƶ�
    /// </summary>
    private void Move()
    {
        float motor = maxMotor;// * Input.GetAxis("Vertical");
        float steer = maxSteer * Input.GetAxis("Horizontal");
        foreach (AxleInfo info in axleInfos)
        {
            //����ǰ����ת��
            if (info.motor)
            {
                info.rightWheel_wc.motorTorque = motor;
                info.leftWheel_wc.motorTorque = motor;
            }
            if (info.steer)
            {
                info.rightWheel_wc.steerAngle = steer;
                info.leftWheel_wc.steerAngle = steer;
            }
        }
    }

    /// <summary>
    /// ɲ��
    /// </summary>
    private void Brake()
    {
        float brake = Input.GetKey(KeyCode.Space) ? maxBrake : 0;

        foreach (AxleInfo info in axleInfos)
        {
            if (info.brake)
            {
                info.rightWheel_wc.brakeTorque = brake;
                info.leftWheel_wc.brakeTorque = brake;
            }
        }
    }

    /// <summary>
    /// �������ӵ�λ��
    /// </summary>
    private void UpdateWheelTrans()
    {
        foreach (AxleInfo info in axleInfos)
        {
            Quaternion r_qua;
            Quaternion l_qua;
            Vector3 r_pos;
            Vector3 l_pos;
            info.rightWheel_wc.GetWorldPose(out r_pos, out r_qua);
            info.leftWheel_wc.GetWorldPose(out l_pos, out l_qua);

            info.rightWheel_trans.transform.position = r_pos;
            info.leftWheel_trans.transform.position = l_pos;
            info.rightWheel_trans.rotation = r_qua;
            info.leftWheel_trans.rotation = Quaternion.Euler(l_qua.eulerAngles.x, l_qua.eulerAngles.y + 180, l_qua.eulerAngles.z);
        }
    }
}

/// <summary>
/// ������Ϣ��
/// </summary>
[Serializable]
public class AxleInfo
{
    public WheelCollider rightWheel_wc;//�����ӵ�WheelCollider��ײ��
    public WheelCollider leftWheel_wc;//�����ӵ�WheelCollider��ײ��
    public Transform rightWheel_trans;//�����ӵ�Transform���
    public Transform leftWheel_trans;//�����ӵ�Transform���
    [Header("�Ƿ��ж���")]
    public bool motor;//�Ƿ��ж���������or������
    [Header("�Ƿ��ܹ�����ת��")]
    public bool steer;//�Ƿ��ܹ�����ת��
    [Header("�Ƿ���ɲ������")]
    public bool brake;//�Ƿ���ɲ������
}