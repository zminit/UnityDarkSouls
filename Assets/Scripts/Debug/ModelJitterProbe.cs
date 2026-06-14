using UnityEngine;

/// <summary>
/// Lightweight scene-view probe for checking whether visible jitter comes from the physics root
/// or from the animated model child. Enable it only while diagnosing movement jitter.
/// </summary>
public class ModelJitterProbe : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    Transform physicsRoot;
    [SerializeField]
    Transform modelRoot;

    [Header("Debug")]
    [SerializeField]
    bool drawGizmos = true;
    [SerializeField]
    bool logLargeDeltas;
    [SerializeField]
    float largeDeltaThreshold = 0.04f;
    [SerializeField]
    float arrowScale = 20f;

    Vector3 lastPhysicsRootPosition;
    Vector3 lastModelRootPosition;
    bool hasLastFrame;

    private void Reset()
    {
        physicsRoot = transform;

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
            modelRoot = animator.transform;
    }

    private void LateUpdate()
    {
        if (physicsRoot == null)
            physicsRoot = transform;

        if (modelRoot == null)
        {
            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
                modelRoot = animator.transform;
        }

        if (physicsRoot == null || modelRoot == null)
            return;

        Vector3 physicsPosition = physicsRoot.position;
        Vector3 modelPosition = modelRoot.position;

        if (hasLastFrame && logLargeDeltas)
        {
            float physicsDelta = Vector3.Distance(physicsPosition, lastPhysicsRootPosition);
            float modelDelta = Vector3.Distance(modelPosition, lastModelRootPosition);

            if (physicsDelta >= largeDeltaThreshold || modelDelta >= largeDeltaThreshold)
                Debug.Log(
                    $"ModelJitterProbe large delta | physicsRoot={physicsDelta:F4}, modelRoot={modelDelta:F4}, frame={Time.frameCount}",
                    this);
        }

        lastPhysicsRootPosition = physicsPosition;
        lastModelRootPosition = modelPosition;
        hasLastFrame = true;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        DrawDeltaArrow(physicsRoot, lastPhysicsRootPosition, Color.yellow);
        DrawDeltaArrow(modelRoot, lastModelRootPosition, Color.cyan);
    }

    private void DrawDeltaArrow(Transform target, Vector3 lastPosition, Color color)
    {
        if (target == null || !hasLastFrame)
            return;

        Vector3 delta = target.position - lastPosition;
        if (delta.sqrMagnitude <= 0.000001f)
            return;

        Gizmos.color = color;
        Vector3 origin = target.position + Vector3.up * 1.2f;
        Gizmos.DrawLine(origin, origin + delta * arrowScale);
        Gizmos.DrawSphere(origin + delta * arrowScale, 0.025f);
    }
}
