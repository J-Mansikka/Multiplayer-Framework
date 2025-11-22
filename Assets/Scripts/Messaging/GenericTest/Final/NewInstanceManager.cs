using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NewInstanceManager : NewObject
{
    INewSpawner spawner;
    //NewObject[] worldObjectInstances;
    //PacketManager worldPacketManager;

    private bool serverVersion;
    private int reservedNumbers;
    private NewInstanceAction action;
    private MnetInt playerID;
    //private Queue<int> activeInstances;
    private List<int> activeIds;
    private Queue<int> availableInstanceIDs;

    public override void CreateSnapshot(PacketManager snapshotManager)
    {
        // Luodaan spawn action jokaselle aktiiviselle
        // !!!! ELI SPAWNIS PITÄÄ VERRATA ONKO JO OLEMASSA? TÄÄHÄN PAUKKUU KAIKILLE JOTKA ON JO PELISSÄ
        // Pitääks sittenki olla vielä erillinen instance id joka ei oo vaan index vaan oikeesti uniikki per objekti
        // Ehkä vois tehdä niin että jos connection state on normal ni skipataan instancemanageri snappi?... kaikki snapit?
        // Eli snap olis täysin ylimääränen update ennen normi tickkiä ja ne sitte uus pelaaja yhdistää ja pitäis olla ajantasalla?
        // if connectionState initialising && packetType.snap -> read ja muuten ignore aina? SÄÄÄSTÄIS MYÖS KAISTAA

        //for (int i = Mnet.reservedInstanceSlots; i < local.worldObjectInstances.Length; i++)
        //{
        
        for (int i = 0; i < activeIds.Count; i++)
        { 
            InstanceStruct activeObjectStruct;
            activeObjectStruct.objectID = local.worldObjectInstances[activeIds[i]].objectID;
            activeObjectStruct.instanceID = local.worldObjectInstances[activeIds[i]].instanceID;
            action.SetValue(activeObjectStruct);
            action.UpdateVariable();
            //action.SnapshotUpdate(snapshotManager);
            Debug.Log("InstanceManager Snapshot loop for " + local.worldObjectInstances[activeIds[i]]);
            Call("snapshotcheck");
        }

        // Toinen looppi jossa kutsutaan aktiivisten snapshotit
        for (int i = 0;i < activeIds.Count; i++)
        {
            local.worldObjectInstances[activeIds[i]].CreateSnapshot(snapshotManager);
        }
        snapshotManager.NextWritePacket();

    }


    public void SetupInstanceManager(MnetNetwork local, bool serverVersion)
    {
        instanceID = 0;
        //this.worldObjectInstances = worldObjectInstances;
        local.worldObjectInstances[0] = this;
        //this.worldPacketManager = worldPacketManager;
        //this.spawner = spawner;
        this.serverVersion = serverVersion;
        //packetManager = worldPacketManager;
        availableInstanceIDs = new Queue<int>();
        activeIds = new List<int>();
        this.local = local;
        if(serverVersion)  AddAvailableInstanceNumbers();
        //activeInstances = new Queue<int>();
    }

    public void AddAvailableInstanceNumbers()
    {
        reservedNumbers = 1 + Mnet.maxPlayerCount * Mnet.objectsPerPlayer;
        availableInstanceIDs.Clear();
        for (int i = reservedNumbers; i < local.worldObjectInstances.Length; i++)
        {
            availableInstanceIDs.Enqueue(i);
        }
    }

    public void SwitchSceneSpawner(INewSpawner sceneSpawner)
    {
        spawner = sceneSpawner;
    }

    public void SpawnPlayerRequest(int playerNumber)
    {
        playerID.Value = playerNumber;
        // Player numbers start at 1 so that they can displayed in-game without additional editing
        // This means the actual index is 1 (instanceManager) + player number - 1 * object count
        int playerIndex = 1 + (playerNumber - 1) * Mnet.objectsPerPlayer;

        for (int i = 0; i < Mnet.objectsPerPlayer; i++)
        {
            InstanceStruct instanceUpdate;
            instanceUpdate.objectID = i;
            instanceUpdate.instanceID = playerIndex+i;
            action.Value = instanceUpdate;
            Debug.Log("PlayerSpawnReq: PlayerNumber = "+playerNumber+", O = " + instanceUpdate.objectID + ", I = " + instanceUpdate.instanceID);
            NewObject playerObj = spawner.SpawnPlayerObject(i);
            playerObj.objectID = instanceUpdate.objectID;
            playerObj.instanceID = instanceUpdate.instanceID;
            playerObj.local = local;
            local.AddPlayerObject(playerObj, playerNumber);            
            activeIds.Add(playerObj.instanceID);
            Call("spawnplayer");
            NewController[] controllers = playerObj.GetComponentsInChildren<NewController>();
            for (int o = 0; o < controllers.Length; o++)
            {
                Destroy(controllers[i]);
            }
        }
    }

    public void DespawnPlayerRequest(int playerObjectID, int playerNumber)
    {
        InstanceStruct instanceUpdate;
        instanceUpdate.objectID = local.worldObjectInstances[playerNumber].objectID;
        instanceUpdate.instanceID = playerNumber;
        action.Value = instanceUpdate;
        Call("despawnplayer");
    }

    public NewObject SpawnRequest(int spawningObjectID)
    {
        InstanceStruct instanceUpdate;
        instanceUpdate.objectID = spawningObjectID;
        instanceUpdate.instanceID = availableInstanceIDs.Dequeue();
        action.Value = instanceUpdate;
        Call("spawn");
        if (serverVersion)
        {
            NewObject serverSideSpawn = spawner.SpawnObject(spawningObjectID);
            serverSideSpawn.objectID = instanceUpdate.objectID;
            serverSideSpawn.instanceID = instanceUpdate.instanceID;
            serverSideSpawn.local = local;
            local.worldObjectInstances[instanceUpdate.instanceID] = serverSideSpawn;
            return serverSideSpawn;
        }
        return null;
    }

    public void DespawnRequest(int despawningInstanceID)
    {
        InstanceStruct instanceUpdate;
        instanceUpdate.objectID = local.worldObjectInstances[despawningInstanceID].objectID;
        instanceUpdate.instanceID = despawningInstanceID;
        action.Value = instanceUpdate;
        availableInstanceIDs.Enqueue(action.Value.instanceID);
        activeIds.Remove(instanceUpdate.instanceID);
        Call("despawn");
    }

    public void _Spawn()
    {
        Debug.Log("Regular spawn " + gameObject.name+" "+action.Value.instanceID);
        NewObject spawned;
        if (!serverVersion)
        {
            spawned = spawner.SpawnObject(action.Value.objectID);
            spawned.objectID = action.Value.objectID;
            spawned.instanceID = action.Value.instanceID;
            spawned.local = local;
            local.worldObjectInstances[action.Value.instanceID] = spawned;
            if(!local.clientControlledInstances.Contains(spawned.instanceID))
            {
                NewController[] controllers = spawned.GetComponentsInChildren<NewController>();
                for (int i = 0; i < controllers.Length; i++)
                {
                    Destroy(controllers[i]);
                }
            }
        }
        else
        {
            spawned = local.worldObjectInstances[action.Value.instanceID];
            activeIds.Add(spawned.instanceID);
        }
        //activeInstances.Enqueue(action.Value.instanceID);
        // Data pitäis olla valmiina täs vaiheessa (Serveri sai heti suoraan ja clientit oikeassa järjestyksessä (spawn, data ja nyt setup)
        if (!spawned.isInitialized)  spawned.Initialize();
        spawned.UpdateState();
        Debug.Log("SPAWN ACTION " + action.Value.objectID + " " + action.Value.instanceID);
    }

    public void _Despawn()
    {
        spawner.DespawnObject(local.worldObjectInstances[action.Value.instanceID]);
    }

    public void _SpawnPlayer()
    {
        // PITÄISKÖ TAAS HETI SPAWNATA EKA SERVUL JA SIT CLIENTIL?
        // Serverversion boolil vois kyl tässäki erottaa et pitää vaa hakee playernumballa oikee connection?
        // Mite vitus client löytää ittensä? Servu voi hakee yhteydet mut mites itte client? casti?

        // Aseta objekti valmiiks ja kutsu abstract core metodi? -> (obj, number) Varmaanki paras idea joo
        Debug.Log("Spawn player "+local.gameObject.name);
        NewObject spawned;
        if (!serverVersion && local.worldObjectInstances[action.Value.instanceID] == null)
        {
            Debug.Log("KÄYTII METODIN SISÄLLÄ");
            spawned = spawner.SpawnPlayerObject(action.Value.objectID);
            spawned.objectID = action.Value.objectID;
            spawned.instanceID = action.Value.instanceID;
            spawned.local = local;
            local.worldObjectInstances[action.Value.instanceID] = spawned;
            activeIds.Add(spawned.instanceID);
        }
        else
        {
            spawned = local.worldObjectInstances[action.Value.instanceID];
        }
        if(!spawned.isInitialized)  spawned.Initialize();
        local.AddPlayerObject(spawned, playerID.Value);
        spawned.UpdateState();
    }
    
    public void _DespawnPlayer()
    {
        spawner.DespawnObject(local.worldObjectInstances[action.Value.instanceID]);
    }

    public void _SnapshotCheck()
    {
        Debug.Log("SNAP CHECK "+action.Value.instanceID);
        // TARVIIKO SPAWNATA IS THE QUESTION
        if (local.worldObjectInstances[action.Value.instanceID] == null)
        {
            // SPAWN
            Debug.Log("EXTRA SPAWN FROM SNAP CHECK");
            _Spawn();
        }
        else if(local.worldObjectInstances[action.Value.instanceID].objectID != action.Value.objectID)
        {
            // FORCE DESPAWN -> SPAWN
            spawner.DespawnObject(local.worldObjectInstances[action.Value.instanceID]);
            _Spawn();
        }
    }

    public void UpdateInstanceArraySize()
    {

    }

    // Join scene that is already active (Mid game, need to instantiate objects AND get snapshot of all data)
    public void OnActiveSceneLoad()
    {

    }

    // Join scene that is has never been activated before (Can get objects directly)
    public void GetExistingObjectsInScene(INewSpawner spawner)
    {
        this.spawner = spawner;
        NewObject[] instancesInScene = spawner.GetSceneObjects();
        availableInstanceIDs.Clear();
        // Hae mitkä slotit on vapaana
        for (int i = reservedNumbers; i < local.worldObjectInstances.Length; i++)
        {
            if (local.worldObjectInstances[i] == null)
            {
                availableInstanceIDs.Enqueue(i);
            }
        }

        if (instancesInScene != null)
        {
            for (int i = 0; i < instancesInScene.Length; i++)
            {
                int freeInstanceID = availableInstanceIDs.Dequeue();
                local.worldObjectInstances[freeInstanceID] = instancesInScene[i];
                local.worldObjectInstances[freeInstanceID].instanceID = freeInstanceID;
                local.worldObjectInstances[freeInstanceID].local = local;
            }
        }

        for (int i = 0; i < local.worldObjectInstances.Length; i++)
        {
            if (local.worldObjectInstances[i] != null) Debug.Log(local.worldObjectInstances[i].name);
        }
    }

    public override void UpdateState()
    {

    }
}
