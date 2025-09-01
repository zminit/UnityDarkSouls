using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StateType
{
    Idle,
    Walking,
    Running,
    Jumping,
    Attacking,
    Rolling,
    Sprinting,
    Locomotion
}

public class LocomotionState : IState
{
    Animator animator;
    InputHandler inputHandler;
    EventCenter eventCenter;
    PlayerManager playerManager;
    CharacterStateMachine machine;

    StateType currentType = StateType.Idle;


    public bool IsInterruptible { get; set; } = true;

    public LocomotionState(CharacterStateMachine machine)
    {
        animator = machine.animator;
        inputHandler = machine.inputHandler;
        eventCenter = machine.eventCenter;
        playerManager = machine.player;
        this.machine = machine;

        
    }

    public void Enter()
    {
        RollingState.AddListenToRoll(inputHandler, eventCenter);
        JumpingState.AddListenToJump(inputHandler, eventCenter);
        animator.applyRootMotion = false;
        animator.CrossFade("Locomotion", 0.1f);
    }

    public void Exit()
    {
        JumpingState.RemoveListenToJump(inputHandler);
        RollingState.RemoveListenToRoll(inputHandler);
    }

    public void FixedUpdate()
    {
        switch (currentType) 
        {
            case StateType.Idle:
                break;
            case StateType.Walking:
                playerManager.Move(new Vector2(inputHandler.horizontal, inputHandler.vertical), playerManager.WalkSpeed, Vector3.up);
                break;
            case StateType.Running:
                playerManager.Move(new Vector2(inputHandler.horizontal, inputHandler.vertical), playerManager.RunSpeed, Vector3.up);
                break;
            case StateType.Sprinting:
                playerManager.Move(new Vector2(inputHandler.horizontal, inputHandler.vertical), playerManager.SprintSpeed, Vector3.up);
                break;
            default:
                break;
        }
    }

    public void Update()
    {
        if (inputHandler.Movement < 0.1f)
        {
            animator.SetFloat("Vertical", 0f);
            animator.SetFloat("Horizontal", 0f);
            currentType = StateType.Idle;
        }
        else
        {
            if(inputHandler.B_Input && inputHandler.B_Input_Time > 0.5f)
            {
                animator.SetFloat("Vertical", 2.0f);
                animator.SetFloat("Horizontal", 0f);
                currentType = StateType.Sprinting;
                machine.SetStateIndex(currentType);
                return;
            }
            Transform playerTransform = animator.transform;
            Transform mainCamera = Camera.main.transform; // Get the main camera transform
            float vertical = inputHandler.vertical; // Get vertical input from InputHandler
            float horizontal = inputHandler.horizontal; // Get horizontal input from InputHandler
            Vector2 modelForward = new Vector2(playerTransform.forward.x, playerTransform.forward.z); // Get the player's forward direction
            Vector2 forward = new Vector2(mainCamera.forward.x, mainCamera.forward.z);
            Vector2 right = new Vector2(mainCamera.right.x, mainCamera.right.z);
            forward.Normalize();
            right.Normalize();
            Vector2 locomotionDir = forward * vertical + right * horizontal;
            Utils.PlayerLocomotion.HandleAnimatorInputByLocomotionInput(modelForward, locomotionDir, out vertical, out horizontal); // Handle animator input based on locomotion input
            if (inputHandler.Alt_Input || inputHandler.Movement <= 0.55f)
            {
                vertical = Mathf.Clamp(inputHandler.vertical, -0.5f, 0.5f);
                horizontal = Mathf.Clamp(inputHandler.horizontal, -0.5f, 0.5f);
                animator.SetFloat("Vertical", vertical);
                animator.SetFloat("Horizontal", horizontal);
                currentType = StateType.Walking;
                machine.SetStateIndex(currentType);
                return;
            }
            animator.SetFloat("Vertical", vertical);
            animator.SetFloat("Horizontal", horizontal);
            currentType = StateType.Running;
        }
        machine.SetStateIndex(currentType);
    }

}

