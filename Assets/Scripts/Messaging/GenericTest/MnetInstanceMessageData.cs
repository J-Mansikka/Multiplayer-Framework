
public enum ObjectInstanceAction
{
    None,
    Spawn,
    Despawn
}
public class MnetInstanceMessageData
{
    public ObjectInstanceAction action;
    public int objectID;
    public int objectType;

    public MnetInstanceMessageData()
    {
        action = ObjectInstanceAction.None;
        objectID = 0;
        objectType = 0;
    }
}
