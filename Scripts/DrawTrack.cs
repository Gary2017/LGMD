using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawTrack : MonoBehaviour
{
    // 绘制轨迹组件
    public LineRenderer line;
    public List<Vector3> points;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        AddPoints();
    }

    // 绘制轨迹方法
    public void AddPoints()
    {
        Vector3 pt = transform.position + Vector3.up * 0.2f;
        if (points.Count > 0 && (pt - lastPoint).magnitude < 0.1f)
            return;
        if (pt != new Vector3(0, 0, 0))
            points.Add(pt);

        line.positionCount = points.Count;
        if (points.Count > 0)
            line.SetPosition(points.Count - 1, lastPoint);
    }
    public Vector3 lastPoint
    {
        get
        {
            if (points == null)
                return Vector3.zero;
            return (points[points.Count - 1]);
        }
    }
}

