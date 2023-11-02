using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TEST_Movement : MonoBehaviour
{
    public float delay = 0f;
    float curTime = 0f;
    bool move;
    public Button button;
    bool toggle;

    CharacterController cc;
    // Start is called before the first frame update
    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
    }

    private void FixedUpdate()
    {

        cc.Move(Vector3.up * -10f * Time.deltaTime);

    }
}
