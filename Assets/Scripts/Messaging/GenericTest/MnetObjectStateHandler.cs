using System.Collections.Generic;
using UnityEngine;

public class MnetObjectStateHandler : MnetObject
{
    /*
     tarvitaan objecti lista tyypeistä
    itse spawnaus tekee joku toinen ja ilmottaa tälle
    Eli tän osuus on lähinnä kirjottaa paketti........
    voi olla että on tosi yksinkertanen ja täysin serverin/clientin alanen eikä mikään iso juttu
    eli MnetObject ja mitkä variaableit tarvii?

    Variaabelit että toimii
    Varmanki arrayta ni luo tarvittaessa
    Miten luodaan unityssa
    Instantiate olis väliaikane mut demoon vois luoda super yksinkertasen poolin
     Jos vaan mnetobject ni serveri voi päivittää tai gamemanageri enmätie

    MITEN OLLA YKSINKERTANEN JA NOPEE ELI VOIS ITEROIDA LÄPI?
     
     */

    // !!!!!! TÄÄ ON MELKO VARMASTI IHAN PÄIN HELVETTIÄ NYT
    [SerializeField]
    public IMnetInstancer objectInstancer;      // TARVITAAN JUU
    public List<MnetObject> playerPrefabs;      // TURHAKE
    public List<MnetObject> networkedPrefabs;       // TURHAKE. HANDLER VAA KOMMUNIKOI, KÄYTTÄJÄ MÄÄRÄÄ
    private Queue<int> freeObjectIndex;           // Keeps track of free object IDs and hands them out on spawning and returns them on despawning
    private List<MnetObject> activeObjects;         // Mikä helvetti tää on?
    public ushort maxActionsPerTick = 512;

    public MnettInstanceMessageSegments actions;
    private MnetInstanceMessageData newAction;

    private void Awake()
    {
        //actions = new MnettInstanceMessageSegments(this,maxActionsPerTick);
        //Setup();

        // Set up the ID index container
        freeObjectIndex = new Queue<int>(MnetSettings.maxSyncedObjects);
        // Object Handler is always ID number 0, followed by the players
        int reservedSlots = MnetSettings.maxPlayerCount + 1;
        // Starting slots are reserverd for the spawner object and players objects.
        // Rest is assigned and reassigned to spawning/despawning objects
        for (int i = reservedSlots; i < MnetSettings.maxSyncedObjects; i++)
        {
            freeObjectIndex.Enqueue((short)i);
        }
        /// typeID ja handlerin lisäys ei pitäsi vaikuttaa mihinkään tässä vaiheessa et voi lyödä tähä
        networkedPrefabs.InsertRange(0, playerPrefabs);

        // Set messaging mode. If left to auto, instance messenger will override with its own
        foreach (MnetObject netObject in networkedPrefabs )
        {
            //// if (netObject.messagingMode == MessagingDirection.Auto) netObject.messagingMode = messagingMode;
        }
        /* pidetään objectit erossa messengeristä
        for (int i = 0; i < networkedPrefabs.Count; i++)
        {
            networkedPrefabs[i].AttachHandler(this, (short)i);
        }
        */

    }

    private void Start()
    {
        /// Onko viisasta? Eihän niitä tarvita enää sitte
        /// Mut jos on poolissa tai jossain ylhäällä ni ei se muistia vapauta.. Ehkä varmuuden vuoks joo ottaa pois
        networkedPrefabs = null;
    }

    public void HandlerSetup(List<MnetObject> activeObjects)
    {
        this.activeObjects = activeObjects;
        objectInstanceID = 0;
        activeObjects.Add(this);
    }

    // !! Serverit
    public void AddSpawnMessage(MnetObject spawningObjectInstance)
    {
        /// Kristus et on ruma setti. numberofactions pitää kasvaa yhdellä että saadaa koko mut sit ei toimi sellasenaa enää indexinä ellei minus yks
        actions.numberOfActions++;
        newAction = actions.GetAndSet()[actions.numberOfActions - 1];
        newAction.action = ObjectInstanceAction.Spawn;
        int objectInstanceID = freeObjectIndex.Dequeue();
        newAction.objectID = objectInstanceID;
        spawningObjectInstance.objectInstanceID = objectInstanceID;
        newAction.objectType = spawningObjectInstance.objectTypeID;
        activeObjects.Add(spawningObjectInstance);
    }

    public void AddDespawnMessage(MnetObject despawningObjectInstance)
    {
        actions.numberOfActions++;
        newAction = actions.GetAndSet()[actions.numberOfActions - 1];
        newAction.action = ObjectInstanceAction.Despawn;
        newAction.objectID = despawningObjectInstance.objectInstanceID;
        newAction.objectType = 0;
        freeObjectIndex.Enqueue(despawningObjectInstance.objectInstanceID);
        activeObjects.Remove(despawningObjectInstance);
    }

    public override void Tick()
    {
        /*
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
        */
    }



}
