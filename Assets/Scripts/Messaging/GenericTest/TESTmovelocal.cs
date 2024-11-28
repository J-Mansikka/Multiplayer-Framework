using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


public class TESTmovelocal : MonoBehaviour
{
    public TESTmoveremote remote;

    int x = 0;
    int y = 0;
    float timePassed;
    float speed = 5f;

    private void Awake()
    {
        timePassed = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        timePassed += Time.deltaTime;
        //Debug.Log("LOCAL MOVE TIME " + timePassed);


        if (timePassed > ServerSettings.clientSendRate)
        {
            Vector3 move = new Vector3(x, 0f, y) * speed * timePassed;
            transform.Translate(move);
            remote.AddMove(move, timePassed);
            x = 0;
            y = 0;
            if(Input.GetKey(KeyCode.UpArrow)) { y = 1; }
            else if(Input.GetKey(KeyCode.DownArrow)) { y = -1; }

            if (Input.GetKey(KeyCode.LeftArrow)) { x = -1; }
            else if (Input.GetKey(KeyCode.RightArrow)) { x = 1; }
            timePassed -= ServerSettings.clientSendRate;
        }
    }
}
