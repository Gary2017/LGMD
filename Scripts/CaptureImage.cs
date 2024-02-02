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
    /// �������ͼ
    /// </summary>
    /// <param name="camera">Camera.Ҫ�����������</param>
    /// <param name="rect">Rect.����������</param>
    /// <returns>The screenshot2.</returns>
    void CaptureCamera(Camera camera, Rect rect, int count)
    {
        RenderTexture rt = new RenderTexture((int)rect.width, (int)rect.height, 0);//����һ��RenderTexture����
        camera.targetTexture = rt;//��ʱ������������targetTextureΪrt, ���ֶ���Ⱦ������
        camera.Render();
        //ps: --- ����������ϵڶ������������ʵ��ֻ��ͼĳ����ָ�������һ�𿴵���ͼ��
        //ps: camera2.targetTexture = rt;
        //ps: camera2.Render();
        //ps: -------------------------------------------------------------------

        RenderTexture.active = rt;//�������rt, �������ж�ȡ���ء�
        Texture2D screenShot = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(rect, 0, 0);//ע�����ʱ�����Ǵ�RenderTexture.active�ж�ȡ����
        screenShot.Apply();

        //������ز�������ʹ��camera��������Ļ����ʾ
        camera.targetTexture = null;
        //ps: camera2.targetTexture = null;
        RenderTexture.active = null; //JC: added to avoid errors
        GameObject.Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();//�����Щ�������ݣ���һ��pngͼƬ�ļ�
        string filename = Application.dataPath + "/Screenshot" + count.ToString() + ".png";
        System.IO.File.WriteAllBytes(filename, bytes);
        Debug.Log(string.Format("������һ����Ƭ: {0}", filename));
    }

}
