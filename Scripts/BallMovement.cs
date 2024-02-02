using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallMovement : MonoBehaviour
{
    public Vector3 speed = new Vector3(0.5f, 0, 0);
    //Vector3 speed = new Vector3(0, 0, -0.75f);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        GetComponent<Transform>().position += speed * Time.fixedDeltaTime;
    }
}
