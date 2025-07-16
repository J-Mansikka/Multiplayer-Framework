using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MnetInstanceManager : MnetObject, IMnetInstanceManager
{
    public GameObject spawnHolder;
    private MnetMessager messager;
    private MnetVariableArray<MnetInstanceInfo> instances;
    private MnetObject[] syncedObjects;
    public IMnetInstanceSpawner spawner;
    private Queue<int> availableInstanceID;
    private Queue<int> objectsToActivate;

    private void Awake()
    {
        spawner = spawnHolder.GetComponent<IMnetInstanceSpawner>();
        messager = GetComponent<MnetMessager>();
        availableInstanceID = new Queue<int>();
        objectsToActivate = new Queue<int>();
        instances = new MnetVariableArray<MnetInstanceInfo>();
        instances.arraySize = Mnet.maxSyncedObjects;
        instances.Setup();
        for (int i = 0; i < instances.arraySize; i++)
        {
            instances[i] = new MnetInstanceInfo();
            // We need to reserve some of the starting slots for the manager and players
            // So if max players is set to 8, free slots start at 9. Slot 0 is always the instance manager
            if (i > Mnet.maxPlayerCount)
            {
                availableInstanceID.Enqueue(i);
            }
        }
        // Instance manager is always on slot 0 and will be processed first
        // !!! There is no need for ObjectID since manager will never spawn or despawn
        objectInstanceID = 0;
    }


    public void SetupInstanceManager(MnetObject[] worldObjectArray, Ownership role)
    {
        syncedObjects = worldObjectArray;
        ownership = role;
        syncedObjects[objectInstanceID] = this;
    }

    public override void PrepareSnapshotUpdate()
    {
        for (int i = 0; i < syncedObjects.Length; i++)
        {
            if (syncedObjects[i] != null)
            {
                instances.changes.Add(i);
            }
        }
    }

    public override void SnapshotTick()
    {
        // Server?
        //spawner.Tick();
        
        // !!! SNAPSHOTIN PITÄÄ VALMISTAA OMISTAJA ELI SERVERI

        // !!! CLIENTIN PITÄIS VERRATA VAAN NYKY TILANNETTA SAATUUN ELI ONKS TÄÄ YKS HARVOI KU TARVII KAKS ERI METODIA? TAI isSERVER?

        // Client tsekkaa olevia objekteja saatuun
        // Eli onko on olemassa vai pitääkö spawnata
        // instanceID ja objectID vertaus olevaan ehkä? Kömpelö tapa tarkistaa ettei oo ruudussa väärän tyyppinen (mut jos on orc ja desync eri orc ni ei toimis siltikää)

        
            // Tää ja tsekkaa viel heränneel aivol alempi ja mieti oikeesti tää client/server erotus.. Serverin ei kai pitäis koskaan kutsua näitä. info lähtee itestään?
            // vai onks lähetys just se ku pitää hoitaa tickissä eli sit tarvitaa isServer erotus toiminnoil tai ServerTick ja ClientTick takas... mut turha muis tobjectes eli ei
    }

    public override void RegularTick()
    {

        while(objectsToActivate.Count > 0)
        {
            int instanceID = objectsToActivate.Dequeue();
            syncedObjects[instanceID].gameObject.SetActive(true);
            syncedObjects[instanceID].Activate();
        }

        // Server?
        //spawner.Tick();
        

        // !!! Näyttää idea hyvältä mutta mites snappi tsekki sun muut? Varmaa tarttis uude enumi koska tää olis hyvä olla siisti. Eli lopullinen if, else if, else ?

        // Client check messages
        // !!! Tää laukee useamman kerran serveri puolella eikööö eli spawnataan, lisätään viesti, spawnataan... joka lisää viestin? ei kai
        // ELLEI changes nollata lähetyksen jälkee? Eiks se olis loogisinta, eli serialize -> clean
        // Nyt changes häviää kun ollaan kirjotettu JA luettu mikä pitöis olla juuuuuust oikein.

        // !!! Serverin changes (koska ne lähti deserialize kautta) pitäis olla aina 0 ni tää ei laukee
        foreach(int i in instances.changes)
        {
            if(instances[i].Value.action == ObjectInstanceAction.Spawned)
            {
                RemoteSpawn(instances[i].Value.objectID);
            }
            else if(instances[i].Value.action == ObjectInstanceAction.Despawned)
            {
                RemoteDespawn(syncedObjects[i]);
            }

        }

        

    }

    // !!! Manageri saa scenen objektit spawnerilta
    // Eli ku peli alkaa ni mahdollista, myöhemmin pitää luoda yksitellen ja se saa eri setupin
    // TÄÄ ON VÄHÄ PERSEESTÄ. ON TURHA KOPIOIDA ARRAY TÄTÄ KAUTTA ELI EHKÄÄ TÄÄ VOIS OLLA TAPA VAAN KERÄTÄ OBJEKTIT SPAWNERILTA
    public void TryGetSceneObjects()
    {
        //syncedObjects = arrayToUse;
        //syncedObjects = new MnetObject[MnetSettings.maxSyncedObjects];

        MnetObject[] sceneObjects = spawner.GetActiveObjects();
        if (sceneObjects != null)
        {
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                // Process the scene object by giving it its instanceID and adding it to the synced objects array
                MnetObject curObj = sceneObjects[i];
                curObj.objectInstanceID = availableInstanceID.Dequeue();
                syncedObjects[curObj.objectInstanceID] = curObj;
            }
        }

    }

    public void AddObjectToArray(MnetObject obj)
    {
        obj.objectInstanceID = availableInstanceID.Dequeue();
        syncedObjects[obj.objectInstanceID] = obj;
    }

    public void LocalSpawn(MnetObject spawnedObj)
    {
        AddObjectToArray(spawnedObj);
        if(spawnedObj.ownership == Ownership.Auto) spawnedObj.ownership = Ownership.Local;
        instances[spawnedObj.objectInstanceID].Value
            = new MnetInstanceMessageSegment(ObjectInstanceAction.Spawned,spawnedObj.objectInstanceID,spawnedObj.objectTypeID);
        objectsToActivate.Enqueue(spawnedObj.objectInstanceID);
    }

    public void LocalDespawn(MnetObject despawningObj)
    {
        syncedObjects[despawningObj.objectInstanceID] = null;
        availableInstanceID.Enqueue(despawningObj.objectInstanceID);
        instances[despawningObj.objectInstanceID].Value
            = new MnetInstanceMessageSegment(ObjectInstanceAction.Despawned, despawningObj.objectInstanceID, despawningObj.objectTypeID);
    }

    public void RemoteSpawn(int newObjectID)
    {
        MnetObject newObj = spawner.SpawnRequest(newObjectID);
        if(newObj.ownership == Ownership.Auto) newObj.ownership = Ownership.Remote;
        AddObjectToArray(newObj);
        objectsToActivate.Enqueue(newObj.objectInstanceID);
    }

    public void RemoteDespawn(MnetObject objToDespawn)
    {
        spawner.DespawnRequest(objToDespawn);
        syncedObjects[objToDespawn.objectInstanceID] = null;
        availableInstanceID.Enqueue(objToDespawn.objectInstanceID);
    }

    private void CreateNewMessage(ObjectInstanceAction act, int instance, int obj)
    {
        instances[instance].Value = new MnetInstanceMessageSegment(act, instance, obj);
    }
}
