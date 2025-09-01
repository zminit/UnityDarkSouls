using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils; // Assuming you have a namespace for utility functions

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

    private PlayerControlls inputActions;

    private void OnEnable()
    {
        Alt_Started += () => Alt_Input = true;
        Alt_Canceled += () => Alt_Input = false;
        B_Input_Started += () => B_Input = true;
        B_Input_Canceled += () => B_Input = false;
        A_Input_Started += () => A_Input = true;
        A_Input_Canceled += () => A_Input = false ;

        if (inputActions == null)
        {
            inputActions = new PlayerControlls();
            inputActions.Player.Alt.started += ctx => Alt_Started?.Invoke();
            inputActions.Player.Alt.canceled += ctx => Alt_Canceled?.Invoke();
            inputActions.Player.Sprint.started += ctx => B_Input_Started?.Invoke();
            inputActions.Player.Sprint.canceled += ctx => B_Input_Canceled?.Invoke();
            inputActions.Player.Jump.started += ctx => A_Input_Started?.Invoke();
            inputActions.Player.Jump.canceled += ctx => A_Input_Canceled?.Invoke();
            inputActions.Player.Move.performed += ctx => playerMovement = ctx.ReadValue<Vector2>();
        }
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    public void TickUp(float delta)
    {
        vertical = playerMovement.y;
        horizontal = playerMovement.x;
        HandleMove();
        HandleB_Input(delta);
    }

    private void HandleB_Input(float delta)
    {
        if (B_Input)
        {
            B_Input_Time += delta;
        }
        else
        {
            B_Input_Time = 0.0f;
        }
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
