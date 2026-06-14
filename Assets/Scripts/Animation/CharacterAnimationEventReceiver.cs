using CFSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

