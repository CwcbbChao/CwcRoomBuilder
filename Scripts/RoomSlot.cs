using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 挂载在结构件或摆件预制件子节点上的插槽组件。
    /// 声明该物体在此处对外暴露一个指定类型的插槽接口。
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Cwcbb.Tools.RoomBuilderV2", "CwcRoomBuilder", null)]
    [AddComponentMenu("Cwcbb/Room Builder/Room Slot")]
    public class RoomSlot : MonoBehaviour
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("插槽性质配置")]
        [Tooltip("引用的插槽类型（SocketSO）")]
        [SerializeField]
        private SlotType _slotType;

        [Header("投放规则")]
        [Tooltip("此插槽在生成摆件时的概率限制（0-1 之间）")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _spawnChance = 1f;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取引用的插槽类型（SocketSO）。
        /// </summary>
        public SlotType SlotType => _slotType;

        /// <summary>
        /// 获取当前插槽的物理方向。
        /// </summary>
        public SlotDirection Direction => _slotType != null ? _slotType.DefaultDirection : SlotDirection.Up;

        /// <summary>
        /// 获取此插槽生成摆件的概率。
        /// </summary>
        public float SpawnChance => _spawnChance;

        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle & Editor Callbacks)

        private void OnDrawGizmos()
        {
            if (_slotType == null)
            {
                Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.5f);
                Gizmos.DrawSphere(transform.position, 0.15f);
                return;
            }

            Color color;
            Vector3 directionVector;

            switch (Direction)
            {
                case SlotDirection.Up:
                    color = Color.green;
                    directionVector = transform.up;
                    break;
                case SlotDirection.Down:
                    color = Color.yellow;
                    directionVector = -transform.up;
                    break;
                case SlotDirection.Horizontal:
                    color = Color.cyan;
                    directionVector = transform.forward;
                    break;
                default:
                    color = Color.white;
                    directionVector = transform.forward;
                    break;
            }

            color.a = 0.8f;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position, 0.12f);

            Gizmos.color = new Color(color.r, color.g, color.b, 0.9f);
            Vector3 arrowEnd = transform.position + directionVector * 0.4f;
            Gizmos.DrawLine(transform.position, arrowEnd);
            Gizmos.DrawWireSphere(arrowEnd, 0.04f);
        }

        #endregion

        #region 6. 公共方法 (Public Methods)
        // 当前类无公共方法
        #endregion

        #region 7. 私有方法 (Private Methods)
        // 当前类无私有方法
        #endregion
    }
}
