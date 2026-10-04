using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 插槽挂点类型定义资源 (SocketSO)。
    /// 作为结构件与装饰品之间的挂载接口协议，标识物理挂载表面类型 (Up, Down, Horizontal)。
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Cwcbb.Tools.RoomBuilderV2", "CwcRoomBuilder", null)]
    [CreateAssetMenu(fileName = "NewSlotType", menuName = "Cwcbb/RoomBuilder/Slot Type", order = 10)]
    public class SlotType : ScriptableObject
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("基础物理方向")]
        [Tooltip("此插槽类型自带的默认挂载表面方向 (Up 顶面, Down 底面, Horizontal 侧立面)")]
        [SerializeField]
        private SlotDirection _defaultDirection = SlotDirection.Up;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取此插槽的默认物理表面方向。
        /// </summary>
        public SlotDirection DefaultDirection => _defaultDirection;

        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle)
        // 当前类无生命周期方法
        #endregion

        #region 6. 公共方法 (Public Methods)
        // 当前类无公共方法
        #endregion

        #region 7. 私有方法 (Private Methods)
        // 当前类无私有方法
        #endregion
    }
}
