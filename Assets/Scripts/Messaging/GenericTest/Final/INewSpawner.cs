using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface INewSpawner
{
    public NewObject SpawnObject(int objectID);
    public void DespawnObject(NewObject returningObject);

    public NewObject SpawnPlayerObject(int objectID);

    public void DespawnPlayer(NewObject returningPlayerObject);

    // If the player joins before the scene has become active, some instances may already be loaded and can be added to managers list directly
    public NewObject[] GetSceneObjects();

}
