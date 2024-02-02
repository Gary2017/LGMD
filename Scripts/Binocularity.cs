using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class Binocularity : MonoBehaviour
{
    // global frames count
    public static int globalFrames;
    public Camera LocustEyeLeft;
    public Camera LocustEyeRight;
    public GameObject Monitor;
    public bool IsMove;
    // camera pixel width
    public int c_width;
    // camera pixel height
    public int c_height;

    public LGMDx Locust_left;
    public LGMDx Locust_right;

    public GameObject target;

    MLPController mlp;  // 双目MLP
    MonocularMLPController m_mlp;  // 单目MLP

    float speed;
    bool avoidState;

    // file operation
    string path;
    StreamWriter sw;
    FileInfo fi;


    // Start is called before the first frame update
    void Start()
    {
        globalFrames = 0;
        Locust_left  = new LGMDx(c_width, c_height, 25);
        Locust_right = new LGMDx(c_width, c_height, 25);

        speed = this.GetComponentInParent<BallMovement>().speed.z;
        mlp = new MLPController();  // 神经网络控制器
        m_mlp = new MonocularMLPController();
        avoidState = false;

        path = Application.dataPath + "/TrainSet.txt";
        fi = new FileInfo(path);

        if (!File.Exists(path))
        {
            sw = fi.CreateText();
        }
        else
        {
            sw = fi.AppendText();   //在原文件后面追加内容      
        }
    }


    void FixedUpdate()
    {
        ReadFromCamera(LocustEyeLeft, ref Locust_left.seen);
        Locust_left.LGMDxProcessing(globalFrames + 1);

        ReadFromCamera(LocustEyeRight, ref Locust_right.seen);
        Locust_right.LGMDxProcessing(globalFrames + 1);


        int cur_frame = (globalFrames + 1) % 2;
        float distance = GetDistance();
        // float distance = target.transform.position.z - transform.position.z;  // 真实距离


        // 检测到碰撞
        if ((Locust_left.collision == 1 || Locust_right.collision == 1) && !avoidState)
        {

            if (Locust_left.Competition_result > Locust_right.Competition_result)
                Debug.Log("Binocularity: Turn right!");
            else
                Debug.Log("Binocularity: Turn left!");

            avoidState = true;  // 表示进入躲避状态
            // 获取双目预测距离
            double[] inputs = { (double)Locust_left.NMP[cur_frame], (double)Locust_right.NMP[cur_frame], (double)Locust_left.FFI[cur_frame], (double)Locust_right.FFI[cur_frame], (double)speed };
            float pred_distance = (float)mlp.getOutput(inputs);

            // 获取单目预测距离
            double[] inputs1 = { (double)Locust_left.NMP[cur_frame], (double)Locust_left.FFI[cur_frame], (double)speed };
            float pred_distance1 = (float)m_mlp.getOutput(inputs1);

            //Debug.Log(distance);
            //Debug.Log(pred_distance1);
            //计算最保守(合适)的速度
            float speed_x = ((pred_distance * 2f) / 1.732f + 0.5f) / (pred_distance / speed);  // 平移最大所需距离 除以 距离碰撞时间


            if (Locust_left.collision == 1)
                GetComponentInParent<BallMovement>().speed.x = speed_x;
            else
                GetComponentInParent<BallMovement>().speed.x = -speed_x;
        }

        // 在转弯状态下已避开障碍物
        if (avoidState && Locust_left.NMP[cur_frame] < 0.5f && Locust_right.NMP[cur_frame] < 0.5f)
        {
            avoidState = false;
            Locust_left.collision = 0;
            Locust_right.collision = 0;
            GetComponentInParent<BallMovement>().speed.x = 0;
        }
        //  sw.WriteLine(Locust_left.NMP[cur_frame].ToString("G3") + ' ' + Locust_right.NMP[cur_frame].ToString("G3"));


        //  获取训练集
        //if (distance <= 0.3f || Locust_left.collision == 1 || Locust_right.collision == 1)  // 距离过短时退出
        //    UnityEditor.EditorApplication.isPlaying = false;

        //if ((Locust_left.NMP[cur_frame] >= 0.5f || Locust_right.NMP[cur_frame] >= 0.5f) && globalFrames >= 2)
        //    sw.WriteLine(Locust_left.NMP[cur_frame].ToString("G4") + ' ' + Locust_right.NMP[cur_frame].ToString("G4") + ' ' + Locust_left.FFI[cur_frame].ToString("G4") + ' ' + Locust_right.FFI[cur_frame].ToString("G4") + ' ' + speed.ToString("G4") + ' ' + distance.ToString("G4"));

        globalFrames++;

    }

    float GetDistance()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        float result1 = 99f;
        float result2 = 99f;
        float result3 = 99f;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.TransformDirection(Vector3.forward), out hit))
        {
            result1 = hit.distance;
        }

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f + Vector3.left * 0.3f, transform.TransformDirection(Vector3.forward), out hit))
        {
            result2 = hit.distance;
        }

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f + Vector3.right * 0.3f, transform.TransformDirection(Vector3.forward), out hit))
        {
            result3 = hit.distance;
        }

        return Math.Min(Math.Min(result1, result2), result3);
    }

    void OnApplicationQuit()
    {
        sw.Close();
        sw.Dispose();
    }


    void ReadFromCamera(Camera ventral_camera, ref float[,,] ventral_image)
    {
        RenderTexture rt = new RenderTexture(c_width, c_height, -1);
        ventral_camera.targetTexture = rt;
        ventral_camera.Render();
        RenderTexture.active = rt;
        Texture2D screenShotLeft = new Texture2D(c_width, c_height, TextureFormat.RGB24, false);
        screenShotLeft.ReadPixels(new Rect(0, 0, c_width, c_height), 0, 0);
        screenShotLeft.Apply();

        for (int y = 1; y < c_height - 1; y++)
        {
            for (int x = 1; x < c_width - 1; x++)
            {
                Color c = screenShotLeft.GetPixel(x, y);
                ventral_image[y, x, globalFrames % 2] = c.grayscale * 255;
                //ventral_image[y, x, globalFrames % time_window] = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            }
        }
        //Monitor.GetComponent<Renderer>().material.mainTexture = screenShotLeft;
        RenderTexture.active = null;
        ventral_camera.targetTexture = null;
        Destroy(screenShotLeft);

        GameObject.Destroy(rt);
    }


    public class LGMDx
    {
        #region LGMD FIELD

        /* PARAMS */
        public float[,,] seen;
        protected int width;     // width of input frame
        protected int height;    // height of input frame
        protected int frames;    // frame number per second
        protected int Ncell;     // number of pixels
        protected int Np;        // radius in convolving matrix normally set to 1
        protected int Nsp;       // number of successive spikes
        protected int Nts;       // successive spikes in time steps indicating collision detection
        protected int Tffi;      // FFI threshold
        protected float dc;      // a proportion in DC Component
        protected float[,] W_I;  // the connection weight matrix of lateral inhibition
        protected float[,] W_G;  // local convolving matrix in grouping layer
        protected float[,] W_P;  // central bias pooling
        protected float Wi;      // inhibitory bias
        protected float Beta;    // the parameter related to the angular size
        protected float Ts;      // spike threshold
        protected float Lambda;  // controls the expected output value of the S cell

        protected float[,,] Photoreceptor;  // Photoreceptors after high-pass filtering
        public float[] FFI;              // feed forward inhibition in time series
        protected float[,,] ON_EXC;         // excitations in ON channel
        protected float[,,] OFF_EXC;        // excitations in off channel
        protected float[,] ON_INH;          // inhibitions in ON channel
        protected float[,] OFF_INH;         // inhibitions in OFF channel
        protected float[,] S_ON;            // ON summation cell
        protected float[,] S_OFF;           // OFF summation cell
        protected float[,] G_CELLS_ON;      // grouping cells (on pathway)
        protected float[,] G_CELLS_OFF;     // grouping cells (off pathway)
        public float Competition_result;
        public float[] NMP;              // normalized membrane potential
        protected float[] SFA;              // SMP after spiking frequency adaptation
        protected float[] DIFF_NMP;         // differences between successive NMPs output to calculate derivative
        protected byte spike;               // spiking 1 or not spiking 0
        public byte collision;              // collision detection true (1) or false (0)
        protected float peak;
        

        #endregion


        #region CONSTRUCTOR
        public LGMDx(int width, int height, int fps)
        {
            this.width = width;
            this.height = height;

            Ncell = width * height;
            Np = 1;
            Nsp = 0;
            Nts = 4;
            Wi = 0.3f;
            Beta = 1f;  // 约小膜电位越高
            Ts = 0.7f;
            Tffi = 15;
            Lambda = 50;
            dc = 0.1f;

            peak = 0;

            // convolving matrix init
            W_I = new float[2 * Np + 1, 2 * Np + 1];
            W_G = new float[2 * Np + 1, 2 * Np + 1];
            W_P = new float[height, width];

            LocalInhKernel(ref W_I);
            GroupKernel(ref W_G);
            CentralBiasKernel(ref W_P);

            //layers inits
            seen = new float[height, width, 2];
            Photoreceptor = new float[height, width, 2];
            ON_EXC = new float[height, width, 2];
            OFF_EXC = new float[height, width, 2];
            ON_INH = new float[height, width];
            OFF_INH = new float[height, width];
            S_ON = new float[height, width];
            S_OFF = new float[height, width];
            G_CELLS_ON = new float[height, width];
            G_CELLS_OFF = new float[height, width];
            FFI = new float[2];
            NMP = new float[2];
            SFA = new float[2];
            DIFF_NMP = new float[2];
            spike = 0;
            collision = 0;
        }
        #endregion

        #region METHODS

        protected void LocalInhKernel(ref float[,] mat)
        {
            for (int i = -1; i < Np + 1; i++)
            {
                for (int j = -1; j < Np + 1; j++)
                {
                    if (i == 0 && j == 0)           // center
                        mat[i + 1, j + 1] = 0;
                    else if (i == 0 || j == 0)      // nearest
                        mat[i + 1, j + 1] = 0.25f;
                    else                            // diagonal
                        mat[i + 1, j + 1] = 0.125f;
                }
            }
        }

        protected void GroupKernel(ref float[,] mat)
        {
            for (int i = 0; i < 2 * Np + 1; i++)
            {
                for (int j = 0; j < 2 * Np + 1; j++)
                {
                    mat[i, j] = 1 / 9f;
                }
            }
        }

        protected void CentralBiasKernel(ref float[,] mat)
        {
            float W = (float)width;
            float H = (float)height;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    mat[i, j] = (float)(1f / Math.Exp(Math.Pow((i - W / 2) / (W / 4), 2) + Math.Pow((j - (H / 2)) / (H / 4), 2)) + 0.5f);
                }
            }
        }

        protected float HalfWaveRectificationAndDC_ON(float pre_output, float cur_input)
        {
            return cur_input > 0 ? cur_input + dc * pre_output : 0;
        }

        protected float HalfWaveRectificationAndDC_OFF(float pre_output, float cur_input)
        {
            return cur_input < 0 ? -cur_input + dc * pre_output : 0;
        }


        protected float Convolving(int x, int y, float[,,] matrix, float[,] kernel, int pre_t)
        {
            float tmp = 0;
            int r, c;
            for (int i = -Np; i < Np + 1; i++)
            {
                r = x + i;
                while (r < 0)
                    r += 1;
                while (r >= height)
                    r -= 1;
                for (int j = -Np; j < Np + 1; j++)
                {
                    c = y + j;
                    while (c < 0)
                        c += 1;
                    while (c >= width)
                        c -= 1;
                    tmp += matrix[r, c, pre_t] * kernel[i + Np, j + Np];
                }
            }
            return tmp;
        }

        protected float G_Convolving(int x, int y, float[,] matrix, float[,] kernel)
        {
            float tmp = 0;
            int r, c;
            for (int i = -Np; i < Np + 1; i++)
            {
                r = x + i;
                while (r < 0)
                    r += 1;
                while (r >= height)
                    r -= 1;
                for (int j = -Np; j < Np + 1; j++)
                {
                    c = y + j;
                    while (c < 0)
                        c += 1;
                    while (c >= width)
                        c -= 1;
                    tmp += matrix[r, c] * kernel[i + Np, j + Np];
                }
            }
            return tmp;
        }

        protected float SummationCompute(float excitation, float inh, float wi)
        {
            float temp = excitation - inh * wi;
            return temp > 0 ? temp : 0;
        }

        protected float NormalizedSummation(float Scell, float lambda)
        {
            if (Scell >= 1f)
                return (float)(lambda * Math.Log10(Scell));
            return Scell;
        }

        protected float Activation(float winner, float beta)
        {
            return (float)(1 - Math.Pow(Math.Exp(Math.Pow(beta, -1) * winner * Math.Pow(Ncell, -1)), -1));
            //return (float)(1 - 1 / Math.Exp(winner / beta / Ncell));
        }

        protected float SFA_Profile(float pre_diff, float cur_diff, float pre_sfa, float cur_mp)
        {
            float tmp_mp;
            if (cur_diff <= 0)  //first-derivative -> negative  (NMP decrease)
            {
                tmp_mp = 1f * pre_sfa + cur_diff;
            }
            else if (cur_diff > 0 && cur_diff <= pre_diff)   //first-derivative -> positive & second-derivative -> negative  (NMP increase slowly)
            {
                //tmp_mp = hp_delay_fast * (pre_sfa + cur_diff);
                tmp_mp = cur_mp;

            }
            else   //second-derivative -> positive  (NMP increase or decrease rapidly)
            {
                tmp_mp = cur_mp;
            }
            if (tmp_mp < 0)
                tmp_mp = 0;
            return tmp_mp;
        }

        protected  byte Spiking(float nmp, float threshold)
        {
            byte spi = (byte)(nmp > threshold ? 1 : 0);
            if (spi == 0)
                Nsp = 0;
            else
                Nsp += spi;
            return spi;
        } 

        protected byte CollisionDetection()
        {
            if (Nsp >= Nts)
                return 1;
            else
                return 0;
        }



        #endregion

        #region LGMD MODEL PROCESSING
        public void LGMDxProcessing(int t)
        {
            int cur_frame = t % 2;
            int pre_frame = (t - 1) % 2;
            float tmp_ffi = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Photoreceptor[y, x, cur_frame] = seen[y, x, pre_frame] - seen[y, x, cur_frame] + (float)(1f / (1f + Math.Exp(1f))) * Photoreceptor[y, x, pre_frame];
                    //Photoreceptor[y, x, cur_frame] = seen[y, x, pre_frame] - seen[y, x, cur_frame];
                    tmp_ffi += Mathf.Abs(Photoreceptor[y, x, pre_frame]);
                    Photoreceptor[y, x, cur_frame] *= W_P[y, x];  // central bias processing
                }
            }

            FFI[cur_frame] = tmp_ffi / Ncell;
            if (FFI[cur_frame] >= Tffi)
            {
                Debug.Log("FFI Triggered");
                NMP[cur_frame] = 0;
                SFA[cur_frame] = 0;
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ON_EXC[y, x, cur_frame] = HalfWaveRectificationAndDC_ON(ON_EXC[y, x, pre_frame], Photoreceptor[y, x, cur_frame]);
                        OFF_EXC[y, x, cur_frame] = HalfWaveRectificationAndDC_OFF(OFF_EXC[y, x, pre_frame], Photoreceptor[y, x, cur_frame]);
                        
                    }
                }
                
                float SUM_ON = 0, SUM_OFF = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ON_INH[y, x] = Convolving(y, x, ON_EXC, W_I, pre_frame);
                        OFF_INH[y, x] = Convolving(y, x, OFF_EXC, W_I, pre_frame);
                        S_OFF[y, x] = SummationCompute(OFF_EXC[y, x, cur_frame], OFF_INH[y, x], Wi);
                        S_ON[y, x] = SummationCompute(ON_EXC[y, x, cur_frame], ON_INH[y, x], Wi);

                        S_OFF[y, x] = NormalizedSummation(S_OFF[y, x], Lambda);
                        S_ON[y, x] = NormalizedSummation(S_ON[y, x], Lambda);
                        //SUM_OFF += S_OFF[y, x];
                        //SUM_ON += S_ON[y, x];
                    }
                }

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        G_CELLS_ON[y, x] = G_Convolving(y, x, S_ON, W_G);
                        G_CELLS_ON[y, x] = (G_CELLS_ON[y, x] >= 30 ? G_CELLS_ON[y, x] : 0);

                        G_CELLS_OFF[y, x] = G_Convolving(y, x, S_OFF, W_G);
                        G_CELLS_OFF[y, x] = (G_CELLS_OFF[y, x] >= 30 ? G_CELLS_OFF[y, x] : 0);

                        //G_CELLS_ON[y, x] = NormalizedSummation(G_CELLS_ON[y, x], Lambda);
                        //G_CELLS_OFF[y, x] = NormalizedSummation(G_CELLS_OFF[y, x], Lambda);

                        SUM_ON += G_CELLS_ON[y, x];
                        SUM_OFF += G_CELLS_OFF[y, x];
                    }
                }

                //float Competition_result = SUM_ON + SUM_OFF;
                float C_max = Mathf.Max(SUM_ON, SUM_OFF), C_min = Mathf.Min(SUM_ON, SUM_OFF);
                Competition_result = C_max + C_min - C_min * C_min / (C_max + 0.01f);
                //Debug.Log(Competition_result);
                NMP[cur_frame] = Activation(Competition_result, Beta);
                //Difference between SMPs
                DIFF_NMP[cur_frame] = NMP[cur_frame] - NMP[pre_frame];
                //Debug.Log(DIFF_NMP[cur_frame]);
                //SFA[cur_frame] = SFA_Profile(DIFF_NMP[pre_frame], DIFF_NMP[cur_frame], SFA[pre_frame], NMP[cur_frame]);

            }
            spike = Spiking(NMP[cur_frame], Ts);
            //Debug.Log(NMP[cur_frame]);
            //if (globalFrames > 5)
            //    peak = Math.Max(peak, NMP[cur_frame]);
            //Debug.Log(peak);
            //Debug.Log(NMP[cur_frame]);
            // Collision checking
            if (collision == 0)
                collision = CollisionDetection();
                //Debug.Log(collision);

            // AddTxtTextByFileInfo(NMP[cur_frame].ToString() + ' ' + collision.ToString());
        }
        #endregion

    }
}
