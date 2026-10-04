using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 装饰元素组配置资源（ScriptableObject）。
    /// 包含一系列装饰变体元素，定义目标匹配挂点以及生成的二级插槽类型。
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Cwcbb.Tools.RoomBuilderV2", "CwcRoomBuilder", null)]
    [CreateAssetMenu(fileName = "NewDecorationGroup", menuName = "Cwcbb/RoomBuilder/Decoration Group", order = 30)]
    public class DecorationGroup : ScriptableObject
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("组物理层级设置")]
        [Tooltip("此组中的全部装饰品在生成时，其下所有实例化对象递归设置的目标 Layer")]
        [SerializeField]
        private LayerMask _layer;

        [Header("拓扑距离场与参考坐标系 (0.0 = 靠墙, 1.0 = 中心)")]
        [Tooltip("本组装饰品生效的 SDF 归一化距离比例区间 [0.0, 1.0]。例如 (0, 0.3) 为靠墙 30% 区域，(0.6, 1.0) 为室内中心 40% 区域")]
        [SerializeField]
        private Vector2 _distanceRatioRange = new Vector2(0f, 1f);

        [Tooltip("是否跟随 2D 符号距离场 (SDF) 梯度方向对齐基准轴（开启后基准轴正前方指向房间内部，具体朝向由各变体的 RotationOffset 自由偏置）")]
        [SerializeField]
        private bool _followSdfGradient = false;

        [Tooltip("仅用于墙面侧向挂点：离墙角/拐角的最小避让保留边距 (单位：米)")]
        [SerializeField]
        private float _wallCornerPadding = 0f;

        [Header("物理体积与空间避让")]
        [Tooltip("此组物品是否为具有物理体积的实体摆件 (如家具/箱子/石柱)。\n" +
                 "开启：系统全自动基于 3D 几何包围盒进行相交硬拦截，确保实体之间绝不穿模；\n" +
                 "关闭：视为平面贴花/轻量散落物 (如传单/水渍)，无厚度自由贴附。")]
        [SerializeField]
        private bool _isVolumeObject = true;

        [Tooltip("此组物品是否避让门通道区域 (家具大件建议开启，地面传单/污渍建议关闭)")]
        [SerializeField]
        private bool _avoidDoorPassage = true;

        [Tooltip("是否开启 3D 包围盒防穿墙安全校验 (仅实体体积大件需要开启)")]
        [SerializeField]
        private bool _preventWallPenetration = true;

        [Header("分布摆放策略")]
        [Tooltip("物品在插槽中的排布策略：Random 为自然随机散布，Uniform 为等间距规律步进排列")]
        [SerializeField]
        private DistributionPattern _distributionPattern = DistributionPattern.Random;

        [Tooltip("当处于 Uniform 等间距排列模式时的插槽取样步长 (例如为 2 表示每隔 1 个挂点放一个)")]
        [SerializeField]
        [Min(1)]
        private int _uniformStride = 2;

        [Header("装饰摆件资产列表")]
        [Tooltip("此组包含的所有摆件的配置投放规则列表")]
        [SerializeField]
        private List<DecorationElement> _decorations = new List<DecorationElement>();

        [Header("自动插槽设置")]
        [Tooltip("此组内的所有装饰元素统一匹配并挂载的父级插槽类型列表")]
        [SerializeField]
        private List<SlotType> _allowedSlots = new List<SlotType>();

        [Tooltip("此组物品生成后，其上提供的二级虚拟插槽类型，若为空则代表该物品无法再承载任何其他物体")]
        [SerializeField]
        private SlotType _providedSlotType;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取此装饰组统一递归应用的目标 Layer 掩码。
        /// </summary>
        public LayerMask Layer => _layer;

        /// <summary>
        /// 获取 SDF 距离场归一化比例区间 [0.0, 1.0]。
        /// </summary>
        public Vector2 DistanceRatioRange => _distanceRatioRange;

        /// <summary>
        /// 获取是否跟随 SDF 梯度方向对齐基准轴。
        /// </summary>
        public bool FollowSdfGradient => _followSdfGradient;

        /// <summary>
        /// 获取墙角避让保留边距。
        /// </summary>
        public float WallCornerPadding => _wallCornerPadding;

        /// <summary>
        /// 获取此组是否为实体体积摆件。
        /// </summary>
        public bool IsVolumeObject => _isVolumeObject;

        /// <summary>
        /// 获取此组是否避让门通道区域。
        /// </summary>
        public bool AvoidDoorPassage => _avoidDoorPassage;

        /// <summary>
        /// 获取此组是否开启防穿墙安全校验。
        /// </summary>
        public bool PreventWallPenetration => _preventWallPenetration;

        /// <summary>
        /// 获取此组的分布排布策略模式。
        /// </summary>
        public DistributionPattern DistributionPattern => _distributionPattern;

        /// <summary>
        /// 获取 Uniform 模式下的等间距取样步长。
        /// </summary>
        public int UniformStride => _uniformStride;

        /// <summary>
        /// 获取此组包含的装饰物规则列表。
        /// </summary>
        public List<DecorationElement> Decorations => _decorations;

        /// <summary>
        /// 获取此装饰组允许匹配的父级插槽类型列表。
        /// </summary>
        public List<SlotType> AllowedSlots => _allowedSlots;

        /// <summary>
        /// 获取此装饰组生成的物品上提供的二级虚拟插槽类型。
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
