using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class MnetOrc : MnetObject
{
    public bool client;
    public MnetString name;
    public MnetInt level;
    public MnetInt exp;
    private void Awake()
    {
        //variables = new MnetVariable[] {hitpoints, level, phoneNumber, coins, xp, power, enemyClass, nimi, phone, osote, radio};
        //variables = new MnetInt[10];
        //int intFill = 1;

        Setup(this);

        //for (int i = 0; i < 40; i++)
        //{
        //    Debug.Log("bits = "+i+". Bytes needed = "+ (1 + (int)((i-1) / 8)));
        //}
        //int intti = 2049;
        //byte[] asBytte = new byte[4];
        //BinaryPrimitives.TryWriteInt32LittleEndian(asBytte.AsSpan(), intti); //BitConverter.GetBytes(intti);
        //foreach (byte b in asBytte)
        //{
        //    print(Convert.ToString(b, toBase: 2));
        //}
        //byte[] testickle = new byte[currentSize];

        //GetChanges(testickle.AsSpan());

        //if (target != null)
        //{
        //    target.GetComponent<MnetObject>().ReceiveChanges(testickle.AsSpan());
        //}

        /*
        foreach (byte b in testickle)
        {
            print(Convert.ToString(b, toBase: 2));
        }
        */

    }

    public override void Tick()
    {
        Debug.Log("Ticking");
    }
}
