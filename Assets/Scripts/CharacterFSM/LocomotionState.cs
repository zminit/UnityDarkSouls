using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace CFSM
{
    enum LocomotionType
    {
        Idle,
        Walk,
        Run,
        Sprint,
    }

    public class LocomotionState : BaseState, IStateMachine
    {
        public BlackBoard bb;
        public Animator animator;
        public Rigidbody playerRigidbody;
        public Transform playerTransform;
        public Camera mainCamera;
        public InputHandler inputHandler;
        public PlayerManager playerManager;

        Dictionary<int, BaseState> LocomotionStateDir; //移动子状态表
        int currentStateIndex;

        public LocomotionState(CharacterFSM fsm) : base(fsm) 
        {
            bb = fsm.bb;
            animator = fsm.animator;
            playerRigidbody = fsm.playerBody;
            playerTransform = fsm.playerTransform;
            mainCamera = fsm.mainCamera;
            inputHandler = fsm.inputHandler;
            playerManager = fsm.playerManager;
            LocomotionStateDir = new Dictionary<int, BaseState>();
            LocomotionStateDir.Add((int)LocomotionType.Idle, new Idle(this));
            LocomotionStateDir.Add((int)LocomotionType.Walk, new Walk(this));
            LocomotionStateDir.Add((int)LocomotionType.Run, new Run(this));
            LocomotionStateDir.Add((int)LocomotionType.Sprint, new Sprint(this));
            currentStateIndex = 0;

        }
        public override void Enter()
        {
            Debug.Log("Locomotion...");
            animator.CrossFade("StrafeCommonLocomotion", 0.2f);
            SwitchState(currentStateIndex);
        }

        public override void Exit()
        {
        }

        public override void FixedUpdate()
        {
            StateFixedUpdate();
        }

        public override void Update()
        {
            Listen();
            StateUpdate();
        }

        public void StateUpdate()
        {
            LocomotionStateDir[currentStateIndex].Update();
        }

        public void StateFixedUpdate()
        {
            LocomotionStateDir[currentStateIndex].FixedUpdate();
        }

        public void SwitchState(int id)
        {
            if (!LocomotionStateDir.ContainsKey(id))
                return;
            LocomotionStateDir[currentStateIndex].Exit();
            currentStateIndex = id;
            LocomotionStateDir[currentStateIndex].Enter();
        }

        public void MakeTransition(int id, int priority, Func<bool> listen, Action transAction)
        {
            if(LocomotionStateDir.ContainsKey(id))
                LocomotionStateDir[id].AddListen(priority, listen, transAction);
        }

    }

    public class Idle : BaseState
    {
        Animator animator;
        public Idle(LocomotionState fsm) : base(fsm)
        {
            animator = fsm.animator;
        }
        public override void Enter()
        {
            Debug.Log("Idling...");
            animator.CrossFade("StrafeCommonLocomotion", 0.2f);
            animator.SetFloat("Vertical", 0f);
            animator.SetFloat("Horizontal", 0f);
        }

        public override void Exit()
        {
        }

        public override void FixedUpdate()
        {
        }

        public override void Update()
        {
            Listen();
        }
    }

    public class Walk : BaseState
    {
        InputHandler inputHandler;
        Animator animator;
        Transform playerTransform;
        Transform mainCamera;
        public Walk(LocomotionState fsm) : base(fsm)
        {
            animator = fsm.animator;
            inputHandler = fsm.inputHandler;
            playerTransform = fsm.playerTransform;
            mainCamera = fsm.mainCamera.transform;

            //Walk -> Idle
            AddListen(10, ()=>inputHandler.Movement <= 0.2f, () => { fsm.SwitchState((int)LocomotionType.Idle); }); //状态转移至Idle
            //Idle -> Walk
            fsm.MakeTransition(
                (int)LocomotionType.Idle, 
                5,
                ()=>inputHandler.Movement > 0.2f && inputHandler.Movement < 0.55f,
                () => { fsm.SwitchState((int)LocomotionType.Walk); }
                );
        }
        public override void Enter()
        {
            Debug.Log("Walking...");
            animator.CrossFade("StrafeCommonLocomotion", 0.2f);
        }

        public override void Exit()
        {
        }

        public override void FixedUpdate()
        {
        }

        public override void Update()
        {
            Listen();
            float vertical = inputHandler.vertical; // Get vertical input from InputHandler
            float horizontal = inputHandler.horizontal; // Get horizontal input from InputHandler
            Vector2 modelForward = new Vector2(playerTransform.forward.x, playerTransform.forward.z); // Get the player's forward direction
            Vector2 forward = new Vector2(mainCamera.forward.x, mainCamera.forward.z);
            Vector2 right = new Vector2(mainCamera.right.x, mainCamera.right.z);
            forward.Normalize();
            right.Normalize();
            Vector2 locomotionDir = forward * vertical + right * horizontal;
            locomotionDir.Normalize();
            Utils.PlayerLocomotion.HandleAnimatorInputByLocomotionInput(modelForward, locomotionDir, out vertical, out horizontal); // Handle animator input based on locomotion input
            vertical = Mathf.Clamp(vertical, -1f, 1f);
            horizontal = Mathf.Clamp(horizontal, -1f, 1f);
            animator.SetFloat("Vertical", vertical);
            animator.SetFloat("Horizontal", horizontal);
        }
    }

    public class Run : BaseState
    {
        InputHandler inputHandler;
        Animator animator;
        Transform playerTransform;
        Transform mainCamera;
        public Run(LocomotionState fsm) : base(fsm)
        {
            animator = fsm.animator;
            inputHandler = fsm.inputHandler;
            playerTransform = fsm.playerTransform;
            mainCamera = Camera.main.transform;

            //Run -> Idle
            AddListen(10, () => inputHandler.Movement <= 0.2f, () => { fsm.SwitchState((int)LocomotionType.Idle); }); //状态转移至Idle
            //Idle -> Run
            fsm.MakeTransition(
                (int)LocomotionType.Idle,
                5,
                () => inputHandler.Movement > 0.55f,
                () => { fsm.SwitchState((int)LocomotionType.Run); }
                );
            //Walk -> Run
            fsm.MakeTransition(
                (int)LocomotionType.Walk,
                5,
                () => inputHandler.Movement > 0.55f,
                () => { fsm.SwitchState((int)LocomotionType.Run); });
            //Run->Walk
            AddListen(10, ()=> inputHandler.Movement <= 0.55f, () => { fsm.SwitchState((int)LocomotionType.Walk); });
        }

        public override void Enter()
        {
            Debug.Log("Running...");
            animator.CrossFade("StrafeCommonLocomotion", 0.2f);
        }

        public override void Exit()
        {
        }

        public override void FixedUpdate()
        {
        }

        public override void Update()
        {
            Listen();
            float vertical = inputHandler.vertical; // Get vertical input from InputHandler
            float horizontal = inputHandler.horizontal; // Get horizontal input from InputHandler
            Vector2 modelForward = new Vector2(playerTransform.forward.x, playerTransform.forward.z); // Get the player's forward direction
            Vector2 forward = new Vector2(mainCamera.forward.x, mainCamera.forward.z);
            Vector2 right = new Vector2(mainCamera.right.x, mainCamera.right.z);
            forward.Normalize();
            right.Normalize();
            Vector2 locomotionDir = forward * vertical + right * horizontal;
            locomotionDir.Normalize();
            locomotionDir *= 2;
            Utils.PlayerLocomotion.HandleAnimatorInputByLocomotionInput(modelForward, locomotionDir, out vertical, out horizontal); // Handle animator input based on locomotion input
            vertical = Mathf.Clamp(vertical, -2f, 2f);
            horizontal = Mathf.Clamp(horizontal, -2f, 2f);
            animator.SetFloat("Vertical", vertical);
            animator.SetFloat("Horizontal", horizontal);
        }
    }

    public class Sprint : BaseState
    {
        Transform mainCamera;
        PlayerManager playerManager;
        Rigidbody playerRigidbody;
        Animator animator;
        InputHandler inputHandler;
        Transform playerTransform;

        int updateState = 0; //0， 1：循环模式， 2：刹车模式, 3:缓冲模式
        public Sprint(LocomotionState fsm) : base(fsm)
        {
            mainCamera = fsm.mainCamera.transform;
            playerManager = fsm.playerManager;
            playerRigidbody = fsm.playerRigidbody;
            animator = fsm.animator;
            inputHandler = fsm.inputHandler;
            playerTransform = fsm.playerTransform;

            //Idle -> Sprint
            fsm.MakeTransition(
                (int)LocomotionType.Idle,
                5,
                ()=> inputHandler.B_Input && inputHandler.B_Input_Time > 0.5f && inputHandler.Movement > 0.55f,
                ()=> { fsm.SwitchState((int)LocomotionType.Sprint); }
                );
            //Walk -> Sprint
            fsm.MakeTransition(
                (int)LocomotionType.Walk,
                5,
                () => inputHandler.B_Input && inputHandler.B_Input_Time > 0.5f && inputHandler.Movement > 0.55f,
                () => { fsm.SwitchState((int)LocomotionType.Sprint); }
                );
            //Run -> Sprint
            fsm.MakeTransition(
                (int)LocomotionType.Run,
                5,
                () => inputHandler.B_Input && inputHandler.B_Input_Time > 0.5f && inputHandler.Movement > 0.55f,
                () => { fsm.SwitchState((int)LocomotionType.Sprint); }
                );
            //刹车监听
            AddListen(4, () => {
                if (updateState != 1)
                    return false;
                if (!inputHandler.B_Input || inputHandler.Movement < 0.55f)
                    return true;
                Quaternion rotation = Utils.PlayerLocomotion.GetPlayerRotationAngle(mainCamera, inputHandler.vertical, inputHandler.horizontal, playerTransform.forward);
                Vector3 forward = playerTransform.forward;
                Vector3 lookDir = rotation * forward;
                forward.y = 0;
                forward.Normalize();
                float angle = Vector3.SignedAngle(forward, lookDir, Vector3.up);
                if (Mathf.Abs(angle) > 90f)
                    return true;
                return false;
                }, 
                () => { 
                    updateState = 2;
                    Debug.Log("开始刹车");
                    animator.CrossFade("SprintEnd", 0.2f);
                    
                }//将模式更改为刹车模式
                );
            //监听是否刹车终止，终止前停止响应4级以下的状态转移
            AddListen(4,
                () =>
                {
                    if (updateState != 2)
                        return false;
                    AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0); //刹车执行80%后进入缓冲模式，开始响应输入
                    if (info.IsName("SprintEnd") && info.normalizedTime > 0.8f) { updateState = 3; }
                    return updateState == 2;
                },
                () => { }
                ); 
            //Sprint -> Idle
            AddListen(5, () => inputHandler.Movement < 0.2f && updateState == 3, () => { fsm.SwitchState((int)LocomotionType.Idle); });
            //Sprint -> Walk
            AddListen(5, () => inputHandler.Movement > 0.2f && inputHandler.Movement < 0.55f, () => { fsm.SwitchState((int)LocomotionType.Walk); });
            //Sprint -> Run
            AddListen(5, () => inputHandler.Movement > 0.55f && !inputHandler.B_Input, () => { fsm.SwitchState((int)LocomotionType.Run); });

        }

        public override void Enter()
        {
            Debug.Log("Sprinting...");
            updateState = 1;
            animator.CrossFade("StrafeSprinting", 1f);
        }

        public override void Exit()
        {
            updateState = 0;
        }

        public override void FixedUpdate()
        {
        }

        public override void Update()
        {
            Listen();
            if(updateState == 3 && inputHandler.B_Input && inputHandler.B_Input_Time > 0.5f)
            {
                updateState = 1;
                animator.CrossFade("StrafeSprinting", 1f);
            }
        }

    }
}


