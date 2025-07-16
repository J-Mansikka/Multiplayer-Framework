using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class MnetOrc : MnetObject
{
    public bool client;
    public MnetStringUnicode name;
    public MnetInt level;
    public MnetInt exp;
    public MnetStringUnicode longTest_1;
    public MnetStringUnicode longTest_2;
    public MnetStringUnicode longTest_3;
    public MnetVariableArray<MnetInt> intit;
    public MnetVariableArray<MnetStringUnicode> stringit;

    private MnetVector3 startPos;
    private MnetVector3 endPos;
    private MnetFloat timeSpent;

    private float timePassed;
    private float distance;
    public bool moving;


    public void Awake()
    {
        //variables = new MnetVariable[] {hitpoints, level, phoneNumber, coins, xp, power, enemyClass, nimi, phone, osote, radio};
        //variables = new MnetInt[10];
        //int intFill = 1;
        startPos = new MnetVector3();
        startPos.Value = new Vector3(-20f, 1f, 0f);
        endPos = new MnetVector3();
        endPos.Value = new Vector3(20f, 1f, 0f);
        timeSpent = new MnetFloat();
        distance = 10f;
        Initialize();
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

    private void Update()
    {
        if (moving)
        {
            timePassed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos.Value, endPos.Value, timePassed / distance);
        }
    }


    public byte ActionSayMyName()
    {
        Debug.Log("I am called " + name);
        return 1;
    }

    public byte ActionMove()
    {
        timePassed -= distance;
        timeSpent.Value = timePassed;
        Vector3 tempPos = startPos.Value;
        startPos.Value = endPos.Value;
        endPos.Value = tempPos;
        return 2;
    }

    public override void SyncUp()
    {
        moving = true;
    }

    public override void RegularTick()
    {
        moving = true;
        Debug.Log("ORC TICK CALLED "+timePassed);
        if(timePassed > distance)
        {
            Sync(ActionMove());
        }
    }

    public override void SnapshotTick()
    {
        Debug.Log("Remote Tick");
    }
}
