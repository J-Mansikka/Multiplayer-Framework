using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestiUkkoMove : MonoBehaviour
{
    CharacterController cc;
    Vector3 startPos;
    Animator animator;

    public float moveSpeed;
    int moveSpeedID;

    private void Awake()
    {
        startPos = transform.position;
        cc = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        moveSpeedID = Animator.StringToHash("TESTSPEED");
    }

    private void Update()
    {
        animator.SetFloat(moveSpeedID, moveSpeed);

        cc.Move(new Vector3(0f,-5f, moveSpeed) * Time.deltaTime);
    }
}
