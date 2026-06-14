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

    [Header("Movement Sync")]
    [SerializeField]
    bool forceRigidbodyInterpolation = true;
    [SerializeField]
    bool syncAnimatorWithPhysics = true;

    [SerializeField]
    Transform LeftFoot;
    [SerializeField]
    Transform RightFoot;

    [Header("Ground Check Debug")]
    [SerializeField]
    bool showFootGroundRays;
    [SerializeField]
    float LandCheckBias;

    [Header("Armed Animation")]
    [SerializeField]
    bool isArmed;
    [SerializeField]
    string upperBodyLayerName = "Upper Body Layer";
    [SerializeField]
    float upperBodyWeightBlendSpeed = 8f;
    [SerializeField]
    int baseLayerIndex;
    [SerializeField]
    string baseLocomotionStateName = "CommonLocomotion";

    [Header("Draw Weapon Animation")]
    [SerializeField]
    string armedUpperBodyAnimationName = "LocomotionWithWeapon";
    [SerializeField]
    string armsLayerName = "Arms";
    [SerializeField]
    string armedSprintAnimationName = "SprintWithWeapon";
    [SerializeField]
    float drawWeaponCrossFadeDuration = 0.1f;
    [SerializeField]
    float sheatheWeaponCrossFadeDuration = 0.1f;
    [SerializeField]
    float baseLocomotionCrossFadeDuration = 0.1f;
    [SerializeField]
    float armedUpperBodyCrossFadeDuration = 0.1f;
    [SerializeField]
    float armsLayerWeightBlendSpeed = 8f;
    [SerializeField]
    float armedSprintCrossFadeDuration = 0.1f;
    int upperBodyLayerIndex = -1;
    int armsLayerIndex = -1;
    float upperBodyLayerWeight;
    float armsLayerWeight;
    bool isDrawingWeapon;
    bool isSheathingWeapon;
    bool wasUpperBodyLayerEnabled;
    bool wasArmsLayerEnabled;

    #endregion

    public bool IsArmed => isArmed;
    public bool IsDrawingWeapon => isDrawingWeapon;
    public bool IsSheathingWeapon => isSheathingWeapon;
    public bool IsChangingWeaponState => isDrawingWeapon || isSheathingWeapon;
    public bool CanDrawWeapon => !isArmed
        && !IsChangingWeaponState
        && (characterFSM == null || characterFSM.CurrentStateType == CharacterStateType.Locomotion);
    public bool CanSheatheWeapon => isArmed
        && !IsChangingWeaponState
        && (characterFSM == null || characterFSM.CurrentStateType == CharacterStateType.Locomotion);
    public float DrawWeaponCrossFadeDuration => drawWeaponCrossFadeDuration;
    public float SheatheWeaponCrossFadeDuration => sheatheWeaponCrossFadeDuration;
    public float BaseLocomotionCrossFadeDuration => baseLocomotionCrossFadeDuration;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        characterFSM = GetComponent<CFSM.CharacterFSM>();
        ConfigureMovementSync();
        OnLandHandler = new OnLandHandler(LeftFoot, RightFoot);
        CacheUpperBodyLayer();
        CacheArmsLayer();
        wasUpperBodyLayerEnabled = ShouldEnableUpperBodyLayer();
        wasArmsLayerEnabled = ShouldEnableArmsLayer();
        upperBodyLayerWeight = wasUpperBodyLayerEnabled ? 1f : 0f;
        armsLayerWeight = wasArmsLayerEnabled ? 1f : 0f;
        ApplyUpperBodyLayerWeight();
        ApplyArmsLayerWeight();
        SyncGroundRayDebugSettings();
    }

    void ConfigureMovementSync()
    {
        if (rb != null && forceRigidbodyInterpolation)
            rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (animator != null && syncAnimatorWithPhysics)
            animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
    }

    private void Update()
    {
        SyncGroundRayDebugSettings();
        UpdateUpperBodyLayerWeight();
        UpdateArmsLayerWeight();
    }

    private void OnValidate()
    {
        SyncGroundRayDebugSettings();
    }

    public void SetArmed(bool armed)
    {
        isArmed = armed;
    }

    public void ToggleArmed()
    {
        isArmed = !isArmed;
    }

    public bool RequestToggleWeapon()
    {
        if (characterFSM == null)
            return false;

        characterFSM.RequestState(
            isArmed ? StateRequestType.SheatheWeapon : StateRequestType.DrawWeapon,
            isArmed ? CharacterStateType.SheatheWeapon : CharacterStateType.DrawWeapon,
            StatePriorities.WeaponAction,
            RequestSource.Input);

        return true;
    }

    public bool RequestDrawWeapon()
    {
        if (characterFSM == null)
            return false;

        characterFSM.RequestState(
            StateRequestType.DrawWeapon,
            CharacterStateType.DrawWeapon,
            StatePriorities.WeaponAction,
            RequestSource.Input);

        return true;
    }

    public bool RequestSheatheWeapon()
    {
        if (characterFSM == null)
            return false;

        characterFSM.RequestState(
            StateRequestType.SheatheWeapon,
            CharacterStateType.SheatheWeapon,
            StatePriorities.WeaponAction,
            RequestSource.Input);

        return true;
    }

    public void BeginDrawWeaponAction()
    {
        isDrawingWeapon = true;
        isSheathingWeapon = false;
    }

    public void BeginSheatheWeaponAction()
    {
        isSheathingWeapon = true;
        isDrawingWeapon = false;
    }

    public void CancelWeaponAction()
    {
        isDrawingWeapon = false;
        isSheathingWeapon = false;
    }

    public void CompleteDrawWeaponFromAnimationEvent()
    {
        if (!isDrawingWeapon)
            return;

        isArmed = true;
        isDrawingWeapon = false;
        characterFSM?.RequestState(
            StateRequestType.AnimationEnd,
            CharacterStateType.Locomotion,
            StatePriorities.Locomotion,
            RequestSource.Animation,
            force: true);

        if (CanPlayArmedUpperBodyLocomotion())
            CrossFadeUpperBodyState(armedUpperBodyAnimationName, armedUpperBodyCrossFadeDuration);
    }

    public void CompleteSheatheWeaponFromAnimationEvent()
    {
        if (!isSheathingWeapon)
            return;

        isSheathingWeapon = false;
        characterFSM?.RequestState(
            StateRequestType.AnimationEnd,
            CharacterStateType.Locomotion,
            StatePriorities.Locomotion,
            RequestSource.Animation,
            force: true);
    }

    public void MarkWeaponSheathedFromAnimationEvent()
    {
        if (!isSheathingWeapon)
            return;

        isArmed = false;
    }

    void UpdateUpperBodyLayerWeight()
    {
        CacheUpperBodyLayer();
        if (animator == null || upperBodyLayerIndex < 0)
            return;

        bool shouldEnable = ShouldEnableUpperBodyLayer();
        if (shouldEnable && !wasUpperBodyLayerEnabled && CanPlayArmedUpperBodyLocomotion())
            CrossFadeUpperBodyState(armedUpperBodyAnimationName, armedUpperBodyCrossFadeDuration);

        float targetWeight = shouldEnable ? 1f : 0f;
        upperBodyLayerWeight = Mathf.MoveTowards(
            upperBodyLayerWeight,
            targetWeight,
            upperBodyWeightBlendSpeed * Time.deltaTime);
        ApplyUpperBodyLayerWeight();
        wasUpperBodyLayerEnabled = shouldEnable;
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

    void CacheArmsLayer()
    {
        if (animator == null)
            return;

        armsLayerIndex = string.IsNullOrEmpty(armsLayerName)
            ? -1
            : animator.GetLayerIndex(armsLayerName);
    }

    bool HasAnimatorState(int layerIndex, string stateName)
    {
        int shortNameHash = Animator.StringToHash(stateName);
        string layerName = animator.GetLayerName(layerIndex);
        int fullPathHash = Animator.StringToHash($"{layerName}.{stateName}");
        return animator.HasState(layerIndex, shortNameHash)
            || animator.HasState(layerIndex, fullPathHash);
    }

    bool ShouldEnableUpperBodyLayer()
    {
        return CanPlayArmedUpperBodyLocomotion();
    }

    bool ShouldEnableArmsLayer()
    {
        return CanPlayArmedSprint();
    }

    bool CanPlayArmedUpperBodyLocomotion()
    {
        return isArmed
            && characterFSM != null
            && characterFSM.CurrentStateType == CharacterStateType.Locomotion
            && characterFSM.CurrentMoveMode != MoveMode.Sprint
            && IsAnimatorInState(baseLayerIndex, baseLocomotionStateName);
    }

    bool CanPlayArmedSprint()
    {
        return isArmed
            && characterFSM != null
            && characterFSM.CurrentStateType == CharacterStateType.Locomotion
            && characterFSM.CurrentMoveMode == MoveMode.Sprint;
    }

    bool IsAnimatorInState(int layerIndex, string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName) || layerIndex < 0)
            return false;

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (current.IsName(stateName))
            return true;

        if (!animator.IsInTransition(layerIndex))
            return false;

        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layerIndex);
        return next.IsName(stateName);
    }

    void CrossFadeUpperBodyState(string stateName, float duration)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return;

        CacheUpperBodyLayer();
        if (upperBodyLayerIndex < 0 || !HasAnimatorState(upperBodyLayerIndex, stateName))
            return;

        animator.CrossFade(stateName, duration, upperBodyLayerIndex);
    }

    void UpdateArmsLayerWeight()
    {
        CacheArmsLayer();
        if (animator == null || armsLayerIndex < 0)
            return;

        bool shouldEnable = ShouldEnableArmsLayer();
        if (shouldEnable && !wasArmsLayerEnabled)
            CrossFadeArmsState(armedSprintAnimationName, armedSprintCrossFadeDuration);

        float targetWeight = shouldEnable ? 1f : 0f;
        armsLayerWeight = Mathf.MoveTowards(
            armsLayerWeight,
            targetWeight,
            armsLayerWeightBlendSpeed * Time.deltaTime);
        ApplyArmsLayerWeight();
        wasArmsLayerEnabled = shouldEnable;
    }

    void ApplyArmsLayerWeight()
    {
        if (animator != null && armsLayerIndex >= 0)
            animator.SetLayerWeight(armsLayerIndex, armsLayerWeight);
    }

    void CrossFadeArmsState(string stateName, float duration)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return;

        CacheArmsLayer();
        if (armsLayerIndex < 0 || !HasAnimatorState(armsLayerIndex, stateName))
            return;

        animator.CrossFade(stateName, duration, armsLayerIndex);
    }

    void CrossFadeBaseState(string stateName, float duration)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return;

        if (!HasAnimatorState(baseLayerIndex, stateName))
            return;

        animator.CrossFade(stateName, duration, baseLayerIndex);
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

    public void Move(Vector3 moveDir, float speed, Vector3 normal)
    {
        moveDir.Normalize();
        moveDir *= speed;
        moveDir = Vector3.ProjectOnPlane(moveDir, normal); // 确保移动方向在地面上
        Vector3 currentVelocity = rb.velocity;
        rb.velocity = new Vector3(moveDir.x, currentVelocity.y, moveDir.z); // 只覆盖水平速度，保留垂直速度给重力和跳跃使用
    }

    public void LookRotate(Vector3 lookDir, Vector3 normal)
    {
        if (lookDir.sqrMagnitude > 0.1f)
        {
            lookDir.Normalize();
            lookDir = lookDir - (normal * (Vector3.Dot(normal, lookDir)));
            Quaternion targetRotation = Quaternion.LookRotation(lookDir, normal);
            float deltaTime = Time.inFixedTimeStep ? Time.fixedDeltaTime : Time.deltaTime;
            Quaternion nextRotation = Quaternion.Slerp(rb.rotation, targetRotation, deltaTime * 5f);
            rb.MoveRotation(nextRotation); // 通过 Rigidbody 旋转，避免绕过插值导致模型抖动
        }
    }
}
