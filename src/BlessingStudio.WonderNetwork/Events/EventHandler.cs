namespace BlessingStudio.WonderNetwork.Events;

public delegate void EventHandler<in T>(T @event) where T : IEvent;
