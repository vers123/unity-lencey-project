using UnityEngine;      // MonoBehaviour、Transform、Vector3 等基础类型

namespace Lencey
{
    /// <summary>
    /// 相机跟随：始终停在目标的后上方，并看向目标。
    /// </summary>
    /// <remarks>
    /// 【为什么写在 LateUpdate】
    /// Update 里目标可能还在移动（读输入 / 改 Transform）。
    /// LateUpdate 在所有 Update 之后执行，保证这一帧目标已经动完，
    /// 相机才不会「晚一帧」，负责画面会抖动。
    /// 【原理】
    /// 相机位置 = 目标位置 + 偏移量(offset)，每帧重算一次即可跟随。
    /// </remarks>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;                          // 要跟随的目标（Inspector 里把玩家拖进来）
        [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -8f); // 相对偏移：y=+6 在上方，z=-8 在后方
        [SerializeField] private float smoothTime = 0.15f;                  // 平滑时间：大约多久追上目标，越小越跟手

        // SmoothDamp 用 ref 传出一个「当前速度」作为内部状态。
        // 必须写成字段：写成局部变量的话每次调用都从 0 开始，平滑效果就没了
        private Vector3 currentVelocity;

        private void LateUpdate()
        {
            // 目标没设置时直接返回，避免空引用报错（比如忘记在 Inspector 里拖目标）
            if (target == null) return;

            // 目标位置 + 偏移量  = 相机「应该在」的位置
            Vector3 desiredPosition = target.position + offset;

            // Vector3.SmoothDamp：弹簧阻尼式平滑，参数含义是「大约多久追上」。
            // 相比 Lerp(a, b, 0.1f) 每帧固定走 10%，SmoothDamp 与帧率无关，且能真正到达目标
            transform.position = Vector3.SmoothDamp(
                transform.position,     // 当前位置
                desiredPosition,        // 目标位置
                ref currentVelocity,    // 内部速度状态（字段）
                smoothTime);            // 平滑时间

            // 让相机始终朝向目标。
            // 如果想保持水平、不俯视，可改为只水平朝向：
            //   Vector3 flat = new Vector3(target.position.x, transform.position.y, target.position.z);
            //   transform.LookAt(flat);
            transform.LookAt(target);
        }
    }
}
