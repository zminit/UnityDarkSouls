using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationMoveHandler : MonoBehaviour
{
    public Animator animator;
    public Transform ModelRoot;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ModelRoot = transform.parent;
    }

    private void Start()
    {
    }
    private void OnAnimatorMove()
    {
        Vector3 deltaPos = animator.deltaPosition;
        Quaternion deltaRot = animator.deltaRotation;
        ModelRoot.position += deltaPos;
        ModelRoot.rotation *= deltaRot;
    }
}
