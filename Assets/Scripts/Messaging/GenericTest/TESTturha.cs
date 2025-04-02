using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TESTturha : MonoBehaviour
{
    public bool fuckingGo;
    public bool testPackets;
    public MnetObject send;
    public MnetObject receive;
    private MnetPacket[] paketit;
    public MnetMessager messageSender;
    public MnetMessager messageReceiver;

    void Start()
    {

        paketit = new MnetPacket[10];

    }

    // Update is called once per frame
    void Update()
    {
        if (testPackets)
        {
            foreach(MnetObject obj in messageSender.objectsBeingSynced)
            {
                obj.ForceSizeTesti();
            }
            messageSender.WriteRegularPacket(messageSender.genericBuffer);
            messageSender.genericBuffer.packetForProcessing = messageSender.genericBuffer.buffer[0];
            /*
            for (int i = 0; i < 50; i++)
            {
                print(messageSender.buffer.buffer[0][i]);
            }
            */
            messageReceiver.ReadRegularPacket(messageSender.genericBuffer);
            testPackets = false;
        }

        if(fuckingGo)
        {
             // ! !! MITES TOI WRITE TOIMIKAA? IHA VAA CURRENTLENGTH KASVAA KUNNES TÄYNNÄ?
                // Laske kuinka iso eka paketti on ja kokeile antaa kahdessa erässä.
            send.ForceSizeTesti();
            int starttiPak = 0;

            for (int i = 0; i < paketit.Length; i++)
            {
                paketit[i] = new MnetPacket(false);
            }
            MnetPacket paketti = paketit[starttiPak];
            while (send.WriteChanges(paketti))
            {
                starttiPak++;
                paketti = paketit[starttiPak];
                for(int i = 0;i < paketit[starttiPak - 1].currentLength;i++)
                {
                  //print(" "+i+": "+paketit[starttiPak - 1][i]);
                }
            }
            //print("WROTE FIRST FLAG " + System.Convert.ToString(paketti[2], toBase: 2));
            //print("Paketti määrä " + (starttiPak + 1));
            //send.WriteChanges(paketti);


            // Vaihda jo oikee logiikka eli kirjota ku valmis ja laske pakettien määrä
            // ja sitte vaan luet segmentti kerrallaa kunnes paketti valmis ja toistaa kunnes kaikki paketit käyty läpi. Ei magic #
            bool reading = true;
            int curPak = 0;
            int packetSize;
            int readFrom;
            int segmentLength;
            while (curPak <= starttiPak)
            {
                paketti = paketit[curPak];
                packetSize = paketti.currentLength;
                packetSize -= paketti.headerLength;
                readFrom = paketti.headerLength;
                while (packetSize > 0)
                {
                    readFrom += 2;  // Skip ID
                    segmentLength = paketti[readFrom];
                    readFrom += 1; // Skip size
                    //print("PROCESSING FLAGS " + System.Convert.ToString(paketti[readFrom], toBase: 2));
                    //print("LENGTH " + segmentLength);
                    packetSize -= segmentLength+3;
                    receive.ReadChanges(paketti.Span(readFrom, segmentLength));
                    readFrom += segmentLength;
                }
                
                curPak++;                
            }
            /*
            print("KOKO PAKETTI");
            for (int i = 0; i < ServerSettings.maxPacketDataSize; i++)
            {
                print(" "+i+": "+paketti[i]);
            }
            */

            /*
            int firstStart = 3;//ServerSettings.headerServerCombinedLength + 3;
            int firstLength = paketti.Span(firstStart - 1, 1)[0];
            int secondStart = firstStart + firstLength;
            int secondLength = paketti.Span(secondStart - 1, 1)[0];
            print("First: " + firstLength + ". Second: " + secondLength);
            receive.ReadChanges(paketti.Span(firstStart,firstLength));
            receive.ReadChanges(paketti.Span(secondStart,secondLength));
            */
            fuckingGo = false;
        }
    }
}
