using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 范围门销系统组件。
    /// 挂载此组件的游戏对象在 3D 场景中表现为一个包围盒，能自动裁减掉生成时重合的墙体节点以开辟通道。
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Cwcbb.Tools.RoomBuilderV2", "CwcRoomBuilder", null)]
    [AddComponentMenu("Cwcbb/Room Builder/Door Pin")]
    public class DoorPin : MonoBehaviour
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("关联与物理配置")]
        [Tooltip("匹配的房间生成器标识 ID。只有与 RoomBuilder 的 generatorId 一致的 DoorPin 才会生效。")]
        public int generatorId = 0;

        [Tooltip("门销的影响范围大小（长、宽、高）。")]
        public Vector3 boundsSize = new Vector3(3f, 3f, 3f);

        [Tooltip("门销的本地坐标位置偏移（锚点偏置）")]
        public Vector3 positionOffset = Vector3.zero;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)
        // 当前类无公共属性
        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle & Editor Callbacks)

        /// <summary>
        /// 当游戏对象被加载或在编辑器中绘制 Gizmos 时被调用，绘制包围盒。
        /// </summary>
        protected virtual void OnDrawGizmos()
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Vector3 center = positionOffset + boundsSize * 0.5f;

            Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f);
            Gizmos.DrawWireCube(center, boundsSize);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(positionOffset, 0.12f);

            if (positionOffset != Vector3.zero)
            {
                Gizmos.DrawLine(Vector3.zero, positionOffset);
            }

            Gizmos.matrix = oldMatrix;
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