public class RollingState : IState
{
    Animator animator;
    EventCenter eventCenter;
    CharacterStateMachine machine;
    InputHandler inputHandler;

    Action<StateEvent> callback;

    #region 设置监听事件

    static Action Roll;
    public static void AddListenToRoll(InputHandler inputHandler, EventCenter eventCenter)
    {
        Roll = new Action(() =>
            {
                if (inputHandler.B_Input_Time < 0.5f)
                {
                    StateEvent rollEvent = new StateEvent
                    {
                        type = StateEventType.RollRequested
                    };
                    eventCenter.TriggerEvent(rollEvent);
                }
            }
        );
        inputHandler.B_Input_Canceled += Roll;
    }

    public static void RemoveListenToRoll(InputHandler inputHandler)
    {
        if (Roll != null)
        {
            inputHandler.B_Input_Canceled -= Roll;
            Roll = null;
        }
    }
    #endregion

    public RollingState(CharacterStateMachine machine)
    {
        animator = machine.animator;
        eventCenter = machine.eventCenter;
        this.machine = machine;
        inputHandler = machine.inputHandler;

        callback += (StateEvent e) =>
        {
            if(CanRoll())
            {
                machine.SwitchState(this);
            }
        };
        eventCenter.RegisterEvent(StateEventType.RollRequested, callback);
        #region 在动画结束时切换状态
        AnimationController[] animationControllers = animator.GetBehaviours<AnimationController>();
        foreach (var controller in animationControllers)
        {
            if (controller.animationName == "Rolling")
            {
                Debug.Log("Found Rolling AnimationController");
                controller.OnStateEnd += ()=>{
                    machine.SwitchState(GetNextState()); 
                };
            }
        }
        #endregion
    }
    public bool IsInterruptible { get; set; } = false;

    public void Enter()
    {
        animator.applyRootMotion = true;
        animator.CrossFade("Rolling", 0.1f);
        machine.SetStateIndex(StateType.Rolling);
    }

    public void Exit()
    {

    }

    public void FixedUpdate()
    {
    }

    public void Update()
    {
    }

    bool CanRoll()
    {
        return true;
    }

    IState GetNextState()
    {
        return machine.CharacterStateDict[StateType.Locomotion];
    }
}

public class JumpingState : IState
{
    Animator animator;
    EventCenter eventCenter;
    CharacterStateMachine machine;
    InputHandler inputHandler;

    #region 设置监听事件
    static Action Jump;
    public static void AddListenToJump(InputHandler inputHandler, EventCenter eventCenter)
    {
        Jump = new Action(() =>
            {
                if (inputHandler.A_Input_Time < 0.5f)
                {
                    StateEvent jumpEvent = new StateEvent
                    {
                        type = StateEventType.JumpRequested
                    };
                    eventCenter.TriggerEvent(jumpEvent);
                }
            }
        );
        inputHandler.A_Input_Canceled += Jump;
    }
    public static void RemoveListenToJump(InputHandler inputHandler)
    {
        if (Jump != null)
        {
            inputHandler.A_Input_Canceled -= Jump;
            Jump = null;
        }
    }
    #endregion
    public JumpingState(CharacterStateMachine machine)
    {
        animator = machine.animator;
        eventCenter = machine.eventCenter;
        this.machine = machine;
        inputHandler = machine.inputHandler;
        eventCenter.RegisterEvent(StateEventType.JumpRequested, (e) => 
        {
            if (CanJump())
            {
                machine.SwitchState(this);
            }
        });
    }
    public bool IsInterruptible { get; set; } = true;
    public void Enter()
    {
        animator.applyRootMotion = true;
        animator.CrossFade("Jumping", 0.1f);
        machine.SetStateIndex(StateType.Jumping);
    }
    public void Exit()
    {
    }
    public void FixedUpdate()
    {
    }
    public void Update()
    {
    }
    bool CanJump()
    {
        return true; // Implement your jump logic here
    }
}
