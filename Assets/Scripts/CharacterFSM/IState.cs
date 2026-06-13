using System;
using System.Collections.Generic;
using UnityEngine;

namespace CFSM
{
    /// <summary>
    /// 角色状态机的主状态类型。主状态只表达会影响中断、优先级和行为控制权的核心状态。
    /// </summary>
    public enum CharacterStateType
    {
        Locomotion,
        Jump,
        Attack,
        DrawWeapon,
        SheatheWeapon,
        Guard,
        Dodge,
        Hit,
        Airborne
    }

    /// <summary>
    /// 状态机接收的抽象请求类型。输入、动画事件、战斗系统都应转换为请求后再提交给状态机。
    /// </summary>
    public enum StateRequestType
    {
        Move,
        Jump,
        Attack,
        DrawWeapon,
        SheatheWeapon,
        Guard,
        Dodge,
        Hit,
        Land,
        Airborne,
        AnimationEnd
    }

    /// <summary>
    /// 攻击入口类型，后续可扩展连段段数、蓄力、武器招式等信息。
    /// </summary>
    public enum AttackType
    {
        Light,
        Heavy
    }

    /// <summary>
    /// 请求来源，用于调试和区分请求由输入、状态内部、动画事件或战斗系统产生。
    /// </summary>
    public enum RequestSource
    {
        Input,
        State,
        Animation,
        Combat,
        Debug
    }

    /// <summary>
    /// 当前移动语义。输入适配器负责从 Alt、Shift 和 Move 组合中计算该值。
    /// </summary>
    public enum MoveMode
    {
        Idle,
        Walk,
        Run,
        Sprint
    }

    public enum CharacterAnimationEventType
    {
        OpenComboWindow,
        CloseComboWindow,
        TryConsumeCombo,
        AttackEnd,
        OpenCancelWindow,
        CloseCancelWindow
    }

    /// <summary>
    /// 默认状态优先级。数值越高，同一帧内越先尝试处理。
    /// </summary>
    public static class StatePriorities
    {
        public const int Locomotion = 10;
        public const int Jump = 50;
        public const int Attack = 60;
        public const int WeaponAction = 20;
        public const int Guard = 70;
        public const int Dodge = 80;
        public const int Airborne = 90;
        public const int Hit = 100;
    }

    /// <summary>
    /// 状态机黑板，用于在状态、输入适配器和未来战斗系统之间共享少量临时数据。
    /// </summary>
    public class BlackBoard
    {
        private readonly Dictionary<string, object> board = new Dictionary<string, object>();

        /// <summary>
        /// 写入或覆盖黑板值。
        /// </summary>
        public void SetValue(string key, object value)
        {
            board[key] = value;
        }

        /// <summary>
        /// 按类型读取黑板值。类型不匹配时返回 false，避免外部直接做不安全转换。
        /// </summary>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (board.TryGetValue(key, out object raw) && raw is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <summary>
    /// 状态切换请求。状态机只消费 StateRequest，不直接依赖具体输入系统或按键。
    /// </summary>
    public struct StateRequest
    {
        /// <summary>请求语义，例如 Jump、Attack、Hit。</summary>
        public StateRequestType type;

        /// <summary>请求希望切换到的目标主状态。</summary>
        public CharacterStateType targetState;

        /// <summary>请求优先级。高优先级请求会在同一帧中先被处理。</summary>
        public int priority;

        /// <summary>请求来源，用于调试和未来过滤规则。</summary>
        public RequestSource source;

        /// <summary>请求附加数据，例如 AttackRequestPayload 或 GuardRequestPayload。</summary>
        public object payload;

        /// <summary>是否强制尝试切换。force 会跳过普通中断限制，但仍会检查目标状态 CanEnter。</summary>
        public bool force;

        /// <summary>请求创建时间，用于输入缓冲过期判断。</summary>
        public float time;

        /// <summary>
        /// 创建一个带时间戳的状态请求。
        /// </summary>
        public static StateRequest Create(
            StateRequestType type,
            CharacterStateType targetState,
            int priority,
            RequestSource source,
            object payload = null,
            bool force = false)
        {
            return new StateRequest
            {
                type = type,
                targetState = targetState,
                priority = priority,
                source = source,
                payload = payload,
                force = force,
                time = Time.time
            };
        }
    }

    /// <summary>
    /// 攻击请求附加数据。v1 用于区分轻攻击、重攻击和空中攻击。
    /// </summary>
    public struct AttackRequestPayload
    {
        /// <summary>轻攻击或重攻击。</summary>
        public AttackType attackType;

        /// <summary>是否按空中攻击入口处理。</summary>
        public bool isAirAttack;

        public AttackRequestPayload(AttackType attackType, bool isAirAttack = false)
        {
            this.attackType = attackType;
            this.isAirAttack = isAirAttack;
        }
    }

    /// <summary>
    /// 防御请求附加数据。当前用于表达按住/松开，未来可扩展完美防御窗口等信息。
    /// </summary>
    public struct GuardRequestPayload
    {
        /// <summary>防御键是否处于按住状态。</summary>
        public bool isPressed;

        public GuardRequestPayload(bool isPressed)
        {
            this.isPressed = isPressed;
        }
    }

