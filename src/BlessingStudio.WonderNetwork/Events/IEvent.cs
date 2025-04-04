namespace BlessingStudio.WonderNetwork.Events;

public interface IEvent
{
    public string GetEventName()
    {
        return GetType().Name;
    }
}
