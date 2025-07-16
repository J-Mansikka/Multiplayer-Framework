using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MnetInstanceMessageSegment
{
    public ObjectInstanceAction action;
    public int instanceID;
    public int objectID;

    public MnetInstanceMessageSegment(ObjectInstanceAction newAction, int instance, int obj)
    {
        action = newAction;
        instanceID = instance;
        objectID = obj;
    }
}
