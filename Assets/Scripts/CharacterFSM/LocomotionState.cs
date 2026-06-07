using UnityEngine;

namespace CFSM
{
    /// <summary>
    /// 默认移动状态。负责 Idle、Walk、Run、Sprint 的子行为，不再把它们拆成主状态。
    /// </summary>
    public class LocomotionState : CharacterStateBase
    {
        /// <summary>状态机引用，用于读取 Inspector 中配置的动画名并播放动画。</summary>
        private readonly CharacterFSM machine;

        /// <summary>上一次播放的移动模式，用于避免每帧重复 CrossFade。</summary>
        private MoveMode lastMoveMode = MoveMode.Idle;

        public LocomotionState(CharacterFSM machine)
        {
            this.machine = machine;
        }

        /// <summary>当前状态类型。</summary>
        public override CharacterStateType StateType => CharacterStateType.Locomotion;

        /// <summary>移动状态优先级最低，通常作为默认回退状态。</summary>
        public override int Priority => StatePriorities.Locomotion;

        /// <summary>
        /// Locomotion 可以被主要动作请求打断，包括跳跃、攻击、防御、闪避和受击。
        /// </summary>
        public override bool CanInterruptBy(StateContext ctx, StateRequest request)
        {
            return request.force
                || request.type == StateRequestType.Jump
                || request.type == StateRequestType.Attack
                || request.type == StateRequestType.Guard
                || request.type == StateRequestType.Dodge
                || request.type == StateRequestType.Hit;
        }

        /// <summary>
        /// 进入移动状态时关闭 root motion，并切换到移动 BlendTree/动画。
        /// </summary>
        public override void Enter(StateContext ctx, StateRequest request)
        {
            if (ctx.animator != null)
                ctx.animator.applyRootMotion = false;

            lastMoveMode = MoveMode.Idle;
            UpdateLocomotionAnimation(ctx, true);
        }

        /// <summary>
        /// 当前移动状态没有额外退出清理。
        /// </summary>
        public override void Exit(StateContext ctx)
        {
        }

        /// <summary>
        /// 更新 Animator 移动参数，把相机相对输入转换为角色本地方向上的 Vertical/Horizontal。
        /// </summary>
        public override void Tick(StateContext ctx)
        {
            if (ctx.animator == null || ctx.playerTransform == null)
                return;

            UpdateLocomotionAnimation(ctx, false);

            Vector2 locomotionDir = GetCameraRelativeInput(ctx);
            Vector2 modelForward = new Vector2(ctx.playerTransform.forward.x, ctx.playerTransform.forward.z).normalized;

            float speedScale = GetAnimatorSpeedScale(ctx);
            Utils.PlayerLocomotion.HandleAnimatorInputByLocomotionInput(
                modelForward,
                locomotionDir * speedScale,
                out float vertical,
                out float horizontal);

            vertical = Mathf.Clamp(vertical, -speedScale, speedScale);
            horizontal = Mathf.Clamp(horizontal, -speedScale, speedScale);

            ctx.animator.SetFloat("Vertical", Mathf.Lerp(ctx.animator.GetFloat("Vertical"), vertical, Time.deltaTime * 10f));
            ctx.animator.SetFloat("Horizontal", Mathf.Lerp(ctx.animator.GetFloat("Horizontal"), horizontal, Time.deltaTime * 10f));
        }

        /// <summary>
        /// 执行刚体移动和角色朝向旋转。无输入时清掉水平速度，避免角色继续滑动。
        /// </summary>
        public override void FixedTick(StateContext ctx)
        {
            if (ctx.playerManager == null)
                return;

            if (ctx.movementAmount <= 0.05f)
            {
                StopHorizontalVelocity(ctx);
                return;
            }

            Vector3 moveDir = GetCameraRelativeMoveDirection(ctx);
            if (moveDir.sqrMagnitude <= 0.0001f)
                return;

            if (ctx.playerManager.canRotate)
                ctx.playerManager.LookRotate(moveDir, Vector3.up);

            ctx.playerManager.Move(moveDir, GetMoveSpeed(ctx), Vector3.up);
        }

        /// <summary>
        /// 保留垂直速度，只清除水平速度。用于无移动输入时停止角色平面移动。
        /// </summary>
        private static void StopHorizontalVelocity(StateContext ctx)
        {
            if (ctx.playerBody == null)
                return;

            Vector3 velocity = ctx.playerBody.velocity;
            ctx.playerBody.velocity = new Vector3(0f, velocity.y, 0f);
        }

        /// <summary>
        /// 根据输入强度和疾跑键计算 Animator 参数缩放值。
        /// </summary>
        private static float GetAnimatorSpeedScale(StateContext ctx)
        {
            switch (ctx.moveMode)
            {
                case MoveMode.Sprint:
                case MoveMode.Run:
                    return 2f;
                case MoveMode.Walk:
                    return 1f;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 根据当前移动模式切换 Locomotion/Sprinting 动画。Walk 和 Run 共用移动 BlendTree。
        /// </summary>
        private void UpdateLocomotionAnimation(StateContext ctx, bool force)
        {
            MoveMode targetMode = ctx.moveMode == MoveMode.Sprint ? MoveMode.Sprint : MoveMode.Run;
            if (!force && targetMode == lastMoveMode)
                return;

            lastMoveMode = targetMode;

            if (targetMode == MoveMode.Sprint)
                machine.CrossFade(machine.sprintAnimation, 0.15f);
            else
                machine.CrossFade(machine.locomotionAnimation, 0.2f);
        }

        /// <summary>
        /// 根据输入强度和疾跑键选择实际移动速度。
        /// </summary>
        private static float GetMoveSpeed(StateContext ctx)
        {
            switch (ctx.moveMode)
            {
                case MoveMode.Sprint:
                    return ctx.playerManager.SprintSpeed;
                case MoveMode.Run:
                    return ctx.playerManager.RunSpeed;
                case MoveMode.Walk:
                    return ctx.playerManager.WalkSpeed;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 获取相机相对输入方向，用于 Animator 参数计算。
        /// </summary>
        private static Vector2 GetCameraRelativeInput(StateContext ctx)
        {
            if (ctx.mainCamera == null)
                return ctx.moveInput.normalized;

            Vector3 moveDir = GetCameraRelativeMoveDirection(ctx);
            return new Vector2(moveDir.x, moveDir.z);
        }

        /// <summary>
        /// 将输入轴转换为世界空间中的相机相对移动方向。
        /// </summary>
        private static Vector3 GetCameraRelativeMoveDirection(StateContext ctx)
        {
            Vector3 forward = ctx.mainCamera != null ? ctx.mainCamera.transform.forward : ctx.playerTransform.forward;
            Vector3 right = ctx.mainCamera != null ? ctx.mainCamera.transform.right : ctx.playerTransform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDir = forward * ctx.moveInput.y + right * ctx.moveInput.x;
            if (moveDir.sqrMagnitude > 1f)
                moveDir.Normalize();

            return moveDir;
        }
    }
}
