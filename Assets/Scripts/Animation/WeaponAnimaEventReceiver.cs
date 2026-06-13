using CFSM;
using UnityEngine;

public class WeaponAnimaEventReceiver : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    PlayerManager playerManager;

    [Header("Weapon")]
    [SerializeField]
    Transform weaponTransform;

    [Header("Attach Target")]
    [SerializeField]
    Transform attachTarget;

    [Header("Sheath Attach Target")]
    [SerializeField]
    Transform sheathAttachTarget;

    [Header("Local Pose After Attach")]
    [SerializeField]
    bool resetLocalPosition = true;
    [SerializeField]
    bool resetLocalRotation = true;
    [SerializeField]
    bool resetLocalScale;
    [SerializeField]
    Vector3 localPosition;
    [SerializeField]
    Vector3 localEulerAngles;
    [SerializeField]
    Vector3 localScale = Vector3.one;

    [Header("Local Pose After Sheath Attach")]
    [SerializeField]
    bool resetSheathLocalPosition = true;
    [SerializeField]
    bool resetSheathLocalRotation = true;
    [SerializeField]
    bool resetSheathLocalScale;
    [SerializeField]
    Vector3 sheathLocalPosition;
    [SerializeField]
    Vector3 sheathLocalEulerAngles;
    [SerializeField]
    Vector3 sheathLocalScale = Vector3.one;

    public Transform WeaponTransform => weaponTransform;
    public Transform AttachTarget => attachTarget;
    public Transform SheathAttachTarget => sheathAttachTarget;

    private void Awake()
    {
        if (playerManager == null)
            playerManager = GetComponentInParent<PlayerManager>();
    }

    public void SetWeaponTransform(Transform weapon)
    {
        weaponTransform = weapon;
    }

    public void SetAttachTarget(Transform target)
    {
        attachTarget = target;
    }

    public void SetSheathAttachTarget(Transform target)
    {
        sheathAttachTarget = target;
    }

    public void AttachToHand()
    {
        AttachToTarget();
    }

    public void AttachToTarget()
    {
        AttachTo(attachTarget);
    }

    public void AttachToSheath()
    {
        AttachTo(
            sheathAttachTarget,
            resetSheathLocalPosition,
            resetSheathLocalRotation,
            resetSheathLocalScale,
            sheathLocalPosition,
            sheathLocalEulerAngles,
            sheathLocalScale);
    }

    public void AttachTo(Transform target)
    {
        AttachTo(
            target,
            resetLocalPosition,
            resetLocalRotation,
            resetLocalScale,
            localPosition,
            localEulerAngles,
            localScale);
    }

    void AttachTo(
        Transform target,
        bool shouldResetLocalPosition,
        bool shouldResetLocalRotation,
        bool shouldResetLocalScale,
        Vector3 targetLocalPosition,
        Vector3 targetLocalEulerAngles,
        Vector3 targetLocalScale)
    {
        if (weaponTransform == null || target == null)
            return;

        weaponTransform.SetParent(target, false);

        if (shouldResetLocalPosition)
            weaponTransform.localPosition = targetLocalPosition;

        if (shouldResetLocalRotation)
            weaponTransform.localRotation = Quaternion.Euler(targetLocalEulerAngles);

        if (shouldResetLocalScale)
            weaponTransform.localScale = targetLocalScale;
    }

    public void OnDrawWeaponEnd()
    {
        playerManager?.CompleteDrawWeaponFromAnimationEvent();
    }

    public void OnSheatheWeaponEnd()
    {
        playerManager?.CompleteSheatheWeaponFromAnimationEvent();
    }
}

public class CharacterAnimationEventReceiver : MonoBehaviour
{
    [SerializeField]
    CharacterFSM characterFSM;

    private void Awake()
    {
        if (characterFSM == null)
            characterFSM = GetComponentInParent<CharacterFSM>();
    }

    public void OpenComboWindow()
    {
        Notify(CharacterAnimationEventType.OpenComboWindow);
    }

    public void CloseComboWindow()
    {
        Notify(CharacterAnimationEventType.CloseComboWindow);
    }

    public void TryConsumeCombo()
    {
        Notify(CharacterAnimationEventType.TryConsumeCombo);
    }

    public void OnAttackEnd()
    {
        Notify(CharacterAnimationEventType.AttackEnd);
    }

    public void OpenCancelWindow()
    {
        Notify(CharacterAnimationEventType.OpenCancelWindow);
    }

    public void CloseCancelWindow()
    {
        Notify(CharacterAnimationEventType.CloseCancelWindow);
    }

    void Notify(CharacterAnimationEventType eventType)
    {
        if (characterFSM == null)
            characterFSM = GetComponentInParent<CharacterFSM>();

        characterFSM?.NotifyAnimationEvent(eventType);
    }
}
