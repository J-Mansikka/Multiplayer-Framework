using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MessagingDirection { Incoming, Outgoing }
public class MnetObjectContainer
{
    private MessagingDirection _direction;    //// Koska n‰it‰ on useampi ni t‰‰l on kanssa eli auto vois olla toimiva sittenki?
    private MnetObject[] _objects;
    private Queue<short> freeIndex;
    private MnetObject _first;
    public MnetObject[] Objects { get { return _objects; } }
    public MnetObject First { get { return _first; } }
    //public MnetObject Last { get; }


    public MnetObjectContainer(MessagingDirection direction, int startingSize = MnetSettings.startingReservedSizeForSyncedObjects)
    {
        _objects = new MnetObject[startingSize];
        for (int i = 0; i < startingSize; i++)
        {
            freeIndex.Enqueue((short)i);
        }
        _direction = direction;
    }

    public MnetObjectContainer(MnetObject[] objectsToSync, MessagingDirection direction)
    {
        _objects = new MnetObject[objectsToSync.Length];
        _direction = direction;
        SetupContainer(objectsToSync);
    }

    public void AddObjects(MnetObject[] objectsToSync)
    {
        SetupContainer(objectsToSync);
    }

    private void SetupContainer(MnetObject[] objectsToSync)
    {
        MnetObject previouslyProcessed = objectsToSync[0];
        MnetObject currentlyProcessing;
        _first = previouslyProcessed;
        for (int i = 1; i < objectsToSync.Length; i++)
        {
            currentlyProcessing = objectsToSync[i];
            _objects[i] = currentlyProcessing;
            currentlyProcessing.prevActiveObject = previouslyProcessed;
            previouslyProcessed.nextActiveObject = currentlyProcessing;
            previouslyProcessed = _objects[i];
        }
    }


    public void AddObject(MnetObject obj, int objIndex)
    {

    }

    public void RemoveObject(int objIndex)
    {
        // Caching the obj to make this more readible
        MnetObject objToRemove = _objects[objIndex];
        // Remove object from the link and connect the two now loose ends
        objToRemove.prevActiveObject.nextActiveObject = objToRemove.nextActiveObject;
        objToRemove.nextActiveObject.prevActiveObject = objToRemove.prevActiveObject;
        // Remove object from the array, leaving an empty space
        objToRemove = null;
    }
}
