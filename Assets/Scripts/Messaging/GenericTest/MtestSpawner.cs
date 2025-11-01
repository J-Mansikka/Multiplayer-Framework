using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MtestSpawner : MonoBehaviour, IMnetUserInstanceSpawner
{
    public WANHAMnetObject[] playerObjects;
    public WANHAMnetObject[] objects;
    //public MnetInstanceManager manager;

    public WANHAMnetObject[] GetActiveObjects()
    {
        return objects;
    }

    public void DespawnRequest(WANHAMnetObject objectToDespawn)
    {
        throw new System.NotImplementedException();
    }

    public WANHAMnetObject SpawnRequest(int objectID)
    {
        throw new System.NotImplementedException();
    }

    public void Tick()
    {
        throw new System.NotImplementedException();
    }

    public WANHAMnetObject[] GetPlayerObjects()
    {
        return playerObjects;
    }
}
