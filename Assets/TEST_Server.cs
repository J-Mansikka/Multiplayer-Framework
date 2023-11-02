using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;

public class TEST_Server : MonoBehaviour
{
    //private UdpConnection connection;
    private UDPAsync connection;
    private UDPAsync directConnection;
    public TMP_Text text;
    public TestiUkkoMove ukko;

    bool createNewConnection;

    public string sendIp;
    public int sendPort;
    public int receivePort;

    int counter;

    void Start()
    {

        //connection = new UdpConnection();
        connection = new UDPAsync();
        connection.StartConnection("", sendPort, receivePort);
    }

    void FixedUpdate()
    {
        if(connection.receivedMessage.Length > 0)
        {
            Debug.Log("printing");
            text.text = text.text + " " + connection.receivedMessage;
            string[] messageParts = connection.receivedMessage.Split(" ");
            

            if (messageParts[0] == "letmein")
            {
                print("Creating new connection to " + messageParts[1] + messageParts[2]);
                directConnection = new UDPAsync();
                directConnection.StartConnection(messageParts[1], int.Parse(messageParts[2]), receivePort);
            }
            

        }

        if(directConnection != null)
        {
            if(directConnection.receivedMessage.Length > 0)
            {
                print("DIRECT CONNECTION RECEIVED " + directConnection.receivedMessage);
                directConnection.receivedMessage = "";
            }

            counter++;
            if(counter > 100)
            {
                counter = 0;
                directConnection.Send("HELLO FROM SERVER!");
            }
        }

        connection.receivedMessage = "";
    }

    void OnDestroy()
    {
        
        if(connection != null) connection.Stop();
    }
}
