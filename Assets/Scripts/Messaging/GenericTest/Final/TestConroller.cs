using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestConroller : NewController
{
    float canMove = 1f;
    Vector2 movement;
    TestPlayer player;

    private void Awake()
    {
        player = GetComponent<TestPlayer>();
    }

    private void Update()
    {
        canMove -= Time.deltaTime;

        if(canMove < 0f)
        {
            movement = Vector2.zero;

            if (Input.GetKey(KeyCode.UpArrow))
            {
                movement.y = 1;
                canMove = 1f;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                movement.y = -1;
                canMove = 1f;
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                movement.x = -1;
                canMove = 1f;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                movement.x = 1;
                canMove = 1f;
            }

            if(movement !=  Vector2.zero)
            {
                Debug.Log("SEND MOVEMENT INPUT");
                player.pos.Value = movement;
                player.Call("move");
            }
        }
    }
}
