using System;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 体素表面暴露朝向位掩码。
    /// 标记为 System.Flags，支持在 Inspector 中进行多选。
    /// </summary>
    [Flags]
    public enum VoxelFaceDirection
    {
        None = 0,
        Up = 1 << 0,      // 1 顶面 (天花板)
        Down = 1 << 1,    // 2 底面 (地板)
        Forward = 1 << 2, // 4 前墙 (北)
        Back = 1 << 3,    // 8 后墙 (南)
        Left = 1 << 4,    // 16 左墙 (西)
        Right = 1 << 5,   // 32 右墙 (东)
        Horizontal = Forward | Back | Left | Right, // 60 水平墙面组合
        All = Up | Down | Horizontal                // 63 全朝向
    }
}
