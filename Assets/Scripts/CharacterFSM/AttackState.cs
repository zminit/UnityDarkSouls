using UnityEngine;

namespace CFSM
{
    /// <summary>
    /// 攻击状态。v1 支持轻攻击、重攻击、空中攻击入口，连段和完整取消规则后续扩展。
    /// </summary>
    public class AttackState : CharacterStateBase
    {
        /// <summary>状态机引用，用于读取 Inspector 中配置的攻击动画名。</summary>
        private readonly CharacterFSM machine;

        /// <summary>进入攻击状态的时间，用于取消窗口和超时回退。</summary>
        private float enteredAt;

        /// <summary>是否进入取消窗口。v1 只允许取消到 Dodge。</summary>
        private bool cancelWindow;

        /// <summary>当前攻击实际播放的动画状态名。</summary>
        private string currentAnimation;

        private int comboIndex;
        private bool isLightCombo;
        private bool comboWindowOpen;
        private bool bufferedLightAttack;
        private bool comboConsumed;
        private float fallbackDuration;

        /// <summary>进入攻击后至少等待该时长才打开取消窗口。</summary>
        private const float MinCancelTime = 0.35f;

        /// <summary>动画状态名未匹配或动画事件缺失时的兜底退出时间。</summary>
        private const float NormalizedEndTime = 0.95f;

        public AttackState(CharacterFSM machine)
        {
            this.machine = machine;
        }

        /// <summary>当前状态类型。</summary>
        public override CharacterStateType StateType => CharacterStateType.Attack;

        /// <summary>攻击请求优先级。</summary>
        public override int Priority => StatePriorities.Attack;

        /// <summary>攻击默认不可被普通输入中断，具体中断规则由 CanInterruptBy 控制。</summary>
        public override bool IsInterruptible => false;

        /// <summary>
        /// 攻击可被强制请求或受击中断；进入取消窗口后允许闪避中断。
        /// </summary>
        public override bool CanInterruptBy(StateContext ctx, StateRequest request)
        {
            if (request.force || request.type == StateRequestType.Hit)
                return true;

            if (cancelWindow && request.type == StateRequestType.Dodge)
                return true;

            return false;
        }

        /// <summary>
        /// 进入攻击时根据 payload 选择动画，并启用 root motion。
        /// </summary>
        public override void Enter(StateContext ctx, StateRequest request)
        {
            enteredAt = Time.time;
            cancelWindow = false;
            isLightCombo = IsLightAttack(request);
            comboWindowOpen = false;
            bufferedLightAttack = false;
            comboConsumed = false;
            comboIndex = 0;
            currentAnimation = ResolveAnimation(request);
            fallbackDuration = ResolveFallbackDuration(request);

            EnsureWeaponInHand(ctx);

            if (ctx.animator != null)
                ctx.animator.applyRootMotion = true;

            machine.CrossFade(currentAnimation, 0.1f);
        }

        /// <summary>
        /// 退出攻击时关闭取消窗口标记。
        /// </summary>
        public override void Exit(StateContext ctx)
        {
            cancelWindow = false;
            comboWindowOpen = false;
            bufferedLightAttack = false;
            comboConsumed = false;
        }

        public override bool TryHandleRequest(StateContext ctx, StateRequest request)
        {
            if (!isLightCombo
                || !comboWindowOpen
                || !IsLightAttack(request)
                || !CanChainToNextCombo())
            {
                return false;
            }

            bufferedLightAttack = true;
            return true;
        }

        public override void HandleAnimationEvent(StateContext ctx, CharacterAnimationEventType eventType)
        {
            switch (eventType)
            {
                case CharacterAnimationEventType.OpenComboWindow:
                    comboWindowOpen = true;
                    break;
                case CharacterAnimationEventType.CloseComboWindow:
                    comboWindowOpen = false;
                    break;
                case CharacterAnimationEventType.TryConsumeCombo:
                    TryConsumeCombo(ctx);
                    break;
                case CharacterAnimationEventType.AttackEnd:
                    RequestLocomotion(ctx);
                    break;
                case CharacterAnimationEventType.OpenCancelWindow:
                    cancelWindow = true;
                    break;
                case CharacterAnimationEventType.CloseCancelWindow:
                    cancelWindow = false;
                    break;
            }
        }

