using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 结构元素组配置资源（ScriptableObject）。
    /// 包含一系列结构变体元素、支持的体素暴露方向、物理 Layer 以及拓扑距离场比例区间与高低差配置。
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Cwcbb.Tools.RoomBuilderV2", "CwcRoomBuilder", null)]
    [CreateAssetMenu(fileName = "NewStructureGroup", menuName = "Cwcbb/RoomBuilder/Structure Group", order = 20)]
    public class StructureGroup : ScriptableObject
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("组物理层级设置")]
        [Tooltip("此结构组在生成时，其下所有实例化对象递归设置的目标 Layer")]
        [SerializeField]
        private LayerMask _layer;

        [Header("暴露方向匹配")]
        [Tooltip("此组所支持与适用的体素暴露方向位掩码")]
        [SerializeField]
        private VoxelFaceDirection _supportedDirections = VoxelFaceDirection.All;

        [Header("拓扑距离场与高度分层 (0.0 = 靠墙, 1.0 = 中心)")]
        [Tooltip("本组结构件生效的 SDF 归一化距离比例区间 [0.0, 1.0]。例如 (0, 0.3) 仅用于外圈 30% 边缘地砖，(0.6, 1.0) 用于中心地砖")]
        [SerializeField]
        private Vector2 _distanceRatioRange = new Vector2(0f, 1f);

        [Tooltip("本层级结构件在生成时自动抬升的 Y 轴高度 offset (单位：米)。例如设为 0.5 可使中心地砖生成为 raised high-platform 高台")]
        [SerializeField]
        private float _heightOffset = 0f;

        [Tooltip("是否跟随 2D 符号距离场 (SDF) 梯度方向对齐基准轴（开启后基准轴正前方指向房间内部，具体朝向由各变体的 RotationOffset 自由偏置）")]
        [SerializeField]
        private bool _followSdfGradient = false;

        [Header("结构瓦片资产列表")]
        [Tooltip("此组包含的全部结构件摆放规则列表")]
        [SerializeField]
        private List<StructureElement> _elements = new List<StructureElement>();

        [Header("自动插槽设置")]
        [Tooltip("此结构组生成的瓦片所提供的虚拟插槽类型，若为空则不提供")]
        [SerializeField]
        private SlotType _providedSlotType;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取统一递归应用的目标 Layer 掩码。
        /// </summary>
        public LayerMask Layer => _layer;

        /// <summary>
        /// 获取支持的暴露方向位掩码。
        /// </summary>
        public VoxelFaceDirection SupportedDirections => _supportedDirections;

        /// <summary>
        /// 获取 SDF 距离场归一化比例区间 [0.0, 1.0]。
        /// </summary>
        public Vector2 DistanceRatioRange => _distanceRatioRange;

        /// <summary>
        /// 获取高低差 Y 轴 offset。
        /// </summary>
        public float HeightOffset => _heightOffset;

        /// <summary>
        /// 获取是否跟随 SDF 梯度方向对齐基准轴。
        /// </summary>
        public bool FollowSdfGradient => _followSdfGradient;

        /// <summary>
        /// 获取组内包含的结构件规则列表。
        /// </summary>
        public List<StructureElement> Elements => _elements;

        /// <summary>
        /// 获取生成的瓦片所提供的虚拟插槽类型。
        /// </summary>
        public SlotType ProvidedSlotType => _providedSlotType;

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
