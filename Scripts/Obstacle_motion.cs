using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle_motion : MonoBehaviour
{
    public float range;
    public float speed;
    int direction = 1;
    Vector3 startPostion;
    // Start is called before the first frame update
    void Start()
    {
        startPostion = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.right * Time.deltaTime * speed * direction);
        if (Vector3.Distance(transform.position, startPostion) > range && direction == -1)
        {
            direction = 1;
            transform.Translate(Vector3.right * Time.deltaTime * speed * direction);
        }
        else if (Vector3.Distance(transform.position, startPostion) > range && direction == 1)
        {
            direction = -1;
            transform.Translate(Vector3.right * Time.deltaTime * speed * direction);
        }
    }
}
