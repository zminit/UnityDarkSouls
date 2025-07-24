using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    public Transform targetTransform;
    public Transform cameraTransform;
    public Transform cameraPivotTransform;
    public Transform myTransform;
    private Vector3 cameraTransformPosition;
    private LayerMask ignoreLayers;

    public static CameraHandler singleton;

    private float lookSpeed = 0.1f;
    private float pivotSpeed = 0.1f;
    private float followSpeed = 0.1f;

    private float defaultPosition;
    private float lookAngle;
    private float pivotAngle;
    private float minPivot = -35f;
    private float maxPivot = 35f;

}
