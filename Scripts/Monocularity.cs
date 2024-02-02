using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Monocularity : MonoBehaviour
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

    private LGMDx Locust_left;
    private LGMDx Locust_right;



    // Start is called before the first frame update
    void Start()
    {
        globalFrames = 0;
        Locust_left = new LGMDx(c_width, c_height, 0, (int)(c_width * 0.6f), 25);
        Locust_right = new LGMDx(c_width, c_height, (int)(c_width * 0.4f), c_width, 25);
    }


    void FixedUpdate()
    {
        ReadFromCamera(LocustEye, ref Locust_left.seen);
        Locust_left.LGMDxProcessing(globalFrames + 1);

        ReadFromCamera(LocustEye, ref Locust_right.seen);
        Locust_right.LGMDxProcessing(globalFrames + 1);

        if (Locust_left.collision == 1 && Locust_right.collision == 0)
            Debug.Log("Monocularity: Turn right!");
        if (Locust_left.collision == 0 && Locust_right.collision == 1)
            Debug.Log("Monocularity: Turn left!");

        globalFrames++;

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
        protected int width_start;     // width of input frame
        protected int width_end;     // width of input frame
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
        protected float[] FFI;              // feed forward inhibition in time series
        protected float[,,] EXC;         // excitations
        protected float[,] INH;          // inhibitions
        protected float[,] S;            // summation cell
        protected float[,] G_CELLS;      // grouping cells 
        protected float[] NMP;              // normalized membrane potential
        protected byte spike;               // spiking 1 or not spiking 0
        public byte collision;              // collision detection true (1) or false (0)
        protected float peak;


        #endregion


        #region CONSTRUCTOR
        public LGMDx(int width, int height, int width_start, int width_end, int fps)
        {
            this.width = width;
            this.height = height;
            this.width_start = width_start;
            this.width_end = width_end;
            Ncell = width * height;
            Np = 1;
            Nsp = 0;
            Nts = 4;
            Wi = 0.3f;
            Beta = 1f;  // 约小膜电位越高
            Ts = 0.7f;
            Tffi = 20;
            Lambda = 50;
            dc = 0.1f;

            peak = 0;

            // convolving matrix init
            W_I = new float[2 * Np + 1, 2 * Np + 1];
            W_G = new float[2 * Np + 1, 2 * Np + 1];

            LocalInhKernel(ref W_I);
            GroupKernel(ref W_G);
            //layers inits
            seen = new float[height, width, 2];
            Photoreceptor = new float[height, width, 2];
            EXC = new float[height, width, 2];
            INH = new float[height, width];
            S = new float[height, width];
            G_CELLS = new float[height, width];
            FFI = new float[2];
            NMP = new float[2];
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


        protected float ExcABS(float cur_input)
        {
            return cur_input > 0 ? cur_input : -cur_input;
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

        protected float SummationCompute(float excitation, float inh, float wi)
        {
            if (excitation < 0)
                excitation = -excitation;
            if (inh < 0)
                inh = -inh;
            float temp = excitation - inh * wi;
            return temp > 0 ? temp : 0;
        }

        protected float NormalizedSummation(float Scell, float lambda)
        {
            if (Scell >= 1f)
                return (float)(lambda * Math.Log10(Scell));
            return Scell;
        }

        protected float Activation(float summation)
        {
            return (float)(Math.Pow(1 + Math.Exp(-summation * Math.Pow(Ncell, -1)), -1));
        }

        protected byte Spiking(float nmp, float threshold)
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
                for (int x = width_start; x < width_end; x++)
                {
                    Photoreceptor[y, x, cur_frame] = seen[y, x, pre_frame] - seen[y, x, cur_frame];
                    tmp_ffi += Mathf.Abs(Photoreceptor[y, x, pre_frame]);
                }
            }

            FFI[cur_frame] = tmp_ffi / Ncell;
            if (FFI[pre_frame] >= Tffi)
            {
                //Debug.Log("FFI Triggered");
                NMP[cur_frame] = 0;
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = width_start; x < width_end; x++)
                    {
                        EXC[y, x, cur_frame] = Photoreceptor[y, x, cur_frame];

                    }
                }

                float SUM = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = width_start; x < width_end; x++)
                    {
                        INH[y, x] = Convolving(y, x, EXC, W_I, pre_frame);
                        S[y, x] = SummationCompute(EXC[y, x, cur_frame], INH[y, x], Wi);
                        S[y, x] = (S[y, x] >= 15 ? S[y, x] : 0);
                        SUM += S[y, x];
                    }
                }
                NMP[cur_frame] = Activation(SUM);
                //Debug.Log(NMP[cur_frame]);

            }
            spike = Spiking(NMP[cur_frame], Ts);
            // Collision checking
            if (collision == 0)
            {
                collision = CollisionDetection();
                // Debug.Log(collision);
            }

            //AddTxtTextByFileInfo(NMP[cur_frame].ToString() + ' ' + collision.ToString());
        }
        #endregion

    }

}

