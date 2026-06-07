using UnityEngine;

namespace CFSM
{
    /// <summary>
    /// 跳跃状态。负责起跳、滞空、落地流程，并预留空中攻击和受击/击飞中断入口。
    /// </summary>
    public class JumpState : CharacterStateBase
    {
        /// <summary>状态机引用，用于播放 Inspector 中配置的跳跃动画名。</summary>
        private readonly CharacterFSM machine;

        /// <summary>跳跃内部流程标记：0 起跳，1 等待滞空动画，2 滞空，3 落地，4 准备退出。</summary>
        private int jumpStatus;

        public JumpState(CharacterFSM machine)
        {
            this.machine = machine;
        }

        /// <summary>当前状态类型。</summary>
        public override CharacterStateType StateType => CharacterStateType.Jump;

        /// <summary>跳跃请求优先级。</summary>
        public override int Priority => StatePriorities.Jump;

        /// <summary>
        /// 跳跃状态允许被攻击、闪避、受击和击飞中断，为之后空中攻击和击飞流程预留入口。
        /// </summary>
        public override bool CanInterruptBy(StateContext ctx, StateRequest request)
        {
            return request.force
                || request.type == StateRequestType.Attack
                || request.type == StateRequestType.Hit
                || request.targetState == CharacterStateType.Airborne
                || request.type == StateRequestType.Dodge;
        }

        /// <summary>
        /// 进入跳跃状态时重置内部流程，并关闭 root motion，让刚体速度控制起跳。
        /// </summary>
        public override void Enter(StateContext ctx, StateRequest request)
        {
            jumpStatus = 0;

            if (ctx.animator != null)
                ctx.animator.applyRootMotion = false;
        }

        /// <summary>
        /// 退出跳跃状态时重置内部流程，避免下次跳跃继承旧阶段。
        /// </summary>
        public override void Exit(StateContext ctx)
        {
            jumpStatus = 0;
        }

        /// <summary>
        /// 驱动跳跃生命周期：起跳施加速度，检测滞空动画，落地后请求回到 Locomotion。
        /// </summary>
        public override void Tick(StateContext ctx)
        {
            if (ctx.animator == null || ctx.playerBody == null)
                return;

            switch (jumpStatus)
            {
                case 0:
                    machine.CrossFade(machine.jumpStartAnimation, 0.1f);
                    ctx.playerBody.velocity += Vector3.up * 5f;
                    jumpStatus = 1;
                    break;
                case 1:
                    if (IsCurrentAnimation(ctx, machine.jumpLoopAnimation))
                        jumpStatus = 2;
                    break;
                case 2:
                    if (IsGrounded(ctx))
                    {
                        machine.CrossFade(machine.landingAnimation, 0.1f);
                        jumpStatus = 3;
                    }
                    break;
                case 3:
                    AnimatorStateInfo info = ctx.animator.GetCurrentAnimatorStateInfo(0);
                    if (info.IsName(machine.landingAnimation) && info.normalizedTime > 0.8f)
                        jumpStatus = 4;
                    break;
                case 4:
                    if (!IsCurrentAnimation(ctx, machine.landingAnimation)
                        || ctx.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.95f)
                    {
                        ctx.SubmitRequest(StateRequest.Create(
                            StateRequestType.Land,
                            CharacterStateType.Locomotion,
                            StatePriorities.Locomotion,
                            RequestSource.State,
                            force: true));
                    }
                    break;
            }
        }

        /// <summary>
        /// v1 暂不实现空中位移控制，后续可在这里加入空中微调。
        /// </summary>
        public override void FixedTick(StateContext ctx)
        {
        }

        /// <summary>
        /// 查询角色是否接触地面。
        /// </summary>
        private static bool IsGrounded(StateContext ctx)
        {
            return ctx.playerManager != null
                && ctx.playerManager.OnLandHandler != null
                && ctx.playerManager.OnLandHandler.OnLandCheck();
        }

        /// <summary>
        /// 判断 Animator 当前层是否正在播放指定状态。
        /// </summary>
        private static bool IsCurrentAnimation(StateContext ctx, string animationName)
        {
            return ctx.animator != null
                && !string.IsNullOrEmpty(animationName)
                && ctx.animator.GetCurrentAnimatorStateInfo(0).IsName(animationName);
        }
    }
}
