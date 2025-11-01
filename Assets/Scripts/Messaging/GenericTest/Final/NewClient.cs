using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class NewClient : MnetNetwork
{
    public bool testConnection;

    public string clientIP;
    public int clientPort;
    private NewConnection connection;

    private void Awake()
    {
        connection = new NewConnection(this);
    }


    /// <summary>
    /// 
    /// 
    ///  CONNECTION
    /// - Try contact server with info
    /// - Get response of new connection
    /// - Switch to new connection
    /// - Get scene number
    /// - Load scene
    /// - Send ready message
    /// - Get object + scene info
    /// - Keep receiving packets until buffer full
    /// - Send ready message and activate player object
    /// 
    /// 
    /// 
    /// </summary>


    private void LateUpdate()
    {
        // ifActive ja muut boolit etc



        // LOOP JOSSA OTETAAN VIESTIT VASTAAN
        // Erotellaan heti aluks REGULAR ja HANDSHAKE prosessit

        // STATES
        // Connected
        // -Active
        // -Initializing
        // Connecting
        // -GetConnectionInfo
        // -GetSceneInfo
        // -GetObjectInfo ? Täs vaihees ruvetaa jo kerää pakettei

        if (testConnection)
        {
            SendHandshake("127.0.0.1", 42069);
            testConnection = false;
        }

        if (connection.isActive)
        {
            // InputTimer
            // UpdateTimer
            // ReceiveTimer
            // SendTimer
            // TickTimer
            receiveTimer += Time.deltaTime;
            if(receiveTimer > Mnet.receiveRate)
            {
                receiveTimer -= Mnet.receiveRate;
                Listen();
            }
        }


    }

    public void RegularUpdate()
    {

    }

    public void Listen()
    {
        // TImeout ja timereita voidaan tarvita lopulliseen

        if (connection.socket.Available > 0)
        {
            // ReceiveTimer tarvitaan

            switch (connection.connectionState)
            {
                case ConnectionState.Active:
                    {
                        // input, write, send, tick
                        break;
                    }
                case ConnectionState.Initializing:
                    {
                        break;
                    }
                case ConnectionState.Handshake:
                    {
                        connection.socket.Receive(connection.messager.AsEmpty());
                        connection.messager.InitializeReceived();
                        Connect();
                        break;
                    }
                case ConnectionState.Connecting:
                    {
                        connection.socket.Receive(connection.messager.AsEmpty());
                        connection.messager.InitializeReceived();
                        GetSceneInfo();
                        break;
                    }
                case ConnectionState.Loading:
                    {
                        break;
                    }
                default:
                    {
                        // IDLE
                        break;
                    }
            }
        }
    }

    public void SendHandshake(string serverIP, int serverPort)
    {
        connection.connectionState = ConnectionState.Handshake;
        connection.isActive = true;
        connection.SetLocalEndpoint(new IPEndPoint(IPAddress.Parse(clientIP), clientPort));
        //connection.ConnectTo(new IPEndPoint(IPAddress.Parse(serverIP), serverPort));
        connection.messager.Clear();
        connection.messager.WriteMessage(Mnet.messageHandshakeRequest);
        connection.local.Address.TryWriteBytes(connection.messager.WriteBytes(4), out _);
        MnetTools.Int32ToBytes(connection.messager.WriteBytes(4), connection.local.Port);
        connection.socket.SendTo(connection.messager.bytes,connection.messager.currentLength,SocketFlags.None,new IPEndPoint(IPAddress.Parse(serverIP),serverPort));
    }

    public void Connect()
    {
        string receivedMessage = connection.messager.ReadMessage();
        print("CLIENT: " + receivedMessage);
        if (receivedMessage == Mnet.messageHandshakeResponse)
        {
            try
            {
                IPEndPoint newEndpoint = new IPEndPoint(new IPAddress(connection.messager.ReadBytes(4))
                    , MnetTools.BytesToInt32(connection.messager.ReadBytes(4)));
                connection.ConnectTo(newEndpoint);
                connection.connectionState = ConnectionState.Connecting;
                ConnectionTest();
            }
            catch
            {
                Debug.LogError("CLIENT GOT BAD ENDPOINT DATA");
                // Not valid IP or port
            }
        }
    }

    public void ConnectionTest()
    {
        connection.messager.Clear();
        connection.messager.WriteMessage(Mnet.messageNewConnectionTest);
        connection.socket.Send(connection.messager.Data);
    }

    public void GetSceneInfo()
    {
        string receivedMessage = connection.messager.ReadMessage();

        if(receivedMessage == Mnet.messageNewConnectionVerified)
        {

        }
    }

    public void SetupScene()
    {

    }

    public void ActivateConnection()
    {

    }


    protected override void ReadSystemMessage(Packet packet)
    {
        throw new System.NotImplementedException();
    }

    private void OnDisable()
    {
        connection.socket.Close();
        connection.socket.Dispose();
    }
}