        /// <summary>
        /// 更新攻击生命周期。动画结束或兜底超时后请求回到 Locomotion。
        /// </summary>
        public override void Tick(StateContext ctx)
        {
            float elapsed = Time.time - enteredAt;
            if (elapsed >= MinCancelTime)
                cancelWindow = true;

            if (elapsed > fallbackDuration)
            {
                RequestLocomotion(ctx);
                return;
            }

            if (!isLightCombo && HasAnimationFinished(ctx))
                RequestLocomotion(ctx);
        }

        /// <summary>
        /// v1 攻击移动主要交给 root motion，暂不做额外物理控制。
        /// </summary>
        public override void FixedTick(StateContext ctx)
        {
        }

        /// <summary>
        /// 根据攻击请求 payload 决定本次攻击使用的动画。
        /// </summary>
        private string ResolveAnimation(StateRequest request)
        {
            if (request.payload is AttackRequestPayload payload)
            {
                if (payload.isAirAttack)
                    return machine.airAttackAnimation;

                if (payload.attackType == AttackType.Heavy)
                    return machine.heavyAttackAnimation;
            }

            return GetComboAnimation(0);
        }

        /// <summary>
        /// 判断当前攻击动画是否播放到接近结束。
        /// </summary>
        private bool HasAnimationFinished(StateContext ctx)
        {
            if (ctx.animator == null || string.IsNullOrEmpty(currentAnimation))
                return false;

            AnimatorStateInfo info = ctx.animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(currentAnimation) && info.normalizedTime >= NormalizedEndTime;
        }

        private static void EnsureWeaponInHand(StateContext ctx)
        {
            if (ctx.playerManager == null || ctx.playerManager.IsArmed)
                return;

            WeaponAnimaEventReceiver receiver = ctx.playerManager.GetComponentInChildren<WeaponAnimaEventReceiver>();
            if (receiver != null)
                receiver.AttachToHand();

            ctx.playerManager.SetArmed(true);
        }

        private void TryConsumeCombo(StateContext ctx)
        {
            if (comboConsumed)
                return;

            comboConsumed = true;

            if (bufferedLightAttack && CanChainToNextCombo())
                PlayNextCombo(ctx);
        }

        private void PlayNextCombo(StateContext ctx)
        {
            comboIndex++;
            enteredAt = Time.time;
            cancelWindow = false;
            comboWindowOpen = false;
            bufferedLightAttack = false;
            comboConsumed = false;
            currentAnimation = GetComboAnimation(comboIndex);
            fallbackDuration = machine.lightComboFallbackDuration;

            machine.CrossFade(currentAnimation, 0.08f);
        }

        private bool CanChainToNextCombo()
        {
            return comboIndex < GetComboCount() - 1;
        }

        private string GetComboAnimation(int index)
        {
            if (machine.lightComboAnimations != null
                && index >= 0
                && index < machine.lightComboAnimations.Length
                && !string.IsNullOrEmpty(machine.lightComboAnimations[index]))
            {
                return machine.lightComboAnimations[index];
            }

            return machine.lightAttackAnimation;
        }

        private int GetComboCount()
        {
            return machine.lightComboAnimations != null && machine.lightComboAnimations.Length > 0
                ? machine.lightComboAnimations.Length
                : 1;
        }

        private static bool IsLightAttack(StateRequest request)
        {
            if (request.type != StateRequestType.Attack)
                return false;

            if (request.payload is AttackRequestPayload payload)
                return payload.attackType == AttackType.Light && !payload.isAirAttack;

            return true;
        }

        private float ResolveFallbackDuration(StateRequest request)
        {
            if (request.payload is AttackRequestPayload payload)
            {
                if (payload.isAirAttack)
                    return machine.airAttackFallbackDuration;

                if (payload.attackType == AttackType.Heavy)
                    return machine.heavyAttackFallbackDuration;
            }

            return machine.lightComboFallbackDuration;
        }

        private static void RequestLocomotion(StateContext ctx)
        {
            ctx.SubmitRequest(StateRequest.Create(
                StateRequestType.AnimationEnd,
                CharacterStateType.Locomotion,
                StatePriorities.Locomotion,
                RequestSource.State,
                force: true));
        }

    }
}
