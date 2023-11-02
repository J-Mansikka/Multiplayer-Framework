using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TEST_Client : MonoBehaviour
{
    private UdpConnection connection;
    public TMP_InputField sendText;

    public string sendIp;

    void Start()
    {
        //string sendIp = "127.0.0.1";
        int sendPort = 51000;
        int receivePort = 11001;

        connection = new UdpConnection();
        connection.StartConnection(sendIp, sendPort, receivePort);
    }

    public void SendMessage()
    {
        connection.Send(sendText.text);
        sendText.text = "";
    }

    void OnDestroy()
    {
        if (connection != null) connection.Stop();
    }
}
