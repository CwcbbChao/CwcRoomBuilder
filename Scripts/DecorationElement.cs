using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 装饰摆件的离散/连续随机旋转模式。
    /// </summary>
    public enum DecorationRotationMode
    {
        /// <summary> 固定不自转 (0°) - 严格对齐插槽或墙面 </summary>
        None = 0,

        /// <summary> 二向对齐 (0°, 180°) - 双向长凳、双通地毯 </summary>
        TwoWay180 = 1,

        /// <summary> 四向对齐 (0°, 90°, 180°, 270°) - 方凳、箱子 </summary>
        FourWay90 = 2,

        /// <summary> 八向对齐 (0°, 45°, 90°, 135°...) - 斜向散落物品 </summary>
        EightWay45 = 3,

        /// <summary> 完全连续随机 (0°~360°) - 落叶、水渍、圆形花盆、杂物 </summary>
        FullContinuous = 4
    }

    /// <summary>
    /// 装饰元素变体配置。
    /// 继承自 RoomElement 基类，特化了用于数量区间控制、同款隔离半径以及随机旋转缩放的参数。
    /// </summary>
    [System.Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Cwcbb.Tools.RoomBuilderV2", "CwcRoomBuilder", null)]
    public class DecorationElement : RoomElement
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("投放数量与空间限制")]
        [Tooltip("在当前房间中，此装饰品所允许生成的最大与最小数量区间限制")]
        [SerializeField]
        private Vector2Int _amountRange = new Vector2Int(1, 3);

        [Tooltip("该预制件同款物品自身的安全排斥半径。以此为半径内将禁止生成同款物品，用于控制同种物品在房间内的平均稀疏分布")]
        [SerializeField]
        private float _spacing = 1.0f;

        [Header("随机变换控制")]
        [Tooltip("在区间内进行随机的缩放大小计算")]
        [SerializeField]
        private Vector2 _scaleRange = new Vector2(0.9f, 1.1f);

        [Tooltip("装饰摆件的随机旋转模式 (None=固定 0°, TwoWay180=二向, FourWay90=四向, EightWay45=八向, FullContinuous=完全连续 360°)")]
        [SerializeField]
        private DecorationRotationMode _rotationMode = DecorationRotationMode.FullContinuous;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取生成数量上限与下限。
        /// </summary>
        public Vector2Int AmountRange => _amountRange;

        /// <summary>
        /// 获取安全隔离半径。
        /// </summary>
        public float Spacing => _spacing;

        /// <summary>
        /// 获取随机缩放区间。
        /// </summary>
        public Vector2 ScaleRange => _scaleRange;

        /// <summary>
        /// 获取装饰摆件的随机旋转模式。
        /// </summary>
        public DecorationRotationMode RotationMode => _rotationMode;

        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle)
        // 当前类无生命周期方法
        #endregion

        #region 6. 公共方法 (Public Methods)

        /// <summary>
        /// 重置此装饰件的所有字段（包含基类字段）为初始默认配置。
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _amountRange = new Vector2Int(1, 3);
            _spacing = 1.0f;
            _scaleRange = new Vector2(0.9f, 1.1f);
            _rotationMode = DecorationRotationMode.FullContinuous;
        }

        #endregion

        #region 7. 私有方法 (Private Methods)
        // 当前类无私有方法
        #endregion
    }
}
