using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class UdpServer : MonoBehaviour
{
    public bool udpClientIsRunning;
    public string serverIP;
    public int serverPort;
    private UdpClient udpClient;
    private UdpClient testServersideClientListener;
    private IPEndPoint serverEP;
    private Thread udpThread;
    private List<PlayerHandler> players;
    private Dictionary<IPEndPoint,PlayerHandler> connectedPlayers;
    private MonniPacketHolder[] packetsToProcess;
    private int serverCounter;

    public GameObject lagTestObjectMovement;
    //private PlayerMovement lagTestInput = 0;
    //private float curDelta = 0f;
    //private PlayerMovement oldInput = 0;
    //private float oldDelta = 0f;
    byte curId = 0;

    public TMP_Text printTest;
    private string newTestMessage = "";
    private float checkMessageDelay = 0f;

    private static readonly object receivedBufferLock = new object();

    //int frame = 0;

    int packetCount = 0;

    Queue<MonniPacketHolder> messageBuffer;

    bool[] packetBuffer;

    int testingPacketSize = 7;  // 1 index 1 input 12 pos


    private void Awake()
    {
        //receivedBufferLock = new object();
        //packetBuffer = new bool[256];
        players = new List<PlayerHandler>();
        messageBuffer = new Queue<MonniPacketHolder>();
        packetsToProcess = new MonniPacketHolder[0];
        connectedPlayers = new Dictionary<IPEndPoint, PlayerHandler>();
    }

    private void Start()
    {
        if (udpClientIsRunning)
        {

            udpThread = new Thread(new ThreadStart(ListenForMessages));
            udpThread.Start();
        }
    }

    private void CreateNewPlayer(IPEndPoint newPlayerEP)
    {
        PlayerHandler newPlayer = new PlayerHandler(curId, "TESTINGNAME", lagTestObjectMovement, 5,udpClient,newPlayerEP);
        curId++;
        connectedPlayers.Add(newPlayerEP, newPlayer);
        players.Add(newPlayer);
    }

    private void Update()
    {
       

        lock(receivedBufferLock) 
        {
            packetsToProcess = messageBuffer.ToArray();
            messageBuffer.Clear();
        }

        // ALL OF THIS IS DUMB, REPLACE

        /*
        for (int i = 0; i < processingMessages.Length; i++)
        {
            if (connectedPlayers.ContainsKey(processingMessages[i].sender))
            {
                byte[] packet = processingMessages[i].message;
                // HARDCODE DOUBLE PACKET JUST FOR TESTING FIX
                // SIIRRÄ PLAYERIIN JA SIELLÄ SITTEN LOGIIKKA TSEKKAA ETTÄ ONKO JAETTU. EHKÄ VOIS KÄYTTÄÄ SUORAAN SIZE BYTEE EKANA (olis enemmä ku yks byte mut ei täys kahta)
                if (packet.Length > testingPacketSize)
                {
                    Debug.Log("NUMERO ON " + packet[3]);
                    byte[] firstHalf = new byte[packet.Length / (packet.Length / testingPacketSize)];
                    //Array.Copy(packet, 0, firstHalf, 0, firstHalf.Length);
                    byte[] secondHalf = new byte[packet.Length / (packet.Length / testingPacketSize)];
                    //Array.Copy(packet, secondHalf.Length, secondHalf, 0, secondHalf.Length);
                    for (int p = 0; p < testingPacketSize; p++)
                    {
                        firstHalf[p] = packet[p];
                        secondHalf[p] = packet[p + testingPacketSize];
                    }
                    connectedPlayers[processingMessages[i].sender].ReadPlayerBytes(firstHalf);//processingMessages[i].message);
                    connectedPlayers[processingMessages[i].sender].ReadPlayerBytes(secondHalf);
                }
                else
                {
                    //print("SINGLE");
                    connectedPlayers[processingMessages[i].sender].ReadPlayerBytes(packet);
                }
            }
            // Tupla tsekkaus mut jos tapahtuvat vaan kun lisätään uusi pelaaja niin ei ole ongelma.
            else if (Encoding.ASCII.GetString(processingMessages[i].message) == "connect plz")
            {
                CreateNewPlayer(processingMessages[i].sender);
            }
        }
        processingMessages = new MonniMessageHolder[0];

        */

        for (int i = 0; i < packetsToProcess.Length; i++)
        {
            if (connectedPlayers.ContainsKey(packetsToProcess[i].sender))
            {
                connectedPlayers[packetsToProcess[i].sender].ReadPlayerBytes(packetsToProcess[i].packet);

                //byte[] packet = packetsToProcess[i].packet;
                // HARDCODE DOUBLE PACKET JUST FOR TESTING FIX
                // SIIRRÄ PLAYERIIN JA SIELLÄ SITTEN LOGIIKKA TSEKKAA ETTÄ ONKO JAETTU. EHKÄ VOIS KÄYTTÄÄ SUORAAN SIZE BYTEE EKANA (olis enemmä ku yks byte mut ei täys kahta)

                /*
                if (packet.Length > testingPacketSize)
                {
                    Debug.Log("NUMERO ON " + packet[3]);
                    byte[] firstHalf = new byte[packet.Length / (packet.Length / testingPacketSize)];
                    //Array.Copy(packet, 0, firstHalf, 0, firstHalf.Length);
                    byte[] secondHalf = new byte[packet.Length / (packet.Length / testingPacketSize)];
                    //Array.Copy(packet, secondHalf.Length, secondHalf, 0, secondHalf.Length);
                    for (int p = 0; p < testingPacketSize; p++)
                    {
                        firstHalf[p] = packet[p];
                        secondHalf[p] = packet[p + testingPacketSize];
                    }
                    connectedPlayers[packetsToProcess[i].sender].ReadPlayerBytes(firstHalf);//processingMessages[i].message);
                    connectedPlayers[packetsToProcess[i].sender].ReadPlayerBytes(secondHalf);
                }
                else
                {
                    //print("SINGLE");
                    connectedPlayers[packetsToProcess[i].sender].ReadPlayerBytes(packet);
                }
                */
            }
            // Tupla tsekkaus mut jos tapahtuvat vaan kun lisätään uusi pelaaja niin ei ole ongelma.
            else if (Encoding.ASCII.GetString(packetsToProcess[i].packet) == "connect plz")
            {
                print("CREATED NEW PLAYER");
                CreateNewPlayer(packetsToProcess[i].sender);
            }
        }



        // TODO: Parempi tapa nollata array tai vaihtaa kokonaan joten. queue + mass add?
        packetsToProcess = new MonniPacketHolder[0];

        /*
        checkMessageDelay += Time.deltaTime;

        if (checkMessageDelay > 1f)
        {
            checkMessageDelay = 0f;
            lock (receivedBufferLock)
            {
                if (newTestMessage.Length > 0)
                {
                    printTest.text += " " + newTestMessage;
                    newTestMessage = "";
                }
            }
        }
        */

        foreach(PlayerHandler player in players)
        {
            //player.Move();
            player.Tick();
        }
    }

    private void FixedUpdate()
    {
        /*
        foreach (PlayerHandler player in players)
        {
            player.UpdatePlayer();
        }
        /*
        lock (receivedBufferLock)
        {
            if (oldInput != 0)
            {
                float xMov = 0f;
                if (oldInput.HasFlag(PlayerMovement.left)) xMov = -1f;
                if (oldInput.HasFlag(PlayerMovement.right)) xMov = 1f;
                float zMov = 0f;
                if (oldInput.HasFlag(PlayerMovement.forward)) zMov = 1f;
                if (oldInput.HasFlag(PlayerMovement.backward)) zMov = -1f;
                lagTestObjectMovement.transform.position += (new Vector3(xMov, 0f, zMov) * Time.deltaTime * 10f);

                oldInput = 0;
            }
        }
        
        lock (receivedBufferLock)
        {


            /*
            if (moveBuffer.Count > 0)
            {
                PlayerMovement newMove = moveBuffer.Dequeue();
                float xMov = 0f;
                if (newMove.HasFlag(PlayerMovement.left)) xMov = -1f;
                if (newMove.HasFlag(PlayerMovement.right)) xMov = 1f;
                float zMov = 0f;
                if (newMove.HasFlag(PlayerMovement.forward)) zMov = 1f;
                if (newMove.HasFlag(PlayerMovement.backward)) zMov = -1f;
                lagTestObjectMovement.transform.position += (new Vector3(xMov, 0f, zMov) * Time.deltaTime * 10f);
                //print("PACKET: " + curId + " NEW POS = "+lagTestObjectMovement.transform.position);
                //print(frame + " " + lagTestObjectMovement.transform.position);
                packetCount++;
            }
            
        }
            


        frame++;
        */
    }

    private void ListenForMessages()
    {
        serverEP = new IPEndPoint(IPAddress.Any, serverPort);
        udpClient = new UdpClient(serverEP);
        IPEndPoint incomingEP = new IPEndPoint(IPAddress.Any, 0);
        byte[] newIncomingPacket = new byte[0];
        //Dictionary<IPEndPoint,MonniPlayer> players = new Dictionary<IPEndPoint,MonniPlayer>();

        // 1. Check if existing client
        // 2. Check if trying to join server
        // 3. Print to console and ignore

        //int testis = 2;
        while (udpClientIsRunning)
        //while(testis > 0)
        {
            //testis--;

            newIncomingPacket = udpClient.Receive(ref incomingEP);


            //
            //  1. EXISTING PLAYER 2. CONNECTION ATTEMPT 3. GARBAGE? ehkä printtaa varmuuden vuoks
            //

            lock (receivedBufferLock)
            {
                if (connectedPlayers.ContainsKey(incomingEP) || Encoding.ASCII.GetString(newIncomingPacket) == "connect plz")
                {
                    messageBuffer.Enqueue(new MonniPacketHolder(incomingEP, newIncomingPacket));
                }
                /*
                else if(Encoding.ASCII.GetString(newIncomingPacket) == "connect plz")
                {
                    //addNewPlayer = true;
                    //connectedPlayers.Add(incomingEP);
                    messageBuffer.Enqueue(new MonniMessageHolder(incomingEP, newIncomingPacket));
                }
                */
                else
                {
                    Debug.LogWarning("Got unknown message from " + incomingEP);
                }
            }

            /*
            if (curId == 255)
            {
                for (int i = 0; i < 256; i++)
                {
                    if (packetBuffer[i] == false)
                    {
                        print("MISSING PACKET " + i);
                    }
                }
                packetBuffer = new bool[256];
            }


            lock (receivedBufferLock)
            {
                if (newIncomingPacket.Length == 4)
                {
                    packetBuffer[newIncomingPacket[2]] = true;
                    messageBuffer.Enqueue((PlayerMovement)newIncomingPacket[3]);
                    curId++;
                    //oldDelta = BitConverter.ToSingle(newIncomingPacket, 6);

                }

                packetBuffer[curId] = true;
                packetBuffer[newIncomingPacket[0]] = true;
                messageBuffer.Enqueue((PlayerMovement)newIncomingPacket[1]);
                curId++;
            }
            
            //curDelta = BitConverter.ToSingle(newIncomingPacket, 1);

            

            /*
            string message = Encoding.ASCII.GetString(newIncomingPacket);
            Debug.Log("RECEIVED " + message + " FROM " + newPossibleClientEP.ToString());

            if (players.TryGetValue(newPossibleClientEP, out MonniPlayer existingPlayer))
            {
                existingPlayer.Testing();
            }
            else if(message == "connect")
            {
                players.Add(newPossibleClientEP, new MonniPlayer(players.Count, "TESTIMIES"));
                Debug.Log("New player created.");
            }
            else
            {
                lock (receivedBufferLock)
                {
                    newTestMessage = message;
                }
            }

            */
            
        }
    }

    private void OnDisable()
    {
        Debug.Log("PACKET COUNT SERVER " + packetCount);

        udpClientIsRunning = false;
        if(udpThread != null)
        {
            Debug.Log("Thread aborted...");
            udpThread.Abort();
        }
    }
}

