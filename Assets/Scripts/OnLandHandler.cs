using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnLandHandler
{
    Transform LeftFoot;
    Transform RightFoot;
    int LayerMask = 1;

    public OnLandHandler(Transform LeftFoot, Transform RightFoot)
    {
        this.LeftFoot = LeftFoot;
        this.RightFoot = RightFoot;
    }

    /// <summary>
    /// ��ȡ��һ����ŵ�
    /// </summary>
    /// <param name="moveDir">λ��</param>
    /// <param name="pos">�����ŵ�λ��</param>
    /// <returns>����boolֵ�������Ƿ��⵽��Ŵ�</returns>
    public bool GetNextFeetPos(Vector3 moveDir, out Vector3 pos)
    {
        pos = Vector3.zero;
        Vector3 LtoR = (RightFoot.position - LeftFoot.position).normalized;
        LtoR = LtoR - (Vector3.Dot(LtoR, LeftFoot.up) * LeftFoot.up);
        if(Vector3.Dot(LtoR ,LeftFoot.forward) > 0)
        {
            //�ҽ���ǰ
            RaycastHit hitPoint;
            if(Physics.Raycast(RightFoot.position + RightFoot.up * 0.5f, Vector3.down, out hitPoint, 1.5f, LayerMask))
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
    /// ������
    /// </summary>
    /// <returns></returns>
    public bool OnLandCheck()
    {
        bool isOnLand = false;
        RaycastHit leftHit;
        RaycastHit rightHit;
        isOnLand =  Physics.Raycast(LeftFoot.position, Vector3.down, out leftHit, 1.0f, LayerMask);
        isOnLand |= Physics.Raycast(RightFoot.position, Vector3.down, out rightHit, 1.0f, LayerMask);
        return isOnLand;
    }
}
