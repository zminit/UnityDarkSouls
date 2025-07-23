using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DS
{
    public class InputController : MonoBehaviour
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

        public void TickInput()
        {

        }

        public void MoveInput()
        {

        }
    }

}

