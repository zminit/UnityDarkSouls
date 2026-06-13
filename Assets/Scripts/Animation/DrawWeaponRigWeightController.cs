using UnityEngine;
using UnityEngine.Animations.Rigging;

public class DrawWeaponRigWeightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    Rig targetRig;

    [Header("Blend")]
    [SerializeField]
    float blendToOneDuration = 0.2f;
    [SerializeField]
    float initialWeight;
    [SerializeField]
    bool resetWeightOnEnable = true;

    float blendStartWeight;
    float blendTargetWeight;
    float blendStartedAt;
    float blendDuration;
    bool isBlending;

    public float CurrentWeight => targetRig != null ? targetRig.weight : 0f;
    public bool IsBlending => isBlending;

    private void Awake()
    {
        if (targetRig == null)
            targetRig = GetComponent<Rig>();
    }

    private void OnEnable()
    {
        if (resetWeightOnEnable)
            SetWeightImmediate(initialWeight);
    }

    private void Update()
    {
        if (!isBlending || targetRig == null)
            return;

        if (blendDuration <= 0f)
        {
            SetWeightImmediate(blendTargetWeight);
            return;
        }

        float t = Mathf.Clamp01((Time.time - blendStartedAt) / blendDuration);
        targetRig.weight = Mathf.Lerp(blendStartWeight, blendTargetWeight, t);

        if (t >= 1f)
            isBlending = false;
    }

    public void StartBlendToOne()
    {
        StartBlendToWeight(1f, blendToOneDuration);
    }

    public void StartBlendToZero()
    {
        StartBlendToWeight(0f, blendToOneDuration);
    }

    public void StartBlendToWeight(float targetWeight)
    {
        StartBlendToWeight(targetWeight, blendToOneDuration);
    }

    public void StartBlendToWeight(float targetWeight, float duration)
    {
        if (targetRig == null)
            return;

        blendStartWeight = targetRig.weight;
        blendTargetWeight = Mathf.Clamp01(targetWeight);
        blendStartedAt = Time.time;
        blendDuration = Mathf.Max(0f, duration);
        isBlending = true;
    }

    public void SetWeightImmediate(float weight)
    {
        if (targetRig == null)
            return;

        targetRig.weight = Mathf.Clamp01(weight);
        isBlending = false;
    }
}
