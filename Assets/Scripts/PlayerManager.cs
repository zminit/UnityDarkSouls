using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    InputHandler inputHandler;
    Rigidbody rb;

    public OnLandHandler OnLandHandler;


    #region Properties
    public float WalkSpeed = 0.5f; 
    public float RunSpeed  = 2.0f; 
    public float SprintSpeed = 5.0f;
    public bool canRotate = true;

    [SerializeField]
    Transform LeftFoot;
    [SerializeField]
    Transform RightFoot;
    #endregion
    private void Awake()
    {
        inputHandler = GetComponent<InputHandler>();
        rb = GetComponent<Rigidbody>();
        OnLandHandler = new OnLandHandler(LeftFoot, RightFoot);
    }

    void Start()
    {
    }

    void Update()
    {
        inputHandler.TickUp(Time.deltaTime);
    }

    public void Move(Vector3 moveDir, float speed, Vector3 normal)
    {
        moveDir.Normalize();
        moveDir *= speed;
        moveDir = Vector3.ProjectOnPlane(moveDir, normal); // 确保移动方向在地面上
        rb.velocity = moveDir; // 设置刚体速度以实现移动
    }
    public void LookRotate(Vector3 lookDir, Vector3 normal)
    {
        if (lookDir.sqrMagnitude > 0.1f)
        {
            lookDir.Normalize();
            lookDir = lookDir - (normal * (Vector3.Dot(normal, lookDir)));
            Quaternion targetRotation = Quaternion.LookRotation(lookDir, normal);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.deltaTime * 5f); // 平滑旋转
        }
    }
}
