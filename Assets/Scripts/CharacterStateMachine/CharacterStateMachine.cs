using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CharacterStateMachine : MonoBehaviour
{
    #region debug
    [Header("µ±Ç°×´Ì¬")]
    public int currentStateIndex = 0;

    public void SetStateIndex(StateType t)
    {
        currentStateIndex = (int)t;
    }
    #endregion

    public PlayerManager player;
    public Animator animator;
    public Rigidbody rb;
    public EventCenter eventCenter;
    public InputHandler inputHandler;

    public IState currentState;
    public Dictionary<StateType, IState> CharacterStateDict;

    private void Awake()
    {
        player = GetComponent<PlayerManager>();
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        eventCenter = new EventCenter();
        inputHandler = GetComponent<InputHandler>();

        CharacterStateDict = new Dictionary<StateType, IState>
        {
            { StateType.Locomotion, new LocomotionState(this) },
            { StateType.Rolling, new RollingState(this) },
        };
    }

    private void Start()
    {
        SwitchState(CharacterStateDict[StateType.Locomotion]);
    }

    private void Update()
    {
        currentState?.Update();
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    public void SwitchState(IState state)
    {
        currentState?.Exit();
        currentState = state;
        currentState.Enter();
    }


}
