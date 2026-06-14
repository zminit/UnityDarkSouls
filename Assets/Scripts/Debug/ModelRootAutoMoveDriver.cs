using System.Collections.Generic;
using CFSM;
using UnityEngine;

/// <summary>
/// Deterministic diagnostic movement source. It bypasses real player input during jitter captures.
/// </summary>
[DisallowMultipleComponent]
public class ModelRootAutoMoveDriver : MonoBehaviour, IStateRequestSource
{
    [SerializeField]
    CharacterFSM characterFSM;
    [SerializeField]
    MoveMode moveMode = MoveMode.Run;
    [SerializeField]
    Vector2 moveInput = Vector2.up;

    bool registered;

    private void Awake()
    {
        if (characterFSM == null)
            characterFSM = GetComponent<CharacterFSM>();
    }

    private void OnEnable()
    {
        TryRegister();
    }

    private void Start()
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

    public void Configure(CharacterFSM fsm, MoveMode mode, Vector2 input)
    {
        characterFSM = fsm;
        moveMode = mode;
        moveInput = input.sqrMagnitude > 1f ? input.normalized : input;
        TryRegister();
    }

    public void PollRequests(StateContext ctx, List<StateRequest> results)
    {
        if (!isActiveAndEnabled)
            return;

        ctx.SetMovement(moveInput, moveInput, moveMode);
        ctx.blackBoard.SetValue("GuardPressed", false);
        ctx.blackBoard.SetValue("MoveMode", moveMode);

        results.Add(StateRequest.Create(
            StateRequestType.Move,
            CharacterStateType.Locomotion,
            StatePriorities.Locomotion,
            RequestSource.Debug));
    }

    private void TryRegister()
    {
        if (registered || characterFSM == null)
            return;

        characterFSM.RegisterRequestSource(this);
        registered = true;
    }
}
