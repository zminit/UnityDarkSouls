using System.Collections.Generic;
using UnityEngine;

namespace CFSM
{
    /// <summary>
    /// 角色请求驱动状态机。负责收集抽象 StateRequest、按优先级调度并驱动当前状态生命周期。
    /// </summary>
    public class CharacterFSM : MonoBehaviour
    {
        [Header("Debug")]
        /// <summary>当前主状态类型，显示在 Inspector 中便于运行时调试。</summary>
        [SerializeField] private CharacterStateType currentStateType = CharacterStateType.Locomotion;

        /// <summary>当前主状态名称，显示在 Inspector 中便于快速查看。</summary>
        [SerializeField] private string currentStateName;

        /// <summary>临时诊断开关：开启后只把 Locomotion 的移动更新放到 Update 中执行，用于排查 FixedUpdate 物理步进导致的离散移动。</summary>
        [SerializeField] private bool moveLocomotionInUpdateTest;

        /// <summary>临时诊断开关：开启后 Locomotion 在 Update 中直接改 Transform 位置，完全绕开 Rigidbody 速度积分。</summary>
        [SerializeField] private bool useTransformMoveForLocomotionTest;

        [Header("Input Buffer")]
        /// <summary>输入缓冲保留时间。短时间内无法执行的高优先级请求会在该时间内重试。</summary>
        [SerializeField] private float bufferDuration = 0.2f;

        [Header("Animation Names")]
        /// <summary>Locomotion 状态播放的 Animator 状态名。</summary>
        public string locomotionAnimation = "CommonLocomotion";

        /// <summary>疾跑状态播放的 Animator 状态名。Sprint 是 Locomotion 的子状态，不单独作为主状态。</summary>
        public string sprintAnimation = "Sprinting";

        /// <summary>跳跃起跳动画状态名。</summary>
        public string jumpStartAnimation = "JumpStart";

        /// <summary>跳跃滞空动画状态名。</summary>
        public string jumpLoopAnimation = "Jumping";

        /// <summary>落地动画状态名。</summary>
        public string landingAnimation = "Landing";

        /// <summary>轻攻击动画状态名。</summary>
        public string lightAttackAnimation = "Attack_Light";

        public string[] lightComboAnimations =
        {
            "Combo_01",
            "Combo_02",
            "Combo_03",
            "Combo_04",
            "Combo_05"
        };

        /// <summary>重攻击动画状态名。</summary>
        public string heavyAttackAnimation = "Attack_Heavy";

        /// <summary>空中攻击动画状态名。</summary>
        public string airAttackAnimation = "Attack_Air";

        /// <summary>防御动画状态名。</summary>
        public string guardAnimation = "Guard";

        /// <summary>闪避/翻滚动画状态名。</summary>
        public string dodgeAnimation = "Dodge";

        /// <summary>受击动画状态名。</summary>
        public string hitAnimation = "Hit";

        /// <summary>击飞/滞空受击动画状态名。</summary>
        public string airborneAnimation = "Jumping";

        [Header("Weapon Action Animation Names")]
        public string drawWeaponAnimation = "DrawSword";
        public string sheatheWeaponAnimation = "SheatheSword";

        [Header("Action Timing")]
        public float lightComboFallbackDuration = 3.0f;
        public float heavyAttackFallbackDuration = 3.0f;
        public float airAttackFallbackDuration = 2.5f;
        public float drawWeaponFallbackDuration = 3.0f;
        public float sheatheWeaponFallbackDuration = 3.0f;

        [Header("Dodge Settings")]
        /// <summary>闪避物理位移速度。当前 DodgeState 默认使用刚体位移，不依赖 root motion。</summary>
        public float dodgeSpeed = 7.5f;

        /// <summary>闪避实际位移持续时间。</summary>
        public float dodgeDuration = 0.35f;

        /// <summary>主状态注册表。状态类型到状态实例的一对一映射。</summary>
        private readonly Dictionary<CharacterStateType, CharacterStateBase> states =
            new Dictionary<CharacterStateType, CharacterStateBase>();

        /// <summary>外部请求源列表，例如输入适配器、AI 或未来调试面板。</summary>
        private readonly List<IStateRequestSource> requestSources = new List<IStateRequestSource>();

        /// <summary>当前帧待处理请求。状态内部和外部请求源都会向该列表追加请求。</summary>
        private readonly List<StateRequest> pendingRequests = new List<StateRequest>(8);

        /// <summary>状态共享上下文，集中持有角色组件和当前输入快照。</summary>
        private StateContext ctx;

        /// <summary>当前正在运行的状态实例。</summary>
        private CharacterStateBase currentState;

        /// <summary>是否存在等待重试的缓冲请求。</summary>
        private bool hasBufferedRequest;

        /// <summary>当前缓冲请求。v1 只保留一个最高优先级请求槽。</summary>
        private StateRequest bufferedRequest;

