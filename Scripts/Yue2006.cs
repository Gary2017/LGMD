using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Yue2006 : MonoBehaviour
{
    // global frames count
    public static int globalFrames;
    public Camera LocustEye;
    public GameObject Monitor;
    public bool IsMove;
    // camera pixel width
    public int c_width;
    // camera pixel height
    public int c_height;
    public float speed;

    public LGMDSingle Locust;

    private bool moveState;  // 移动状态
    private bool rotateState;  // 转向状态
    private Quaternion targetRotation;
    private float timeCount;

    private double timeconsume = 0;

    // Start is called before the first frame update
    void Start()
    {
        DeleteFileInfo();
        globalFrames = 0;
        Locust = new LGMDSingle(c_width, c_height);
        moveState = true;
        rotateState = false;
    }

    void FixedUpdate()
    {
        ReadFromCamera(LocustEye, ref Locust.seen);

        System.Diagnostics.Stopwatch time = new System.Diagnostics.Stopwatch();
        time.Start();
        Locust.LGMDSProcessing(globalFrames + 1);
        time.Stop();
        print("执行 " + time.Elapsed.TotalMilliseconds + " 毫秒");
        timeconsume += time.Elapsed.TotalMilliseconds;

        globalFrames++;

        if (IsMove)
        {
            if (moveState)
            {
                transform.Translate(Vector3.forward * speed * Time.fixedDeltaTime, Space.Self);
            }

            if (moveState && Locust.COLLISION == 1)
            {
                rotateState = true;
                moveState = false;
                Locust.COLLISION = 0;
                timeCount = 0;
                targetRotation = GetRandomTurnAngle(transform.localEulerAngles);
            }

            if (rotateState)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, timeCount);
                timeCount += Time.fixedDeltaTime;
                if (timeCount > 0.98f)
                {
                    moveState = true;
                    rotateState = false;
                    timeCount = 0;
                    Locust.COLLISION = 0;
                }
                // transform.Rotate(Vector3.up);
            }
        }
    }

    void OnApplicationQuit()
    {
        print(timeconsume /= 90);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name != "Plane")
        {
            transform.Rotate(Vector3.up, 180);
            Locust.COLLISION = 0;
            timeCount = 0;
            Debug.Log("Collision occurs!");
        }
    }

    public Quaternion GetRandomTurnAngle(Vector3 curRotation)
    {
        float random_angle = UnityEngine.Random.Range(90f, 150f);
        float leftright = UnityEngine.Random.Range(0, 1f);
        if (leftright < 0.5f)
            random_angle = -random_angle;
        return Quaternion.Euler(0, curRotation.y + random_angle, 0);
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

    public class LGMDSingle
    {
        #region LGMD FIELD

        public float[,,] seen;
        /// <summary>
        ///  G layer convolution mask
        /// </summary>
        protected float[,] We;
        /// <summary>
        /// local inhibition bias
        /// </summary>
        protected float Wi;
        /// <summary>
        /// local threshold in S layer
        /// </summary>
        protected int Ts;
        /// <summary>
        /// spiking threshold
        /// </summary>
        protected float Tsp;
        /// <summary>
        /// inhibiting area radius
        /// </summary>
        protected byte Np;
        /// <summary>
        /// FFI threshold
        /// </summary>
        protected int Tffi;
        /// <summary>
        /// total number of cells in each pre-synaptic layer
        /// </summary>
        protected int Ncell;
        /// <summary>
        /// number of successive spikes
        /// </summary>
        protected byte Nsp;
        /// <summary>
        /// number of successive discrete time steps
        /// </summary>
        protected byte Nts;
        /// <summary>
        /// coefficient in sigmoid transformation
        /// </summary>
        protected float Csig;
        /// <summary>
        /// kernel in convolution of excitations forming inhibitions
        /// </summary>
        protected float[,] WI;
        /// <summary>
        /// photoreceptor layer matrix
        /// </summary>
        protected float[,] Photoreceptors;
        /// <summary>
        /// excitation layer matrix
        /// </summary>
        protected float[,,] Excitations;
        /// <summary>
        /// inhibition layer matrix
        /// </summary>
        protected float[,] Inhibitions;
        /// <summary>
        /// summation layer matrix
        /// </summary>
        protected float[,] Summations;
        /// <summary>
        /// passing coefficient
        /// </summary>
        protected float[,] GroupCe;
        /// <summary>
        /// GroupLaye
        /// </summary>
        protected float[,] GroupLayer;
        /// <summary>
        /// membrane potential
        /// </summary>
        protected float mp;
        /// <summary>
        /// sigmoid membrane potential
        /// </summary>
        protected float smp;
        /// <summary>
        /// FFI value
        /// </summary>
        protected float ffi;
        /// <summary>
        /// spike value - spiking (1) or not (0)
        /// </summary>
        protected byte spike;
        /// <summary>
        /// collision event recognition (1) or not (0)
        /// </summary>
        protected byte collision;
        /// <summary>
        /// input frame width
        /// </summary>
        protected int width;
        /// <summary>
        /// input frame height
        /// </summary>
        protected int height;
        protected float peak;

        #endregion

        #region LGMD PROPERTY

        /// <summary>
        /// property of neural membrane potential
        /// </summary>
        public float MP
        {
            get { return mp; }
            set { mp = value; }
        }

        /// <summary>
        /// property of sigmoid membrane potential
        /// </summary>
        public float SMP
        {
            get { return smp; }
            set { smp = value; }
        }

        /// <summary>
        /// property of spike
        /// </summary>
        public byte SPIKE
        {
            get { return spike; }
            set { spike = value; }
        }

        /// <summary>
        /// property of collision detection
        /// </summary>
        public byte COLLISION
        {
            get { return collision; }
            set { collision = value; }
        }

        /// <summary>
        /// property of photoreceptor layer
        /// </summary>
        public float[,] PHOTOS
        {
            get { return Photoreceptors; }
            set { Photoreceptors = value; }
        }

        /// <summary>
        /// property of FFI value
        /// </summary>
        public float FFI
        {
            get { return ffi; }
            set { ffi = value; }
        }

        /// <summary>
        /// property of excitation layer
        /// </summary>
        public float[,,] EXC
        {
            get { return Excitations; }
            set { Excitations = value; }
        }

        /// <summary>
        /// property of inhibition layer
        /// </summary>
        public float[,] INH
        {
            get { return Inhibitions; }
            set { Inhibitions = value; }
        }

        /// <summary>
        /// property of summation layer
        /// </summary>
        public float[,] SUM
        {
            get { return Summations; }
            set { Summations = value; }
        }

        /// <summary>
        /// property of FFI threshold
        /// </summary>
        public int TFFI
        {
            get { return Tffi; }
            set { Tffi = value; }
        }

        /// <summary>
        /// property of local threshold in S layer
        /// </summary>
        public int TS
        {
            get { return Ts; }
            set { Ts = value; }
        }

        /// <summary>
        /// property of spiking threshold
        /// </summary>
        public float TSP
        {
            get { return Tsp; }
            set { Tsp = value; }
        }

        /// <summary>
        /// property of coefficient in sigmoid transformation
        /// </summary>
        public float COE_SIG
        {
            get { return Csig; }
            set { Csig = value; }
        }

        /// <summary>
        /// property of local inhibition bias
        /// </summary>
        public float W_I
        {
            get { return Wi; }
            set { Wi = value; }
        }

        #endregion

        #region CONSTRUCTOR

        /// <summary>
        /// Default constructor
        /// </summary>
        public LGMDSingle() { }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public LGMDSingle(int width /*frame width*/, int height /*frame height*/)
        {
            this.width = width;
            this.height = height;
            Ncell = width * height;
            seen = new float[height, width, 2];
            Photoreceptors = new float[height, width];
            Excitations = new float[height, width, 2];
            Inhibitions = new float[height, width];
            Summations = new float[height, width];
            GroupCe = new float[height, width];
            GroupLayer = new float[height, width];
            Np = 1;
            WI = new float[2 * Np + 1, 2 * Np + 1];
            We = new float[2 * Np + 1, 2 * Np + 1];
            //gene: Wi
            Wi = 0.6f;
            //gene: Ts
            Ts = 30;
            //gene: Tffi
            Tffi = 10;
            //gene: Tsp
            Tsp = 0.78f;
            Nsp = 0;
            Nts = 5;
            //gene: Csig
            Csig = 1;
            ffi = 0;
            mp = 0;
            smp = 0.5f;
            spike = 0;
            collision = 0;
            peak = 0;
            localIW(WI, Np);
            groupWe(We, Np);

            // Console.WriteLine("LGMD model parameters setting with a single visual processing pathway\n");
        }

        #endregion

        #region METHOD

        /// <summary>
        /// Constructing local convolution kernel
        /// </summary>
        /// <param name="mat"></param>
        private void localIW(float[,] mat, byte np)
        {
            for (int i = -1; i < np + 1; i++)
            {
                for (int j = -1; j < np + 1; j++)
                {
                    if (i == 0 && j == 0)
                        continue;
                    else if (i == 0 || j == 0)
                        mat[i + 1, j + 1] = 0.25f;
                    else
                        mat[i + 1, j + 1] = 0.125f;
                }
            }
        }

        private void groupWe(float[,] mat, byte np)
        {
            for (int i = 0; i < 2 * Np + 1; i++)
            {
                for (int j = 0; j < 2 * Np + 1; j++)
                {
                    mat[i, j] = 1 / 9f;
                }
            }
        }

        /// <summary>
        /// Photoreceptor layer computation at each local cell
        /// </summary>
        /// <param name="pre_input"></param>
        /// <param name="cur_input"></param>
        /// <returns></returns>
        protected float pcellValue(float pre_input, float cur_input)
        {
            return cur_input - pre_input;
        }

        /// <summary>
        /// Spatiotemporal convolution forming inhibitions at each local cell
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="matrix"></param>
        /// <param name="kernel"></param>
        /// <param name="np"></param>
        /// <param name="pre_t"></param>
        /// <returns></returns>
        protected float icellValue(int x, int y, float[,,] matrix, float[,] kernel, int np, int pre_t)
        {
            float tmp = 0;
            int r, c;
            for (int i = -np; i < np + 1; i++)
            {
                r = x + i;
                while (r < 0)
                    r += 1;
                while (r >= height)
                    r -= 1;
                for (int j = -np; j < np + 1; j++)
                {
                    c = y + j;
                    while (c < 0)
                        c += 1;
                    while (c >= width)
                        c -= 1;
                    tmp += matrix[r, c, pre_t] * kernel[i + np, j + np];
                }
            }
            return tmp;
        }

        protected float Convolving(int x, int y, float[,] matrix, float[,] kernel)
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

        /// <summary>
        /// Summation layer computation at each local cell
        /// </summary>
        /// <param name="evalue"></param>
        /// <param name="ivalue"></param>
        /// <param name="wi"></param>
        /// <param name="ts"></param>
        /// <returns></returns>
        protected float scellValue(float evalue, float ivalue, float wi, int ts)
        {
            return Math.Abs(evalue) - Math.Abs(ivalue) * wi;
            //if (evalue * ivalue <= 0)
            //    return 0;
            //else
            //{
            //    float tmpValue = evalue - ivalue * wi;
            //    if (tmpValue >= ts)
            //        return tmpValue;
            //    else
            //        return 0;
            //}
        }

        /// <summary>
        /// Sigmoid transformation
        /// </summary>
        /// <param name="mp"></param>
        /// <param name="ncell"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        protected float Sigmoid(float mp, int ncell, float k)
        {
            return (float)Math.Pow(1 + Math.Exp(-mp * Math.Pow(ncell * k, -1)), -1);
        }

        /// <summary>
        /// Spiking mechanism
        /// </summary>
        /// <param name="smp"></param>
        /// <param name="tsp"></param>
        /// <param name="nsp"></param>
        /// <returns></returns>
        protected byte Spiking(float smp, float tsp, ref byte nsp)
        {
            if (smp >= tsp)
            {
                nsp += 1;
                return 1;
            }
            else
            {
                nsp = 0;
                return 0;
            }
        }

        /// <summary>
        /// Collision detection
        /// </summary>
        /// <param name="nsp"></param>
        /// <param name="nts"></param>
        /// <returns></returns>
        protected byte collisionDetecting(byte nsp, byte nts)
        {
            if (nsp >= nts)
                return 1;
            else
                return 0;
        }

        #endregion

        #region LGMD PROCESSING WITH A SINGLE PATHWAY

        /// <summary>
        /// Integrated visual processing of the LGMD1 mdoel with a single pathway
        /// </summary>
        /// <param name="img1"></param>
        /// <param name="img2"></param>
        /// <param name="t"></param>
        public void LGMDSProcessing(int t)
        {
            int cur_frame = t % 2;
            int pre_frame = (t - 1) % 2;
            float tmp_ffi = 0;
            mp = 0;
            //Photoreceptor and excitation layers processing
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Photoreceptors[y, x] = pcellValue(seen[y, x, cur_frame], seen[y, x, pre_frame]);
                    Excitations[y, x, cur_frame] = Photoreceptors[y, x];
                    tmp_ffi += Math.Abs(Excitations[y, x, pre_frame]);
                }
            }
            //FFI calculation and check
            ffi = tmp_ffi / Ncell;
            if (ffi >= Tffi)
            {
                mp = 0; //min value
            }
            else
            {
                //Inhibition and summation layers processing
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Inhibitions[y, x] = icellValue(y, x, Excitations, WI, Np, pre_frame);
                        Summations[y, x] = scellValue(Excitations[y, x, cur_frame], Inhibitions[y, x], Wi, Ts);
                    }
                }

                float maxCe = -256;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        GroupCe[y, x] = Convolving(y, x, Summations, We);
                        maxCe = Math.Max(maxCe, Math.Abs(GroupCe[y, x]));
                    }
                }

                float weight = (float)Math.Pow(0.01f + maxCe / 4f, -1);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        GroupLayer[y, x] = Summations[y, x] * GroupCe[y, x] * weight;
                        GroupLayer[y, x] = (GroupLayer[y, x] >= Ts ? GroupLayer[y, x] : 0);
                        mp += GroupLayer[y, x];
                    }
                }
                //LGMD1 membrane potential
                smp = Sigmoid(mp, Ncell, Csig);
                //if (globalFrames > 5)
                //    peak = Math.Max(peak, smp);
                //Debug.Log(peak);
                Debug.Log(smp);
                //Spiking
                spike = Spiking(smp, Tsp, ref Nsp);
                //Collision detecting
                if (collision == 0)
                {
                    collision = collisionDetecting(Nsp, Nts);
                }

                //Print to Console
                AddTxtTextByFileInfo(smp.ToString() + ' ' + collision.ToString());
                //Console.WriteLine("{0} {1:F} {2:F} {3:F} {4} {5}", t, mp, smp, ffi, spike, collision);
            }

            #endregion
        }
        public void AddTxtTextByFileInfo(string txtText)
        {
            string path = Application.dataPath + "/Yue2006.txt";
            StreamWriter sw;
            FileInfo fi = new FileInfo(path);

            if (!File.Exists(path))
            {
                sw = fi.CreateText();
            }
            else
            {
                sw = fi.AppendText();   //在原文件后面追加内容      
            }
            sw.WriteLine(txtText);
            sw.Close();
            sw.Dispose();
        }
    }
    public void DeleteFileInfo()
    {
        string path = Application.dataPath + "/Yue2006.txt";
        FileInfo fi = new FileInfo(path);

        if (File.Exists(path))
        {
            fi.Delete();
        }
    }
}
