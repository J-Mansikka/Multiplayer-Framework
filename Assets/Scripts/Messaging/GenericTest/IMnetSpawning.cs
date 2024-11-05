using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMnetSpawning
{
    public MnetObject RemoteSpawnRequest(short objectTypeID, short objectID);

    public MnetObject RemoteDespawnRequest(short objectID);

    public void SendSpawnAction();
    public void SendDespawnAction();
}