        /// <summary>当前主状态类型，对外只读。</summary>
        public CharacterStateType CurrentStateType => currentStateType;

        public MoveMode CurrentMoveMode => ctx != null ? ctx.moveMode : MoveMode.Idle;

        /// <summary>
        /// 初始化状态上下文、注册状态和默认请求源。
        /// </summary>
        private void Awake()
        {
            NormalizeSerializedAnimationNames();

            ctx = new StateContext
            {
                playerManager = GetComponent<PlayerManager>(),
                playerTransform = transform,
                playerBody = GetComponent<Rigidbody>(),
                mainCamera = Camera.main,
                animator = GetComponentInChildren<Animator>(),
                blackBoard = new BlackBoard(),
                SubmitRequest = SubmitRequest
            };

            RegisterStates();
            RegisterRequestSources();
        }

        /// <summary>
        /// 启动时强制进入 Locomotion，确保状态机有稳定默认状态。
        /// </summary>
        private void Start()
        {
            SwitchState(CharacterStateType.Locomotion, StateRequest.Create(
                StateRequestType.Move,
                CharacterStateType.Locomotion,
                StatePriorities.Locomotion,
                RequestSource.State,
                force: true));
        }

        /// <summary>
        /// 每帧收集请求、处理状态切换、运行当前状态逻辑，并尝试消费缓冲请求。
        /// </summary>
        private void Update()
        {
            if (currentState == null)
                return;

            ctx.currentStateType = currentStateType;
            CollectExternalRequests();
            ProcessPendingRequests();

            currentState.Tick(ctx);

            if (ShouldRunLocomotionTransformMoveInUpdate())
                RunLocomotionTransformMoveTest();
            else if (ShouldRunLocomotionFixedTickInUpdate())
                currentState.FixedTick(ctx);

            ProcessPendingRequests();
            ProcessBufferedRequest();

            currentStateName = currentStateType.ToString();
        }

        /// <summary>
        /// 将物理更新委托给当前状态。
        /// </summary>
        private void FixedUpdate()
        {
            if (ShouldSkipLocomotionFixedTick())
                return;

            currentState?.FixedTick(ctx);
        }

        private bool ShouldRunLocomotionFixedTickInUpdate()
        {
            return moveLocomotionInUpdateTest
                && !useTransformMoveForLocomotionTest
                && currentState != null
                && currentStateType == CharacterStateType.Locomotion;
        }

        private bool ShouldSkipLocomotionFixedTick()
        {
            return (moveLocomotionInUpdateTest || useTransformMoveForLocomotionTest)
                && currentStateType == CharacterStateType.Locomotion;
        }

        private bool ShouldRunLocomotionTransformMoveInUpdate()
        {
            return useTransformMoveForLocomotionTest
                && currentStateType == CharacterStateType.Locomotion;
        }

        private void RunLocomotionTransformMoveTest()
        {
            if (ctx == null || ctx.playerTransform == null || ctx.playerManager == null)
                return;

            if (ctx.movementAmount <= 0.05f)
            {
                StopHorizontalVelocityForTransformMoveTest();
                return;
            }

            Vector3 moveDir = GetCameraRelativeMoveDirectionForTransformMoveTest();
            if (moveDir.sqrMagnitude <= 0.0001f)
                return;

            if (ctx.blackBoard == null
                || !ctx.blackBoard.TryGetValue("UseStrafeMove", out bool useStrafeMove)
                || !useStrafeMove)
            {
                if (ctx.playerManager.canRotate)
                    LookRotateForTransformMoveTest(moveDir, Vector3.up);
            }

            float speed = GetMoveSpeedForTransformMoveTest();
            ctx.playerTransform.position += moveDir.normalized * speed * Time.deltaTime;
            StopHorizontalVelocityForTransformMoveTest();
        }

        private Vector3 GetCameraRelativeMoveDirectionForTransformMoveTest()
        {
            Vector3 forward = ctx.mainCamera != null ? ctx.mainCamera.transform.forward : ctx.playerTransform.forward;
            Vector3 right = ctx.mainCamera != null ? ctx.mainCamera.transform.right : ctx.playerTransform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDir = forward * ctx.moveInput.y + right * ctx.moveInput.x;
            return moveDir.sqrMagnitude > 1f ? moveDir.normalized : moveDir;
        }

        private float GetMoveSpeedForTransformMoveTest()
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

        private void LookRotateForTransformMoveTest(Vector3 lookDir, Vector3 normal)
        {
            if (ctx.playerTransform == null || lookDir.sqrMagnitude <= 0.1f)
                return;

            lookDir.Normalize();
            lookDir -= normal * Vector3.Dot(normal, lookDir);
            Quaternion targetRotation = Quaternion.LookRotation(lookDir, normal);
            ctx.playerTransform.rotation = Quaternion.Slerp(
                ctx.playerTransform.rotation,
                targetRotation,
                Time.deltaTime * 5f);
        }

