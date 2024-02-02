using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TimeController : MonoBehaviour
{
    public int Duration;
    private int curTime;
    // Start is called before the first frame update
    void Start()
    {
        curTime = 0;
    }

    void FixedUpdate()
    {
        curTime++;
        if (Duration == curTime)
            Time.timeScale = 0f;
            //EditorApplication.isPlaying = false;
    }
}
