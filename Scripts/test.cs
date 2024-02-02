using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{

    //Vector3 speed = new Vector3(0, 0, -1f);
    Vector3 speed = new Vector3(0.5f, 0, 0);

    // Start is called before the first frame update
    void Start()
    {
    }

    void FixedUpdate()
    {
        GetComponent<Transform>().position += speed * Time.fixedDeltaTime;
    }

}
