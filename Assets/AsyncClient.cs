using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

[Flags] public enum PlayerMovement
{
    no_input = 0,
    forward=1,
    backward=2,
    left=4,
    right=8,
    jump=16,
    crouching=32,
    sprinting=64,
}
public class AsyncClient: MonoBehaviour
{
    bool running; 
    public float simulatedLag = 0f;
    [Range(0, 100)]
    public int packetLoss = 0;
    private int lostPacketsInRow;
    //private UdpConnection connection;
    public TMP_InputField sendText;
    public GameObject realtimeMove;
    private Queue<MessagePlayerMovement> testDelayedInputs;
    private int realTimeCounter;

    private PlayerMovement inputs;
    private bool sendOldInput;
    private MessagePlayerMovement previousInput;
    public int port;
    public string serverIP;
    public int serverPort;

    public bool useConnect;

    private Vector3 nextPosition;

    private UdpClient client;
    private IPEndPoint ep;

    int packetCount = 0;
    float dt;

    int packetSize = 2;

    byte seqNumber;

    float waitAWhile = 2f;

    Vector3 distance = Vector3.zero;
 
    CharacterController cc;

    float updateTimer = 0f;
    PlayerMovement currentMove;

    int dumbTest;
    Vector3 prevPos;
    float distanceTest;
    int frameCount;
    bool printti = true;
    bool hasMoved;

    private void Awake()
    {
        client = new UdpClient(port);
        testDelayedInputs = new Queue<MessagePlayerMovement>();
        simulatedLag = simulatedLag / 1000f;
        cc = realtimeMove.GetComponent<CharacterController>();
        nextPosition = Vector3.zero;
        prevPos = realtimeMove.transform.position;
    }
    void Start()
    {

    }

    private void Update()
    {
        if (running)
        {
            if (Input.GetKey(KeyCode.W))
            {
                inputs |= PlayerMovement.forward;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                inputs |= PlayerMovement.backward;
            }

            if (Input.GetKey(KeyCode.A))
            {
                inputs |= PlayerMovement.left;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                inputs |= PlayerMovement.right;
            }

            //SimMove();


            //Debug.Log(move);



            if (updateTimer > Time.fixedDeltaTime) //&& inputs != 0)
            {



                // LIIKE TAPAHTUU UPDATESSA. PITÄÄ AINA OTTAA HUOMIOON KUINKA PALJON ON LIIKUTTU KU LÄHETTÄÄ.
                // UPDATE LIIKE PITÄÄ MYÖS OLLA VARMASTI SAMA ELI JOS JÄÄ YHTEEN FRAMEE ENÄÄ MILLI NIIN OLKOOT
                // JOS DESYNC JATKUU NI SITTE PITÄÄ VAAN EHKÄ YRITTÄÄ LIIKUTTAA SERVERIN PELAAJA KOHTI ANNETTUA "OIKEAA KOHTAA" EI PITÄIS SILTI PÄÄST SEINIST LÄPI

                float xMov = 0f;
                if (inputs.HasFlag(PlayerMovement.left)) xMov = -1f;
                if (inputs.HasFlag(PlayerMovement.right)) xMov = 1f;
                float zMov = 0f;
                if (inputs.HasFlag(PlayerMovement.forward)) zMov = 1f;
                if (inputs.HasFlag(PlayerMovement.backward)) zMov = -1f;

                Debug.Log("C: "+seqNumber+" "+updateTimer);

                cc.Move(new Vector3(xMov, 0f, zMov) * updateTimer * ServerSettings.tempPlayerSpeed);

                if (inputs != 0)
                {
                    //Debug.Log("CLIENT: " + seqNumber + " " + updateTimer + " " + (updateTimer * ServerSettings.tempPlayerSpeed));

                }
                MessagePlayerMovement newMes = new MessagePlayerMovement();
                //Debug.Log("DISTANCE MOVED BY CLIENT: " + (distance - realtimeMove.transform.position) + " IN "+updateTimer);
                distance = realtimeMove.transform.position;
                newMes.Setup(seqNumber, (byte)inputs, updateTimer);
                testDelayedInputs.Enqueue(newMes);

                seqNumber++;






                currentMove = inputs;

                inputs = 0;


                SendMessage();


                updateTimer = 0f;
            }
            else
            {
                updateTimer += Time.deltaTime;
            }




        }
    }

    public void SimMove()
    {
        float xMov = 0f;
        if (currentMove.HasFlag(PlayerMovement.left)) xMov = -1f;
        if (currentMove.HasFlag(PlayerMovement.right)) xMov = 1f;
        float zMov = 0f;
        if (currentMove.HasFlag(PlayerMovement.forward)) zMov = 1f;
        if (currentMove.HasFlag(PlayerMovement.backward)) zMov = -1f;

        Vector3 move = new Vector3(xMov, 0f, zMov) * Time.deltaTime * ServerSettings.tempPlayerSpeed;
        //Debug.Log("CLIENT: INPUT = "+xMov+". DT = "+deltaTime+". FT = "+frameTime);
        //Debug.Log(move);
        cc.Move(move);
        currentMove = 0;
        //nextPosition = new Vector3(xMov, 0f, zMov) * ServerSettings.tempPlayerSpeed;
        //cc.Move(new Vector3(xMov, 0f, zMov) * Time.fixedDeltaTime * 10f);
        //realtimeMove.transform.position += (new Vector3(xMov, 0f, zMov) * Time.fixedDeltaTime * 10f);
        //Debug.Log("REALTIME: " + realTimeCounter + " "+delta);
    }

