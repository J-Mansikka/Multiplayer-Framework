using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Profiling;

public class UDPAsync
{
    private UdpClient client;
    private IPEndPoint ep;

    private readonly Queue<string> incomingQueue = new Queue<string>();
    byte[] curReceivedBytes;
    private string senderIp;
    private int senderPort;
    public string receivedMessage;

    public bool keepListening;

    public void StartConnection(string sendIp, int sendPort, int receivePort)
    {
        this.senderIp = sendIp;
        this.senderPort = sendPort;
        keepListening = true;
        receivedMessage = "";
        if (sendIp.Length == 0) 
        {
            Debug.Log("Auto EP");
            ep = new IPEndPoint(IPAddress.Any, receivePort);
        }
        else
        {
            ep = new IPEndPoint(IPAddress.Parse(sendIp), sendPort);
        }

        try
        {
            client = new UdpClient(ep);
        }
        catch (Exception e)
        {
            Debug.Log("Failed to listen for UDP at port " + receivePort + ": " + e.Message);
            //return;
        }
        Debug.Log("Created receiving client at ip "+sendIp+"  and port " + receivePort);
        curReceivedBytes = new byte[0];

        ListenForMessages();
    }


    private void ListenForMessages()
    {
        try
        {
            Debug.Log("Receiving");
            client.BeginReceive(new AsyncCallback(GetMessages), null);
            
        }
        catch (SocketException e)
        {
            // 10004 thrown when socket is closed
            if (e.ErrorCode != 10004) Debug.Log("Socket exception while receiving data from udp client: " + e.Message);
        }
        catch (Exception e)
        {
            Debug.Log("Error receiving data from udp client: " + e.Message);
        }

    }



    public void GetMessages(IAsyncResult ar)
    {
        Debug.Log("Triggered receiving");
        byte[] receiveBytes = client.EndReceive(ar, ref ep);
        string returnData = Encoding.UTF8.GetString(receiveBytes);

        receivedMessage = returnData;

        ListenForMessages();
    }


    public void Send(string message)
    {
        //Debug.Log(String.Format("Send msg to ip:{0} port:{1} msg:{2}", senderIp, senderPort, message));
        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse(senderIp), senderPort);
        Byte[] sendBytes = Encoding.UTF8.GetBytes(message);
        client.Send(sendBytes, sendBytes.Length, serverEndpoint);
    }

    public void Stop()
    {
        client.Close();
    }
}