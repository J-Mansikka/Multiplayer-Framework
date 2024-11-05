using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;
using UnityEngine;
using System.Buffers.Binary;
using System.Text;

/*
    Pelaajan toiminnot serverin puolella, eli bufferissa paketit ja inputit.
    Hoitaa syncin ja rollbackin
 */
public class MnetRemoteClientConnection
{
    private MnetServer server;
    public uint clientDictionaryKey;
    public long localIP;
    public ulong clientID;
    public int localPort;
    public Socket connection;
    public IPEndPoint localEP;
    public IPEndPoint remoteEP;
    private bool connected;
    private float handshakeAttemptTimeRemaining;

    private MnetPacketBuffer buffer;
    private Queue<short> freeObjectIndex;
    public MnetObject[] objectsBeingSynced;


    private int currentPacketNumber;
    private int lastTickSize;
    private int currentFrameNumber;
    private float currentFrameTime;

    private byte[] handshakeBytes;
    private bool serverRunning;


    public MnetRemoteClientConnection(MnetServer server, uint clientDictionaryKey, IPAddress localIP, int localPort, IPAddress clientIP, int clientPort)
    {
        this.server = server;
        this.clientDictionaryKey = clientDictionaryKey;
        this.localPort = localPort;
        // Create random id for client that will be used to verify the user
        byte[] randomUlong = new byte[16];
        for (int i = 0; i < randomUlong.Length; i++)
        {
            randomUlong[i] = (byte)UnityEngine.Random.Range(0,255);
        }

        clientID = BinaryPrimitives.ReadUInt64LittleEndian(randomUlong);
        handshakeBytes = new byte[32];
        buffer = new MnetPacketBuffer(ServerSettings.clientPacketBufferSize);
        localEP = new IPEndPoint(localIP, localPort);
        remoteEP = new IPEndPoint(clientIP, clientPort);
        connection.Bind(localEP);
        connection.Connect(remoteEP);
        handshakeAttemptTimeRemaining = ServerSettings.clientHandshakeTimeout;
        connected = false;
    }

    public void HandShake(float frameTime)
    {
        handshakeAttemptTimeRemaining -= frameTime;

        if (connection.Available != 0)
        {
            connection.Receive(handshakeBytes.AsSpan());

            string messageReceived = Encoding.ASCII.GetString(handshakeBytes.AsSpan().Slice(0,16));
            if (messageReceived == ServerSettings.messageClientToHandlerHandshake)
            {

                // Lähetä ID ja worldstate ja ala lähettää paketteja. Worldstate tarvii frame numeron kanssa
                // Tick tulee heti perään eli oikeaa pakettia alkaa tulemaan
                // Ei tarvii venata bufferia, taitaa tulla itsestään
                // Miten varmistaa että pelaaja ilmestyy samaan aikaan kaikkialla?


                handshakeBytes[0] = ServerSettings.newClientKeyStart;
                BinaryPrimitives.WriteUInt64LittleEndian(handshakeBytes.AsSpan().Slice(1,8), clientID);
                handshakeBytes[9] = ServerSettings.newClientKeyEnding;                

                for (int i = 0; i < ServerSettings.redundantCopiesHandshake; i++)
                {
                    connection.Send(handshakeBytes);
                }
                connected = true;
            }
        }
        else if(handshakeAttemptTimeRemaining <= 0f)
        {
            server.DisconnectPlayer(clientDictionaryKey);
        }


    }

    public void Receive(float frameTime)
    {
        // Ota vastaan paketit jos available
        if (connection.Available != 0)
        {
            connection.Receive(handshakeBytes);
            // Clientit ei jaa paketteja ni tsekkaa frame ja talleta bufferiin
        }
    }

    public void PlayerUpdate(float frameTime)
    {
        // Liikuta pelaajaa inputtien mukaan
        if (connected)
        {
            Receive(frameTime);
        }
        else
        {
            HandShake(frameTime);
        }
    }

    public void SendWorldPacket()
    {

    }
}
