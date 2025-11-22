using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DEMOreceiver : MonoBehaviour
{
    private DEMOseriObj obj;
    private MnetNetwork ep;

    public MnetNetwork senderEP;
    public bool GET;

    private void Awake()
    {
        obj = GetComponent<DEMOseriObj>();
        ep = GetComponent<MnetNetwork>();
    }

    private void Update()
    {
        if (GET)
        {


        }


    }


}
