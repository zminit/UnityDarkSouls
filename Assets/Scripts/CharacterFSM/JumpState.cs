using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CFSM
{
    public class JumpState : BaseState
    {

        PlayerManager playerManager;
        InputHandler inputHandler;
        Animator animator;
        Rigidbody rb;

        bool jumpFlag = false;
        int jumpStatus = 0; // 0 : 初始化， 1：开始， 2：进行中， 3：结束， 4：缓冲就绪
        public JumpState(CharacterFSM fsm) : base(fsm)
        {
            playerManager = fsm.playerManager;
            inputHandler = fsm.inputHandler;
            animator = fsm.animator;
            rb = fsm.playerBody;

            
            inputHandler.A_Input_Started += ()=>{ jumpFlag = true; };
            fsm.MakeTransition((int)CharacterStateType.LocomotionState, 6,
                () => jumpFlag,
                () => { jumpFlag = false; fsm.SwitchState((int)CharacterStateType.JumpState); });

            AddListen(5,
                () =>
                {
                    if (!playerManager.OnLandHandler.OnLandCheck())
                    {
                        return false;
                    }
                    AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                    return jumpStatus == 4 && info.IsName("Landing") && info.normalizedTime > 0.95f;
                },
                () => { Debug.Log("jump to Locomotion"); fsm.SwitchState((int)CharacterStateType.LocomotionState); });
        }

        public override void Enter()
        {
            jumpStatus = 0;
            Debug.Log("Jumping");
        }

        public override void Exit()
        {
            jumpStatus = 0;
        }

        public override void FixedUpdate()
        {

        }

        public override void Update()
        {
            if(Listen()) return;
            switch (jumpStatus)
            {
                case 0:
                    animator.CrossFade("JumpStart", 0.1f);
                    rb.velocity += 5f * Vector3.up;
                    Debug.Log("jumpStatus to 1");
                    jumpStatus = 1;
                    break;
                case 1:
                    if (animator.GetCurrentAnimatorStateInfo(0).IsName("Jumping"))
                    {
                        Debug.Log("jumpStatus to 2");
                        jumpStatus = 2;
                    }
                    break;
                case 2:
                    if (playerManager.OnLandHandler.OnLandCheck())
                    {
                        animator.CrossFade("Landing", 0.1f);
                        Debug.Log("jumpStatus to 3");
                        jumpStatus = 3;
                    }
                    break;
                case 3:
                    AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                    if (info.IsName("Landing") && info.normalizedTime > 0.8f)
                    {
                        Debug.Log("jumpStatus to 4");
                        jumpStatus = 4;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}

