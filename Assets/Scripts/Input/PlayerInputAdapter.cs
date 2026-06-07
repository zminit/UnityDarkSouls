using System.Collections.Generic;
using CFSM;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInputAdapter : MonoBehaviour, IStateRequestSource
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private CharacterFSM characterFSM;

    [Header("Timing")]
    [SerializeField] private float dodgeTapTime = 0.5f;
    [SerializeField] private float sprintHoldTime = 0.5f;
    [SerializeField] private float moveDeadZone = 0.05f;

    private bool registered;
    private Vector2 bufferedDodgeInput;

    private void Awake()
    {
        if (inputReader == null)
            inputReader = GetComponent<InputReader>();

        if (characterFSM == null)
            characterFSM = GetComponent<CharacterFSM>();
    }

    private void OnEnable()
    {
        TryRegister();
    }

    private void OnDisable()
    {
        if (!registered || characterFSM == null)
            return;

        characterFSM.UnregisterRequestSource(this);
        registered = false;
    }

    private void Start()
    {
        TryRegister();
    }

    public void PollRequests(StateContext ctx, List<StateRequest> results)
    {
        if (!isActiveAndEnabled)
            return;

        if (inputReader == null || !inputReader.AcceptsGameplayInput)
        {
            ctx.SetMovement(Vector2.zero, Vector2.zero, MoveMode.Idle);
            bufferedDodgeInput = Vector2.zero;
            return;
        }

        PlayerInputSnapshot input = inputReader.Player;
        Vector2 rawMove = input.Move;
        bool hasMove = rawMove.sqrMagnitude > moveDeadZone * moveDeadZone;
        bool sprintHeld = input.Sprint.IsPressed
            && input.Sprint.HeldTime >= sprintHoldTime
            && hasMove;

        if (input.Sprint.WasPressedThisFrame)
            bufferedDodgeInput = hasMove ? rawMove : Vector2.zero;

        if (input.Sprint.IsPressed && hasMove)
            bufferedDodgeInput = rawMove;

        MoveMode moveMode = MoveMode.Idle;
        Vector2 moveInput = Vector2.zero;

        if (hasMove)
        {
            if (sprintHeld)
            {
                moveMode = MoveMode.Sprint;
                moveInput = rawMove;
            }
            else if (input.Alt.IsPressed)
            {
                moveMode = MoveMode.Walk;
                moveInput = rawMove.normalized;
            }
            else
            {
                moveMode = MoveMode.Run;
                moveInput = rawMove;
            }
        }

        ctx.SetMovement(moveInput, rawMove, moveMode);
        ctx.blackBoard.SetValue("GuardPressed", input.Guard.IsPressed);
        ctx.blackBoard.SetValue("MoveMode", moveMode);

        results.Add(StateRequest.Create(
            StateRequestType.Move,
            CharacterStateType.Locomotion,
            StatePriorities.Locomotion,
            RequestSource.Input));

        if (input.Jump.WasPressedThisFrame)
        {
            results.Add(StateRequest.Create(
                StateRequestType.Jump,
                CharacterStateType.Jump,
                StatePriorities.Jump,
                RequestSource.Input));
        }

        if (input.LightAttack.WasPressedThisFrame)
        {
            results.Add(StateRequest.Create(
                StateRequestType.Attack,
                CharacterStateType.Attack,
                StatePriorities.Attack,
                RequestSource.Input,
                new AttackRequestPayload(AttackType.Light, ctx.currentStateType == CharacterStateType.Jump)));
        }

        if (input.HeavyAttack.WasPressedThisFrame)
        {
            results.Add(StateRequest.Create(
                StateRequestType.Attack,
                CharacterStateType.Attack,
                StatePriorities.Attack,
                RequestSource.Input,
                new AttackRequestPayload(AttackType.Heavy, ctx.currentStateType == CharacterStateType.Jump)));
        }

        if (input.Guard.WasPressedThisFrame)
        {
            results.Add(StateRequest.Create(
                StateRequestType.Guard,
                CharacterStateType.Guard,
                StatePriorities.Guard,
                RequestSource.Input,
                new GuardRequestPayload(true)));
        }

        if (input.Sprint.WasReleasedThisFrame && input.Sprint.HeldTime <= dodgeTapTime)
        {
            Vector2 dodgeInput = hasMove ? rawMove : bufferedDodgeInput;

            bool isBackwardInputOnly = IsLocalBackwardDodgeInput(ctx, dodgeInput);
            if (isBackwardInputOnly)
            {
                bufferedDodgeInput = Vector2.zero;
                return;
            }

            if (dodgeInput.sqrMagnitude > moveDeadZone * moveDeadZone)
                dodgeInput.Normalize();
            else
                dodgeInput = Vector2.zero;

            results.Add(StateRequest.Create(
                StateRequestType.Dodge,
                CharacterStateType.Dodge,
                StatePriorities.Dodge,
                RequestSource.Input,
                new DodgeRequestPayload(dodgeInput)));
        }

        if (input.Sprint.WasReleasedThisFrame)
            bufferedDodgeInput = Vector2.zero;
    }

    private void TryRegister()
    {
        if (registered || characterFSM == null)
            return;

        characterFSM.RegisterRequestSource(this);
        registered = true;
    }

    private bool IsLocalBackwardDodgeInput(StateContext ctx, Vector2 input)
    {
        if (input.sqrMagnitude <= moveDeadZone * moveDeadZone)
            return false;

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 worldDirection = GetCameraRelativeWorldDirection(ctx, input);
        if (worldDirection.sqrMagnitude <= moveDeadZone * moveDeadZone)
            return false;

        if (ctx.playerTransform == null)
            return input.y < -moveDeadZone && Mathf.Abs(input.x) <= moveDeadZone;

        Vector3 localDirection = ctx.playerTransform.InverseTransformDirection(worldDirection.normalized);
        return localDirection.z < -moveDeadZone && Mathf.Abs(localDirection.x) <= moveDeadZone;
    }

    private static Vector3 GetCameraRelativeWorldDirection(StateContext ctx, Vector2 input)
    {
        Transform basis = ctx.mainCamera != null ? ctx.mainCamera.transform : ctx.playerTransform;
        Vector3 forward = basis != null ? basis.forward : Vector3.forward;
        Vector3 right = basis != null ? basis.right : Vector3.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = forward * input.y + right * input.x;
        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }
}
