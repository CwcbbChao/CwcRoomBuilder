using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 结构网格瓦片的离散对齐旋转模式。
    /// 仅提供与正方形网格兼容的 90 度倍数朝向，确保拼接绝不漏缝。
    /// </summary>
    public enum StructureRotationMode
    {
        /// <summary> 固定朝向不自转 (0°) </summary>
        None = 0,

        /// <summary> 二向对齐 (0°, 180°) - 适用于条纹/木纹地砖打乱 </summary>
        TwoWay180 = 1,

        /// <summary> 四向对齐 (0°, 90°, 180°, 270°) - 适用于中心对称方砖打乱 </summary>
        FourWay90 = 2
    }

    /// <summary>
    /// 结构元素变体配置。
    /// 继承自 RoomElement 基类，特化了用于体素网格无缝拼接对齐与加权随机的物理参数。
    /// </summary>
    [System.Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Cwcbb.Tools.RoomBuilderV2", "CwcRoomBuilder", null)]
    public class StructureElement : RoomElement
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("结构瓦片特有对齐配置")]
        [Tooltip("确定的轴向缩放覆盖，在生成时硬性应用于逻辑轴点父物体上。确保拼接时严丝合缝")]
        [SerializeField]
        private Vector3 _scaleOverride = Vector3.one;

        [Tooltip("结构件离散网格旋转模式 (None=固定 0°, TwoWay180=二向 0°/180°, FourWay90=四向 0°/90°/180°/270°)")]
        [SerializeField]
        private StructureRotationMode _rotationMode = StructureRotationMode.None;

        [Tooltip("加权生成的概率权重。权重越高，被随机选中的概率越大")]
        [SerializeField]
        [Range(0, 1000)]
        private int _weight = 100;

        #endregion

        #region 3. 非序列化私有字段
        // 当前类无非序列化私有字段
        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取强制缩放覆盖值。
        /// </summary>
        public Vector3 ScaleOverride => _scaleOverride;

        /// <summary>
        /// 获取结构件离散网格旋转模式。
        /// </summary>
        public StructureRotationMode RotationMode => _rotationMode;

        /// <summary>
        /// 获取生成的加权随机权重。
        /// </summary>
        public int Weight => _weight;

        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle)
        // 当前类无生命周期方法
        #endregion

        #region 6. 公共方法 (Public Methods)

        /// <summary>
        /// 重置此结构件的所有字段（包含基类字段）为初始默认配置。
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _weight = 100;
            _scaleOverride = Vector3.one;
            _rotationMode = StructureRotationMode.None;
        }

        #endregion

        #region 7. 私有方法 (Private Methods)
        // 当前类无私有方法
        #endregion
    }
}
