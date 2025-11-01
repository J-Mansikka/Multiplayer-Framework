using System.Collections;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public delegate void SyncedAction();
public class MnetObject : MonoBehaviour
{
    // FINALIZE: Singleton

    public MnetNetwork endpoint;

    public int ObjectID { get; private set; }
    public int instanceID { get; private set; }
    protected Queue<int> updatedVars;
    //private Dictionary<string, int> call;
    private Dictionary<string, int> actionDictionary;
    protected List<SyncedAction> syncedMethods;
    protected MnetVariable[] syncedVariables;

    protected void Initialize(int id)//MnetObject owner)
    {
        // !!! TESTING SHIT

        endpoint = GetComponent<MnetNetwork>();

        ObjectID = id;

        //syncedActions = new Dictionary<int, Func<int>>();
        //methodDictionary = new SortedDictionary<int, SyncedAction>();
        //newActions = new LinkedList<byte>();

        //Call = new Dictionary<string, int>();

        //// Poistin this parametrin ja kaikki n‰ytt‰‰ silti toimivan? miks se tarvittiin alunperin?
        /// Oikeesti whats up?

        //Type type = owner.GetType();
        //Debug.Log(type);
        FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);//type.GetFields();
        MethodInfo[] methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        SortedList<string, MnetVariable> vars = new SortedList<string, MnetVariable>();
        SortedList<string, MethodInfo> methodCollection = new SortedList<string, MethodInfo>();
        MnetVariable extractedVar;

        updatedVars = new Queue<int>();
        syncedMethods = new List<SyncedAction>();
        actionDictionary = new Dictionary<string, int>();

        foreach (FieldInfo field in fields)
        {
            //Debug.Log(this.name + " " + field.Name);
            if (field.FieldType.IsSubclassOf(typeof(MnetVariable)))
            {
                extractedVar = (MnetVariable)field.GetValue(this);//owner);

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
            if (meth.Name.Length >= 6 && meth.Name.Substring(0, 6) == "Action")
            {
                Debug.Log("FOUND METHOD " + meth.Name);
                methodName = methodName.ToLower();
                actionDictionary.Add(methodName.Substring(6), methodIndex++);
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



        /*  !! !!  Olis selkee jos olis variaabeleil sijainti n‰inki talletettu mutta ylim‰‰r‰set fieldit tuhlaa rammia joten k‰ytet‰‰ vaa j‰rjestyst
        MnetVariable curVar;
        for (int i = 0; i < variables.Length; i++)
        {
            curVar = variables[i];
            
            curVar.flagIndex = (byte)(i / 8);
            curVar.flagValue = (byte)Math.Pow(2, i - curVar.flagIndex * 8);
            
        }
        */
        // !!! INT 
        //!!!! Poiistettu headerLength = (short)(variableBitFlags.Length + MnetSettings.objectHeaderIdAndSizeLength);
        //headerLength = MnetSettings.objectHeaderIdAndSizeLength + variableflagByteCount;
        // !!! EIKS OO PAREMPI ET CURRENT SIZE ON VAAN DATA? EI BIT FLAGIT VARMAA KOSKAA OO HIRVEE MƒƒRƒ
        //currentSize = headerLength;
        //firstVariableToSerialize = 0;
        //Debug.Log("BYTES NEEDED FOR VARIABLES "+variableBitFlags.Length);


        int variableId = 1; // VariableID 0 marks a method call instead

        foreach (MnetVariable var in syncedVariables)
        {
            /*
            try
            {
                var.owner = this; //owner;
            }
            catch
            {
                //Debug.LogError("At least one variable in "+owner+" has not been initialized!\n"
                //    +"Variables must be initialized, either by Unity by making them public or using their constructor in the owner object. ");
            }
            */
            // Variable setup sets the category, static byte size and steps to ensure that the starting value is not null
            var.Setup();
            // !!!!! Korjaa sen ku nullattavat merkataa ekassa initialisoinnis (mnetbool, mnetstring etc)
            var.hasChanged = false;
            var.id = variableId++;
            // If the variable can be split, we need to intialize its byte array that temporarily contains the data
            if (var.sizeCategory > VariableSize.Limited)
            {
                // Most likely the size will surpass one segment, but we can use it as a starting size since it grows automatically when needed
                // Segmentointi poistettu ja t‰‰ oli v‰‰rin. Koko oli 1 vaikka ideana oli kai olla 256
                var.bytes = new byte[256];
            }
            //
            //
            //
            //currentSize += var.sizeInBytes;
            //
            //
            //
            // !!!!! KATO ALEMPI EIKS Nƒƒ O TURHII
            /*
            if (var.sizeInBytes < 0 && !var.varyingSize)
            {
                //Debug.LogError("[ERROR: sizeInBytes not set.] "+var+" on "+owner+" is set to have a specific size in bytes , but it was not set in Setup method.");
            }
            */

            // ! !!! !  Jos koko vaihtelee ni eih‰ sill‰ v‰ltt‰m‰tt‰ ole kokoa aluks. Pit‰‰ vaan tsekata ku l‰hett‰‰ et onks koko 0 ja sitte yritt‰ p‰ˆivtt‰‰
            /*
            if (var.varyingSize)
            {
                var.SetSize();
            }
            */
        }

        //!!    currentSize = 0;

        //Debug.Log("CURRENT OBJECT SIZE " + currentSize);

    }

    public void Setup(int instanceNumber)
    {
        instanceID = instanceNumber;
    }

    public void SerializeVariable(MnetVariable updatedVar)
    {
        endpoint.worldPackets.WriteVariable(updatedVar);
    }

    public MnetVariable GetVariable(int id)
    {
        // Since ID value of 0 is used to signify a method call, we have to substract variable ID by one to get the index in array
        return syncedVariables[id-1];
    }

    public void Call(string method)
    {
        //Write changedVariables on UPDATE


        // Write method on UPDATE
        InvokeMethod(actionDictionary[method]);
    }

    public void InvokeMethod(int index)
    {
        // Invoke Method on TICK
        syncedMethods[index].Invoke();
    }

}
