using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MnetPlayerInputs : MnetObject
{
    public MnetClientTest clint;

    private MnetBool inputArray;
    public float updateStep = 0.2f;
    private float currentStep;

    private CharacterController cc;
    private Vector3 movement;

    private Vector3 oldPos;
    private Vector3 newPos;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        movement = new Vector3(0f,0f,0f);
        inputArray = new MnetBool();
        //variables = new MnetVariable[]{inputArray};
        //Setup();
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
        
        if(Input.GetKey(KeyCode.UpArrow))
        {
            inputArray.Set(true, 0);
            inputArray.Set(false, 1);
            movement.z = 1;
        }
        else if(Input.GetKey(KeyCode.DownArrow))
        {
            inputArray.Set(false, 0);
            inputArray.Set(true, 1);
            movement.z = -1;
        }
        if(Input.GetKey(KeyCode.LeftArrow))
        {
            inputArray.Set(true, 2);
            inputArray.Set (false, 3);
            movement.x = -1;
        }
        else if(Input.GetKey(KeyCode.RightArrow))
        {
            inputArray.Set(false, 2);
            inputArray.Set(true, 3);
            movement.x = 1;
        }

        cc.Move(movement);
       

        //oldPos = transform.position;
        //newPos = transform.position + new Vector3(x, 3, y);
    }
}

