using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * HANDSHAKE PACKET[TYPE 1b?][MESSAGE 16b][IP ADDRESS 4b][PORT NUMBER 4b]
 * 
 * CLIENT PACKET [TYPE 1b] miks ei packet numba? [LAST FRAME/TICK RECEIVED 4b][IF FUCKED, HOW MANY 1b][PACKET ID 4b * x][INPUT DATA ?][PREVIOUS INPUTS...
 * SERVER PACKET [TYPE 1b][PACKET NUMBER 4b][TICK NUMBER 4b][PACKET SIZE 2b][TICK TIME 4b][SPLIT / TOTAL 2b][DATA X * Yb]
 * MISSING PACKET REQUEST [TYPE 1b][AMOUNT][DATA = PACKETID * AMOUNT]
 * SNAPSHOT REQUEST PACKET: [TYPE 1b][TICK NUMBER][MISSED PACKET COUNT (JOS HIRVEE NI VOIS KÄSKEE DISCONNECTAA)]
 */

public enum ByteSize
{
    Byte = 1, Short = 2, Int32 = 4
}
public static class Mnet
{
    // TEMP AND TESTING VALUES
    //public int testMessageSize = 6;

    //public const byte newConnectionIdentifier = 85;         //01010101
    //public const byte newClientKeyStart = 240;              //11110000
    //public const byte newClientKeyEnding = 15;              //00001111

    public const int int8 = 1;
    public const int int16 = 2;
    public const int int32 = 4;
    public const int int64 = 8;

    public const int ipAddressLength = 4;
    public const int portLength = 4;
    

    public const int messageLength = 16;
    public const string messageHandshakeRequest = "openthesilodoors";
    public const string messageHandshakeResponse = "heresyourhandler";
    public const string messageNewConnectionTest = "areyoumynewdaddy";
    public const string messageNewConnectionVerified = "ourpowerscombine";
    public const string messageReadyToStart = "readyandwilling!";
    public const string messageDisconnectByServer = "getoutofmyhouse!";
    public const string messageDisconnectByClient = "imgoinghomeseeya";

    public const byte newClientHandshakeIdentifier = 204;
    /*public const int packetCopiesRegular = 1;
    public const int packetCopiesSafer = 2;
    public const int packetCopiesImportant = 3;*/
    public const int heartbeatLimit = 250;
    public const int timeoutLimit = 1000;
    public const int maxMissedPacketsBeforeSnapshot = 20;
    public const int maxMissedPacketsBeforeDropout = 50;
    public const int snapshotCooldownInTicks = 250;
    public const float processingBufferDelay = 0.2f;    // sekunneissa bufferi delay eli jos ping
    public const int bufferBetweenWrittenAndProcessedTicks = (int)(processingBufferDelay / tickRate)+1; // Kui paljo varataa tickejä yhteysongelmien varalle. Simulaatio nopeude säätely?

    public const int maxPlayerCount = 8;
    public const int objectsPerPlayer = 2;
    public const int maxSyncedObjects = 512;
    public const int bytesReservedForInstanceID = 2;
    public const int startingReservedSizeForSyncedObjects = 128;
    public const int serverPacketBufferSize = 4096;
    public const int clientPacketBufferSize = 128;          /// Yhteen pakettiin mahtuu paljo historiaa ni voi olla paljo pienempi ku serveri
    public const int worldStatePacketBufferSize = 128;
    public const int newConnectionListenerPacketSize = 28;
    public const int maxPacketDataSize = 1400; //1400;
    public const int minimumSpaceNeededForWriting = 20; // Lowest amount of space available that still allows a write attempt in segments and packets
    //public const int maxSegmentSize = 255;  // If size stored in 1 byte, 255 bytes is the max size of a single segment
                                            //public const int maxSizeForVariableSegment = 100; Ei oo käytös? Max koko on se mikä mahtuu segmenttii
                                            // ???



    //public const int headerServerMessageTypePosition = 0;
    //public const int headerServerMessageTypeLength = 1;
    //public const int headerServerPacketNumberPosition = headerServerMessageTypeLength;// 4
    //public const int headerServerPacketNumberLength = 4;
    //public const int headerServerTickNumberPosition = headerServerMessageTypeLength + headerServerPacketNumberLength;// 4
    //public const int headerServerTickNumberLength = 4;
    //public const int headerServerSizePosition = headerServerMessageTypeLength + headerServerPacketNumberLength + headerServerTickNumberLength;// 2
    //public const int headerServerSizeLength = 2;
    //public const int headerServerTickTimePosition = headerServerMessageTypeLength + headerServerPacketNumberLength 
    //    + headerServerTickNumberLength + headerServerSizeLength;// 4
    //public const int headerServerTickTimeLength = 4;
    //public const int headerServerTickSplitInfoPosition = headerServerMessageTypeLength + headerServerPacketNumberLength
    //    + headerServerTickNumberLength + headerServerSizeLength + headerServerTickTimeLength;     // 2
    //public const int headerServerTickSplitInfoLength = 2;
    //public const short headerServerCombinedLength = headerServerMessageTypeLength + headerServerPacketNumberLength
    //    + headerServerTickNumberLength + headerServerSizeLength + headerServerTickTimeLength + headerServerTickSplitInfoLength;

    // Header section positions and lengths. Length is the byte count of the item so headerSizeLength = 2 = int16 = max 32767
    public const int headerMessageTypePosition = 0;
    public const int headerMessageTypeLength = 1;
    public const int headerPacketNumberPosition = headerMessageTypePosition + headerMessageTypeLength;
    public const int headerPacketNumberLength = 4;
    public const int headerTickNumberPosition = headerPacketNumberPosition + headerPacketNumberLength;
    public const int headerTickNumberLength = 4;
    public const int headerSizePosition = headerTickNumberPosition + headerTickNumberLength;
    public const int headerSizeLength = 2;
    public const int headerDeltaTimePosition = headerSizePosition + headerSizeLength;
    public const int headerDeltaTimeLength = 4;
    public const int headerSequencePosition = headerDeltaTimePosition + headerDeltaTimeLength;
    public const int headerSequenceLength = 2;
    public const int headerPacketCountPosition = headerSequencePosition + headerSequenceLength; 
    public const int headerPacketCountLength = 2;   // index and total
    public const int headerCombinedLength = headerPacketCountPosition + headerPacketCountLength;// 1+4+4+2+4+2*2 = 19
    public const int headerClientTypePosition = 0;
    public const int headerClientLastProcessedTickPosition = 1; // 4
    public const int headerClientMissingPacketCountPosition = 5; // 1
    public const int headerClientCombinedLength = 6;
    //public const int headerPacketNumberPosition = 0;           // 4
    //public const int headerSizePosition = 4;          // 2
    //public const int headerTimePosition = 6;          // 4
    //public const int headerFrameNumberPosition = 10;  // 4
    //public const int headerFrameSplitPosition = 14;   // 2
    //public const short headerCombinedLength = 16;
    //public const int threadUpdateRate = 5;
    //public const int bytesReservedForActionID = 1;
    //public const int bytesReserverdForActionCount = 1;
    public const int bytesReservedForArrayIndex = 1;
    public const int bytesReservedForObjectID = 2;// ! OBJECT VOIS ANTAA?    //2;
    public const int bytesReservedForLimitedItemSize = 1;
    //public const int bytesReservedForSegmentSize = 1;
    //public const int maxItemSizeBytes = 2;  // 32k
    public const int objectHeaderIdLength = bytesReservedForObjectID; //+ bytesReservedForSegmentSize;
    public const int bytesReservedForSplitItemSize = 2; // Splitattu voi olla useamman paketin kokone ni kaks ehdottomasti
    public const int variableVaryingHeaderLength = 1; // Ehkä loogista joo että vaihteleva koko max olis 256 ja siitä isommat voidaa olettaa että splitataan  bytesReservedForSegmentSize;
    public const int variableMultipartHeaderLength = bytesReservedForSplitItemSize * 3;//  + maxItemSizeBytes; // Eli nyt olis alotuskohta, määrä ja koko? eli 2 * 3 ?
    public const int variableDividableHeaderAdjustment = 100;

    public const float receiveRate = 0.0125f;       // 80hz
    public const float clientSendRate = 0.015625f;  // 64hz
    public const float serverSendRate = 0.03125f;   // 32hz
    public const float inputRate = 0.01f;           // 100hz
    public const float tickRate = 0.03125f;         // 32hz

    // OLD SHIT
    public const float tempPlayerSpeed = 15f;
    public const byte flagSegmentsStart = 4;
    public const byte typeSegment = 2;
    public const byte sequenceSegment = 3;
    public const int idLength = 2;
    public const int maxSegmentsInMessage = 16;
    // Jos message possit hardkoodattu ni vois lisätä tänne et olis ihan nimellä eikä vaan [2] etc vaan indexSegment
}