        private void StopHorizontalVelocityForTransformMoveTest()
        {
            if (ctx.playerBody == null)
                return;

            Vector3 velocity = ctx.playerBody.velocity;
            ctx.playerBody.velocity = new Vector3(0f, velocity.y, 0f);
        }

        /// <summary>
        /// 提交一个完整状态请求。外部系统和状态内部都可通过该接口请求切换。
        /// </summary>
        public void SubmitRequest(StateRequest request)
        {
            pendingRequests.Add(request);
        }

        /// <summary>
        /// 便捷请求接口，用于不需要手动构建 StateRequest 的调用场景。
        /// </summary>
        public void RequestState(
            StateRequestType requestType,
            CharacterStateType targetState,
            int priority,
            RequestSource source = RequestSource.Debug,
            object payload = null,
            bool force = false)
        {
            SubmitRequest(StateRequest.Create(requestType, targetState, priority, source, payload, force));
        }

        public void NotifyAnimationEvent(CharacterAnimationEventType eventType)
        {
            if (currentState == null || ctx == null)
                return;

            currentState.HandleAnimationEvent(ctx, eventType);
        }

        /// <summary>
        /// 注册一个请求源。重复注册同一实例会被忽略。
        /// </summary>
        public void RegisterRequestSource(IStateRequestSource source)
        {
            if (source != null && !requestSources.Contains(source))
                requestSources.Add(source);
        }

        /// <summary>
        /// 注销一个请求源，用于输入适配器或 AI 组件 Disable 时停止继续提交请求。
        /// </summary>
        public void UnregisterRequestSource(IStateRequestSource source)
        {
            if (source != null)
                requestSources.Remove(source);
        }

        /// <summary>
        /// 安全播放 Animator CrossFade。空 Animator 或空动画名会被忽略。
        /// </summary>
        public void CrossFade(string animationName, float transitionDuration = 0.1f, int layer = 0)
        {
            if (ctx.animator != null && !string.IsNullOrEmpty(animationName))
                ctx.animator.CrossFade(animationName, transitionDuration, layer);
        }

        /// <summary>
        /// 兼容场景中已经序列化的旧动画名。代码默认值变化不会自动覆盖已有组件实例。
        /// </summary>
        private void NormalizeSerializedAnimationNames()
        {
            if (string.IsNullOrEmpty(dodgeAnimation) || dodgeAnimation == "Rolling")
                dodgeAnimation = "Dodge";

            if (string.IsNullOrEmpty(sprintAnimation))
                sprintAnimation = "Sprinting";

            if (string.IsNullOrEmpty(locomotionAnimation))
                locomotionAnimation = "CommonLocomotion";

            if (string.IsNullOrEmpty(airborneAnimation) || airborneAnimation == "Airborne")
                airborneAnimation = "Jumping";
        }

        /// <summary>
        /// 创建并注册所有主状态实例。新增主状态时应在这里加入状态表。
        /// </summary>
        private void RegisterStates()
        {
            states[CharacterStateType.Locomotion] = new LocomotionState(this);
            states[CharacterStateType.Jump] = new JumpState(this);
            states[CharacterStateType.Attack] = new AttackState(this);
            states[CharacterStateType.DrawWeapon] = new DrawWeaponState(this);
            states[CharacterStateType.SheatheWeapon] = new SheatheWeaponState(this);
            states[CharacterStateType.Guard] = new GuardState(this);
            states[CharacterStateType.Dodge] = new DodgeState(this);
            states[CharacterStateType.Hit] = new HitState(this);
            states[CharacterStateType.Airborne] = new AirborneState(this);
        }

        /// <summary>
        /// 注册当前版本的默认输入适配器。之后重构输入系统时替换这里即可。
        /// </summary>
        private void RegisterRequestSources()
        {
            RegisterRequestSource(new GroundCheckRequestSource());

            InputReader inputReader = GetComponent<InputReader>();
            if (inputReader == null)
                inputReader = gameObject.AddComponent<InputReader>();

            PlayerInputAdapter playerInputAdapter = GetComponent<PlayerInputAdapter>();
            if (playerInputAdapter == null)
                playerInputAdapter = gameObject.AddComponent<PlayerInputAdapter>();

            if (playerInputAdapter.enabled)
                RegisterRequestSource(playerInputAdapter);
        }

        /// <summary>
        /// 从所有请求源收集本帧请求。已有的帧间请求不会在这里被清空。
        /// </summary>
        private void CollectExternalRequests()
        {
            for (int i = 0; i < requestSources.Count; i++)
                requestSources[i].PollRequests(ctx, pendingRequests);
        }

