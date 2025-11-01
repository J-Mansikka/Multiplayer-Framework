using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using UnityEditor.MemoryProfiler;

public abstract class MnetNetwork : MonoBehaviour
{

    //public PacketManager PacketManager;
    public int currentTickNumber { get; protected set; }
    public float deltaTime { get; protected set; }
    public PacketManager worldPackets { get; private set; }
    // Clientille sitte myös inputTimer
    protected float tickTimer = 0f;
    protected float sendTimer = 0f;
    protected float receiveTimer = 0f;
    protected MnetObject[] activeObjects;
    //protected Queue<int> updatedObjects;
    bool sendPacket = false;

    private void Awake()
    {
        currentTickNumber = 69;
        worldPackets = new PacketManager(Mnet.serverPacketBufferSize, this);
        activeObjects = new MnetObject[100];
        activeObjects[69] = GetComponent<MnetObject>();
    }

    public MnetObject GetObject(int instanceID)
    {
        return activeObjects[instanceID];
    }




    public void DEMOtestTick()
    {
        worldPackets.packetToRead.currentLength = worldPackets.packetToRead.GetSize();
        Tick();
    }

    protected void Tick()
    {
        worldPackets.ReadTick(currentTickNumber);
        currentTickNumber++;
    }

    protected void CheckIncomingPackets(NewConnection connection, PacketManager local, PacketManager received)
    {
        if (connection.socket.Available > 0)
        {
            while (connection.socket.Receive(received.incomingPacket.Data) > 0)
            {
                // Unnecessary, but makes the code easier to read
                //Packet receivedPacket = connection.packetManager.incomingPacket;

                received.incomingPacket.InitializeReceived();

                if (received.incomingPacket.packetType == PacketType.Regular)
                {
                    int packetNumber = received.incomingPacket.GetPacketNumber();
                    if (packetNumber == connection.expectedPacketNumber)
                    {
                        received.incomingPacket.InitializeReceived();
                        // We try to remove the correct packet from the missing since packets can arrive in wrong order
                        connection.missingPackets.Remove(packetNumber);
                        // It's possible that we have gotten later packets already, so finding the next needed one will be done in a loop
                        while (received.incomingPacket.isNew)
                        {
                            received.incomingPacket = received.incomingPacket.nextPacket;
                            connection.expectedPacketNumber++;
                        }

                    }
                    else if (packetNumber > connection.expectedPacketNumber)
                    {
                        Packet laterPacket = received.GetPacket(packetNumber);
                        int receivedPacketNumber = received.incomingPacket.GetPacketNumber();
                        if(connection.latestPacketNumberReceived < receivedPacketNumber)  connection.latestPacketNumberReceived = receivedPacketNumber;
                        // If the network is sending duplicate packets on purpose or by error, this packet might already exist
                        if (!laterPacket.isNew)
                        {
                            laterPacket.SwapData(received.incomingPacket);
                            laterPacket.InitializeReceived();
                        }
                    }
                }
                else if (received.incomingPacket.packetType == PacketType.ResendRequest)
                {
                    ResendPacket(received.incomingPacket, connection, local);
                }
                else if (received.incomingPacket.packetType == PacketType.SystemMessage)
                {
                    ReadSystemMessage(received.incomingPacket);
                }
                connection.playerPackets.incomingPacket = connection.playerPackets.incomingPacket.nextPacket;
            }
        }
        // Check if we are still missing packets
        if(connection.latestPacketNumberReceived > connection.expectedPacketNumber)
        {
            Packet packetToCheck = received.incomingPacket;
            int missingNumber = connection.expectedPacketNumber;
            int numberOfChecks = connection.latestPacketNumberReceived - connection.expectedPacketNumber;
            for (int i = 0; i < numberOfChecks; i++)
            {
                if (!packetToCheck.isNew)  connection.missingPackets.Add(missingNumber);
                missingNumber++;
                packetToCheck = packetToCheck.nextPacket;
            }
            AskForResend(connection);
            connection.missingPackets.Clear();
        }
    }

    protected void SendPackets()
    {

    }

    protected void AskForResend(NewConnection connection)
    {

        connection.messager.SetPacketType(PacketType.ResendRequest);
        connection.messager.currentLength = Mnet.headerCombinedLength;
        foreach(int packetNumber in connection.missingPackets)
        {
            MnetTools.IntegerToBytes(connection.messager.WriteBytes(Mnet.headerPacketNumberLength), packetNumber);
        }

        int sendRate = connection.sendRate * (int)PacketPriority.Important;
        for (int i = 0; i < sendRate; i++)
        {
            connection.socket.Send(connection.messager.Data);
        }
        connection.missingPackets.Clear();
    }

    protected void ResendPacket(Packet missingInfoPacket, NewConnection connection, PacketManager localPackets)
    {
        int copiesToSend = connection.sendRate * (int)PacketPriority.Safe;
        Packet packetToSend;
        while (missingInfoPacket.BytesLeftToRead > 0)
        {
            // Read packet number from the received packet and get it from the local PacketManager
            packetToSend = localPackets.GetPacket(MnetTools.BytesToInteger(missingInfoPacket.ReadBytes(Mnet.headerPacketNumberLength)));            
            for (int i = 0; i < copiesToSend; i++)
            {
                connection.socket.Send(packetToSend.Data);
            }
        } 
    }

    protected abstract void ReadSystemMessage(Packet packet);

}
