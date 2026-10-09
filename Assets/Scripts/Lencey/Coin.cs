using UnityEngine;

namespace Lencey
{
    /// <summary>
    /// 金币：被玩家碰到就销毁自己，并给分数管理器加分。
    /// 要求：挂这个脚本的物体 Collider 必须勾选 Is Trigger。
    /// </summary>
    public class Coin : MonoBehaviour
    {
        [SerializeField] private int value = 1;             // 这枚金币值多少分，在 Inspector 可调
        [SerializeField] private float spinSpeed = 120f;    // 自传速度（度/秒），装饰效果

        private void Update()
        {
            // 绕世界 Y 轴自转，让金币看起来在转
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        /// <summary>
        /// OnTriggerEnter：有别的 Collider 进入这个触发器时调用。
        /// 要求：本物体 Collider 勾选 Is Trigger：进入的那一方至少有一个 Rigidbody。
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // 用 CompareTag 比 gameObject.CompareTag 更安全、性能略好。
            // 玩家物体需要在 Inspector 里把 Tag 设成 "Player"（要先在 Tags 里新建）。
            if (other.CompareTag("Player")) 
            {
                Collect();
            }
        }

        private void Collect() 
        {
            // 通过单例拿到分数管理器，加 value 分
            // 注意：ScoreManager 必须存在与场景里，负责 Instance 为 null，这里会报错
            ScoreManager.Instance.AddScore(value);

            // 销毁自己。Destroy 是在下一帧统一销毁，不会在当帧逻辑中立刻移除
            Destroy(gameObject);
        }
    }
}
