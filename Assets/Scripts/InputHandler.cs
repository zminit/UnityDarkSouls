using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DS
{
    public class InputHandler : MonoBehaviour
    {
        public float horizontal;
        public float vertical;
        public float mouseAmount;
        public float mouseX;
        public float mouseY;

        PlayerControlls inputActions;

        Vector2 movementInput;
        Vector2 cameraInput;

        private void OnEnable()
        {
            if(inputActions == null)
            {
                inputActions = new PlayerControlls();
                inputActions.PlayerMovement.Movement.performed += inputActions => movementInput = inputActions.ReadValue<Vector2>();
                inputActions.PlayerMovement.Camera.performed += i => cameraInput = i.ReadValue<Vector2>();
            }

            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }

        public void TickInput(float delta)
        {
            MoveInput(delta);
        }

        public void MoveInput(float delta)
        {
            horizontal = movementInput.x;
            vertical = movementInput.y;
            // mouseAmount ����δ֪
            mouseAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));
            mouseX = cameraInput.x;
            mouseY = cameraInput.y;
        }
    }

}

