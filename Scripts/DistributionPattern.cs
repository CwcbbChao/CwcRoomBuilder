namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 装饰品在挂点中的排布分布策略。
    /// </summary>
    public enum DistributionPattern
    {
        /// <summary>
        /// 随机自然散布：打乱插槽顺序，适合杂物、传单、生活化散落物。
        /// </summary>
        Random = 0,

        /// <summary>
        /// 等间距规律步进：保持插槽几何空间顺序，按固定步长等距排列，适合对称立柱、等距壁灯、整齐排列的长椅。
        /// </summary>
        Uniform = 1
    }
}
