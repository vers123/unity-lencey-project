using UnityEngine;                  // Unity 基础命名空间：MonoBehaviour、Transform、Rigidbody、Physics 等// Unity 基础命名空间：MonoBehaviour、Transform、Rigidbody、Physics 等
using UnityEngine.InputSystem;      // 新版输入系统（Input System 包）的命名空间

// 命名空间：给脚本分类、避免类名冲突。
// 注意 Unity 的硬性规则是 「类名必须等于文件名」 （Tutorial.cs → class Tutorial），命名空间不影响这条规则
namespace Lencey 
{
    /// <summary>
    /// 教程脚本：用 WASD 在地面移动一个带物理的物体，空格跳跃（只有落地才能跳）。
    /// </summary>
    /// <remarks>
    /// 【生命周期执行顺序速查】
    /// Awake                   → 对象创建时，早于 Start，常用来取组件引用
    /// OnEnable                → 对象每次被启用时
    /// Start                   → 第一帧之前，只执行一次
    /// Update                  → 每帧一次（频率由帧率决定）—— 读输入放这里
    /// FixedUpdate             → 固定时间步（默认 0.02s，即每秒 50 次）—— 物理放这里
    /// LateUpdate              → 每帧最后（Update 之后）—— 相机跟随放这里
    /// OnDisable / OnDestroy   → 禁用 / 销毁时做清理
    /// </remarks>
    [RequireComponent(typeof(Rigidbody))]   // 挂本脚本时自动补上 Rigidbody，防止忘记加组件
    public class Tutorial : MonoBehaviour 
    {
        // ===== Inspector 可调参数 =====
        // [SerializeField] 让 private 字段也显示在 Inspector 面板上，可在编辑器里直接调值，
        // 不用改代码重新编译；同时 private 保证别的脚本不能乱改。
        [SerializeField] private float moveSpeed = 5f;              // 跳跃速度（米/秒）
        [SerializeField] private float jumpForce = 5f;              // 跳跃冲量大小
        [SerializeField] private float groundCheckMargin = 0.1f;    // 落地检测余量，防止贴地瞬间漏判
        [SerializeField] private LayerMask groundMask = ~0;         // 射线只检测哪些层；建议只勾 Ground 层（~0 = 全部层）

        // ===== 运行时应用 =====
        private Rigidbody rb;                   // 刚体：负责物理移动与受力。不能用 transform 代替
        private Collider col;                   // 碰撞体：用来量物体的实际半高（bounds.extents.y）
        private InputSystem_Actions actions;    // 由 .inputactions 自动生成的输入包装类，不要手动修改它

        // ===== 状态 =====
        private Vector2 moveInput;              // 当前输入方向：x = 左右(A/D)，y = 前后(W/S)
        private bool jumpRequested;             // 跳跃请求：时间回调里置 ture，在 FixedUpdate 里消费

        /// <summary>
        /// Awake：对象被创建时调用（早于 Start）。适合初始化「自己」—— 比如用组件引用。
        /// </summary>
        private void Awake() 
        {
            // GetComponent<T>() 从当前 GameObject 上取组件，比在 Inspector 里拖引用更快、更不易漏填
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();

            // 开启渲染插值：物理按 0.02s 推进、渲染按帧率刷新，插值让画面更顺滑，不会「一跳一跳」
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // 创建输入动作集实例（对应 InputSystem_Actions.inputactions 里的 Player / UI 两张动作图）
            actions = new InputSystem_Actions();


            // 按钮类动作适合用「事件」而不是每帧轮询：按下瞬间回调一次
            actions.Player.Jump.performed += OnJump;
        }

        // OnEnable / OnDisable 成对出现：启用对象时打开输入，禁用时关掉，避免重复回调和输入泄漏
        private void OnEnable() => actions.Player.Enable();

        private void OnDisable() => actions.Player.Disable();

        /// <summary>
        /// OnDestroy：对象被销毁时调用。这里做「反注册」，防止回调仍指向已销毁的对象。
        /// </summary>
        private void OnDestroy()
        {
            actions.Player.Jump.performed -= OnJump;    // 有 += 就要有 -=
            actions?.Dispose();
        }

        /// <summary>
        /// Update：每帧调用一次。输入属于「帧事件」，所以在这里读。
        /// </summary>
        private void Update()
        {
            // ReadValue<Vector2>()：把 Move 动作读成一个二维向量。
            // Move 已绑定 WASD / 方向键 / 手柄左摇杆，同一套代码自动兼容多种设备
            moveInput = actions.Player.Move.ReadValue<Vector2>();
        }

        /// <summary>
        /// FixedUpdate：固定时间步（本项目为 0.02s，即每秒 50 次），与帧率无关。
        /// 所以涉及 Rigidbody / 碰撞的代码都应写在这里。
        /// </summary>
        private void FixedUpdate() 
        {
            // 把二维输入转成三维向量：x → 左右，y → 前后(z)，不产生上下移动
            Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);

            // 用 MovePosition 移动：它走物理求解并做碰撞检测，所以不会穿墙。
            // 对比：transform.Translate / transform.position 直接改位置，物理引擎不知情，会穿墙。
            // 注意这里用 Time.fixedDeltaTime（固定步长），而不是 Time.deltaTime
            Vector3 target = rb.position + dir * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(target);

            // 消费跳跃请求
            if (jumpRequested) 
            {
                // 只有站在地上才能跳跃，否则就会变成无限二段跳
                if (IsGrounded()) 
                {
                    // AddForce + ForceMode.Impulse：施加一个「瞬间冲量」，表现为起跳速度
                    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                }

                // 无论这次有没有跳跃成功，都清掉请求，避免请求堆积到落地后一次性生效
                jumpRequested = false;
            }
        }

        /// <summary>
        /// 落地检测：从物体中心向下发一条射线，命中地面说明站在地上。
        /// </summary>
        /// <returns>true 表示站在地面上</returns>
        private bool IsGrounded() 
        {
            // bounds.extents.y 时碰撞体的「世界坐标半高」（默认 Cube 为 0.5）。
            // 用它而不是写死数字，物体缩放后依然准确
            float distance = col.bounds.extents.y + groundCheckMargin;

            // Debug.DrawRay：只在编辑器的 Scene 视图里画一条红线，方便调试，不影响运行
            Debug.DrawRay(transform.position, Vector3.down * distance, Color.red);

            // Physics.Raycast(起点, 方向, 长度, 检测层,是否命中触发器)
            // 返回 true 表示射线打到了东西。起点位于自身碰撞体内部时不会命中自己
            return Physics.Raycast(
                transform.position,
                Vector3.down,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// 跳跃动作的回调：按下空格时被调用一次，这里只置一个标记。
        /// 不要在回调里直接做物理运算——放到 FixedUpdate 里统一处理。
        /// </summary>
        /// <param name="ctx"></param>
        private void OnJump(InputAction.CallbackContext ctx) 
        {
            jumpRequested = true;
        }

        /// <summary>
        /// OnCollisionEnter：实体碰撞开始时触发（双方都要有 Collider，且至少一方有 Rigidbody）。
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log($"撞到了: {collision.gameObject.name}");
        }

        /// <summary>
        /// OnCollisionEnter：进入「触发器」时触发（Collider 勾选 Is Trigger，会穿透但会通知）。
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"进入了触发区: {other.name}");
        }
    }
}