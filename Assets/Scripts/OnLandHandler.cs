using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnLandHandler
{
    public const float GroundCheckRayLength = 1.0f;

    Transform LeftFoot;
    Transform RightFoot;
    int LayerMask = 1;

    public bool DrawDebugRays { get; set; }

    public float LandCheckBias { get; set; }

    public OnLandHandler(Transform LeftFoot, Transform RightFoot)
    {
        this.LeftFoot = LeftFoot;
        this.RightFoot = RightFoot;
    }

    /// <summary>
    /// 获取下一个落脚点
    /// </summary>
    /// <param name="moveDir">位移</param>
    /// <param name="pos">输出落脚点位置</param>
    /// <returns>返回bool值，代表是否检测到落脚处</returns>
    public bool GetNextFeetPos(Vector3 moveDir, out Vector3 pos)
    {
        pos = Vector3.zero;
        Vector3 LtoR = (RightFoot.position - LeftFoot.position).normalized;
        LtoR = LtoR - (Vector3.Dot(LtoR, LeftFoot.up) * LeftFoot.up);
        if (Vector3.Dot(LtoR, LeftFoot.forward) > 0)
        {
            //右脚在前
            RaycastHit hitPoint;
            if (Physics.Raycast(RightFoot.position + RightFoot.up * 0.5f, Vector3.down, out hitPoint, 1.5f, LayerMask))
            {
                pos = hitPoint.point;
                return true;
            }
        }
        else
        {
            RaycastHit hitPoint;
            if (Physics.Raycast(LeftFoot.position + LeftFoot.up * 0.5f, Vector3.down, out hitPoint, 1.5f, LayerMask))
            {
                pos = hitPoint.point;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 地面检测
    /// </summary>
    /// <returns></returns>
    public bool OnLandCheck()
    {
        RaycastHit leftHit;
        RaycastHit rightHit;
        Vector3 leftRayOrigin = LeftFoot.position + Vector3.up * LandCheckBias;
        Vector3 rightRayOrigin = RightFoot.position + Vector3.up * LandCheckBias;
        bool leftGrounded = Physics.Raycast(leftRayOrigin, Vector3.down, out leftHit, GroundCheckRayLength, LayerMask);
        bool rightGrounded = Physics.Raycast(rightRayOrigin, Vector3.down, out rightHit, GroundCheckRayLength, LayerMask);

        if (DrawDebugRays)
        {
            Debug.DrawRay(leftRayOrigin, Vector3.down * GroundCheckRayLength, leftGrounded ? Color.green : Color.red);
            Debug.DrawRay(rightRayOrigin, Vector3.down * GroundCheckRayLength, rightGrounded ? Color.green : Color.red);
        }

        return leftGrounded || rightGrounded;
    }
}
