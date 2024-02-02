using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptureImage : MonoBehaviour
{
    Camera camera1;
    int curFrame;
    public int width, height;
    public int[] frames;
    // Start is called before the first frame update
    void Start()
    {
        camera1 = GetComponent<Camera>();
        curFrame = 0;
    }

    void FixedUpdate()
    {
        curFrame++;
        foreach (int x in frames)
            if (x == curFrame)
                CaptureCamera(camera1, new Rect(0, 0, width, height), x);
        
    }

    /// <summary>
    /// 对相机截图
    /// </summary>
    /// <param name="camera">Camera.要被截屏的相机</param>
    /// <param name="rect">Rect.截屏的区域</param>
    /// <returns>The screenshot2.</returns>
    void CaptureCamera(Camera camera, Rect rect, int count)
    {
        RenderTexture rt = new RenderTexture((int)rect.width, (int)rect.height, 0);//创建一个RenderTexture对象
        camera.targetTexture = rt;//临时设置相关相机的targetTexture为rt, 并手动渲染相关相机
        camera.Render();
        //ps: --- 如果这样加上第二个相机，可以实现只截图某几个指定的相机一起看到的图像。
        //ps: camera2.targetTexture = rt;
        //ps: camera2.Render();
        //ps: -------------------------------------------------------------------

        RenderTexture.active = rt;//激活这个rt, 并从中中读取像素。
        Texture2D screenShot = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(rect, 0, 0);//注：这个时候，它是从RenderTexture.active中读取像素
        screenShot.Apply();

        //重置相关参数，以使用camera继续在屏幕上显示
        camera.targetTexture = null;
        //ps: camera2.targetTexture = null;
        RenderTexture.active = null; //JC: added to avoid errors
        GameObject.Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();//最后将这些纹理数据，成一个png图片文件
        string filename = Application.dataPath + "/Screenshot" + count.ToString() + ".png";
        System.IO.File.WriteAllBytes(filename, bytes);
        Debug.Log(string.Format("截屏了一张照片: {0}", filename));
    }

}
