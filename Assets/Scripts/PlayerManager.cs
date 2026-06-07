using System;
using System.Collections;
using System.Collections.Generic;
using CFSM;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    Rigidbody rb;
    Animator animator;
    CFSM.CharacterFSM characterFSM;

    public OnLandHandler OnLandHandler;

    #region Properties
    public float WalkSpeed = 0.5f;
    public float RunSpeed = 2.0f;
    public float SprintSpeed = 5.0f;
    public bool canRotate = true;

    [SerializeField]
    Transform LeftFoot;
    [SerializeField]
    Transform RightFoot;

    [Header("Ground Check Debug")]
    [SerializeField]
    bool showFootGroundRays;
    [SerializeField]
    float LandCheckBias;

    [Header("Weapon Animation")]
    [SerializeField]
    bool isArmed = true;
    [SerializeField]
    string upperBodyLayerName = "Upper Body Layer";
    [SerializeField]
    string drawAnimationName = "DrawSword";
    [SerializeField]
    string sheatheAnimationName = "SheatheSword";
    [SerializeField]
    float upperBodyWeightBlendSpeed = 8f;
    [SerializeField]
    float sheatheFallbackDuration = 1.0f;

    int upperBodyLayerIndex = -1;
    float upperBodyLayerWeight;
    WeaponActionState weaponActionState = WeaponActionState.None;
    float weaponActionStartedAt;
    #endregion

    public bool IsArmed => isArmed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        characterFSM = GetComponent<CFSM.CharacterFSM>();
        OnLandHandler = new OnLandHandler(LeftFoot, RightFoot);
        CacheUpperBodyLayer();
        upperBodyLayerWeight = isArmed ? 1f : 0f;
        ApplyUpperBodyLayerWeight();
        SyncGroundRayDebugSettings();
    }

    private void Update()
    {
        SyncGroundRayDebugSettings();
        TickWeaponAction();
        UpdateUpperBodyLayerWeight();
    }

    private void OnValidate()
    {
        SyncGroundRayDebugSettings();
    }

    public bool RequestToggleWeapon()
    {
        if (!CanToggleWeapon())
            return false;

        if (isArmed)
            return RequestSheatheWeapon();

        return RequestDrawWeapon();
    }

    public void SetArmed(bool armed)
    {
        if (weaponActionState != WeaponActionState.None)
            return;

        isArmed = armed;
    }

    bool RequestSheatheWeapon()
    {
        if (!isArmed || weaponActionState != WeaponActionState.None)
            return false;

        if (!TryPlayWeaponAction(sheatheAnimationName))
            return false;

        weaponActionState = WeaponActionState.Sheathing;
        weaponActionStartedAt = Time.time;
        return true;
    }

    bool RequestDrawWeapon()
    {
        if (isArmed || weaponActionState != WeaponActionState.None)
            return false;

        if (!TryPlayWeaponAction(drawAnimationName))
            return false;

        weaponActionState = WeaponActionState.Drawing;
        weaponActionStartedAt = Time.time;
        return true;
    }

    bool TryPlayWeaponAction(string animationName)
    {
        CacheUpperBodyLayer();
        if (animator == null || upperBodyLayerIndex < 0 || string.IsNullOrEmpty(animationName))
            return false;

        int shortNameHash = Animator.StringToHash(animationName);
        int fullPathHash = Animator.StringToHash($"{upperBodyLayerName}.{animationName}");
        if (!animator.HasState(upperBodyLayerIndex, shortNameHash)
            && !animator.HasState(upperBodyLayerIndex, fullPathHash))
            return false;

        animator.CrossFade(animationName, 0.1f, upperBodyLayerIndex);
        return true;
    }

    bool CanToggleWeapon()
    {
        if (characterFSM == null)
            return true;

        return characterFSM.CurrentStateType == CharacterStateType.Locomotion;
    }

    void TickWeaponAction()
    {
        if (weaponActionState == WeaponActionState.None)
            return;

        if (weaponActionState == WeaponActionState.Sheathing && HasWeaponAnimationFinished(sheatheAnimationName))
        {
            isArmed = false;
            weaponActionState = WeaponActionState.None;
        }
        else if (weaponActionState == WeaponActionState.Drawing && HasWeaponAnimationFinished(drawAnimationName))
        {
            isArmed = true;
            weaponActionState = WeaponActionState.None;
        }
    }

    bool HasWeaponAnimationFinished(string animationName)
    {
        if (animator == null || upperBodyLayerIndex < 0)
            return Time.time - weaponActionStartedAt >= sheatheFallbackDuration;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(upperBodyLayerIndex);
        bool animationMatchedAndFinished = info.IsName(animationName) && info.normalizedTime >= 0.95f;
        bool timedOut = Time.time - weaponActionStartedAt >= sheatheFallbackDuration;
        return animationMatchedAndFinished || timedOut;
    }

    void UpdateUpperBodyLayerWeight()
    {
        CacheUpperBodyLayer();
        if (animator == null || upperBodyLayerIndex < 0)
            return;

        float targetWeight = weaponActionState != WeaponActionState.None || isArmed ? 1f : 0f;
        upperBodyLayerWeight = Mathf.MoveTowards(
            upperBodyLayerWeight,
            targetWeight,
            upperBodyWeightBlendSpeed * Time.deltaTime);
        ApplyUpperBodyLayerWeight();
    }

    void ApplyUpperBodyLayerWeight()
    {
        if (animator != null && upperBodyLayerIndex >= 0)
            animator.SetLayerWeight(upperBodyLayerIndex, upperBodyLayerWeight);
    }

    void CacheUpperBodyLayer()
    {
        if (animator == null)
            return;

        upperBodyLayerIndex = string.IsNullOrEmpty(upperBodyLayerName)
            ? -1
            : animator.GetLayerIndex(upperBodyLayerName);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showFootGroundRays)
            return;

        DrawFootGroundRayGizmo(LeftFoot, LandCheckBias);
        DrawFootGroundRayGizmo(RightFoot, LandCheckBias);
    }

    private void SyncGroundRayDebugSettings()
    {
        if (OnLandHandler == null)
            return;

        OnLandHandler.DrawDebugRays = showFootGroundRays;
        OnLandHandler.LandCheckBias = LandCheckBias;
    }

    private static void DrawFootGroundRayGizmo(Transform foot, float landCheckBias)
    {
        if (foot == null)
            return;

        float rayLength = OnLandHandler.GroundCheckRayLength;
        Vector3 rayOrigin = foot.position + Vector3.up * landCheckBias;
        bool hitGround = Physics.Raycast(rayOrigin, Vector3.down, rayLength, 1);
        Gizmos.color = hitGround ? Color.green : Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * rayLength);
    }

    enum WeaponActionState
    {
        None,
        Drawing,
        Sheathing
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
