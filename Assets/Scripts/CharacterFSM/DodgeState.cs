using UnityEngine;

namespace CFSM
{
    /// <summary>
    /// 闪避状态。v1 只负责播放闪避动画并在动画结束后回到 Locomotion。
    /// </summary>
    public class DodgeState : CharacterStateBase
    {
        /// <summary>状态机引用，用于读取 Inspector 中配置的闪避动画名。</summary>
        private readonly CharacterFSM machine;

        /// <summary>进入闪避状态的时间，用于动画未匹配时的兜底退出。</summary>
        private float enteredAt;

        /// <summary>本次闪避的世界方向。无方向输入时使用角色后方。</summary>
        private Vector3 dodgeWorldDirection;

        /// <summary>Dodge BlendTree 使用的 Horizontal/Vertical 参数。</summary>
        private Vector2 dodgeAnimatorInput;

        /// <summary>闪避动画结束检测失败时的兜底退出时间。</summary>
        private const float FallbackDuration = 0.8f;

        public DodgeState(CharacterFSM machine)
        {
            this.machine = machine;
        }

        /// <summary>当前状态类型。</summary>
        public override CharacterStateType StateType => CharacterStateType.Dodge;

        /// <summary>闪避请求优先级，高于攻击、跳跃和防御。</summary>
        public override int Priority => StatePriorities.Dodge;

        /// <summary>闪避默认不可被普通输入中断。</summary>
        public override bool IsInterruptible => false;

        /// <summary>
        /// 闪避只允许强制请求或受击打断，普通移动、跳跃、攻击不会打断。
        /// </summary>
        public override bool CanInterruptBy(StateContext ctx, StateRequest request)
        {
            return request.force || request.type == StateRequestType.Hit;
        }

        /// <summary>
        /// 进入闪避时启用 root motion，并播放闪避动画。
        /// </summary>
        public override void Enter(StateContext ctx, StateRequest request)
        {
            enteredAt = Time.time;
            dodgeAnimatorInput = ResolveDodgeAnimatorInput(ctx, request);
            dodgeWorldDirection = ResolveDodgeWorldDirection(ctx, request);

            if (ctx.animator != null)
            {
                ctx.animator.applyRootMotion = false;
                SetDodgeAnimatorParameters(ctx);
            }

            machine.CrossFade(machine.dodgeAnimation, 0.1f);
        }

        /// <summary>
        /// 当前闪避状态没有额外退出清理。
        /// </summary>
        public override void Exit(StateContext ctx)
        {
            StopHorizontalVelocity(ctx);
        }

        /// <summary>
        /// 闪避动画结束或兜底超时后，请求强制回到 Locomotion。
        /// </summary>
        public override void Tick(StateContext ctx)
        {
            SetDodgeAnimatorParameters(ctx);

            bool dodgeMoveFinished = Time.time - enteredAt >= machine.dodgeDuration;
            bool shouldExit = dodgeMoveFinished && HasAnimationFinished(ctx);
            bool fallbackExit = Time.time - enteredAt > Mathf.Max(FallbackDuration, machine.dodgeDuration);

            if (shouldExit || fallbackExit)
            {
                ctx.SubmitRequest(StateRequest.Create(
                    StateRequestType.AnimationEnd,
                    CharacterStateType.Locomotion,
                    StatePriorities.Locomotion,
                    RequestSource.State,
                    force: true));
            }
        }

        /// <summary>
        /// v1 闪避位移交给 root motion，暂不做额外物理控制。
        /// </summary>
        public override void FixedTick(StateContext ctx)
        {
            if (ctx.playerBody == null)
                return;

            if (Time.time - enteredAt > machine.dodgeDuration)
            {
                StopHorizontalVelocity(ctx);
                return;
            }

            Vector3 velocity = dodgeWorldDirection * machine.dodgeSpeed;
            velocity.y = ctx.playerBody.velocity.y;
            ctx.playerBody.velocity = velocity;
        }

        /// <summary>
        /// 清理闪避产生的水平速度，避免状态结束后继续滑动。
        /// </summary>
        private static void StopHorizontalVelocity(StateContext ctx)
        {
            if (ctx.playerBody == null)
                return;

            Vector3 velocity = ctx.playerBody.velocity;
            ctx.playerBody.velocity = new Vector3(0f, velocity.y, 0f);
        }

