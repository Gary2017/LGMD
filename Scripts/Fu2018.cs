using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class Fu2018 : MonoBehaviour
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

    public LGMDs Locust;

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
        Locust = new LGMDs(c_width, c_height, 25);
        moveState = true;
        rotateState = false;
    }

    void FixedUpdate()
    {
        ReadFromCamera(LocustEye, ref Locust.seen);

        System.Diagnostics.Stopwatch time = new System.Diagnostics.Stopwatch();
        time.Start();
        Locust.LGMDsCircuitry(globalFrames + 1);
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

    public class LGMDs
    {
        #region LGMD FIELD

        /* PARAMS */
        public float[,,] seen;
        protected int width; //width of input frame
        protected int height; //height of input frame
        protected int frames; //total frame number
        protected int Ncell; //number of cells(pixels)
        protected int Np; //radius in convolving matrix normally set to 1
        protected int Nsp; //number of successive spikes
        protected int Nts; //successive spikes in time steps indicating looming detection
        protected int tau_hp_slow; //time constant in high-pass filter in milliseconds
        protected int tau_hp_fast; //time constant in high-pass filter in milliseconds
        protected int[] tau_lp; //time constant in low-pass filter in milliseconds
        protected int Ts; //local threshold filtering S cells
        protected int Tffi; //FFI threshold
        protected int Ksp; //a scale-efficient in spiking mechanisms
        protected float clip_point; //clip point in ON and OFF rectifiers
        protected float W_on; //weight of ON cell contribution
        protected float W_off; //weight of OFF cell contribution
        protected float W_onoff; //weight of ON and OFF cells co-contribution
        protected float Tsp; //firing threshold to invoke spikes
        protected float sp_step; //firing threshold step
        protected float Tsf; //Spike-frequency adaptation threshold
        protected float W_i_on; //inhibitory bias in ON channels
        protected float W_i_off; //inhibitory bias in OFF channels
        //private float coe_ffi; //a constant to multiply ffi
        protected float dc; //a proportion in DC Component
        protected float coe_sig; //a coefficient in Sigmoid function
        protected float[,] W_l;   //local convolving matrix in ON/OFF pathways for lateral connections
        protected float[,] W_g; //local convolving matrix in grouping layer
        protected float time_interval; //time interval between frames in milliseconds
        protected float hp_delay_slow; //time delay in HPF
        protected float hp_delay_fast; //time delay in HPF
        protected float[] lp_delay; //time delay in LPF

        /* MEMBERS */
        protected float[,,] photoreceptors; //Photoreceptors after high-pass filtering
        protected float[,,] ons; //ON cells
        protected float[,,] offs; //OFF cells
        //private float[,] on_lp; //Delayed information in ON channel
        //private float[,] off_lp; //Delayed information in OFF channel
        protected float[,] on_inh; //inhibitions in ON channel
        protected float[,] off_exc; //excitations in OFF channel
        protected float S_on; //local ON summation cell
        protected float S_off; //local OFF summation cell
        protected float[,] scells; //summation cells
        protected float[,] gcells; //grouping cells
        protected float[] ffi; //feed forward inhibition in time series
        //private float ffe; //feed forward excitation
        protected float[] mp; //membrane potential
        protected float[] smp; //sigmoid membrane potential in time series
        protected float[] sfa; //SMP after spiking frequency adaptation
        protected float[] diff_smp; //differences between successive SMPs output to calculate derivative
        protected byte spike; //spiking 1,2 or not spiking 0
        protected byte collision; //collision detection true (1) or false (0)
        protected float peak;

        //BP filters fields
        protected int sd; //sampling distance
        protected int inh_gauss_width;    //inhibitory gaussian kernel width
        protected int exc_gauss_width;    //excitatory gaussian kernel width
        protected float esd;  //excitatory standard deviation
        protected float isd;  //inhibitory standard deviation
        protected float[,] inh_gauss_kernel;  //inhibitory gaussian kernel
        protected float[,] exc_gauss_kernel;  //excitatory gaussian kernel

        #endregion

        #region LGMD PROPERTY

        /// <summary>
        /// property of photoreceptor layer
        /// </summary>
        public float[,,] PHOTOS
        {
            get { return photoreceptors; }
            set { photoreceptors = value; }
        }
        /// <summary>
        /// property of ON cells
        /// </summary>
        public float[,,] ONs
        {
            get { return ons; }
            set { ons = value; }
        }
        /// <summary>
        /// property of OFF cells
        /// </summary>
        public float[,,] OFFs
        {
            get { return offs; }
            set { offs = value; }
        }
        /*
        public float[,] ON_LP
        {
            get { return on_lp; }
            set { on_lp = value; }
        }
        public float[,] OFF_LP
        {
            get { return off_lp; }
            set { off_lp = value; }
        }
        */
        /// <summary>
        /// property of ON inhibition
        /// </summary>
        public float[,] ON_INH
        {
            get { return on_inh; }
            set { on_inh = value; }
        }
        /// <summary>
        /// property of OFF excitation
        /// </summary>
        public float[,] OFF_EXC
        {
            get { return off_exc; }
            set { off_exc = value; }
        }
        /// <summary>
        /// property of summation cells
        /// </summary>
        public float[,] S_CELLS
        {
            get { return scells; }
            set { scells = value; }
        }
        /// <summary>
        /// property of grouping cells
        /// </summary>
        public float[,] G_CELLS
        {
            get { return gcells; }
            set { gcells = value; }
        }
        /// <summary>
        /// property of FFI value
        /// </summary>
        public float[] FFI
        {
            get { return ffi; }
            set { ffi = value; }
        }
        /*
        public float FFE
        {
            get { return ffe; }
            set { ffe = value; }
        }
        */
        /// <summary>
        /// property of neural membrane potential
        /// </summary>
        public float[] MP
        {
            get { return mp; }
            set { mp = value; }
        }
        /// <summary>
        /// property of sigmoid membrane potential
        /// </summary>
        public float[] SMP
        {
            get { return smp; }
            set { smp = value; }
        }
        /// <summary>
        /// property of spike frequency adaptation
        /// </summary>
        public float[] SFA
        {
            get { return sfa; }
            set { sfa = value; }
        }
        public float[] DIFF_SMP
        {
            get { return diff_smp; }
            set { diff_smp = value; }
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
        /// property of local threshold in S layer
        /// </summary>
        public int TS
        {
            get { return this.Ts; }
            set { this.Ts = value; }
        }
        /// <summary>
        /// property of spiking threshold
        /// </summary>
        public float TSP
        {
            get { return this.Tsp; }
            set { this.Tsp = value; }
        }
        /// <summary>
        /// property of FFI threshold
        /// </summary>
        public int TFFI
        {
            get { return this.Tffi; }
            set { this.Tffi = value; }
        }
        /// <summary>
        /// property of coefficient in sigmoid tranformation
        /// </summary>
        public float COE_SIG
        {
            get { return this.coe_sig; }
            set { this.coe_sig = value; }
        }
        /// <summary>
        /// property of inhibitory bias in ON channels
        /// </summary>
        public float W_I_ON
        {
            get { return this.W_i_on; }
            set { this.W_i_on = value; }
        }
        /// <summary>
        /// property of inhibitory bias in OFF channels
        /// </summary>
        public float W_I_OFF
        {
            get { return this.W_i_off; }
            set { this.W_i_off = value; }
        }
        /// <summary>
        /// property of time constant in highpass
        /// </summary>
        public int TAU_HP
        {
            get { return this.tau_hp_slow; }
            set { this.tau_hp_slow = value; }
        }
        /// <summary>
        /// property of time constant in lowpass
        /// </summary>
        public int TAU_LP
        {
            get { return this.tau_lp[0]; }
            set { this.tau_lp[0] = value; }
        }

        #endregion

        #region CONSTRUCTOR

        /// <summary>
        /// Constructor
        /// </summary>
        public LGMDs()
        { }
        /// <summary>
        /// Parametric Constructor
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="fps"></param>
        public LGMDs(int width /*frame width*/, int height /*frame height*/, int fps /*frames per second*/)
        {
            this.width = width;
            this.height = height;
            dc = 0.1f;
            //gene: W_i_on
            W_i_on = 0.3f;
            //gene: W_i_off
            W_i_off = 0.6f;
#if LGMD2
            W_on = 0;
            W_off = 1;
            W_onoff = 0; 
#else
            W_on = 1f;
            W_off = 1f;
            W_onoff = 0.1f;
#endif
            Ncell = width * height;
            //gene: coe_sig
            coe_sig = 1;
            //gene: Ts
            Ts = 30;
            //gene: Tffi
            Tffi = 15;
            //gene: Tsp
            Tsp = 0.74f;
            sp_step = 0.1f;
            //Tsf = 0.003f;
            Tsf = 0;
            Nsp = 0;
            Nts = 6;
            Np = 1;
            clip_point = 0.1f;
            time_interval = 1000 / fps; //ms
            Ksp = 4;
            peak = 0;
            /*********************attention****************************/
            //temporal parameters
            //gene: tau_hp
            tau_hp_slow = 800; //ms
            tau_hp_fast = 400; //ms
            hp_delay_slow = tau_hp_slow / (time_interval + tau_hp_slow);
            hp_delay_fast = tau_hp_fast / (time_interval + tau_hp_fast);
            //gene: tau_lp
            tau_lp = new int[] { 30, 60, 90 }; //ms
            lp_delay = new float[tau_lp.Length];
            for (int i = 0; i < tau_lp.Length; i++)
            {
                lp_delay[i] = time_interval / (time_interval + tau_lp[i]);
            }
            /*********************attention****************************/
            //convolving matrix init
            this.W_l = new float[2 * Np + 1, 2 * Np + 1];
            this.W_g = new float[2 * Np + 1, 2 * Np + 1];
            localKernel(ref W_l);
            groupKernel(ref W_g);
            //layers inits
            seen = new float[height, width, 2];
            photoreceptors = new float[height, width, 2];
            ons = new float[height, width, 2];
            offs = new float[height, width, 2];
            on_inh = new float[height, width];
            off_exc = new float[height, width];
            scells = new float[height, width];
            gcells = new float[height, width];
            ffi = new float[2];
            mp = new float[2];
            smp = new float[2];
            sfa = new float[2];
            diff_smp = new float[2];
            spike = 0;
            collision = 0;
#if BANDPASS
            sd = 4;
            esd = 0.5f * sd;
            isd = sd;
            //widths are always set to odd numbers
            inh_gauss_width = 2 * sd + 1;
            exc_gauss_width = sd + 1;
            inh_gauss_kernel = new float[inh_gauss_width, inh_gauss_width];
            exc_gauss_kernel = new float[exc_gauss_width, exc_gauss_width];
            MakeGaussian(esd, exc_gauss_width, ref exc_gauss_kernel);
            MakeGaussian(isd, inh_gauss_width, ref inh_gauss_kernel);
#endif

            
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Make gauss filter kernel
        /// </summary>
        /// <param name="sigma"></param>
        /// <param name="gauss_width"></param>
        /// <param name="gauss_kernel"></param>
        public void MakeGaussian(float sigma, int gauss_width, ref float[,] gauss_kernel)
        {
            //coordinate of center point
            int center_x = gauss_width / 2;
            int center_y = gauss_width / 2;

            float gaussian; //record different gaussian value according to different sigma
            float distance;
            float sum = 0;

            for (int i = 0; i < gauss_width; i++)
            {
                for (int j = 0; j < gauss_width; j++)
                {
                    distance = (center_x - i) * (center_x - i) + (center_y - j) * (center_y - j);
                    gaussian = (float)(Math.Exp((0 - distance) / (2 * sigma * sigma)) / (2 * Math.PI * sigma * sigma));
                    sum += gaussian;
                    gauss_kernel[i, j] = gaussian;
                }
            }

            for (int i = 0; i < gauss_width; i++)
            {
                for (int j = 0; j < gauss_width; j++)
                {
                    gauss_kernel[i, j] /= sum;
                }
            }
        }
        /// <summary>
        /// Gaussian convolution
        /// </summary>
        /// <param name="input"></param>
        /// <param name="gauss_width"></param>
        /// <param name="gauss_kernel"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        protected int[,] Average_Filter(int[,,] input, int gauss_width, float[,] gauss_kernel, int t)
        {
            int[,] output = new int[height, width];
            int tmp = 0;
            //kernel radius
            int k_radius = gauss_width / 2;
            int r, c;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int i = -k_radius; i < k_radius + 1; i++)
                    {
                        for (int j = -k_radius; j < k_radius + 1; j++)
                        {
                            r = y + i;
                            c = x + j;
                            //if exceeding range, let it equal to near pixel
                            while (r < 0)
                            { r++; }
                            while (r >= height)
                            { r--; }
                            while (c < 0)
                            { c++; }
                            while (c >= width)
                            { c--; }
                            //************************
                            tmp = (int)(input[r, c, t] * gauss_kernel[i + k_radius, j + k_radius]);
                            output[y, x] += tmp;
                        }
                    }
                }
            }
            return output;
        }
        /// <summary>
        /// Subtraction of DoGs with polarity selectivity
        /// </summary>
        /// <param name="first_image"></param>
        /// <param name="second_image"></param>
        /// <returns></returns>
        protected int[,] Sub_Images(int[,] first_image, int[,] second_image)
        {
            int[,] result_image = new int[height, width];

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    result_image[i, j] = first_image[i, j] - second_image[i, j];

                    if (first_image[i, j] >= 0 && second_image[i, j] >= 0)
                    {
                        result_image[i, j] = Math.Abs(result_image[i, j]);
                    }
                    else if (first_image[i, j] <= 0 && second_image[i, j] <= 0)
                    {
                        if (result_image[i, j] > 0)
                            result_image[i, j] = -result_image[i, j];
                    }

                }
            }
            return result_image;
        }
        /// <summary>
        /// Difference of Gaussians algorithm
        /// </summary>
        /// <param name="input_image"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        protected int[,] DOG(int[,,] input_image, int t)
        {
            //MakeGaussian(Esd, exc_gauss_width, ref exc_gauss_kernel);
            //MakeGaussian(Isd, inh_gauss_width, ref inh_gauss_kernel);
            //first convolution
            int[,] tmp1 = Average_Filter(input_image, exc_gauss_width, exc_gauss_kernel, t);
            //second convolution
            int[,] tmp2 = Average_Filter(input_image, inh_gauss_width, inh_gauss_kernel, t);
            //difference of gaussians
            return Sub_Images(tmp1, tmp2);
        }
        /// <summary>
        /// Make convolution kenerl in the dual-pathways
        /// </summary>
        /// <param name="mat"></param>
        protected void localKernel(ref float[,] mat)
        {
            for (int i = -1; i < Np + 1; i++)
            {
                for (int j = -1; j < Np + 1; j++)
                {
                    if (i == 0 && j == 0)   //centre
                        mat[i + 1, j + 1] = 0;
                    else if (i == 0 || j == 0)  //nearest
                        mat[i + 1, j + 1] = 0.25f;
                    else   //diagonal
                        mat[i + 1, j + 1] = 0.125f;
                }
            }
        }
        /// <summary>
        /// Make convolution kernel in the grouping layer
        /// </summary>
        /// <param name="mat"></param>
        protected void groupKernel(ref float[,] mat)
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
        /// The simple version of first-order high-pass filter with no residual visual information
        /// </summary>
        /// <param name="pre_input"></param>
        /// <param name="cur_input"></param>
        /// <returns></returns>
        protected float HighpassFilter(float pre_input, float cur_input)
        {
            return cur_input - pre_input;
        }
        /// <summary>
        /// Spike-Frequency Adaptation (SFA) mechanism
        /// </summary>
        /// <param name="pre_sfa"></param>
        /// <param name="pre_mp"></param>
        /// <param name="cur_mp"></param>
        /// <returns></returns>
        protected float SFA_HPF(float pre_sfa, float pre_mp, float cur_mp)
        {
            float diff_mp = cur_mp - pre_mp;
            if (diff_mp <= Tsf)
            {
                float tmp_mp = hp_delay_slow * (pre_sfa + diff_mp);
                if (tmp_mp < 0.5f)
                    return 0.5f;
                else
                    return tmp_mp;
            }
            else
            {
                float tmp_mp = hp_delay_slow * cur_mp;
                if (tmp_mp < 0.5f)
                    return 0.5f;
                else
                    return tmp_mp;
            }
        }
        /// <summary>
        /// Selective SFA depending on the changing profile of membrane potential
        /// </summary>
        /// <param name="pre_diff"></param>
        /// <param name="cur_diff"></param>
        /// <param name="pre_sfa"></param>
        /// <param name="cur_mp"></param>
        /// <returns></returns>
        protected float SFA_Profile(float pre_diff, float cur_diff, float pre_sfa, float cur_mp)
        {
            float tmp_mp;
            if (cur_diff <= Tsf)  //first-derivative -> negative -> fast adaptation
            {
                tmp_mp = hp_delay_fast * (pre_sfa + cur_diff);
            }
            else if (cur_diff > Tsf && cur_diff <= pre_diff)   //first-derivative -> positive & second-derivative -> negative -> little adaptation
            {
                //tmp_mp = hp_delay_fast * (pre_sfa + cur_diff);
                tmp_mp = hp_delay_slow * cur_mp;

            }
            else   //second-derivative -> positive -> least adaptation
            {
                tmp_mp = hp_delay_slow * cur_mp;
            }
            if (tmp_mp < 0.5f)
                tmp_mp = 0.5f;
            return tmp_mp;
        }

        /// <summary>
        /// Half-wave rectifying in terms of onset response
        /// </summary>
        /// <param name="pre_output"></param>
        /// <param name="cur_input"></param>
        /// <returns></returns>
        protected float HRplusDC_ON(float pre_output, float cur_input)
        {
            if (cur_input >= clip_point)
                return cur_input + dc * pre_output;
            else
                return dc * pre_output;
        }

        /// <summary>
        /// Half-wave rectifying in terms of offset response
        /// </summary>
        /// <param name="pre_output"></param>
        /// <param name="cur_input"></param>
        /// <returns></returns>
        protected float HRplusDC_OFF(float pre_output, float cur_input)
        {
            if (cur_input < clip_point)
                return Math.Abs(cur_input) + dc * pre_output;
            else
                return dc * pre_output;
        }
        /// <summary>
        /// The first-order low-pass filter
        /// </summary>
        /// <param name="cur_input"></param>
        /// <param name="pre_input"></param>
        /// <param name="lp_t"></param>
        /// <returns></returns>
        protected float LowpassFilter(float cur_input, float pre_input, float lp_t)
        {
            return lp_t * cur_input + (1 - lp_t) * pre_input;
        }
        /// <summary>
        /// Convolution
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="matrix"></param>
        /// <param name="kernel"></param>
        /// <returns></returns>
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
        /// Convolution with temporal information
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="inputMatrix"></param>
        /// <param name="kernel"></param>
        /// <param name="cur_frame"></param>
        /// <param name="pre_frame"></param>
        /// <returns></returns>
        protected float Convolve_ST(int x, int y, float[,,] inputMatrix, float[,] kernel, int cur_frame, int pre_frame)
        {
            float tmp = 0;
            int r, c;
            float lp;
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
                    if (i == 0 && j == 0)
                        lp = 0;
                    else if (i == 0 || j == 0)
                        lp = LowpassFilter(inputMatrix[r, c, cur_frame], inputMatrix[r, c, pre_frame], lp_delay[0]);
                    else
                        lp = LowpassFilter(inputMatrix[r, c, cur_frame], inputMatrix[r, c, pre_frame], lp_delay[1]);
                    tmp += lp * kernel[i + Np, j + Np];
                }
            }
            return tmp;
        }
        /// <summary>
        /// Local summation of ON cells
        /// </summary>
        /// <param name="exc"></param>
        /// <param name="inh"></param>
        /// <returns></returns>
        protected float Scell_ON(float exc, float inh)
        {
            return exc - W_i_on * inh;
        }
        /// <summary>
        /// Local summation of OFF cells
        /// </summary>
        /// <param name="exc"></param>
        /// <param name="inh"></param>
        /// <returns></returns>
        protected float Scell_OFF(float exc, float inh)
        {
            return exc - W_i_off * inh;
        }
        /// <summary>
        /// Local interactions of excitations of ON and OFF cells
        /// </summary>
        /// <param name="on_exc"></param>
        /// <param name="off_exc"></param>
        /// <returns></returns>
        protected float SupralinearSummation(float on_exc, float off_exc)
        {
            float tmp = W_on * on_exc + W_off * off_exc + W_onoff * on_exc * off_exc;
            if (tmp >= Ts)
                return tmp;
            else
                return 0;
        }
        /// <summary>
        /// Feed-forward inhibition pathway processing
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        protected float Calc_FFI(int[,,] matrix, int t)
        {
            float tmp = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    tmp += Math.Abs(matrix[i, j, t]);
                }
            }
            return tmp / Ncell;
        }
        /// <summary>
        /// Logarithmic membrane potential calculation
        /// </summary>
        /// <param name="ffe"></param>
        /// <param name="ffi"></param>
        /// <returns></returns>
        protected float Calc_MP(float ffe, float ffi)
        {
            if (ffe == 0 || ffi == 0)
                return 0;
            else
                return (float)(Math.Log(ffe) - Math.Log(/*coe_ffi **/ ffi));
            //return ffe / (coe_ffi * ffi);
        }
        /// <summary>
        /// Sigmoid transformation of membrane potential
        /// </summary>
        /// <param name="Kf"></param>
        /// <returns></returns>
        protected float SigmoidTransfer(float Kf)
        {
            return (float)Math.Pow(1 + Math.Exp(-Kf * Math.Pow(Ncell * coe_sig, -1)), -1);
        }
        /// <summary>
        /// Spiking mechanism
        /// </summary>
        /// <param name="smp"></param>
        /// <returns></returns>
        protected byte SpikingMechanism(float mp)
        {
            if (mp < Tsp)
            {
                Nsp = 0;
                return 0;
            }
            else if (mp >= Tsp && mp < Tsp + sp_step)
            {
                Nsp++;
                return 1;
            }
            else
            {
                Nsp += 2;
                return 2;
            }
        }
        /// <summary>
        /// Spiking mechanism via exponentially mapping
        /// </summary>
        /// <param name="sfa"></param>
        /// <returns></returns>
        protected byte Spiking(float sfa)
        {
            byte spi = (byte)Math.Floor(Math.Exp(Ksp * (sfa - Tsp)));
            if (spi == 0)
                Nsp = 0;
            else
                Nsp += spi;
            return spi;
        }
        /// <summary>
        /// Detect potential collisions
        /// </summary>
        /// <returns></returns>
        protected byte loomingDetecting()
        {
            if (Nsp >= Nts)
                return 1;
            else
                return 0;
        }

        #endregion

        #region LGMD DUAL-PATHWAYS MODEL PROCESSING
        /// <summary>
        /// The signal processing of general LGMDs neural network
        /// </summary>
        /// <param name="img1">first input image</param>
        /// <param name="img2">second input image</param>
        /// <param name="t">current time point</param>
        public void LGMDsCircuitry(int t)
        {
            int cur_frame = t % 2;
            int pre_frame = (t - 1) % 2;
            float tmp_ffi = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    PHOTOS[y, x, cur_frame] = HighpassFilter(seen[y, x, cur_frame], seen[y, x, pre_frame]);
                    tmp_ffi += Math.Abs(PHOTOS[y, x, cur_frame]);
                }
            }
            FFI[cur_frame] = tmp_ffi / Ncell;
            FFI[cur_frame] = LowpassFilter(FFI[cur_frame], FFI[pre_frame], lp_delay[2]);
            if (FFI[cur_frame] >= Tffi)
            {
                MP[cur_frame] = 0;
                SMP[cur_frame] = 0.5f;
                SFA[cur_frame] = 0.5f;
                //Debug.Log("FFI triggered");
            }
            else
            {
#if BANDPASS
                int[,] tmp_dog = DOG(PHOTOS, cur_frame);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ONs[y, x, cur_frame] = HRplusDC_ON(ONs[y, x, pre_frame], tmp_dog[y, x]);
                        OFFs[y, x, cur_frame] = HRplusDC_OFF(OFFs[y, x, pre_frame], tmp_dog[y, x]);
                    }
                }
                float tmp_sum = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ON_INH[y, x] = Convolve_ST(y, x, ONs, W_l, cur_frame, pre_frame);
                        OFF_EXC[y, x] = Convolve_ST(y, x, OFFs, W_l, cur_frame, pre_frame);
                        S_on = Scell_ON(ONs[y, x, cur_frame], ON_INH[y, x]);
                        S_off = Scell_OFF(OFF_EXC[y, x], OFFs[y, x, cur_frame]);
                        S_CELLS[y, x] = SupralinearSummation(S_on, S_off);
                        tmp_sum += S_CELLS[y, x];
                    }
                }
