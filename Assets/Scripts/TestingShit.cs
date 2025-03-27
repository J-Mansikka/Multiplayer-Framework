
using UnityEngine;

public class TestingShit : MonoBehaviour
{

    public Mfloat floatti;
    public Mint sizeTesting;
    byte[] flags = new byte[10];
    public MnetStringUnicode strinki;
    private void Start()
    {   

        // V‰liaikainen sijoitus osan tavuille
        byte[] curItemAsBytes;
        // K‰sitelt‰v‰ index osalle ja bin‰‰rimuuttujalle
        int curItem = 0;
        // Bin‰‰rimuuttujia voi olla yli 8, jolloin tavuja on useampi
        int curFlagByte = 0;
        // Bin‰‰rimuuttujien kirjoituskohta
        int bitMask = 1;
        // Jos osa on muuttunut, se pit‰‰ l‰hett‰‰
        for (int i = 0; i < 28; i++)
        {
            curFlagByte = curItem / 8;

            print("BITFLAG " + bitMask + " ROUNDI " + curItem + " JA " + flags[curFlagByte]);

            // Asetetaan bin‰‰rimuuttuja
            flags[curFlagByte] = (byte)(flags[curFlagByte] | bitMask);

            // Seuraava kierros
            curItem++;
            if (curItem % 8 == 0)
            {
                bitMask = 1;
            }
            else
            {
                bitMask = bitMask << 1;
            }

        }
    }
    /*
    public float prevFixed = 0f;

    private void Start()
    {
        InvokeRepeating("GetTime", 1.0f, 0.04f);
    }

    void GetTime()//FixedUpdate()
    {
        Debug.Log("FixedUpdate realTime: " + (Time.realtimeSinceStartup - prevFixed));
        prevFixed = Time.realtimeSinceStartup;
    }

    void Update()
    {
        //Debug.Log("Update realTime: " + Time.realtimeSinceStartup);
    }
    */
}
