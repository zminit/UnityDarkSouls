using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StateEventType
{
    AttackRequested,
    RollRequested,
    JumpRequested,
    SprintRequested,
}

public class StateEvent
{
    public StateEventType type;
    public object data;
}

public class EventCenter
{
    private Dictionary<StateEventType, List<Action<StateEvent>>> eventDict = new Dictionary<StateEventType, List<Action<StateEvent>>>();

    //ע���¼�
    public void RegisterEvent(StateEventType eventType, Action<StateEvent> callback)
    {
        if (!eventDict.ContainsKey(eventType))
        {
            eventDict[eventType] = new List<Action<StateEvent>>();
        }
        eventDict[eventType].Add(callback);
    }

    //ȡ��ע���¼�
    public void UnregisterEvent(StateEventType eventType, Action<StateEvent> callback)
    {
        if (eventDict.ContainsKey(eventType))
        {
            eventDict[eventType].Remove(callback);
            if (eventDict[eventType].Count == 0)
            {
                eventDict.Remove(eventType);
            }
        }
    }

    //�����¼�
    public void TriggerEvent(StateEvent stateEvent)
    {
        if (eventDict.ContainsKey(stateEvent.type))
        {
            foreach (var callback in eventDict[stateEvent.type])
            {
                callback?.Invoke(stateEvent);
            }
        }
    }
}

public class AttackEvent : StateEvent
{
    public enum AttackType
    {
        Light,
        Heavy
    }

    public AttackType attackType;
    public StateType fromState;
}