        /// <summary>
        /// 解析 Dodge BlendTree 的输入参数。短按 Shift 无方向时使用 (0,-1)，方向闪避时把相机相对输入转换为角色本地方向。
        /// </summary>
        private static Vector2 ResolveDodgeAnimatorInput(StateContext ctx, StateRequest request)
        {
            Vector2 inputDirection = Vector2.zero;

            if (request.payload is DodgeRequestPayload payload)
                inputDirection = payload.inputDirection;
            else if (ctx.rawMoveInput.sqrMagnitude > 0.0025f)
                inputDirection = ctx.rawMoveInput;

            if (inputDirection.sqrMagnitude > 1f)
                inputDirection.Normalize();

            if (inputDirection.sqrMagnitude <= 0.0025f)
                return Vector2.down;

            Vector3 worldDirection = GetCameraRelativeWorldDirection(ctx, inputDirection);
            if (worldDirection.sqrMagnitude <= 0.0025f)
                return Vector2.down;

            if (ctx.playerTransform == null)
                return new Vector2(worldDirection.x, worldDirection.z).normalized;

            Vector3 localDirection = ctx.playerTransform.InverseTransformDirection(worldDirection);
            Vector2 animatorInput = new Vector2(localDirection.x, localDirection.z);
            return animatorInput.sqrMagnitude > 1f ? animatorInput.normalized : animatorInput;
        }

        /// <summary>
        /// 写入 Dodge BlendTree 参数。
        /// </summary>
        private void SetDodgeAnimatorParameters(StateContext ctx)
        {
            if (ctx.animator == null)
                return;

            ctx.animator.SetFloat("Horizontal", dodgeAnimatorInput.x);
            ctx.animator.SetFloat("Vertical", dodgeAnimatorInput.y);
        }

        /// <summary>
        /// 从 DodgeRequestPayload 解析闪避方向。带方向输入时按相机相对方向闪避，否则向角色后方闪避。
        /// </summary>
        private static Vector3 ResolveDodgeWorldDirection(StateContext ctx, StateRequest request)
        {
            Vector2 inputDirection = Vector2.zero;
            bool useBackwardWhenNoInput = true;

            if (request.payload is DodgeRequestPayload payload)
            {
                inputDirection = payload.inputDirection;
                useBackwardWhenNoInput = payload.useBackwardWhenNoInput;
            }
            else if (ctx.rawMoveInput.sqrMagnitude > 0.0025f)
            {
                inputDirection = ctx.rawMoveInput;
            }

            if (inputDirection.sqrMagnitude > 0.0025f)
            {
                inputDirection.Normalize();

                Vector3 direction = GetCameraRelativeWorldDirection(ctx, inputDirection);
                if (direction.sqrMagnitude > 0.0025f)
                    return direction.normalized;
            }

            if (useBackwardWhenNoInput && ctx.playerTransform != null)
            {
                Vector3 backward = -ctx.playerTransform.forward;
                backward.y = 0f;
                if (backward.sqrMagnitude > 0.0025f)
                    return backward.normalized;
            }

            return Vector3.back;
        }

        /// <summary>
        /// 将输入方向转换为相机相对世界方向。和 Locomotion 的移动方向计算保持一致。
        /// </summary>
        private static Vector3 GetCameraRelativeWorldDirection(StateContext ctx, Vector2 inputDirection)
        {
            Transform basis = ctx.mainCamera != null ? ctx.mainCamera.transform : ctx.playerTransform;
            Vector3 forward = basis != null ? basis.forward : Vector3.forward;
            Vector3 right = basis != null ? basis.right : Vector3.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 direction = forward * inputDirection.y + right * inputDirection.x;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        /// <summary>
        /// 判断闪避动画是否播放到接近结束。
        /// </summary>
        private bool HasAnimationFinished(StateContext ctx)
        {
            if (ctx.animator == null || string.IsNullOrEmpty(machine.dodgeAnimation))
                return false;

            AnimatorStateInfo info = ctx.animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(machine.dodgeAnimation) && info.normalizedTime > 0.9f;
        }
    }

