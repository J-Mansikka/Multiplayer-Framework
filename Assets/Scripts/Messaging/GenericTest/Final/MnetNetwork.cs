using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using UnityEditor.MemoryProfiler;
using UnityEngine.SceneManagement;

public abstract class MnetNetwork : MonoBehaviour
{

    public INewSpawner spawner;
    public float deltaTime { get; protected set; }

    public int sceneNumber { get; protected set; }
    public PacketManager worldPackets { get; private set; }
    public PacketManager snapshotPackets { get; private set; }
    public PacketManager activeManager;
    // Clientille sitte myös inputTimer
    protected float updateTimer = 0f;
    protected float tickTimer = 0f;
    protected float receiveTimer = 0f;
    public bool gameRunning { get; protected set; }
    public NewObject[] worldObjectInstances { get; protected set; }
    protected NewInstanceManager instanceManager;
    public HashSet<int> clientControlledInstances;
    //protected Queue<int> updatedObjects;
    bool sendPacket = false;

    public NewObject GetActiveObject(int instanceID)
    {
        return worldObjectInstances[instanceID];
    }




    // Wanha?
    public abstract void AddPlayerObject(NewObject playerObject, int playerID);

    protected void Setup(bool isServer)
    {
        worldPackets = new PacketManager(Mnet.serverPacketBufferSize, this, PacketType.RegularUpdate, false, 0);
        activeManager = worldPackets;
        snapshotPackets = new PacketManager(Mnet.serverSnapshotPacketBufferSize, this, PacketType.SnapshotUpdate, false, 0);
        if(instanceManager == null)  instanceManager = GetComponent<NewInstanceManager>();
        instanceManager.Initialize();
        instanceManager.SetupInstanceManager(this, isServer);
        SceneManager.sceneLoaded += OnSceneLoad;
    }
    public void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(name + " called " + "OnSceneLoad");
        GetSpawner();
        instanceManager.SwitchSceneSpawner(spawner);
    }

    public void GetSpawner()
    {
        string spawnerName = "Spawner" + name;
        spawner = GameObject.Find(spawnerName).GetComponent<INewSpawner>();
    }

    protected void Tick()
    {
        worldPackets.ReadTick();
    }

    protected void ClientTick(NewConnection clientConnection)
    {
        Debug.Log(name + " client tick");
        clientConnection.playerPackets.ReadTick();
    }


    protected void CheckIncoming(NewConnection connection)//, PacketManager local, PacketManager activeManager, PacketManager otherManager)
    {
        while (connection.socket.Available > 0)
        {
            connection.socket.Receive(connection.receivedPacket.AsEmpty());
            connection.receivedPacket.InitializeReceived();
            // PacketType riittää
            print(name + " received incoming " + connection.receivedPacket.PacketType);

            // The received packet is the type expected (regular or snapshot) so we will directly
            if (connection.receivedPacket.PacketType == PacketType.RegularUpdate)
            {
                ReceivedRegularPacket(connection);
            }
            /*
            else if (connection.received.PacketType == PacketType.ResendRequest)
            {
                ReceivedResendRequest(connection);
            }
            */
            else if(connection.receivedPacket.PacketType == PacketType.SnapshotUpdate)
            {
                ReceivedSnapshotPacket(connection);
            }
            /*
            else if(connection.received.PacketType == PacketType.ResendSnapshotRequest)
            {
                ReceivedResendSnapshotRequest(connection);
            }
            */
            else if (connection.receivedPacket.PacketType == PacketType.SystemMessage)
            {
                ReadSystemMessage(connection, connection.receivedPacket);
            }
            /*
            else if (connection.received.PacketType == PacketType.Connecting)
            {

            }
            */
        }
    }

    // Returns true if there are still missing packets
    protected bool CheckForMissingPackets(NewConnection connection, PacketManager activeManager)
    {
        if (activeManager.highestPacketNumberReceived > activeManager.expectedPacketNumber)
        {
            // We will use the messager packet to ask for a resend
            connection.messager.Clear();
            int packetsToCheck = activeManager.highestPacketNumberReceived - activeManager.expectedPacketNumber;
            Packet packetToCheck = activeManager.incomingPacket;
            while (packetsToCheck > 0)
            {
                if (!packetToCheck.isNew)
                {
                    MnetTools.IntegerToBytes(connection.messager.WriteBytes(Mnet.headerPacketNumberLength), activeManager.incomingPacket.GetPacketNumber());
                }
                packetsToCheck--;
            }
            AskForResend(connection, activeManager.BufferType);
            return true;
        }
        return false;
    }

    protected abstract void ReceivedRegularPacket(NewConnection connection);
    protected abstract void ReceivedSnapshotPacket(NewConnection connection);
    protected abstract void ReceivedResendRequest(NewConnection connection);
    protected abstract void ReceivedResendSnapshotRequest(NewConnection connection);
    protected abstract void ReadSystemMessage(NewConnection connection, Packet messagePacket);

    /*
    protected void WANHACheckIncomingPackets(NewConnection connection, PacketManager local, PacketManager received)
    {
        if (connection.socket.Available > 0)
        {
            connection.timeoutCounter = 0;
            while (connection.socket.Receive(received.incomingPacket.Data) > 0)
            {
                // Unnecessary, but makes the code easier to read
                //Packet receivedPacket = connection.packetManager.incomingPacket;

                received.incomingPacket.InitializeReceived();

                if (received.incomingPacket.packetType == PacketType.RegularUpdate)
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
        else
        {
            connection.timeoutCounter++;
            if(connection.timeoutCounter > Mnet.timeoutAutoDisconnectLimit)
            {
                Disconnect(connection, "Disconnected due to timeout");
                return;
            }
            else if(connection.connectionState == ConnectionState.Active && connection.timeoutCounter > Mnet.timeoutPausePlayerLimit)
            {
                ConnectionTimingOut(connection);
                return;
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
    */

    protected void Send(NewConnection connection, Packet packetToSend, int copiesToSend = 1)
    {
        for (int i = 0; i < copiesToSend; i++)
        {
            connection.socket.Send(packetToSend.Data);
        }
    }

    protected void Send(NewConnection connection, Packet packetToSend, PacketPriority priority = PacketPriority.Regular)
    {
        for (int i = 0; i < (int)priority; i++)
        {
            connection.socket.Send(packetToSend.Data);
        }
    }

    protected void SendPackets(NewConnection connection, PacketManager packets)
    {
        while(packets.outgoingPacket.GetTickNumber() == packets.currentTickNumber)
        {
            print(name + " send packet " + packets.outgoingPacket.GetPacketNumber()+" with tick "+packets.outgoingPacket.GetTickNumber());
            Send(connection, packets.outgoingPacket, connection.sendRate);
            packets.outgoingPacket = packets.outgoingPacket.nextPacket;
        }
    }

    protected void AskForResend(NewConnection connection, PacketType requestType)
    {
        connection.messager.SetPacketType(requestType);
        int sendRate = connection.sendRate * (int)PacketPriority.Important;
        Send(connection, connection.messager, sendRate);
    }

    protected void ResendPacket(Packet missingInfoPacket, NewConnection connection, PacketManager localPackets)
    {
        int copiesToSend = connection.sendRate * (int)PacketPriority.Safe;
        Packet packetToSend;
        while (missingInfoPacket.BytesLeftToRead > 0)
        {
            // Read packet number from the received packet and get it from the local PacketManager
            packetToSend = localPackets.GetPacket(MnetTools.BytesToInteger(missingInfoPacket.ReadBytes(Mnet.headerPacketNumberLength)));
            Send(connection,packetToSend, copiesToSend);
        } 
    }

    public abstract void Disconnect(NewConnection connection, string message);

    public abstract void ConnectionTimingOut(NewConnection connection);
}
