using System;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 插槽在物理空间中的挂载/插入方向位域枚举。
    /// </summary>
    [Flags]
    public enum SlotDirection
    {
        None = 0,
        Up = 1 << 0,         // 垂直朝上 (地板、桌面)
        Down = 1 << 1,       // 垂直朝下 (天花板、柜底)
        Horizontal = 1 << 2, // 水平方向 (墙面)
        Any = Up | Down | Horizontal
    }
}
