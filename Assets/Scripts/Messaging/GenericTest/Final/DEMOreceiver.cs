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
            GET = false;
            senderEP.worldPackets.packetToWriteOn.ClosePacket();
            for (int i = 0; i < 5; i++)
            {
                ep.worldPackets.GetPacket(i).DemoGetData(senderEP.worldPackets.GetPacket(i).bytes);
            }
            ep.DEMOtestTick();

        }


    }


}
