using UnityEngine;

/// <summary>
/// Stabilizes the Cinemachine follow target so small Rigidbody or animation jitter on the player root
/// does not become visible camera shake. XZ follows directly; Y can be smoothed or locked while grounded.
/// </summary>
public class CameraTargetStabilizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    Transform followRoot;

    [Header("Offset")]
    [SerializeField]
    Vector3 localOffset = new Vector3(0.037f, 1.484f, 0f);

    [Header("Smoothing")]
    [SerializeField]
    bool smoothXZ = true;
    [SerializeField]
    float xzSmoothTime = 0.04f;
    [SerializeField]
    float maxXZSpeed = 60f;
    [SerializeField]
    bool smoothY = true;
    [SerializeField]
    float ySmoothTime = 0.12f;
    [SerializeField]
    float maxYSpeed = 8f;

    [Header("Rotation")]
    [SerializeField]
    bool followRootRotation;

    Vector3 smoothedPosition;
    Vector3 xzVelocity;
    float yVelocity;
    bool hasInitialized;

    public Transform FollowRoot => followRoot;
    public Vector3 LocalOffset
    {
        get => localOffset;
        set => localOffset = value;
    }

    private void Awake()
    {
        ResolveFollowRoot();
        SnapToTarget();
    }

    private void OnEnable()
    {
        ResolveFollowRoot();
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (followRoot == null)
            ResolveFollowRoot();

        if (followRoot == null)
            return;

        Vector3 targetPosition = followRoot.TransformPoint(localOffset);
        Vector3 nextPosition;
        if (smoothXZ && hasInitialized)
        {
            nextPosition = Vector3.SmoothDamp(
                smoothedPosition,
                targetPosition,
                ref xzVelocity,
                xzSmoothTime,
                maxXZSpeed);
        }
        else
        {
            nextPosition = targetPosition;
            xzVelocity = Vector3.zero;
        }

        float targetY = smoothY && hasInitialized
            ? Mathf.SmoothDamp(smoothedPosition.y, targetPosition.y, ref yVelocity, ySmoothTime, maxYSpeed)
            : targetPosition.y;

        nextPosition.y = targetY;
        smoothedPosition = nextPosition;
        transform.position = smoothedPosition;
        transform.rotation = followRootRotation ? followRoot.rotation : Quaternion.identity;
        hasInitialized = true;
    }

    public void SetFollowRoot(Transform root)
    {
        followRoot = root;
        SnapToTarget();
    }

    public void SnapToTarget()
    {
        if (followRoot == null)
            ResolveFollowRoot();

        if (followRoot == null)
            return;

        transform.position = followRoot.TransformPoint(localOffset);
        transform.rotation = followRootRotation ? followRoot.rotation : Quaternion.identity;
        smoothedPosition = transform.position;
        xzVelocity = Vector3.zero;
        yVelocity = 0f;
        hasInitialized = true;
    }

    void ResolveFollowRoot()
    {
        if (followRoot != null)
            return;

        if (transform.parent != null)
            followRoot = transform.parent;
    }
}