    private void FixedUpdate()
    {
        /*
        if ( running) //&& inputs != 0)
        {
            MessagePlayerMovement newMes = new MessagePlayerMovement();
            newMes.Setup(seqNumber, (byte)inputs,Time.deltaTime);
            testDelayedInputs.Enqueue(newMes);

            seqNumber++;





        }
        SimMove();
        inputs = 0;

        if (running && simulatedLag > 0f)
        {
            simulatedLag -= Time.fixedDeltaTime;
        }
        else
        {
            SendMessage(dt);
        }
        */
    }

    public void ConnectToServer()
    {

        ep = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
        client.Connect(ep);
        Byte[] sendBytes = Encoding.UTF8.GetBytes("connect plz");
        client.Send(sendBytes, sendBytes.Length);
        running = true;
            //ListenForMessages();

    }

    private void ListenForMessages()
    {
        try
        {
            //Debug.Log("Receiving");
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
        byte[] receiveBytes = client.EndReceive(ar, ref ep);
        string returnData = Encoding.UTF8.GetString(receiveBytes);

        Debug.Log("ASYNC CLIENT: "+returnData);

        ListenForMessages();
    }

    public void SendMessage()
    {
        //print("Clicked send");
        //print(sendText.text);



        if (testDelayedInputs.Count > 0)
        {
            //byte[] inputByte;
            //List<byte> inputMessage;
            byte[] inputMessage;
            MessagePlayerMovement curMove = testDelayedInputs.Dequeue();
            if (previousInput == null)
            {
                /*
                inputByte = new byte[6];
                inputByte[0] = curMove.id;
                inputByte[1] = curMove.move;
                for (int i = 2; i < 6; i++)
                {
                    inputByte[i] = 0;
                }
                */

                inputMessage = curMove.ToBytes();
                //Debug.Log("SEND: " + inputMessage[5]);
            }
            else
            {
                inputMessage = previousInput.ToBytes();
                inputMessage = inputMessage.Concat(curMove.ToBytes()).ToArray();
                //Debug.Log("SEND: " + inputMessage[5]);
                //inputMessage.AddRange(curMove.ToBytes());
                /*
                inputByte = new byte[12];
                inputByte[0] = previousInput.id;
                inputByte[1] = previousInput.move;
                for (int i = 2; i < 6; i++)
                {
                    inputByte[i] = 0;
                }
                inputByte[6] = curMove.id;
                inputByte[7] = curMove.move;
                for (int i = 8; i < 12; i++)
                {
                    inputByte[i] = 0;
                }
                */
            }

            previousInput = curMove;

            packetCount++;

            //byte[] curFrameDT = BitConverter.GetBytes(curMove.inputDelta);
            //Array.Copy(curFrameDT, 0, inputByte, 1, 4);

            if (UnityEngine.Random.Range(1, 100) > packetLoss)
            {
                //Debug.Log("SENDING " + inputByte[0]);
                //client.Send(inputByte, inputByte.Length);

                //byte[] asArray = inputMessage.ToArray();
                //Debug.Log("TÄÄLTÄ LÄHTEE "+asArray.Length);
                /*
                for (int i = 0; i < inputMessage.Count; i++)
                {
                    Debug.Log(i+" " + inputMessage[i]);
                }
                */

                //Debug.Log("Sending " + inputMessage[4]+" " + inputMessage[5]);
                //Debug.Log(inputMessage[ServerSettings.sequenceSegment]);
                client.Send(inputMessage, inputMessage.Length); //asArray, asArray.Length);


                /*
                if (sendOldInput)
                {
                    byte[] bothInputs = new byte[4];
                    Array.Copy(inputByte, 0, bothInputs, 0, 2);
                    //byte[] oldInputs = BitConverter.GetBytes(previousInput.inputDelta);
                    bothInputs[3] = previousInput.move;
                    bothInputs[2] = previousInput.id;
                    //Array.Copy(oldInputs, 0, bothInputs, 6, 4);
                    client.Send(bothInputs, 4);
                    sendOldInput = false;
                    lostPacketsInRow = 0;

                }
                else
                {
                    
                    client.Send(inputByte, 2);
                    lostPacketsInRow = 0;
                }
                */
            }
            else
            {
                Debug.Log("DROPPED PACKET");
                //Debug.Log("LOST PACKET "+(PlayerMovement)curMove.move);
            }
        }



        
        //else
        //{
        //    Byte[] sendBytes = Encoding.UTF8.GetBytes(sendText.text);
        //    client.Send(sendBytes, sendBytes.Length);
        //    sendText.text = "";
        //}
    }

    void OnDestroy()
    {
        Debug.Log("PACKET COUNT CLIENT " + packetCount);
        client.Close();
    }
}