#else
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ONs[y, x, cur_frame] = HRplusDC_ON(ONs[y, x, pre_frame], PHOTOS[y, x, cur_frame]);
                        OFFs[y, x, cur_frame] = HRplusDC_OFF(OFFs[y, x, pre_frame], PHOTOS[y, x, cur_frame]);
                    }
                }
                float tmp_sum = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ON_INH[y, x] = Convolve_ST(y, x, ONs, W_l, cur_frame, pre_frame);
                        OFF_EXC[y, x] = Convolve_ST(y, x, OFFs, W_l, cur_frame, pre_frame);
                        S_on = Scell_ON(ONs[y, x, cur_frame], ON_INH[y, x]);
                        S_off = Scell_OFF(OFF_EXC[y, x], OFFs[y, x, cur_frame]);
                        S_CELLS[y, x] = SupralinearSummation(S_on, S_off);
                    }
                }
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        G_CELLS[y, x] = Convolving(y, x, S_CELLS, W_g);
                        G_CELLS[y, x] = G_CELLS[y, x] >= 10 ? G_CELLS[y, x] : 0;
                        tmp_sum += G_CELLS[y, x];
                    }
                }
#endif
                MP[cur_frame] = tmp_sum;
                SMP[cur_frame] = SigmoidTransfer(MP[cur_frame]);
                //Difference between SMPs
                DIFF_SMP[cur_frame] = SMP[cur_frame] - SMP[pre_frame];
                //SFA
                SFA[cur_frame] = SFA_Profile(DIFF_SMP[pre_frame], DIFF_SMP[cur_frame], SFA[pre_frame], SMP[cur_frame]);
                //SFA[cur_frame] = SFA_HPF(SFA[pre_frame], SMP[pre_frame], SMP[cur_frame]);
            }
            //if (globalFrames > 5)
            //    peak = Math.Max(peak, SMP[cur_frame]);
            //Debug.Log(peak);
            //Debug.Log(SMP[cur_frame]);
            //SPIKE = SpikingMechanism(SFA[cur_frame]);
            SPIKE = Spiking(SFA[cur_frame]);
            Debug.Log(SFA[cur_frame]);
            //Collision checking
            if (collision == 0)
            {
                collision = loomingDetecting();
                //attention
                /*if (collision == 1)
                    this.activationTiming = t;*/
            }

            AddTxtTextByFileInfo(SMP[cur_frame].ToString() + ' ' + collision.ToString());
            //Console.WriteLine("Frame {0}---FFI: {1:F}---MP: {2:F}---SMP: {3:F}---SFA: {4:F}---Spike: {5}\n Collision: {6}", t, FFI[cur_frame], MP[cur_frame], SMP[cur_frame], SFA[cur_frame], SPIKE, COLLISION);
            //Console.WriteLine("{0} {1:F} {2:F} {3:F} {4:F} {5} {6}", t, MP[cur_frame], SMP[cur_frame], SFA[cur_frame], FFI[cur_frame], SPIKE, COLLISION);

        }

        #endregion

        public void AddTxtTextByFileInfo(string txtText)
        {
            string path = Application.dataPath + "/Fu2018.txt";
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
        string path = Application.dataPath + "/Fu2018.txt";
        FileInfo fi = new FileInfo(path);

        if (File.Exists(path))
        {
            fi.Delete();
        }
    }
}