    /// <summary>
    /// 闪避请求附加数据。inputDirection 是输入平面方向，空方向时可退回角色默认方向。
    /// </summary>
    public struct DodgeRequestPayload
    {
        public Vector2 inputDirection;
        public bool useBackwardWhenNoInput;

        public DodgeRequestPayload(Vector2 inputDirection, bool useBackwardWhenNoInput = true)
        {
            this.inputDirection = inputDirection;
            this.useBackwardWhenNoInput = useBackwardWhenNoInput;
        }
    }

    /// <summary>
    /// 状态请求来源接口。输入系统、AI、调试面板都可以实现该接口向状态机提交请求。
    /// </summary>
    public interface IStateRequestSource
    {
        /// <summary>
        /// 每帧由状态机调用，将本帧产生的请求追加到 results 中。
        /// </summary>
        void PollRequests(StateContext ctx, List<StateRequest> results);
    }

    /// <summary>
    /// 状态运行上下文。集中持有角色组件、输入快照、当前状态和状态内部提交请求的入口。
    /// </summary>
    public class StateContext
    {
        /// <summary>玩家移动与属性控制组件。</summary>
        public PlayerManager playerManager;

        /// <summary>玩家 Transform。</summary>
        public Transform playerTransform;

        /// <summary>玩家 Rigidbody。</summary>
        public Rigidbody playerBody;

        /// <summary>主相机，用于相机相对移动。</summary>
        public Camera mainCamera;

        /// <summary>角色 Animator。</summary>
        public Animator animator;

        /// <summary>轻量共享数据黑板。</summary>
        public BlackBoard blackBoard;

        /// <summary>当前帧移动输入。</summary>
        public Vector2 moveInput;

        /// <summary>未按移动模式修正前的原始移动输入。</summary>
        public Vector2 rawMoveInput;

        /// <summary>移动输入强度，取横纵轴绝对值较大者。</summary>
        public float movementAmount;

        /// <summary>疾跑键是否按住。</summary>
        public bool sprintHeld;

        /// <summary>当前移动模式，由输入适配器解释 Alt、Shift 和移动输入后写入。</summary>
        public MoveMode moveMode;

        /// <summary>当前主状态类型。</summary>
        public CharacterStateType currentStateType;

        /// <summary>状态内部向状态机提交请求的委托。</summary>
        public Action<StateRequest> SubmitRequest;

        /// <summary>
        /// 更新移动输入快照。输入适配器应在 PollRequests 中调用。
        /// </summary>
        public void SetMovement(Vector2 input, bool sprintHeld)
        {
            MoveMode mode = sprintHeld
                ? MoveMode.Sprint
                : (input.sqrMagnitude > 0.0025f ? MoveMode.Run : MoveMode.Idle);

            SetMovement(input, input, mode);
        }

        /// <summary>
        /// 更新移动输入快照和移动模式。moveInput 可按 Walk/Run 语义修正，rawInput 保留原始 WASD 值。
        /// </summary>
        public void SetMovement(Vector2 input, Vector2 rawInput, MoveMode mode)
        {
            moveInput = input;
            rawMoveInput = rawInput;
            movementAmount = Mathf.Max(Mathf.Abs(input.x), Mathf.Abs(input.y));
            moveMode = input.sqrMagnitude > 0.0025f ? mode : MoveMode.Idle;
            sprintHeld = moveMode == MoveMode.Sprint;
        }
    }

    /// <summary>
    /// 角色状态基类。所有主状态都通过该接口处理进入、退出、帧更新、物理更新和中断规则。
    /// </summary>
    public abstract class CharacterStateBase
    {
        /// <summary>当前状态对应的主状态类型。</summary>
        public abstract CharacterStateType StateType { get; }

        /// <summary>当前状态自身优先级，用于默认中断规则。</summary>
        public virtual int Priority => StatePriorities.Locomotion;

        /// <summary>状态是否允许被普通请求中断。force 请求不受该值限制。</summary>
        public virtual bool IsInterruptible => true;

        /// <summary>
        /// 目标状态进入条件。返回 false 时，即使请求优先级足够也不会进入该状态。
        /// </summary>
        public virtual bool CanEnter(StateContext ctx, StateRequest request)
        {
            return true;
        }

        /// <summary>
        /// 当前状态是否允许被指定请求中断。具体状态可覆盖该函数实现特殊规则。
        /// </summary>
        public virtual bool CanInterruptBy(StateContext ctx, StateRequest request)
        {
            return request.force || (IsInterruptible && request.priority >= Priority);
        }

        /// <summary>
        /// 当前状态接收目标仍是自己的请求。用于攻击连段、蓄力、格挡持续等不需要重新 Enter 的内部输入。
        /// </summary>
        public virtual bool TryHandleRequest(StateContext ctx, StateRequest request)
        {
            return false;
        }

        public virtual void HandleAnimationEvent(StateContext ctx, CharacterAnimationEventType eventType)
        {
        }

        /// <summary>进入状态时调用。</summary>
        public abstract void Enter(StateContext ctx, StateRequest request);

        /// <summary>退出状态时调用。</summary>
        public abstract void Exit(StateContext ctx);

        /// <summary>每帧 Update 调用，用于动画参数、状态内部请求和普通逻辑。</summary>
        public abstract void Tick(StateContext ctx);

        /// <summary>每帧 FixedUpdate 调用，用于刚体移动等物理逻辑。</summary>
        public abstract void FixedTick(StateContext ctx);
    }
}
