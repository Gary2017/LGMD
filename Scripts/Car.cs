using UnityEngine;
using System;

public class Car : MonoBehaviour
{
    public AxleInfo[] axleInfos;//车轮信息

    [Header("重心")]
    public Vector3 massOfCenter;//重心

    [Header("车的一些参数")]
    public float maxSteer = 30;//转弯的力
    public float maxMotor = 300;//动力（前进的力）
    public float maxBrake = 500000000;//刹车的力

    private void Awake()
    {
        GetComponent<Rigidbody>().centerOfMass = massOfCenter;
    }

    private void Update()
    {
        //刹车
        Brake();

        //更新轮子的位置
        UpdateWheelTrans();
    }

    private void FixedUpdate()
    {
        Move();
    }

    /// <summary>
    /// 移动
    /// </summary>
    private void Move()
    {
        float motor = maxMotor;// * Input.GetAxis("Vertical");
        float steer = maxSteer * Input.GetAxis("Horizontal");
        foreach (AxleInfo info in axleInfos)
        {
            //控制前进和转向
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
    /// 刹车
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
    /// 更新轮子的位置
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
/// 车轮信息类
/// </summary>
[Serializable]
public class AxleInfo
{
    public WheelCollider rightWheel_wc;//右轮子的WheelCollider碰撞器
    public WheelCollider leftWheel_wc;//左轮子的WheelCollider碰撞器
    public Transform rightWheel_trans;//右轮子的Transform组件
    public Transform leftWheel_trans;//左轮子的Transform组件
    [Header("是否有动力")]
    public bool motor;//是否有动力（四驱or两驱）
    [Header("是否能够控制转向")]
    public bool steer;//是否能够控制转向
    [Header("是否有刹车动力")]
    public bool brake;//是否有刹车动力
}