        /// <summary>
        /// 按优先级处理待执行请求。只要有一个请求成功切换状态，本轮处理立即结束。
        /// </summary>
        private void ProcessPendingRequests()
        {
            if (pendingRequests.Count == 0)
                return;

            pendingRequests.Sort((a, b) => b.priority.CompareTo(a.priority));

            for (int i = 0; i < pendingRequests.Count; i++)
            {
                StateRequest request = pendingRequests[i];
                if (TryApplyRequest(request))
                {
                    pendingRequests.Clear();
                    return;
                }

                BufferRequestIfUseful(request);
            }

            pendingRequests.Clear();
        }

        /// <summary>
        /// 尝试执行单个请求。会依次检查目标状态存在、当前状态中断规则和目标状态进入条件。
        /// </summary>
        private bool TryApplyRequest(StateRequest request)
        {
            if (!states.TryGetValue(request.targetState, out CharacterStateBase nextState))
                return false;

            if (currentState != null && request.targetState == currentStateType)
                return currentState.TryHandleRequest(ctx, request);

            if (currentState != null && !request.force && !currentState.CanInterruptBy(ctx, request))
                return false;

            if (!nextState.CanEnter(ctx, request))
                return false;

            SwitchState(request.targetState, request);
            return true;
        }

        /// <summary>
        /// 执行实际状态切换，按 Exit -> 更新 current -> Enter 的顺序调用生命周期。
        /// </summary>
        private void SwitchState(CharacterStateType nextType, StateRequest request)
        {
            currentState?.Exit(ctx);
            currentStateType = nextType;
            currentState = states[nextType];
            ctx.currentStateType = nextType;
            currentState.Enter(ctx, request);
        }

        /// <summary>
        /// 将暂时无法执行的高优先级请求写入缓冲槽。v1 只缓冲 Jump 及以上优先级请求。
        /// </summary>
        private void BufferRequestIfUseful(StateRequest request)
        {
            if (request.priority < StatePriorities.Jump)
                return;

            if (!hasBufferedRequest || request.priority >= bufferedRequest.priority)
            {
                bufferedRequest = request;
                bufferedRequest.time = Time.time;
                hasBufferedRequest = true;
            }
        }

        /// <summary>
        /// 尝试消费缓冲请求。超过 bufferDuration 后自动丢弃。
        /// </summary>
        private void ProcessBufferedRequest()
        {
            if (!hasBufferedRequest)
                return;

            if (Time.time - bufferedRequest.time > bufferDuration)
            {
                hasBufferedRequest = false;
                return;
            }

            if (TryApplyRequest(bufferedRequest))
                hasBufferedRequest = false;
        }
    }

    /// <summary>
    /// 每帧检测地面状态。离地时提交 Airborne request，落地回 Locomotion 交给 Airborne/Jump 状态处理。
    /// </summary>
    public class GroundCheckRequestSource : IStateRequestSource
    {
        /// <summary>上一帧是否在地面，用于只在 grounded -> airborne 边沿提交请求。</summary>
        private bool hasSubmittedAirborneRequest;

        /// <summary>开始连续离地的时间。小于 0 表示当前没有离地计时。</summary>
        private float ungroundedSince = -1f;

        /// <summary>离地需要持续多久才进入 Airborne，避免脚底射线一帧抖动导致状态反复切换。</summary>
        private const float AirborneEnterDelay = 0.08f;

        /// <summary>
        /// 读取 PlayerManager.OnLandHandler，并在离地边沿提交 Airborne 请求。
        /// </summary>
        public void PollRequests(StateContext ctx, List<StateRequest> results)
        {
            if (ctx.playerManager == null || ctx.playerManager.OnLandHandler == null)
                return;

            bool isGrounded = ctx.playerManager.OnLandHandler.OnLandCheck();

            if (isGrounded)
            {
                ungroundedSince = -1f;
                hasSubmittedAirborneRequest = false;
                return;
            }

            if (ungroundedSince < 0f)
                ungroundedSince = Time.time;

            if (!hasSubmittedAirborneRequest
                && Time.time - ungroundedSince >= AirborneEnterDelay
                && ShouldEnterAirborne(ctx))
            {
                results.Add(StateRequest.Create(
                    StateRequestType.Airborne,
                    CharacterStateType.Airborne,
                    StatePriorities.Airborne,
                    RequestSource.State,
                    force: true));

                hasSubmittedAirborneRequest = true;
            }
        }

        /// <summary>
        /// Jump/Airborne/Hit 自己管理空中流程，不由普通离地检测重复覆盖。
        /// </summary>
        private static bool ShouldEnterAirborne(StateContext ctx)
        {
            return ctx.currentStateType != CharacterStateType.Jump
                && ctx.currentStateType != CharacterStateType.Airborne
                && ctx.currentStateType != CharacterStateType.Hit
                && ctx.currentStateType != CharacterStateType.Dodge;
        }
    }
}
