using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MtestSpawner : MonoBehaviour, IMnetInstanceSpawner
{
    public MnetObject[] playerObjects;
    public MnetObject[] objects;
    public MnetInstanceManager manager;

    public MnetObject[] GetActiveObjects()
    {
        return objects;
    }

    public void DespawnRequest(MnetObject objectToDespawn)
    {
        throw new System.NotImplementedException();
    }

    public MnetObject SpawnRequest(int objectID)
    {
        throw new System.NotImplementedException();
    }

    public void Tick()
    {
        throw new System.NotImplementedException();
    }

    public MnetObject[] GetPlayerObjects()
    {
        return playerObjects;
    }
}
