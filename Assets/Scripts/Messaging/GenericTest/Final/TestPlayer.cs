using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPlayer : NewObject
{
    public MnetVector2 pos;


    private void Awake()
    {
        Initialize();
    }

    public void _Move()
    {
        transform.position += new Vector3(pos.Value.x, 0, pos.Value.y);
    }

    public override void UpdateState()
    {
        Debug.Log("UPDATE STATE ON PLAYER");
    }
}
