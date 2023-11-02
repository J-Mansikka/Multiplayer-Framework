using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBed : MonoBehaviour
{
    NewClientListener listener;
    public int listenPort;

    private void Start()
    {
        //listener = new NewClientListener(listenPort);
    }


    private void OnDestroy()
    {
        listener.ShutDown();
    }
}