    /// <summary>
    /// 防御状态。v1 保留状态结构，输入系统重构后再接 LB/LT 等防御输入。
    /// </summary>
    public class GuardState : CharacterStateBase
    {
        /// <summary>状态机引用，用于读取 Inspector 中配置的防御动画名。</summary>
        private readonly CharacterFSM machine;

        public GuardState(CharacterFSM machine)
        {
            this.machine = machine;
        }

        /// <summary>当前状态类型。</summary>
        public override CharacterStateType StateType => CharacterStateType.Guard;

        /// <summary>防御请求优先级。</summary>
        public override int Priority => StatePriorities.Guard;

        /// <summary>
        /// 防御可被闪避、受击或强制请求中断。
        /// </summary>
        public override bool CanInterruptBy(StateContext ctx, StateRequest request)
        {
            return request.force
                || request.type == StateRequestType.Dodge
                || request.type == StateRequestType.Hit;
        }

        /// <summary>
        /// 进入防御时关闭 root motion，并播放防御动画。
        /// </summary>
        public override void Enter(StateContext ctx, StateRequest request)
        {
            if (ctx.animator != null)
                ctx.animator.applyRootMotion = false;

            machine.CrossFade(machine.guardAnimation, 0.1f);
        }

        /// <summary>
        /// 当前防御状态没有额外退出清理。
        /// </summary>
        public override void Exit(StateContext ctx)
        {
        }

        /// <summary>
        /// 如果黑板中 GuardPressed 被设置为 false，则请求回到 Locomotion。
        /// </summary>
        public override void Tick(StateContext ctx)
        {
            if (ctx.blackBoard.TryGetValue("GuardPressed", out bool guardPressed) && !guardPressed)
            {
                ctx.SubmitRequest(StateRequest.Create(
                    StateRequestType.Guard,
                    CharacterStateType.Locomotion,
                    StatePriorities.Locomotion,
                    RequestSource.State,
                    force: true));
            }
        }

        /// <summary>
        /// v1 暂不实现防御移动或格挡判定。
        /// </summary>
        public override void FixedTick(StateContext ctx)
        {
        }
    }

    /// <summary>
    /// 受击状态预留。v1 只播放受击动画并在结束或超时后回到 Locomotion。
    /// </summary>
    public class HitState : CharacterStateBase
    {
        /// <summary>状态机引用，用于读取 Inspector 中配置的受击动画名。</summary>
        private readonly CharacterFSM machine;

        /// <summary>进入受击状态的时间，用于兜底退出。</summary>
        private float enteredAt;

        /// <summary>受击动画结束检测失败时的兜底退出时间。</summary>
        private const float FallbackDuration = 0.6f;

        public HitState(CharacterFSM machine)
        {
            this.machine = machine;
        }

        /// <summary>当前状态类型。</summary>
        public override CharacterStateType StateType => CharacterStateType.Hit;

        /// <summary>受击优先级最高。</summary>
        public override int Priority => StatePriorities.Hit;

        /// <summary>受击默认不可被普通请求中断。</summary>
        public override bool IsInterruptible => false;

        /// <summary>
        /// v1 受击只允许强制请求打断，后续可扩展为击飞、倒地等更高层受击链路。
        /// </summary>
        public override bool CanInterruptBy(StateContext ctx, StateRequest request)
        {
            return request.force;
        }

        /// <summary>
        /// 进入受击时启用 root motion，并播放受击动画。
        /// </summary>
        public override void Enter(StateContext ctx, StateRequest request)
        {
            enteredAt = Time.time;

            if (ctx.animator != null)
                ctx.animator.applyRootMotion = true;

            machine.CrossFade(machine.hitAnimation, 0.1f);
        }

        /// <summary>
        /// 当前受击状态没有额外退出清理。
        /// </summary>
        public override void Exit(StateContext ctx)
        {
        }

        /// <summary>
        /// 受击动画结束或兜底超时后，请求回到 Locomotion。
        /// </summary>
        public override void Tick(StateContext ctx)
        {
            if (HasAnimationFinished(ctx) || Time.time - enteredAt > FallbackDuration)
            {
                ctx.SubmitRequest(StateRequest.Create(
                    StateRequestType.AnimationEnd,
                    CharacterStateType.Locomotion,
                    StatePriorities.Locomotion,
                    RequestSource.State,
                    force: true));
            }
        }

