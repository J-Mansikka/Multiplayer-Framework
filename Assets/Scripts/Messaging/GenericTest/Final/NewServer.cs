using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class NewServer : MnetNetwork
{

    public string serverIP;
    public int serverPort;

    private Socket incomingConnections;
    private Packet newMessage;
    //private Dictionary<string, NewConnection> activeConnections;
    private List<NewConnection> activeConnections;
    private Queue<int> availablePlayerNumbers;

    private void Awake()
    {
        activeConnections = new Dictionary<string, NewConnection>();
        availablePlayerNumbers = new Queue<int>();
        newMessage = new Packet();
        for (int i = 0; i < Mnet.maxPlayerCount; i++)
        {
            availablePlayerNumbers.Enqueue(i+1);
        }

        incomingConnections = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        incomingConnections.Blocking = false;
        incomingConnections.Bind(new IPEndPoint(IPAddress.Parse(serverIP), serverPort));
    }

    public void ListenForConnectionRequests()
    {

        while(incomingConnections.Available > 0)
        {
            incomingConnections.Receive(newMessage.AsEmpty());
            newMessage.InitializeReceived();
            string receivedMessage = newMessage.ReadMessage();
            print("SERVER: " + receivedMessage);
            if(receivedMessage == Mnet.messageHandshakeRequest)
            {
                // !!! If address is not actually an address, shit will crash here
                try
                {
                    IPEndPoint receivedEP = new
                        (new IPAddress(newMessage.ReadBytes(4))
                        , MnetTools.BytesToInt32(newMessage.ReadBytes(4)));
                    if (!activeConnections.ContainsKey(receivedEP.Address.ToString()))
                    {
                        AddNewConnection(receivedEP);
                    }

                    SendNewConnectionInfo(activeConnections[receivedEP.Address.ToString()]);
                }
                catch
                {
                    Debug.LogError("SERVER GOT BAD ENDPOINT DATA");
                    // Not valid IPaddress or Port
                }
            }
        }
        
    }

    private void LateUpdate()
    {
        receiveTimer += Time.deltaTime;
        if(receiveTimer > Mnet.receiveRate)
        {
            receiveTimer -= Mnet.receiveRate;
            ListenForConnectionRequests();
            foreach (NewConnection connection in activeConnections)
            {

            }
        }
    }

    public void ReceiveFromConnection()
    {

    }

    public void AddNewConnection(IPEndPoint playerEP)
    {
        if (availablePlayerNumbers.Count > 0)
        {
            int playerNumber = availablePlayerNumbers.Dequeue();
            NewConnection newPlayerConnection = new NewConnection(this);
            newPlayerConnection.SetLocalEndpoint(new IPEndPoint(IPAddress.Parse(serverIP),serverPort+playerNumber));
            newPlayerConnection.ConnectTo(playerEP);
            newPlayerConnection.connectionState = ConnectionState.Connecting;
            activeConnections.Add(playerEP.Address.ToString(), newPlayerConnection);
        }

    }

    public void SendNewConnectionInfo(NewConnection playerConnection)
    {
        // Luo viesti missä message + uuden connectionin infot
        // Message + ip + port + player number.. scene?
        newMessage.Clear();
        newMessage.WriteMessage(Mnet.messageHandshakeResponse);
        playerConnection.local.Address.TryWriteBytes(newMessage.WriteBytes(4), out _);
        MnetTools.Int32ToBytes(newMessage.WriteBytes(4),playerConnection.local.Port);
        MnetTools.Int32ToBytes(newMessage.WriteBytes(4), playerConnection.playerNumber);
        playerConnection.socket.SendTo(newMessage.bytes,newMessage.currentLength,SocketFlags.None,playerConnection.remote);
    }

    public void SendSceneInfo(NewConnection playerConnection)
    {
        playerConnection.messager.Clear();
        playerConnection.messager.WriteMessage(Mnet.messageNewConnectionVerified);
        // SCENE + INFO (Scene number + bool firstLoad (voi ladata suoraa scene objektit sellasenaa)
        // Muut pelaajat etc
    }

    protected override void ReadSystemMessage(Packet packet)
    {
        throw new System.NotImplementedException();
    }

    private void OnDisable()
    {
        incomingConnections.Close();
        incomingConnections.Dispose();
    }
}
