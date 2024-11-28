using System.Collections.Generic;
using UnityEngine;

public class MnetObjectStateHandler : MnetObject
{
    /*
     tarvitaan objecti lista tyypeist‰
    itse spawnaus tekee joku toinen ja ilmottaa t‰lle
    Eli t‰n osuus on l‰hinn‰ kirjottaa paketti........
    voi olla ett‰ on tosi yksinkertanen ja t‰ysin serverin/clientin alanen eik‰ mik‰‰n iso juttu
    eli MnetObject ja mitk‰ variaableit tarvii?

    Variaabelit ett‰ toimii
    Varmanki arrayta ni luo tarvittaessa
    Miten luodaan unityssa
    Instantiate olis v‰liaikane mut demoon vois luoda super yksinkertasen poolin
     Jos vaan mnetobject ni serveri voi p‰ivitt‰‰ tai gamemanageri enm‰tie
     
     */
    [SerializeField]
    public IMnetInstancer objectInstancer;
    public List<MnetObject> playerPrefabs;
    public List<MnetObject> networkedPrefabs;       // prefabs must be in the same order on both the server and the client ends
    private Queue<short> freeObjectIndex;           // Keeps track of free object IDs and hands them out on spawning and returns them on despawning
    private List<MnetObject> activeObjects;
    public ushort maxActionsPerTick = 512;

    public MnettInstanceMessageSegments actions;
    private MnetInstanceMessageData newAction;

    private void Awake()
    {
        actions = new MnettInstanceMessageSegments(this,maxActionsPerTick);
        Setup(this);

        // Set up the ID index container
        freeObjectIndex = new Queue<short>(ServerSettings.maxSyncedObjects);
        // Object Handler is always ID number 0, followed by the players
        int reservedSlots = ServerSettings.maxPlayerCount + 1;
        // Starting slots are reserverd for the spawner object and players objects.
        // Rest is assigned and reassigned to spawning/despawning objects
        for (int i = reservedSlots; i < ServerSettings.maxSyncedObjects; i++)
        {
            freeObjectIndex.Enqueue((short)i);
        }
        /// typeID ja handlerin lis‰ys ei pit‰si vaikuttaa mihink‰‰n t‰ss‰ vaiheessa et voi lyˆd‰ t‰h‰
        networkedPrefabs.InsertRange(0, playerPrefabs);

        // Set messaging mode. If left to auto, instance messenger will override with its own
        foreach (MnetObject netObject in networkedPrefabs )
        {
            if (netObject.messagingMode == MessagingDirection.Auto) netObject.messagingMode = messagingMode;
        }
        /* pidet‰‰n objectit erossa messengerist‰
        for (int i = 0; i < networkedPrefabs.Count; i++)
        {
            networkedPrefabs[i].AttachHandler(this, (short)i);
        }
        */

    }

    private void Start()
    {
        /// Onko viisasta? Eih‰n niit‰ tarvita en‰‰ sitte
        /// Mut jos on poolissa tai jossain ylh‰‰ll‰ ni ei se muistia vapauta.. Ehk‰ varmuuden vuoks joo ottaa pois
        networkedPrefabs = null;
    }

    public void HandlerSetup(List<MnetObject> activeObjects)
    {
        this.activeObjects = activeObjects;
        objectID = 0;
        activeObjects.Add(this);
    }

    // !! Serverit
    public void AddSpawnMessage(MnetObject spawningObjectInstance)
    {
        /// Kristus et on ruma setti. numberofactions pit‰‰ kasvaa yhdell‰ ett‰ saadaa koko mut sit ei toimi sellasenaa en‰‰ indexin‰ ellei minus yks
        actions.numberOfActions++;
        newAction = actions.GetAndSet()[actions.numberOfActions - 1];
        newAction.action = ObjectInstanceAction.Spawn;
        short objectInstanceID = freeObjectIndex.Dequeue();
        newAction.objectID = objectInstanceID;
        spawningObjectInstance.objectID = objectInstanceID;
        newAction.objectType = spawningObjectInstance.objectTypeID;
        activeObjects.Add(spawningObjectInstance);
    }

    public void AddDespawnMessage(MnetObject despawningObjectInstance)
    {
        actions.numberOfActions++;
        newAction = actions.GetAndSet()[actions.numberOfActions - 1];
        newAction.action = ObjectInstanceAction.Despawn;
        newAction.objectID = despawningObjectInstance.objectID;
        newAction.objectType = 0;
        freeObjectIndex.Enqueue(despawningObjectInstance.objectID);
        activeObjects.Remove(despawningObjectInstance);
    }

    public override void Tick()
    {
        int actionCount = actions.numberOfActions;
        for (int i = 0; i < actionCount; i++)
        {
            newAction = actions.Value[i];
            switch (newAction.action)
            {
                case ObjectInstanceAction.Spawn:
                    {
                        objectInstancer.RemoteSpawnRequest(newAction.objectType, newAction.objectID);
                        break;
                    }
                case ObjectInstanceAction.Despawn:
                    {
                        objectInstancer.RemoteDespawnRequest(newAction.objectID);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
    }



}
