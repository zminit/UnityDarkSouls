using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CFSM
{
    public interface IState
    {
        public void Enter();
        public void Exit();
        public  void FixedUpdate();
        public  void Update();
        
    }

    public abstract class BaseState : IState
    {
        protected struct QueueValue
        {
            public int priority;
            public Func<bool> listen;
            public Action transAction;
        }
        protected bool Interruptable;
        protected IStateMachine fsm;
        protected Utils.SortedArray<QueueValue> StateTransQueue = new Utils.SortedArray<QueueValue>(
            (v1, v2) => v1.priority <= v2.priority
            );

        public BaseState(IStateMachine fsm)
        {
            this.fsm = fsm;
        }

        public void AddListen(int priority, Func<bool> listen, Action transAction)
        {
            StateTransQueue.Add(
                new QueueValue(){priority = priority, listen = listen, transAction = transAction}
            );
        }

        protected void Listen()
        {
            foreach(QueueValue v in StateTransQueue)
            {
                if (v.listen.Invoke())
                {
                    v.transAction?.Invoke();
                    return;
                }
            }
        }

        public abstract void Enter();
        public abstract void Exit();
        public abstract void FixedUpdate();
        public abstract void Update();
    }

    public interface IStateMachine
    {
        public void StateUpdate();

        public void StateFixedUpdate();

        public void SwitchState(int id);

        public void MakeTransition(int id, int priority, Func<bool> listen, Action transAction);

    }
}
