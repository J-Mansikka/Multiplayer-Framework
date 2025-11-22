using System.Collections;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public delegate void SyncedAction();
public abstract class NewObject : MonoBehaviour
{

    public int objectID;
    public int instanceID;
    //public MnetNetwork local;
    //protected Queue<int> updatedVars;
    //private Dictionary<string, int> call;
    public MnetNetwork local;
    public bool isInitialized { get; private set; }
    private Dictionary<string, int> actionDictionary;
    protected List<SyncedAction> syncedMethods;
    protected MnetVariable[] syncedVariables;

    //public PacketManager packetManager;

    public void Initialize()//MnetObject owner)
    {
        // !!! TESTING SHIT

        //endpoint = GetComponent<MnetNetwork>();

        //Type type = owner.GetType();
        //Debug.Log(type);
        FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);//type.GetFields();
        MethodInfo[] methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        SortedList<string, MnetVariable> vars = new SortedList<string, MnetVariable>();
        SortedList<string, MethodInfo> methodCollection = new SortedList<string, MethodInfo>();
        MnetVariable extractedVar;

        //updatedVars = new Queue<int>();
        syncedMethods = new List<SyncedAction>();
        actionDictionary = new Dictionary<string, int>();

        foreach (FieldInfo field in fields)
        {
            //Debug.Log(this.name + " " + field.Name);
            if (field.FieldType.IsSubclassOf(typeof(MnetVariable)))
            {
                
                // OIKEE extractedVar = (MnetVariable)field.GetValue(this);//owner);

                

                if(field.GetValue(this) == null)  field.SetValue(this, Activator.CreateInstance(field.FieldType));
                extractedVar = (MnetVariable)field.GetValue(this);

                /*
                if (extractedVar == null)
                {
                    //Debug.Log(field.FieldType);
                    //var vari = Activator.CreateInstance(field.FieldType);
                    //Debug.Log(vari.ToString());
                    //extractedVar = vari as MnetVariable;

                    field.SetValue(this, Activator.CreateInstance(field.FieldType));
                    extractedVar = (MnetVariable)field.GetValue(this);
                    Debug.Log(extractedVar);
                    //var defConstructor = field.GetValue(this).GetType().GetConstructor(Type.EmptyTypes);
                    //field.SetValue(this, Activator.CreateInstance(field.GetValue(this).GetType()));//defConstructor.Invoke);
                    //field.SetValue(extractedVar, Activator.CreateInstance(field.GetType().GetType()));
                    //extractedVar.Value = (MnetVariable)instanssi;
                    //Type inhertedType = field.FieldType;
                    //Debug.Log("TYPE WAS "+inhertedType+" on "+ field.Name);
                    //extractedVar = Activator.CreateInstance<field.GetValue().GetType()>() where ;

                    //extractedVar = var as MnetVariable;
                    /*
                    throw new NullReferenceException("Variable " + field.Name + " on object " + name + " was left null. \n"
                        + "Mnet fields are initialized by Unity if they are set public or use the attribute [SerializeField]. \n" +
                        "You can also call the constructor in the object's Awake function before calling Initialization() method on the object. \n" +
                        "Remember to add the starting Value, if necessary, with SetValue method.");
                    
                }
            */
                extractedVar.owner = this;
                extractedVar.Setup();

                //if(extractedVar.itemMessagingMode == MessagingDirection.Auto) extractedVar.itemMessagingMode = messagingMode;
                vars.Add(field.Name, extractedVar);
                extractedVar.variableName = field.Name;
                //Debug.Log("NIMI "+field.Name);
            }
        }

        syncedVariables = vars.Values.ToList().ToArray();

        int methodIndex = 0;

        foreach (MethodInfo meth in methods)
        {
            //Debug.Log(this.name + " " + meth.Name);

            string methodName = meth.Name;
            if (meth.Name[0]=='_')
            {

                methodName = methodName.ToLower();
                actionDictionary.Add(methodName.Substring(1), methodIndex++);
                syncedMethods.Add((SyncedAction)Delegate.CreateDelegate(typeof(SyncedAction), this, meth));
                //SyncedAction newAct = (SyncedAction)Delegate.CreateDelegate(typeof(SyncedAction), this, meth);
                //Func<int> newFunction = del;
                //byte returnID = newAct.Invoke();
                //methodDictionary.Add(returnID, newAct);
                /*
                extractedVar = (MnetVariable)field.GetValue(this);//owner);

                extractedVar.owner = this;
                extractedVar.Setup();

                //if(extractedVar.itemMessagingMode == MessagingDirection.Auto) extractedVar.itemMessagingMode = messagingMode;
                vars.Add(field.Name, extractedVar);
                extractedVar.variableName = field.Name;

                */
            }
        }

        int variableId = 1; // VariableID 0 marks a method call instead

        foreach (MnetVariable var in syncedVariables)
        {
            // Variable setup sets the category, static byte size and steps to ensure that the starting value is not null
            var.Setup();
            // !!!!! Korjaa sen ku nullattavat merkataa ekassa initialisoinnis (mnetbool, mnetstring etc)
            var.hasChanged = false;
            var.variableID = variableId++;
            // If the variable can be split, we need to intialize its byte array that temporarily contains the data
            if (var.sizeCategory > VariableSize.Limited)
            {
                // Most likely the size will surpass one segment, but we can use it as a starting size since it grows automatically when needed
                // Segmentointi poistettu ja t‰‰ oli v‰‰rin. Koko oli 1 vaikka ideana oli kai olla 256
                var.bytes = new byte[256];
            }
        }

        isInitialized = true;
    }

    public virtual void MnetUpdate()
    {
        // An option
    }

    public MnetVariable GetVariable(int id)
    {
        // Since ID value of 0 is used to signify a method call, we have to substract variable ID by one to get the index in array
        return syncedVariables[id-1];
    }

    public virtual void CreateSnapshot(PacketManager snapshotPackets)
    {
        foreach (MnetVariable var in syncedVariables)
        {
            var.UpdateVariable();
            //var.SnapshotUpdate(snapshotPackets);
        }
    }

    // This method applies its current fields and updates its state.
    // It's called when the object becomes active or when a client received a snapshot update.
    //
    // EXAMPLE: A treasure chest was looted before second player joined. Second player receives chest content as EMPTY when they join
    // UpdateState: Disables interaction with the chest and changes its model to opened and empty version
    // EXAMPLE: A player killed an orc while another player was having connection issues and required a snapshot update. Player receives orc mHealth as 0
    // UpdateState: Orc's state is set to dead with appropriate animation and possible loot. Any AI or behavior is turned off
    // EXAMPLE: Players were in different scenes while one of them changed his armor. Player two receives the state of the other player's inventory when they regroup
    // UpdateState: Player's visual model is updated by applying correct helmet and cuirass to it. Actual armor stats were already received with the data earlier
    public abstract void UpdateState();

    public void Call(string method)
    {
        //Write changedVariables on UPDATE
        local.activeManager.WriteMethodCall(instanceID, actionDictionary[method]);
    }

    public void InvokeMethod(int index)
    {
        // Invoke Method on TICK
        Debug.Log("INVOKED " + index);
        syncedMethods[index].Invoke();
    }

}
