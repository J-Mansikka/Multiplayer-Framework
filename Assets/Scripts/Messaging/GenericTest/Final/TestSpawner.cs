using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestSpawner : MonoBehaviour, INewSpawner
{

    public NewObject[] sceneObjects;

    private void Awake()
    {
        for (int i = 0; i < sceneObjects.Length; i++)
        {
            sceneObjects[i].objectID = i;
        }
    }
    public void DespawnObject(NewObject returningObject)
    {
        Destroy(returningObject);
    }

    public void DespawnPlayer(NewObject returningPlayerObject)
    {
        Destroy(returningPlayerObject);
    }

    public NewObject[] GetSceneObjects()
    {
        NewObject[] simpleFind = FindObjectsByType<NewObject>(FindObjectsSortMode.None);
        for (int i = 0; i < simpleFind.Length; i++)
        {
            Debug.Log("UNITY FOUND " + simpleFind[i].name);
        }
        SortedList<string,NewObject> simpleSort = new SortedList<string,NewObject>();
        for(int i = 0; i < simpleFind.Length; i++)
        {
            NewObject foundObject = simpleFind[i];
            if (foundObject.gameObject.name != "Server" && foundObject.gameObject.name != "Client")  simpleSort.Add(foundObject.gameObject.name, foundObject);
        }

        simpleFind = simpleSort.Values.ToList().ToArray();
        Debug.Log("SPAWNER FOUND " + simpleFind.Length);
        return simpleSort.Values.ToList().ToArray();
    }

    public NewObject SpawnObject(int objectID)
    {
        return Instantiate(sceneObjects[objectID]);
    }

    public NewObject SpawnPlayerObject(int objectID)
    {
        return SpawnObject(objectID);
        //return Instantiate(playerObjects[objectID]);
    }
}
