using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TESTmoveremote : MonoBehaviour
{
    Queue<Vector3> moves;
    Queue<float> timeStep;
    float tickTimer;
    float moveTimer;
    float speed = 5f;
    float velocity = 0f;
    Vector3 start;
    Vector3 destination;
    Vector3 newMove;
    float moveTime;
    float travelTime;

    private void Awake()
    {
        tickTimer = -1f;
        moveTimer = 0f;
        moveTime = 0f;
        moves = new Queue<Vector3>();
        timeStep = new Queue<float>();
        destination = transform.position;
        start = destination;
    }
    private void Update()
    {

        tickTimer += Time.deltaTime;

        //Debug.Log("REMOTE: start "+start+" end "+destination +" timescale "+(moveTimer/moveTime));
        //transform.position = (Vector3.Lerp(start, destination, moveTimer / moveTime));
        if (tickTimer > 1f)
        {
            moveTimer += Time.deltaTime;
            if(moveTime > 0f)transform.position = (Vector3.Lerp(start, destination, moveTimer / moveTime));
            if (moveTimer > moveTime)
            {
                //Debug.Log("THIS " + moveTimer + " RECEIVED " + moveTime);
                moveTimer -= moveTime;
                //Debug.Log("TIMER NOW " + moveTimer);
                newMove = moves.Dequeue();
                moveTime = timeStep.Dequeue();
                //Debug.Log(moveTime);
                start = transform.position;
                //Debug.Log(transform.position.y);
                start = transform.position;
                //velocity = speed * moveTime;
                destination = start + newMove; //new Vector3(newMove.x, 0f, newMove.y) * velocity;
                //transform.position = destination;
                //transform.Translate(new Vector3(newMove.x, 0f, newMove.y) * speed * moveTime);
            }

        }
    }

    public void AddMove(Vector3 move,  float time)
    {
        moves.Enqueue(move);
        timeStep.Enqueue(time);
    }
}
