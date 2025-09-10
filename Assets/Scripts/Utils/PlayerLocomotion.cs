using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
//这是一个处理玩家移动的工具类
{
    public class PlayerLocomotion
    {
        //这是一个处理玩家移动的工具类
        public static void HandleRotation(ref Transform player, Vector3 moveDir)
        {
            Vector3 targetDir = Vector3.Normalize(moveDir);
            Quaternion tr = Quaternion.LookRotation(targetDir);
            player.rotation = Quaternion.Slerp(player.rotation, tr, Time.deltaTime * 10f);
        }

        public static void HandleMovement(ref Rigidbody player, Vector3 moveDir, Vector3 normal, float speed)
        {
            Vector3 targetDir = Vector3.Normalize(moveDir);
            Vector3 movement = targetDir * speed;
            movement = Vector3.ProjectOnPlane(movement, normal);
            player.velocity = movement;
        }

        public static void HandleAnimatorInputByLocomotionInput(Vector2 PlayerModelForward, Vector2 LocomotionDir, out float vertical, out float horizontal)
        {
            // 确保 forward 方向是单位向量，但保留 locomotionDir 的原始长度。
            Vector2 normalizedForward = PlayerModelForward.normalized;

            // 计算前进方向上的分量 (v)
            // 点积结果将是locomotionDir在normalizedForward上的投影长度。
            vertical = Vector2.Dot(LocomotionDir, normalizedForward);

            // 计算垂直方向上的分量 (h)
            // 垂直向量可以通过旋转前进向量90度得到
            Vector2 right = new Vector2(normalizedForward.y, -normalizedForward.x);

            // 再次使用点积计算locomotionDir在垂直方向上的投影
            horizontal = Vector2.Dot(LocomotionDir, right);
        }

        public static Quaternion GetPlayerRotation(Transform camera ,float vertical, float horizontal, Vector3 forward)
        {
            Vector3 lookDir = camera.forward * vertical + camera.right * horizontal;
            lookDir.Normalize();
            forward.Normalize();
            Quaternion rotation = Quaternion.LookRotation(forward, lookDir);
            return rotation;
        }
    
        public static float GetPlayerRotationAngle(Vector3 lookDir, Vector3 playerForward, Vector3 normal)
        {
            lookDir.Normalize();
            playerForward.Normalize();
            normal.Normalize();
            lookDir = lookDir - (Vector3.Dot(lookDir, normal) * normal);
            lookDir.Normalize();
            float angle = Mathf.Acos(Vector3.Dot(lookDir, playerForward));
            angle = angle / Mathf.PI / 2 * 360;
            float t = Vector3.Dot(normal, Vector3.Cross(playerForward, lookDir));
            if (t < 0)
                angle = -angle;
            return angle;
        }
    }
}
