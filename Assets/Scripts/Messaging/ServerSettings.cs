using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ServerSettings
{
    // TEMP AND TESTING VALUES
    //public int testMessageSize = 6;

    public const byte newConnectionIdentifier = 85;         //01010101
    public const byte newClientKeyStart = 240;              //11110000
    public const byte newClientKeyEnding = 15;              //00001111

    public const string messageNewConnectionRequest = "openthesilodoors";
    public const string messageNewConnectionResponse = "heresyourhandler";
    public const string messageClientToHandlerHandshake = "areyoumynewdaddy";
    public const string messageHandlerToClientResponse = "ourpowerscombine";
    public const string messageClientReadyToStart = "readyandwilling!";

    public const byte newClientHandshakeIdentifier = 204;
    public const int redundantCopiesHandshake = 3;
    public const float clientHandshakeTimeout = 60f;

    public const int maxPlayerCount = 8;
    public const int maxSyncedObjects = 512;
    public const int serverPacketBufferSize = 4096;
    public const int clientPacketBufferSize = 128;
    public const int newConnectionListenerPacketSize = 28;
    public const int bytesReservedForItemSizeValue = 2;    // PITÄÄ VAIHTAA JOKU PAREMPI TILALLE. ANTAA ENEMMÄN VAIHTOEHTOI JA STATIC METODI?
    public const int maxPacketSize = 1400;
    public const int maxSegmentsInMessage = 16;
    public const byte flagSegmentsStart = 4;
    public const byte typeSegment = 2;
    public const byte sequenceSegment = 3;

    public const int headerPacketNumberPosition = 0;    // 4
    public const int headerTickNumberPosition = 4;    // 4
    public const int headerSizePosition = 8;            // 2
    public const int headerTimePosition = 10;            // 4
    public const int headerFrameSplitPosition = 14;     // 2
    public const short headerCombinedLength = 16;
    //public const int headerPacketNumberPosition = 0;           // 4
    //public const int headerSizePosition = 4;          // 2
    //public const int headerTimePosition = 6;          // 4
    //public const int headerFrameNumberPosition = 10;  // 4
    //public const int headerFrameSplitPosition = 14;   // 2
    //public const short headerCombinedLength = 16;
    //public const int threadUpdateRate = 5;
    public const int objectHeaderIdAndSizeLength = 4;
    public const int idLength = 1;

    public const float clientSendRate = 0.02f;
    public const float serverSendRate = 0.05f;

    // OLD SHIT
    public const float tempPlayerSpeed = 15f;

    // Jos message possit hardkoodattu ni vois lisätä tänne et olis ihan nimellä eikä vaan [2] etc vaan indexSegment
}
