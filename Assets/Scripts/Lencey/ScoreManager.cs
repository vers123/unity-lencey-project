using UnityEngine;

namespace Lencey
{
    /// <summary>
    /// 分数管理器：场景里只需要一个，用静态字段 Instance 暴露给所有脚本访问。
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        // 单例实例：整个游戏中只有一个 ScoreManager，别的脚本通过 ScoreManager.Instance 调用它
        public static ScoreManager Instance { get; private set; }

        public int CurrentScore { get; private set; }   // 当前分数（只读，外部只能看不能改）

        /// <summary>
        /// Awake 里做单例的 "抢占"：如果已经有一个实例存在，就销毁自己。
        /// 这是经典的 "Singleton Pattern"，保证场景里不会同时存在多个管理器。
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this) 
            {
                Destroy(gameObject);    // 已有别的 ScoreManager 存在，把重复的自己销毁
                return;
            }

            Instance = this;    // 把自己登记为唯一实例
        }

        /// <summary>
        /// 加分。外部脚本（比如 Coin）通过 ScoreManager.Instance.AddScore(n) 调用
        /// </summary>
        public void AddScore(int amount) 
        {
            CurrentScore += amount;
            Debug.Log($"分数更新：{CurrentScore}");  // 暂时用日志看效果，下一课换成 UI 显示
        }
    }
}
