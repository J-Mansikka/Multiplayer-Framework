using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ServerSettings
{
    // TEMP AND TESTING VALUES
    //public int testMessageSize = 6;


    public static int maxPacketSize = 1400;
    public static int maxSegmentsInMessage = 16;
    public static byte flagSegmentsStart = 4;
    public static byte typeSegment = 2;
    public static byte sequenceSegment = 3;
    public static float tempPlayerSpeed = 15f;

    // Jos message possit hardkoodattu ni vois lisätä tänne et olis ihan nimellä eikä vaan [2] etc vaan indexSegment
}
