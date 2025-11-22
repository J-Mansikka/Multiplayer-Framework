using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPrefab : NewObject
{
    public bool testData;
    public MnetInt pubInt;
    private MnetInt privInt;
    public float tickTestRate = 0.25f;
    private float timer = 0f;
    private int counting = 1;

    public MnetStringUnicode message;
    private MnetStringUnicode hiddenMessage;

    private void Awake()
    {
        //privInt = new MnetInt();
        Initialize();
    }

    private void Update()
    {
        if (testData)
        {
            testData = false;
            pubInt.Value = 69;
            privInt.Value = 31;
            Debug.Log("DID SOME MATH "+pubInt.Value+privInt.Value);
        }

        timer += Time.deltaTime;
        if(timer > tickTestRate)
        {
            timer -= tickTestRate;
            string newMessage = "counted to " + counting++;
            //Debug.Log(name + newMessage);
            message.Value = newMessage;
        }
    }

    public override void UpdateState()
    {
        Debug.Log(hiddenMessage);
    }
}