        /// <summary>
        /// v1 暂不实现受击物理位移，后续可在这里加入击退。
        /// </summary>
        public override void FixedTick(StateContext ctx)
        {
        }

        /// <summary>
        /// 判断受击动画是否播放到接近结束。
        /// </summary>
        private bool HasAnimationFinished(StateContext ctx)
        {
            if (ctx.animator == null || string.IsNullOrEmpty(machine.hitAnimation))
                return false;

            AnimatorStateInfo info = ctx.animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(machine.hitAnimation) && info.normalizedTime > 0.9f;
        }
    }

    /// <summary>
    /// 击飞/空中受击状态预留。v1 只等待落地后回到 Locomotion。
    /// </summary>
    public class AirborneState : CharacterStateBase
    {
        private float enteredAt;
        private float groundedSince = -1f;
        private const float MinAirborneDuration = 0.1f;
        private const float LandingConfirmDelay = 0.08f;
        /// <summary>状态机引用，用于读取 Inspector 中配置的击飞动画名。</summary>
        private readonly CharacterFSM machine;

        public AirborneState(CharacterFSM machine)
        {
            this.machine = machine;
        }

        /// <summary>当前状态类型。</summary>
        public override CharacterStateType StateType => CharacterStateType.Airborne;

        /// <summary>击飞按受击优先级处理。</summary>
        public override int Priority => StatePriorities.Airborne;

        /// <summary>击飞默认不可被普通请求中断。</summary>
        public override bool IsInterruptible => false;

        /// <summary>
        /// 击飞只允许强制请求或受击请求打断。
        /// </summary>
        public override bool CanInterruptBy(StateContext ctx, StateRequest request)
        {
            return request.force || request.type == StateRequestType.Hit;
        }

        /// <summary>
        /// 进入击飞状态时关闭 root motion，并播放击飞动画。
        /// </summary>
        public override void Enter(StateContext ctx, StateRequest request)
        {
            enteredAt = Time.time;
            groundedSince = -1f;

            if (ctx.animator != null)
                ctx.animator.applyRootMotion = false;

            machine.CrossFade(machine.airborneAnimation, 0.1f);
        }

        /// <summary>
        /// 当前击飞状态没有额外退出清理。
        /// </summary>
        public override void Exit(StateContext ctx)
        {
        }

        /// <summary>
        /// 检测落地，落地后请求回到 Locomotion。后续可在这里转入倒地或起身状态。
        /// </summary>
        public override void Tick(StateContext ctx)
        {
            if (ctx.playerManager == null || ctx.playerManager.OnLandHandler == null)
                return;

            if (Time.time - enteredAt < MinAirborneDuration)
                return;

            bool isGrounded = ctx.playerManager.OnLandHandler.OnLandCheck();
            if (!isGrounded)
            {
                groundedSince = -1f;
                return;
            }

            if (groundedSince < 0f)
                groundedSince = Time.time;

            if (Time.time - groundedSince >= LandingConfirmDelay)
            {
                ctx.SubmitRequest(StateRequest.Create(
                    StateRequestType.Land,
                    CharacterStateType.Locomotion,
                    StatePriorities.Locomotion,
                    RequestSource.State,
                    force: true));
            }
        }

        /// <summary>
        /// v1 暂不实现击飞物理，后续可在这里加入抛物线、击退或倒地控制。
        /// </summary>
        public override void FixedTick(StateContext ctx)
        {
        }
    }

    public abstract class WeaponActionStateBase : CharacterStateBase
    {
        private readonly CharacterFSM machine;
        private readonly CharacterStateType stateType;
        private readonly StateRequestType requestType;
        private float enteredAt;
        private bool completed;

        protected WeaponActionStateBase(
            CharacterFSM machine,
            CharacterStateType stateType,
            StateRequestType requestType)
        {
            this.machine = machine;
            this.stateType = stateType;
            this.requestType = requestType;
        }

        public override CharacterStateType StateType => stateType;
        public override int Priority => StatePriorities.WeaponAction;
        public override bool IsInterruptible => false;

