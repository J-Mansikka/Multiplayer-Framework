using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * HANDSHAKE PACKET[TYPE 1b?][MESSAGE 16b][IP ADDRESS 4b][PORT NUMBER 4b]
 * 
 * CLIENT PACKET [TYPE 1b] miks ei packet numba? [LAST FRAME/TICK RECEIVED 4b][IF FUCKED, HOW MANY 1b][PACKET ID 4b * x][INPUT DATA ?][PREVIOUS INPUTS...
 * SERVER PACKET [TYPE 1b][PACKET NUMBER 4b][TICK NUMBER 4b][PACKET SIZE 2b][TICK TIME 4b][SPLIT / TOTAL 2b][DATA X * Yb]
 */

public enum ByteSize
{
    Byte = 1, Short = 2, Int32 = 4
}
public static class MnetSettings
{
    // TEMP AND TESTING VALUES
    //public int testMessageSize = 6;

    //public const byte newConnectionIdentifier = 85;         //01010101
    //public const byte newClientKeyStart = 240;              //11110000
    //public const byte newClientKeyEnding = 15;              //00001111

    public const string messageClientNewConnectionRequest = "openthesilodoors";
    public const string messageServerNewConnectionResponse = "heresyourhandler";
    public const string messageClientHandshake = "areyoumynewdaddy";
    public const string messageHandlerHandshakeResponse = "ourpowerscombine";
    public const string messageClientReadyToStart = "readyandwilling!";
    public const string messageDisconnectByServer = "getoutofmyhouse!";
    public const string messageDisconnectByClient = "imgoinghomeseeya";

    public const byte newClientHandshakeIdentifier = 204;
    public const int redundantCopiesHandshake = 3;
    public const float clientTimeoutTime = 60f;

    public const int maxPlayerCount = 8;
    public const int maxSyncedObjects = 512;
    public const int startingReservedSizeForSyncedObjects = 128;
    public const int serverPacketBufferSize = 4096;
    public const int clientPacketBufferSize = 128;          /// Yhteen pakettiin mahtuu paljo historiaa ni voi olla paljo pienempi ku serveri
    public const int worldStatePacketBufferSize = 128;
    public const int newConnectionListenerPacketSize = 28;
    //public const int bytesReservedForItemSizeValue = 2;
    public const int maxPacketDataSize = 1400; //1400;
    public const int minimumSpaceNeededForWriting = 20; // Lowest amount of space available that still allows a write attempt in segments and packets
    public const int maxSegmentSize = 255;  // If size stored in 1 byte, 255 bytes is the max size of a single segment
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
    public const int headerServerMessageTypePosition = 0;
    public const int headerServerMessageTypeLength = 1;
    public const int headerServerPacketNumberPosition = headerServerMessageTypePosition + headerServerMessageTypeLength;
    public const int headerServerPacketNumberLength = 4;
    public const int headerServerTickNumberPosition = headerServerPacketNumberPosition + headerServerPacketNumberLength;
    public const int headerServerTickNumberLength = 4;
    public const int headerServerSizePosition = headerServerTickNumberPosition + headerServerTickNumberLength;
    public const int headerServerSizeLength = 2;
    public const int headerServerTickTimePosition = headerServerSizePosition + headerServerSizeLength;
    public const int headerServerTickTimeLength = 4;
    public const int headerServerTickSplitInfoPosition = headerServerTickTimePosition + headerServerTickTimeLength;
    public const int headerServerTickSplitInfoLength = 2;
    public const int headerServerCombinedLength = headerServerTickSplitInfoPosition + headerServerTickSplitInfoLength;// 1+4+4+2+4+2 = 17
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
    public const int bytesReservedForObjectID = 2;// ! OBJECT VOIS ANTAA?    //2;
    public const int bytesReservedForSegmentSize = 1;
    public const int objectHeaderIdAndSizeLength = bytesReservedForObjectID + bytesReservedForSegmentSize;
    public const int bytesReservedForSplitItemSize = 2;
    public const int variableVaryingHeaderLength = bytesReservedForSegmentSize;
    public const int variableDividableHeaderLength = bytesReservedForSplitItemSize * 2 + bytesReservedForSegmentSize;
    public const int variableDividableHeaderAdjustment = 100;

    public const float clientSendRate = 0.015625f;  // 64hz
    public const float tickRate = 0.03125f;         // 32hz
    public const float serverSendRate = 0.03125f;   // 32hz

    // OLD SHIT
    public const float tempPlayerSpeed = 15f;
    public const byte flagSegmentsStart = 4;
    public const byte typeSegment = 2;
    public const byte sequenceSegment = 3;
    public const int idLength = 2;
    public const int maxSegmentsInMessage = 16;
    // Jos message possit hardkoodattu ni vois lisätä tänne et olis ihan nimellä eikä vaan [2] etc vaan indexSegment
}
