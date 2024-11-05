using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MnetClientTest : MonoBehaviour
{
    private MnetBool[] inputArray;
    public float updateStep = 0.2f;
    private float currentStep;

    private Vector3 movement;

    private Vector3 oldPos;
    private Vector3 newPos;

    private List<byte> inputHistory;
    private List<Vector3> moves;
    

    private void Awake()
    {
        movement = new Vector3(0f, 0f, 0f);
        currentStep = updateStep;
    }

    private void Update()
    {
        currentStep -= Time.deltaTime;
        if (currentStep <= 0f)
        {
            currentStep += updateStep;
            Move();
        }
    }

    private void Move()
    {
        movement.z = 0f;
        movement.x = 0f;
    }

    public void AddMove()
    {

    }
}
