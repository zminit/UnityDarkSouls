#define CHARACTER_FSM_DEBUG

using UnityEngine;

namespace CFSM
{
    /// <summary>
    /// 默认移动状态。负责 Idle、Walk、Run、Sprint 的子行为，不再把它们拆成主状态。
    /// </summary>
    public class LocomotionState : CharacterStateBase
    {
#if CHARACTER_FSM_DEBUG
        private const float DebugWorldMoveDirArrowLength = 1.5f;
        private const float DebugWorldMoveDirArrowHeadLength = 0.3f;
        private const float DebugWorldMoveDirArrowHeadAngle = 25f;
#endif

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

            if (ShouldUseStrafeMove(ctx))
            {
                UpdateStrafeAnimatorParameters(ctx);
                return;
            }

            UpdateFreeMoveAnimatorParameters(ctx);
        }

        private static void UpdateFreeMoveAnimatorParameters(StateContext ctx)
        {
            float vertical = ctx.movementAmount > 0.05f ? GetAnimatorSpeedScale(ctx) : 0f;
            float horizontal = 0f;

            ctx.animator.SetFloat("Vertical", Mathf.Lerp(ctx.animator.GetFloat("Vertical"), vertical, Time.deltaTime * 10f));
            ctx.animator.SetFloat("Horizontal", Mathf.Lerp(ctx.animator.GetFloat("Horizontal"), horizontal, Time.deltaTime * 10f));
        }

        private static void UpdateStrafeAnimatorParameters(StateContext ctx)
        {
            if (ctx.movementAmount <= 0.05f)
            {
                ctx.animator.SetFloat("Vertical", Mathf.Lerp(ctx.animator.GetFloat("Vertical"), 0f, Time.deltaTime * 10f));
                ctx.animator.SetFloat("Horizontal", Mathf.Lerp(ctx.animator.GetFloat("Horizontal"), 0f, Time.deltaTime * 10f));
                return;
            }

            Vector3 worldMoveDir = GetCameraRelativeMoveDirection(ctx);
            Vector3 localMoveDir = ctx.playerTransform.InverseTransformDirection(worldMoveDir.normalized);

            float speedScale = GetAnimatorSpeedScale(ctx);
            float horizontal = localMoveDir.x * speedScale;
            float vertical = localMoveDir.z * speedScale;

            vertical = Mathf.Clamp(vertical, -speedScale, speedScale);
            horizontal = Mathf.Clamp(horizontal, -speedScale, speedScale);

            ctx.animator.SetFloat("Vertical", Mathf.Lerp(ctx.animator.GetFloat("Vertical"), vertical, Time.deltaTime * 10f));
            ctx.animator.SetFloat("Horizontal", Mathf.Lerp(ctx.animator.GetFloat("Horizontal"), horizontal, Time.deltaTime * 10f));
        }

        private static bool ShouldUseStrafeMove(StateContext ctx)
        {
            return ctx.blackBoard != null
                && ctx.blackBoard.TryGetValue("UseStrafeMove", out bool useStrafeMove)
                && useStrafeMove;
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

            bool useStrafeMove = ShouldUseStrafeMove(ctx);
            Vector3 moveDir = GetCameraRelativeMoveDirection(ctx);
            if (moveDir.sqrMagnitude <= 0.0001f)
                return;

#if CHARACTER_FSM_DEBUG
            DrawWorldMoveDirection(ctx, moveDir);
#endif

            if (!useStrafeMove && ctx.playerManager.canRotate)
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

#if CHARACTER_FSM_DEBUG
        private static void DrawWorldMoveDirection(StateContext ctx, Vector3 worldMoveDir)
        {
            if (ctx.playerTransform == null || worldMoveDir.sqrMagnitude <= 0.0001f)
                return;

            Vector3 direction = worldMoveDir.normalized;
            Vector3 origin = ctx.playerBody != null
                ? ctx.playerBody.worldCenterOfMass
                : ctx.playerTransform.position + Vector3.up;

            Vector3 tip = origin + direction * DebugWorldMoveDirArrowLength;
            Vector3 leftHead = Quaternion.AngleAxis(180f - DebugWorldMoveDirArrowHeadAngle, Vector3.up)
                * direction
                * DebugWorldMoveDirArrowHeadLength;
            Vector3 rightHead = Quaternion.AngleAxis(180f + DebugWorldMoveDirArrowHeadAngle, Vector3.up)
                * direction
                * DebugWorldMoveDirArrowHeadLength;

            Debug.DrawLine(origin, tip, Color.red, Time.fixedDeltaTime);
            Debug.DrawLine(tip, tip + leftHead, Color.red, Time.fixedDeltaTime);
            Debug.DrawLine(tip, tip + rightHead, Color.red, Time.fixedDeltaTime);
        }
#endif
    }
}
