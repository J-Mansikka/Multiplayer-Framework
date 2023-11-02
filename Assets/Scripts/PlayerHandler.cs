using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerHandler
{
    public int playerID;
    public string playerName;
    GameObject player;
    //MonniStoredPlayerStep[] playerPacketBuffer;
    MessagePlayerMovement[] playerPacketBuffer;
    //Queue<MonniStoredPlayerStep> actionQueue;
    //Queue<MessagePlayerMovement> actionQueue;
    UdpClient udpClient;
    IPEndPoint playerEP;
    byte nextExpectedMessageNumber;
    //int packetCheckDelay;
    //byte bufferWriteHeadPos;
    //int bufferStartDelay;
    byte movesReceived;

    byte currentMove;
    PlayerMovement currentInputs;
    float distanceMovedSoFar;
    float distanceMovedByClient;
    bool getNextMove = false;
    bool delayedRun = true;
    Vector3 remainingMove = Vector3.zero;
    Vector3 distance = Vector3.zero;

    CharacterController cc;

    // MISSING PACKET SECTION
    int markIndexForRepair = -1;
    int missingPacketResendDelay;
    Dictionary<byte, byte> missingPackets;

    Vector3 moveDirection;

    int frameCount;
    float distanceTest;
    Vector3 prevPos;
    bool printOut = true;
    bool running;
    //public bool receivedMove;


    public PlayerHandler(int playerID, string playerName, GameObject player, int missingPacketResendDelay,UdpClient udpClient, IPEndPoint playerEP)
    {
        this.playerID = playerID;
        this.playerName = playerName;        
        this.player = player;
        this.missingPacketResendDelay = missingPacketResendDelay;
        this.udpClient = udpClient;
        this.playerEP = playerEP;
        cc = player.GetComponent<CharacterController>();
        moveDirection = new Vector3 (0, 0, 0);

        //actionQueue = new Queue<MessagePlayerMovement>();
        playerPacketBuffer = new MessagePlayerMovement[256]; //new MonniStoredPlayerStep[256];
        for (int i = 0; i < 256; i++)
        {
            playerPacketBuffer[i] = new MessagePlayerMovement();
            playerPacketBuffer[i].notInUse = true;
        }
        missingPackets = new Dictionary<byte, byte>();
        //packetCheckDelay = delayCheck * -1 - 1;
        //bufferStartDelay = delayCheck;

        prevPos = player.transform.position;
    }

    /*
    public void Move()
    {
        //Debug.Log(currentMove - 1);
        if (currentMove != 0)
        {

            // NOLLA VITTU
            //Debug.Log(playerPacketBuffer[currentMove - 1].dt.Item);
            // Debug.Log(playerPacketBuffer[currentMove - 1].sequenceNumber);
            Debug.Log("PLAYER :"+moveDirection);
            cc.Move(moveDirection * (Time.deltaTime / playerPacketBuffer[currentMove - 1].dt.Item) * ServerSettings.tempPlayerSpeed);
        }
    }
    */

    public void Tick()
    {
        //bool getNextMove = true;
        //moveDirection = Vector3.zero;

        if (running)
        {
            if (movesReceived < 5 && delayedRun)
            {

            }
            else
            {
                delayedRun = false;

                // GET NEXT MOVE
                while (/*movesReceived > 0 &&*/ getNextMove)//actionQueue.Count > 0)
                {
                    // Bufferi täytettiin valmiiksi
                    /*
                    if(playerPacketBuffer[currentMove] == null)
                    {
                        playerPacketBuffer[currentMove] = new MessagePlayerMovement();
                    }
                    */

                    if (!playerPacketBuffer[currentMove].notInUse)
                    {

                        //cc.Move(remainingMove);
                        distance = player.transform.position;
                        distanceMovedSoFar = 0f;
                        currentInputs = (PlayerMovement)playerPacketBuffer[currentMove].move.Item;//(PlayerMovement)actionQueue.Dequeue().move.Item;
                        distanceMovedByClient = playerPacketBuffer[currentMove].distance.Item;
                        Debug.Log("CURRENT "+currentMove+" DIST"+ distanceMovedByClient);
                        //player.transform.position += (new Vector3(xMov, 0f, zMov) * Time.fixedDeltaTime * 10f);
                        currentMove++;
                        movesReceived--;
                        getNextMove = false;
                    }
                    else if (currentMove < nextExpectedMessageNumber)
                    {
                        Debug.Log("SKIPPED");
                        currentMove++;
                    }
                    else
                    {
                        Debug.LogWarning(player.name + " could not move!");
                        getNextMove = false;
                    }
                }

                if (movesReceived == 0 && getNextMove)
                {
                    //Debug.Log("IDLING!!!!");
                }

                // NORMAL MOVE

                //moveDirection = new Vector3(xMov, 0f, zMov);


                //Debug.Log("DIRECTION: " + (new Vector3(xMov, 0f, zMov) +" TRAVELED: "+distanceMovedByClient+" LEFT: "+(distanceMovedByClient-distanceMovedSoFar)));

                if (!getNextMove)
                {
                    float xMov = 0f;
                    if (currentInputs.HasFlag(PlayerMovement.left)) xMov = -1f;
                    if (currentInputs.HasFlag(PlayerMovement.right)) xMov = 1f;
                    float zMov = 0f;
                    if (currentInputs.HasFlag(PlayerMovement.forward)) zMov = 1f;
                    if (currentInputs.HasFlag(PlayerMovement.backward)) zMov = -1f;

                    //Debug.Log("C DIST:" + distanceMovedByClient + " P DIST:" + distanceMovedSoFar);
                    /*

                    if ((distanceMovedSoFar + Time.deltaTime) < distanceMovedByClient)
                    {


                        cc.Move(new Vector3(xMov, 0f, zMov) * Time.deltaTime * ServerSettings.tempPlayerSpeed);
                        distanceMovedSoFar += Time.deltaTime;

                        float move = player.transform.position.x - prevPos.x;
                        prevPos = player.transform.position;
                        //Debug.Log("P MOVED " + move);
                        distanceTest += move;
                        frameCount++;
                    }
                    else
                    {
                        //Debug.Log("STOP AND MOVES REMAINING = " + movesReceived);
                        remainingMove = (new Vector3(xMov, 0f, zMov) * ServerSettings.tempPlayerSpeed * (distanceMovedByClient - distanceMovedSoFar));
                        cc.Move(remainingMove);

                        float move = player.transform.position.x - prevPos.x;
                        prevPos = player.transform.position;
                        //Debug.Log("MOVED " + move);
                        distanceTest += move;
                        frameCount++;

                        if (printOut)
                        {
                            printOut = false;
                            Debug.Log("PLAYER MOVED " + distanceTest + " IN " + frameCount + " FRAMES.");
                        }
                        
                    Debug.Log("DISTANCE MOVED BY PLAYER: " + (distance - player.transform.position) 
                        + " IN "+(distanceMovedSoFar + (distanceMovedByClient-distanceMovedSoFar))
                        + " BUT RECEIVED "+distanceMovedByClient);
                    

                        getNextMove = true;
                    }
                */
                    cc.Move(new Vector3(xMov, 0f, zMov) * distanceMovedByClient * ServerSettings.tempPlayerSpeed);
                    Debug.Log("DISTANCE MOVED BY PLAYER: " + (distance - player.transform.position)
    + " IN " + (distanceMovedSoFar + (distanceMovedByClient - distanceMovedSoFar))
    + " BUT RECEIVED " + distanceMovedByClient);
                    getNextMove = true;
                }


            }
        }
        //distanceMovedSoFar += Time.deltaTime;

        // RECONCILIATION MOVE
    }

    // Detect message type and use appropriate method to process it
    public void ReadPlayerBytes(byte[] messageBytes)
    {
        //Debug.Log("Message " + messageBytes[ServerSettings.sequenceSegment]);

        int nextMessageStart = 0;
        //receivedMove = true;

        while (nextMessageStart < messageBytes.Length)
        {
            // SPLIT... WE AINT SPLITTIN THIS SHIT LETS GOOO

            //SWITCH
            switch ((MessageType)messageBytes[ServerSettings.typeSegment+nextMessageStart])
            {
                case MessageType.ClientInput:                    
                    nextMessageStart += AddMovementPacket(messageBytes, nextMessageStart);
                    //Debug.Log("PROCESSED and new start " + nextMessageStart);
                    break;
                default:
                    //Debug.Log("DEF " + (MessageType)messageBytes[ServerSettings.typeSegment + nextMessageStart]);
                    nextMessageStart = messageBytes.Length;
                    Debug.LogError("Player message was not recognized.");
                    break;
            }

        }



        /*
        byte messageIndex = messageBytes[3];
        if (playerPacketBuffer[messageIndex] == null)
        {
            playerPacketBuffer[messageIndex] = new MessagePlayerMovement();
        }
        */
        //playerPacketBuffer[messageIndex].ToData(messageBytes);
        /*
        for (int i = 0; i < packetBytes.Length; i++)
        {
            Debug.Log("PACKETTi "+i +" "+ packetBytes[i]);
        }
        */
        //Debug.Log("PACKET NUMBER " + messageBytes[messageIndex] +" LENGTH "+messageBytes.Length);
        //byte packetNumber = packetBytes[3];
        //Debug.Log("BYTE ON "+packetBytes[5]);
        //byte playerInputs = packetBytes[6];
        //Debug.Log("MOVE " + playerInputs);
        //Vector3 rotation = new Vector3(BitConverter.ToInt16(packetBytes,2),BitConverter.ToInt16(packetBytes,4),0f);
        // Vähän nolosti luetaan luvut ja sitten lähtee valmis paketti metodiin jonka kai pitäis vasta tehdä koko setti :L
        //AddMovementPacket(playerPacketBuffer[messageIndex]);//new MonniStoredPlayerStep(packetNumber, playerInputs,player.transform.position));//new MonniMovementPacket(packetBytes[0], (PlayerMovement)packetBytes[1]));
    }

    public ushort AddMovementPacket(byte[] newMove, int newMoveStartPos)//MonniStoredPlayerStep newPacket)//MonniMovementPacket newPacket)
    {
        if (!running)
        {
            getNextMove = true;
            running = true;
        }

        byte moveMessageIndex = newMove[ServerSettings.sequenceSegment + newMoveStartPos];
        //Debug.Log("RECEIVED: " + newMove[newMoveStartPos+ServerSettings.sequenceSegment]);
        // Nullataan vanha että tiedetään jos jää tyhjä kohta.
        //playerPacketBuffer[(byte)(nextExpectedMessageIndex + 1)] = null;
        playerPacketBuffer[(byte)(nextExpectedMessageNumber + 1)].notInUse = true;
        int indexDifference = moveMessageIndex - nextExpectedMessageNumber; //newPacket.indexNumber - nextExpectedMessageIndex;

        ushort lastMessageLength = BitConverter.ToUInt16(newMove,newMoveStartPos);
        // Indexit täsmää
        if (indexDifference == 0)
        {
            //Debug.Log("Created "+nextExpectedMessageNumber);
            //playerPacketBuffer[nextExpectedMessageIndex] = newPacket;
            playerPacketBuffer[nextExpectedMessageNumber].ToData(newMove, newMoveStartPos);
            playerPacketBuffer[nextExpectedMessageNumber].notInUse = false;
            //actionQueue.Enqueue(newPacket);
            nextExpectedMessageNumber++;
            movesReceived++;
        }
        // Nykyinen index on edellä eli saatiin vanha paketti. Otetaan talteen jos vielä puuttuu. 255 on roll over kohta: 255 - 0 = 255
        else if( (indexDifference < 0 && indexDifference > -128)
            || indexDifference == 255)
        {
            //Debug.Log("ADDING OLD!");
            if (playerPacketBuffer[moveMessageIndex].notInUse)
            {
                playerPacketBuffer[moveMessageIndex].ToData(newMove, newMoveStartPos);
                playerPacketBuffer[moveMessageIndex].notInUse = false;
            }
        }
        // Joko paketti puuttuu koska ei koskaan saapunut tai tulee viivellä. Joka tapauksessa merkataan kohta josta aloitetaan korjaaminen.
        else
        {
            Debug.Log("MISSED A PACKET = " + nextExpectedMessageNumber+". Got = "+moveMessageIndex);
            if (markIndexForRepair < 0) markIndexForRepair = nextExpectedMessageNumber;
            playerPacketBuffer[moveMessageIndex].ToData(newMove,newMoveStartPos);
            playerPacketBuffer[moveMessageIndex].notInUse = false;
            //actionQueue.Enqueue(newPacket);
            nextExpectedMessageNumber = moveMessageIndex;
            nextExpectedMessageNumber++;
            movesReceived++;
        }



            /*
             * 
           int idDistanceFromEnd = Math.Abs(newPacket.id - 255);
           int curDistanceFromEnd = Math.Abs(currentPacketIndex - 255);

           // Kaikki toimii oikein ja voidaan vaan tallettaa paketti oikeaan kohtaan.
           if (newPacket.id == currentPacketIndex)
           {
               Debug.Log("PROCESSING PACKET "+newPacket.id+" AND INDEX IS AT " + currentPacketIndex);
               playerPacketBuffer[currentPacketIndex] = newPacket;
               actionQueue.Enqueue(newPacket);
               //bufferWriteHeadPos++;
               currentPacketIndex++;
           }
           // Kohdat ei täsmää ja kumpikaan byte ei ole pyörähtänyt vielä ympäri. Täten voidaan vaan verrata kumpi luku on isompi.
           else if (idDistanceFromEnd > newPacket.id && curDistanceFromEnd > currentPacketIndex
               || idDistanceFromEnd < newPacket.id && curDistanceFromEnd < currentPacketIndex)
           {
               // Paketti on vanha joten talletetaan jos ei ole vielä niin tehty.
               if (newPacket.id < currentPacketIndex)
               {
                   if(playerPacketBuffer[newPacket.id] == null) playerPacketBuffer[newPacket.id] = newPacket;
               }
               // Tullut paketti on edempänä kuin tämä serverside client. Merkataan korjaamista varten ja hypätään uuteen kohtaan.
               else
               {
                   Debug.Log("EKA = " + newPacket.id+" "+currentPacketIndex );
                   if (markIndexForRepair < 0) markIndexForRepair = currentPacketIndex;
                   playerPacketBuffer[newPacket.id] = newPacket;
                   actionQueue.Enqueue(newPacket);
                   currentPacketIndex = newPacket.id;
                   currentPacketIndex++;
               }
           }
           // Kohdat ei täsmää ja joku luku on rollannu yli.
           else
           {
               // current index on jäljessä. Eli merkataan kohta ja index hyppää uudempaan.
               if (idDistanceFromEnd < newPacket.id && curDistanceFromEnd > currentPacketIndex)
               {
                   Debug.Log("TOKA = " + newPacket.id + " " + currentPacketIndex);
                   if (markIndexForRepair < 0) markIndexForRepair = currentPacketIndex;
                   playerPacketBuffer[newPacket.id] = newPacket;
                   actionQueue.Enqueue(newPacket);
                   currentPacketIndex = newPacket.id;
                   currentPacketIndex++;
               }
               // Saapunut paketti on vanhempi eli talletetaan vaan jos ei ole jo bufferissa.
               else if (idDistanceFromEnd > newPacket.id && curDistanceFromEnd < currentPacketIndex)
               {
                   if (playerPacketBuffer[newPacket.id] == null)
                   {
                       playerPacketBuffer[newPacket.id] = newPacket;
                   }
               }
               else
               {
                   Debug.LogError("Something went wrong with packet " + newPacket.id);
               }
           }

           */


            if (markIndexForRepair >= 0)
        {
            //VerifyBuffer();
        }

        return lastMessageLength;
    }

    public void VerifyBuffer()
    {

        /*
         *  VIIVE TARKASTUSTEN vÄLILLÄ ETTEI MEE TUKKOON? 
         *  KERÄÄ PUUTTUVAT LISTAAN JA PYYDÄ UUDESTAAN.
         *  JOS EI EHDI (BUFFERI MENEE YLI), TELEPORTTAA PELAAJA TURVALLISEEN OIKEAAN KOHTAAN 
         *  
         */


        //List<byte> missingPackets = new List<byte>();

        //KÄÄNNÄ SUUNTA. OTA HUOMIOON JOS TULEE LISÄÄ ONGELMIA KU KORJAUS KESKEN

        //missingPackets = new List<byte>();
        // KATSO ONKO PUUTTUVIA
        byte missingCheckIndex = (byte)markIndexForRepair;


        while(missingCheckIndex != nextExpectedMessageNumber)
        {
            if (playerPacketBuffer[missingCheckIndex].notInUse)
            {
                if (!missingPackets.ContainsKey(missingCheckIndex))
                {
                    missingPackets.Add(missingCheckIndex, 0);
                }
                else
                {
                    missingPackets[missingCheckIndex]++;
                    /*
                    if (missingPackets[missingCheckIndex] % 250 == 0)
                    {
                        // SHUT IT DOWN
                        // JUMP PACK TO LAST KNOWN POSITION PACKET
                        // TELL SERVER SHIT IS FUCKED
                    }
                    else if (missingPackets[missingCheckIndex] % missingPacketResendDelay == 0)
                    {
                        // ASK FOR IT AGAIN
                        // MAYBE TRY TO POOL ALL OF THEM : COLLECT ON LIST AND SEND THAT INSTEAD OF EVERY SINGLE ONE ON ITS OWN
                    }
                    */
                }
            }
            else if(missingPackets.ContainsKey(missingCheckIndex))
            {
                missingPackets.Remove(missingCheckIndex);
            }

            missingCheckIndex++;
        }

        List<byte> resendPackets = new List<byte>();

        foreach(var packet in missingPackets) 
        {
            if(packet.Value % 250 == 0)
            {
                // SHUT DOWN
                Debug.LogWarning("ALMOST ROLLING OVER! SHUTTING IT DOWN!");
                markIndexForRepair = -1;
            }
            else if(packet.Value % missingPacketResendDelay == 0)
            {
                resendPackets.Add(packet.Key);
                // ADD TO RESEND PACKET
            }
        }

        //RESEND JA TIMER?

        // MITÄ JOS TULEE VÄLISSÄ LISÄÄ?

        // TIMEOUT 255 max

        // IF TIMER != 0 ASK FOR RESEND ELSE JUST GO

        if(resendPackets.Count > 0)
        {
            Send(resendPackets.ToArray());
        }


        if(missingPackets.Count == 0 && resendPackets.Count == 0)
        {
            // AJA LIIKKEET UUDESTAAN ALUSTA YHDEN FRAMEN SISÄLLÄ
            markIndexForRepair = -1;
        }

        

        /*
        if (bufferStartDelay > 0)
        {
            bufferStartDelay--;
        }
        else
        {
            byte checkPos = (byte)(bufferWriteHeadPos - packetCheckDelay);

            // JOS KORKEEMPI PAKETTI TULEE NI ON ONGELMA JA MERKATAAN NYKYNEN JA HYPÄTÄÄN UUTEEN
            // VANHAT VAAN HYVÄKSYTÄÄN {TAI TEE DUPLICATE TARAKASTUS AINAKI} KOSKA ON LUULTAVASTI PUUTTUVIA


            MonniMovementPacket packetToCheck = playerPacketBuffer[checkPos];

            if (packetToCheck == null || packetToCheck.id != checkPos)
            {

            }
        }
        */
    }

    private void Send(byte[] data)
    {
        Debug.Log("NEED FOLLOWING PACKETS:");
        foreach(byte pac in data)
        {
            Debug.Log(pac);
        }
    }
}
