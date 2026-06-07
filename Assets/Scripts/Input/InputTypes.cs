using System;
using UnityEngine;

public enum InputMode
{
    Gameplay,
    UIOnly,
    GameplayAndUI
}

[Serializable]
public class InputButtonState
{
    [field: SerializeField] public bool IsPressed { get; private set; }
    [field: SerializeField] public bool WasPressedThisFrame { get; private set; }
    [field: SerializeField] public bool WasReleasedThisFrame { get; private set; }
    [field: SerializeField] public float HeldTime { get; private set; }

    public void SetPressed(bool pressed)
    {
        if (pressed == IsPressed)
            return;

        IsPressed = pressed;

        if (pressed)
        {
            WasPressedThisFrame = true;
            HeldTime = 0f;
        }
        else
        {
            WasReleasedThisFrame = true;
        }
    }

    public void Tick(float deltaTime)
    {
        if (IsPressed)
            HeldTime += deltaTime;
    }

    public void ClearFrameState()
    {
        WasPressedThisFrame = false;
        WasReleasedThisFrame = false;
    }

    public void Reset()
    {
        IsPressed = false;
        WasPressedThisFrame = false;
        WasReleasedThisFrame = false;
        HeldTime = 0f;
    }
}

[Serializable]
public class PlayerInputSnapshot
{
    [field: SerializeField] public Vector2 Move { get; private set; }
    [field: SerializeField] public Vector2 Look { get; private set; }
    [field: SerializeField] public float MoveAmount { get; private set; }

    [field: SerializeField] public InputButtonState Jump { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState Sprint { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState LightAttack { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState HeavyAttack { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState Guard { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState Interact { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState Alt { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState Crouch { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState LeftTrigger { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState Sheathe { get; private set; } = new InputButtonState();

    public void SetMove(Vector2 value)
    {
        Move = value;
        MoveAmount = Mathf.Max(Mathf.Abs(value.x), Mathf.Abs(value.y));
    }

    public void SetLook(Vector2 value)
    {
        Look = value;
    }

    public void Tick(float deltaTime)
    {
        Jump.Tick(deltaTime);
        Sprint.Tick(deltaTime);
        LightAttack.Tick(deltaTime);
        HeavyAttack.Tick(deltaTime);
        Guard.Tick(deltaTime);
        Interact.Tick(deltaTime);
        Alt.Tick(deltaTime);
        Crouch.Tick(deltaTime);
        LeftTrigger.Tick(deltaTime);
        Sheathe.Tick(deltaTime);
    }

    public void ClearFrameState()
    {
        Jump.ClearFrameState();
        Sprint.ClearFrameState();
        LightAttack.ClearFrameState();
        HeavyAttack.ClearFrameState();
        Guard.ClearFrameState();
        Interact.ClearFrameState();
        Alt.ClearFrameState();
        Crouch.ClearFrameState();
        LeftTrigger.ClearFrameState();
        Sheathe.ClearFrameState();
    }

    public void Reset()
    {
        SetMove(Vector2.zero);
        SetLook(Vector2.zero);
        Jump.Reset();
        Sprint.Reset();
        LightAttack.Reset();
        HeavyAttack.Reset();
        Guard.Reset();
        Interact.Reset();
        Alt.Reset();
        Crouch.Reset();
        LeftTrigger.Reset();
        Sheathe.Reset();
    }
}

[Serializable]
public class UIInputSnapshot
{
    [field: SerializeField] public Vector2 Navigate { get; private set; }
    [field: SerializeField] public Vector2 Point { get; private set; }
    [field: SerializeField] public Vector2 ScrollWheel { get; private set; }

    [field: SerializeField] public InputButtonState Submit { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState Cancel { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState Click { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState RightClick { get; private set; } = new InputButtonState();
    [field: SerializeField] public InputButtonState MiddleClick { get; private set; } = new InputButtonState();

    public void SetNavigate(Vector2 value)
    {
        Navigate = value;
    }

    public void SetPoint(Vector2 value)
    {
        Point = value;
    }

    public void SetScrollWheel(Vector2 value)
    {
        ScrollWheel = value;
    }

    public void Tick(float deltaTime)
    {
        Submit.Tick(deltaTime);
        Cancel.Tick(deltaTime);
        Click.Tick(deltaTime);
        RightClick.Tick(deltaTime);
        MiddleClick.Tick(deltaTime);
    }

    public void ClearFrameState()
    {
        Submit.ClearFrameState();
        Cancel.ClearFrameState();
        Click.ClearFrameState();
        RightClick.ClearFrameState();
        MiddleClick.ClearFrameState();
        ScrollWheel = Vector2.zero;
    }

    public void Reset()
    {
        SetNavigate(Vector2.zero);
        SetPoint(Vector2.zero);
        SetScrollWheel(Vector2.zero);
        Submit.Reset();
        Cancel.Reset();
        Click.Reset();
        RightClick.Reset();
        MiddleClick.Reset();
    }
}
