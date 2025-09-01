using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    bool IsInterruptible { get; set; }
    void Enter();

    void Update();

    void FixedUpdate();

    void Exit();
}
