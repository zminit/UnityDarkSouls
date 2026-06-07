using System;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public float vertical;
    public float horizontal;
    public Vector2 playerMovement;
    public float Movement;

    public bool B_Input;
    public bool Alt_Input;
    public bool A_Input;
    public float A_Input_Time = 0.0f;
    public float B_Input_Time = 0.0f;
    public Action Alt_Started;
    public Action Alt_Canceled;
    public Action B_Input_Started;
    public Action B_Input_Canceled;
    public Action A_Input_Started;
    public Action A_Input_Canceled;

    [SerializeField] private InputReader inputReader;

    private int lastEventFrame = -1;

    private void Awake()
    {
        EnsureInputReader();
    }

    private void OnEnable()
    {
        EnsureInputReader();
    }

    private void OnDisable()
    {
        ResetSnapshot();
    }

    public void TickUp(float delta)
    {
        EnsureInputReader();

        if (inputReader == null || !inputReader.AcceptsGameplayInput)
        {
            ResetSnapshot();
            return;
        }

        PlayerInputSnapshot input = inputReader.Player;

        DispatchFrameEvents(input);
        SyncButtonValues(input);

        playerMovement = input.Move;
        vertical = playerMovement.y;
        horizontal = playerMovement.x;
        HandleMove();
    }

    private void EnsureInputReader()
    {
        if (inputReader != null)
            return;

        inputReader = GetComponent<InputReader>();
        if (inputReader == null)
            inputReader = gameObject.AddComponent<InputReader>();
    }

    private void DispatchFrameEvents(PlayerInputSnapshot input)
    {
        if (lastEventFrame == Time.frameCount)
            return;

        lastEventFrame = Time.frameCount;

        if (input.Alt.WasPressedThisFrame)
        {
            Alt_Input = true;
            Alt_Started?.Invoke();
        }

        if (input.Alt.WasReleasedThisFrame)
        {
            Alt_Input = false;
            Alt_Canceled?.Invoke();
        }

        if (input.Sprint.WasPressedThisFrame)
        {
            B_Input = true;
            B_Input_Time = input.Sprint.HeldTime;
            B_Input_Started?.Invoke();
        }

        if (input.Sprint.WasReleasedThisFrame)
        {
            B_Input = false;
            B_Input_Time = input.Sprint.HeldTime;
            B_Input_Canceled?.Invoke();
        }

        if (input.Jump.WasPressedThisFrame)
        {
            A_Input = true;
            A_Input_Time = input.Jump.HeldTime;
            A_Input_Started?.Invoke();
        }

        if (input.Jump.WasReleasedThisFrame)
        {
            A_Input = false;
            A_Input_Time = input.Jump.HeldTime;
            A_Input_Canceled?.Invoke();
        }
    }

    private void SyncButtonValues(PlayerInputSnapshot input)
    {
        Alt_Input = input.Alt.IsPressed;
        B_Input = input.Sprint.IsPressed;
        A_Input = input.Jump.IsPressed;

        B_Input_Time = input.Sprint.IsPressed ? input.Sprint.HeldTime : 0f;
        A_Input_Time = input.Jump.IsPressed ? input.Jump.HeldTime : 0f;
    }

    private void ResetSnapshot()
    {
        vertical = 0f;
        horizontal = 0f;
        playerMovement = Vector2.zero;
        Movement = 0f;
        B_Input = false;
        Alt_Input = false;
        A_Input = false;
        A_Input_Time = 0f;
        B_Input_Time = 0f;
    }

    private void HandleMove()
    {
        if (Alt_Input)
        {
            vertical = Mathf.Clamp(vertical, -0.5f, 0.5f);
            horizontal = Mathf.Clamp(horizontal, -0.5f, 0.5f);
        }
        Movement = Mathf.Max(Mathf.Abs(vertical) , Mathf.Abs(horizontal));
    }
}