        protected abstract string AnimationName { get; }
        protected abstract float CrossFadeDuration(PlayerManager playerManager);
        protected abstract float FallbackDuration { get; }
        protected abstract bool CanStart(PlayerManager playerManager);
        protected abstract void Begin(PlayerManager playerManager);
        protected abstract void Complete(PlayerManager playerManager);

        public override bool CanEnter(StateContext ctx, StateRequest request)
        {
            return request.type == requestType
                && ctx.playerManager != null
                && CanStart(ctx.playerManager);
        }

        public override bool CanInterruptBy(StateContext ctx, StateRequest request)
        {
            if (request.force || request.type == StateRequestType.Hit)
                return true;

            if (request.type == StateRequestType.Move && ctx.movementAmount > 0.05f)
                return true;

            return request.type == StateRequestType.Attack
                || request.type == StateRequestType.Jump
                || request.type == StateRequestType.Dodge;
        }

        public override void Enter(StateContext ctx, StateRequest request)
        {
            enteredAt = Time.time;
            completed = false;

            if (ctx.animator != null)
                ctx.animator.applyRootMotion = true;

            Begin(ctx.playerManager);
            machine.CrossFade(AnimationName, CrossFadeDuration(ctx.playerManager));
        }

        public override void Exit(StateContext ctx)
        {
            if (!completed)
                ctx.playerManager?.CancelWeaponAction();
        }

        public override void Tick(StateContext ctx)
        {
            if (HasAnimationFinished(ctx) || Time.time - enteredAt >= FallbackDuration)
                Finish(ctx);
        }

        public override void FixedTick(StateContext ctx)
        {
        }

        private void Finish(StateContext ctx)
        {
            if (completed)
                return;

            completed = true;
            Complete(ctx.playerManager);
            ctx.SubmitRequest(StateRequest.Create(
                StateRequestType.AnimationEnd,
                CharacterStateType.Locomotion,
                StatePriorities.Locomotion,
                RequestSource.State,
                force: true));
        }

        private bool HasAnimationFinished(StateContext ctx)
        {
            if (ctx.animator == null || string.IsNullOrEmpty(AnimationName))
                return false;

            AnimatorStateInfo info = ctx.animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(AnimationName) && info.normalizedTime >= 0.99f;
        }
    }

    public class DrawWeaponState : WeaponActionStateBase
    {
        private readonly CharacterFSM machine;

        public DrawWeaponState(CharacterFSM machine)
            : base(machine, CharacterStateType.DrawWeapon, StateRequestType.DrawWeapon)
        {
            this.machine = machine;
        }

        protected override string AnimationName => machine.drawWeaponAnimation;
        protected override float FallbackDuration => machine.drawWeaponFallbackDuration;

        protected override float CrossFadeDuration(PlayerManager playerManager)
        {
            return playerManager != null ? playerManager.DrawWeaponCrossFadeDuration : 0.1f;
        }

        protected override bool CanStart(PlayerManager playerManager)
        {
            return playerManager != null && !playerManager.IsArmed && !playerManager.IsChangingWeaponState;
        }

        protected override void Begin(PlayerManager playerManager)
        {
            playerManager?.BeginDrawWeaponAction();
        }

        protected override void Complete(PlayerManager playerManager)
        {
            playerManager?.CancelWeaponAction();
        }
    }

    public class SheatheWeaponState : WeaponActionStateBase
    {
        private readonly CharacterFSM machine;

        public SheatheWeaponState(CharacterFSM machine)
            : base(machine, CharacterStateType.SheatheWeapon, StateRequestType.SheatheWeapon)
        {
            this.machine = machine;
        }

        protected override string AnimationName => machine.sheatheWeaponAnimation;
        protected override float FallbackDuration => machine.sheatheWeaponFallbackDuration;

        protected override float CrossFadeDuration(PlayerManager playerManager)
        {
            return playerManager != null ? playerManager.SheatheWeaponCrossFadeDuration : 0.1f;
        }

        protected override bool CanStart(PlayerManager playerManager)
        {
            return playerManager != null && playerManager.IsArmed && !playerManager.IsChangingWeaponState;
        }

        protected override void Begin(PlayerManager playerManager)
        {
            playerManager?.BeginSheatheWeaponAction();
        }

        protected override void Complete(PlayerManager playerManager)
        {
            playerManager?.CancelWeaponAction();
        }
    }
}
