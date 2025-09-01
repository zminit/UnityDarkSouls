using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    InputHandler inputHandler;

    Rigidbody rb;

    #region Properties
    public float WalkSpeed = 0.5f; 
    public float RunSpeed  = 2.0f; 

    public float SprintSpeed = 20f; 
    #endregion
    private void Awake()
    {
        inputHandler = GetComponent<InputHandler>();
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
    }

    void Update()
    {
        inputHandler.TickUp(Time.deltaTime);
    }

    public void Move(Vector2 moveInput, float speed, Vector3 normal)
    {
        if (moveInput.sqrMagnitude < 0.01f) return;

        Vector3 moveDir = Camera.main.transform.forward * moveInput.y + Camera.main.transform.right * moveInput.x;
        moveDir.Normalize();
        moveDir *= speed;
        moveDir = Vector3.ProjectOnPlane(moveDir, normal); // 确保移动方向在地面上
        rb.velocity = moveDir; // 设置刚体速度以实现移动
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.deltaTime * 10f); // 平滑旋转
        }
    }

}
