using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 核心房间生成器组件 (RoomBuilder)。
    /// 完整保留 Pivot 逻辑父锚点与 offsetFraction 贴面偏置规范，
    /// 融汇 2D 归一化符号距离场 (Normalized SDF)、梯度向量场 (Grad D) 90度离散朝向对齐，
    /// 以及 3D OBB 包围盒边界防穿透防护。
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Cwcbb.Tools.RoomBuilderV2", "CwcRoomBuilder", "RoomBuilderV2")]
    [AddComponentMenu("Cwcbb/Room Builder/Room Builder")]
    public class RoomBuilder : MonoBehaviour
    {
        #region 1. 常量与静态字段

        /// <summary>
        /// 墙段结构体，用于计算 2D 距离场与梯度向量。
        /// </summary>
        public struct WallSegment
        {
            public Vector2 start;
            public Vector2 end;
        }

        /// <summary>
        /// 虚拟插槽点数据。
        /// </summary>
        public class VirtualSlot
        {
            public SlotType SlotType;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        private struct OccupiedDecorData
        {
            public Vector3 position;
            public float spacing;
            public DecorationElement config;
            public bool isVolumeObject;
            public Bounds worldBounds;
        }

        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("数据预设")]
        [Tooltip("房间美术资源配置预设，里面包含了结构件组列表和装饰品组列表")]
        [SerializeField]
        private RoomPreset _preset;

        [Header("网格画板尺寸")]
        [Tooltip("画板的宽度（格子数量）")]
        [SerializeField]
        [Range(1, 100)]
        private int _canvasWidth = 10;

        [Tooltip("画板的高度（格子数量）")]
        [SerializeField]
        [Range(1, 100)]
        private int _canvasHeight = 10;

        [Tooltip("网格中单个格子的物理物理大小（米）")]
        [SerializeField]
        private float _tileSize = 3.0f;

        [Tooltip("房间的层高物理高度")]
        [SerializeField]
        private float _roofHeight = 3.0f;

        [Header("装饰规则配置")]
        [Tooltip("装饰品插槽递归嵌套生成的最大层数限制")]
        [SerializeField]
        [Range(1, 5)]
        private int _maxRecursionDepth = 2;

        [Tooltip("房间生成器标识 ID，用于与 DoorPin 的 generatorId 进行匹配以开辟门通道")]
        [SerializeField]
        private int _generatorId = 0;

        [Header("虚拟插槽切分设置")]
        [Tooltip("自动生成虚拟插槽的切分网格步长（米）")]
        [SerializeField]
        private float _slotStepSize = 0.5f;

        [Tooltip("虚拟插槽在水平面上的最大随机抖动偏移量（米）")]
        [SerializeField]
        private float _slotJitter = 0.05f;

        [Header("随机化设置")]
        [Tooltip("当前生成的随机数种子")]
        [SerializeField]
        private int _currentSeed = 123456;

        [HideInInspector]
        [Tooltip("扁平化的一维网格状态数组，供编辑器画板直接读写")]
        public int[] gridData;

        #endregion

        #region 3. 非序列化私有字段 (以 _ 开头)

        private readonly HashSet<Vector2Int> _activeGridCoords = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _doorGridCoords = new HashSet<Vector2Int>();
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
        private readonly List<(GameObject artObj, StructureGroup group)> _spawnedStructures = new List<(GameObject, StructureGroup)>();
        private readonly List<OccupiedDecorData> _occupiedDecorPositions = new List<OccupiedDecorData>();
        private readonly List<WallSegment> _allWallSegments = new List<WallSegment>();
        private readonly List<Vector2> _wallCornerPoints = new List<Vector2>();

        private float _maxRoomSdfDistance = 1.0f;
        private GameObject _roomContainer;

        #endregion

        #region 4. 公共属性 (Properties)

        public RoomPreset Preset => _preset;
        public int CanvasWidth => _canvasWidth;
        public int CanvasHeight => _canvasHeight;
        public float TileSize => _tileSize;
        public float RoofHeight => _roofHeight;
        public int CurrentSeed => _currentSeed;
        public int GeneratorId => _generatorId;

        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle)

        /// <summary>
        /// 在编辑器 Scene 视图中绘制网格辅助线与房间激活/门通道格子。
        /// </summary>
        private void OnDrawGizmos()
        {
            // 1. 绘制网格地表辅助参考线
            Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.25f);
            for (int x = 0; x <= _canvasWidth; x++)
            {
                Vector3 start = transform.TransformPoint(new Vector3(x * _tileSize, 0f, 0f));
                Vector3 end = transform.TransformPoint(new Vector3(x * _tileSize, 0f, _canvasHeight * _tileSize));
                Gizmos.DrawLine(start, end);
            }
            for (int z = 0; z <= _canvasHeight; z++)
            {
                Vector3 start = transform.TransformPoint(new Vector3(0f, 0f, z * _tileSize));
                Vector3 end = transform.TransformPoint(new Vector3(_canvasWidth * _tileSize, 0f, z * _tileSize));
                Gizmos.DrawLine(start, end);
            }

            // 2. 绘制画板中激活的格子 (房间空气格 / 门通道格)
            if (gridData != null && gridData.Length == _canvasWidth * _canvasHeight)
            {
                for (int x = 0; x < _canvasWidth; x++)
                {
                    for (int z = 0; z < _canvasHeight; z++)
                    {
                        int index = x + z * _canvasWidth;
                        int cellValue = gridData[index];
                        if (cellValue != 0)
                        {
                            Vector3 localCenter = new Vector3((x + 0.5f) * _tileSize, 0f, (z + 0.5f) * _tileSize);
                            Vector3 worldCenter = transform.TransformPoint(localCenter);
                            Vector3 size = new Vector3(_tileSize * 0.95f, 0f, _tileSize * 0.95f);

                            if (cellValue == 2) // 门通道格子 (橙色高亮)
                            {
                                Gizmos.color = new Color(1.0f, 0.55f, 0.0f, 0.85f);
                                Gizmos.DrawWireCube(worldCenter, size);
                            }
                            else // 普通房间格子 (天蓝色)
                            {
                                Gizmos.color = new Color(0.2f, 0.65f, 1.0f, 0.6f);
                                Gizmos.DrawWireCube(worldCenter, size);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region 6. 公共方法 (Public Methods)

        public void SetPreset(RoomPreset preset) => _preset = preset;

        public void ResizeGrid(int newWidth, int newHeight)
        {
            int[] newGrid = new int[newWidth * newHeight];
            if (gridData != null)
            {
                int minW = Mathf.Min(_canvasWidth, newWidth);
                int minH = Mathf.Min(_canvasHeight, newHeight);
                for (int y = 0; y < minH; y++)
                {
                    for (int x = 0; x < minW; x++)
                    {
                        newGrid[y * newWidth + x] = gridData[y * _canvasWidth + x];
                    }
                }
            }
            _canvasWidth = newWidth;
            _canvasHeight = newHeight;
            gridData = newGrid;
        }

        public void GenerateWithNewSeed()
        {
            _currentSeed = Random.Range(0, 999999);
            GenerateWithCurrentSeed();
        }

        public void GenerateWithCurrentSeed()
        {
            ClearGeneratedObjects();
            if (_preset == null) return;

            Random.InitState(_currentSeed);

            // 1. 构建基础网格数据与 DoorPin 门洞剔除
            CollectActiveCoordsAndDoors();

            // 2. 搜集房间所有墙段，建立 SDF 距离场与梯度向量场
            BuildWallSegmentsAndSdf();

            // 3. 创建托管容器
            _roomContainer = new GameObject($"[Room_{gameObject.name}]");
            _roomContainer.transform.SetParent(transform, false);

            // 4. 生成结构件 (地板、天花板、墙面)
            SpawnStructureTiles();

            // 5. 生成装饰品
            SpawnDecorations();
        }

        public void ClearGeneratedObjects()
        {
            if (_roomContainer != null)
            {
                DestroyImmediate(_roomContainer);
                _roomContainer = null;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("[Room_") || child.name.StartsWith("[Room_V2_"))
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            _activeGridCoords.Clear();
            _doorGridCoords.Clear();
            _spawnedObjects.Clear();
            _spawnedStructures.Clear();
            _occupiedDecorPositions.Clear();
            _allWallSegments.Clear();
            _wallCornerPoints.Clear();
        }

        public float GetSdfDistance(Vector2 p2D)
        {
            if (_allWallSegments.Count == 0) return 0f;
            float minSq = float.MaxValue;
            foreach (var seg in _allWallSegments)
            {
                float sq = PointToSegmentDistanceSquare(p2D, seg.start, seg.end);
                if (sq < minSq) minSq = sq;
            }
            return Mathf.Sqrt(minSq);
        }

        public float GetNormalizedDistanceRatio(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            Vector2 p2D = new Vector2(localPos.x, localPos.z);
            float dist = GetSdfDistance(p2D);
            if (_maxRoomSdfDistance <= 0.001f) return 0f;
            return Mathf.Clamp01(dist / _maxRoomSdfDistance);
        }

        public Vector2 GetGradientVector2D(Vector2 p2D)
        {
            float eps = 0.1f;
            float dRight = GetSdfDistance(p2D + new Vector2(eps, 0f));
            float dLeft = GetSdfDistance(p2D - new Vector2(eps, 0f));
            float dUp = GetSdfDistance(p2D + new Vector2(0f, eps));
            float dDown = GetSdfDistance(p2D - new Vector2(0f, eps));

            Vector2 grad = new Vector2((dRight - dLeft) / (2f * eps), (dUp - dDown) / (2f * eps));
            if (grad.sqrMagnitude < 0.0001f) return Vector2.zero;
            return grad.normalized;
        }

        public Vector2 GetDiscreteGradientVector2D(Vector2 p2D)
        {
            Vector2 rawGrad = GetGradientVector2D(p2D);
            if (rawGrad.sqrMagnitude < 0.001f) return Vector2.zero;

            float absX = Mathf.Abs(rawGrad.x);
            float absY = Mathf.Abs(rawGrad.y);

            if (Mathf.Abs(absX - absY) > 0.2f)
            {
                if (absX > absY)
                    return new Vector2(Mathf.Sign(rawGrad.x), 0f);
                else
                    return new Vector2(0f, Mathf.Sign(rawGrad.y));
            }

            // 45度角争议拐角：顺时针统一顺延
            if (rawGrad.x >= 0 && rawGrad.y >= 0)
                return new Vector2(1f, 0f);
            else if (rawGrad.x >= 0 && rawGrad.y < 0)
                return new Vector2(0f, -1f);
            else if (rawGrad.x < 0 && rawGrad.y < 0)
                return new Vector2(-1f, 0f);
            else
                return new Vector2(0f, 1f);
        }

        #endregion

        #region 7. 私有方法 (Private Methods)

        private void CollectActiveCoordsAndDoors()
        {
            _activeGridCoords.Clear();
            _doorGridCoords.Clear();

            if (gridData == null || gridData.Length != _canvasWidth * _canvasHeight) return;

            for (int y = 0; y < _canvasHeight; y++)
            {
                for (int x = 0; x < _canvasWidth; x++)
                {
                    int val = gridData[y * _canvasWidth + x];
                    if (val != 0)
                    {
                        _activeGridCoords.Add(new Vector2Int(x, y));
                        if (val == 2)
                        {
                            _doorGridCoords.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }

            // 查找场景中具有相同 generatorId 的 DoorPin 范围门销
            List<DoorPin> pins = new List<DoorPin>();
            DoorPin[] activePins = Object.FindObjectsByType<DoorPin>(FindObjectsSortMode.None);
            if (activePins != null) pins.AddRange(activePins);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                foreach (var pin in GetComponentsInChildren<DoorPin>(true))
                {
                    if (pin != null && !pins.Contains(pin)) pins.Add(pin);
                }

                Transform root = transform.root;
                if (root != null && root != transform)
                {
                    foreach (var pin in root.GetComponentsInChildren<DoorPin>(true))
                    {
                        if (pin != null && !pins.Contains(pin)) pins.Add(pin);
                    }
                }
            }
#endif

            foreach (var pin in pins)
            {
                if (pin == null || pin.generatorId != _generatorId) continue;

                foreach (var coord in _activeGridCoords)
                {
                    if (IsGridOverlappingWithDoorPin(coord.x, coord.y, pin))
                    {
                        _doorGridCoords.Add(coord);
                    }
                }
            }
        }

        private bool IsGridOverlappingWithDoorPin(int x, int z, DoorPin pin)
        {
            if (pin == null) return false;

            float xMin = x * _tileSize;
            float xMax = (x + 1f) * _tileSize;
            float yMin = 0f;
            float yMax = _roofHeight;
            float zMin = z * _tileSize;
            float zMax = (z + 1f) * _tileSize;

            Vector3[] gridCorners = new Vector3[8];
            gridCorners[0] = new Vector3(xMin, yMin, zMin);
            gridCorners[1] = new Vector3(xMax, yMin, zMin);
            gridCorners[2] = new Vector3(xMin, yMin, zMax);
            gridCorners[3] = new Vector3(xMax, yMin, zMax);
            gridCorners[4] = new Vector3(xMin, yMax, zMin);
            gridCorners[5] = new Vector3(xMax, yMax, zMin);
            gridCorners[6] = new Vector3(xMin, yMax, zMax);
            gridCorners[7] = new Vector3(xMax, yMax, zMax);

            float gridLocalXMin = float.MaxValue, gridLocalXMax = float.MinValue;
            float gridLocalYMin = float.MaxValue, gridLocalYMax = float.MinValue;
            float gridLocalZMin = float.MaxValue, gridLocalZMax = float.MinValue;

            for (int i = 0; i < 8; i++)
            {
                Vector3 worldPt = transform.TransformPoint(gridCorners[i]);
                Vector3 localPt = pin.transform.InverseTransformPoint(worldPt);

                if (localPt.x < gridLocalXMin) gridLocalXMin = localPt.x;
                if (localPt.x > gridLocalXMax) gridLocalXMax = localPt.x;
                if (localPt.y < gridLocalYMin) gridLocalYMin = localPt.y;
                if (localPt.y > gridLocalYMax) gridLocalYMax = localPt.y;
                if (localPt.z < gridLocalZMin) gridLocalZMin = localPt.z;
                if (localPt.z > gridLocalZMax) gridLocalZMax = localPt.z;
            }

            float pinMinX = pin.positionOffset.x;
            float pinMaxX = pin.positionOffset.x + pin.boundsSize.x;
            float pinMinY = pin.positionOffset.y;
            float pinMaxY = pin.positionOffset.y + pin.boundsSize.y;
            float pinMinZ = pin.positionOffset.z;
            float pinMaxZ = pin.positionOffset.z + pin.boundsSize.z;

            float overlapX = Mathf.Min(pinMaxX, gridLocalXMax) - Mathf.Max(pinMinX, gridLocalXMin);
            float overlapY = Mathf.Min(pinMaxY, gridLocalYMax) - Mathf.Max(pinMinY, gridLocalYMin);
            float overlapZ = Mathf.Min(pinMaxZ, gridLocalZMax) - Mathf.Max(pinMinZ, gridLocalZMin);

            if (overlapX <= 0f || overlapY <= 0f || overlapZ <= 0f) return false;

            float threshold = _tileSize * 0.1f;
            float requiredOverlapX = Mathf.Min(pin.boundsSize.x * 0.99f, threshold);
            float requiredOverlapZ = Mathf.Min(pin.boundsSize.z * 0.99f, threshold);

            return (overlapX >= requiredOverlapX && overlapZ >= requiredOverlapZ);
        }

        private void BuildWallSegmentsAndSdf()
        {
            _allWallSegments.Clear();
            _wallCornerPoints.Clear();
            _maxRoomSdfDistance = 0.001f;

            if (_activeGridCoords.Count == 0) return;

            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            HashSet<Vector2> uniqueCorners = new HashSet<Vector2>();

            foreach (var cell in _activeGridCoords)
            {
                foreach (var d in dirs)
                {
                    Vector2Int neighbor = cell + d;
                    if (!_activeGridCoords.Contains(neighbor))
                    {
                        GetWallSegmentFromEdge(cell, d, out Vector2 segStart, out Vector2 segEnd);
                        _allWallSegments.Add(new WallSegment { start = segStart, end = segEnd });
                        uniqueCorners.Add(segStart);
                        uniqueCorners.Add(segEnd);
                    }
                }
            }

            _wallCornerPoints.AddRange(uniqueCorners);

            foreach (var cell in _activeGridCoords)
            {
                Vector2 center2D = new Vector2((cell.x + 0.5f) * _tileSize, (cell.y + 0.5f) * _tileSize);
                float dist = GetSdfDistance(center2D);
                if (dist > _maxRoomSdfDistance) _maxRoomSdfDistance = dist;
            }
        }

        private void GetWallSegmentFromEdge(Vector2Int cell, Vector2Int dir, out Vector2 start, out Vector2 end)
        {
            float minX = cell.x * _tileSize;
            float maxX = (cell.x + 1) * _tileSize;
            float minZ = cell.y * _tileSize;
            float maxZ = (cell.y + 1) * _tileSize;

            if (dir == Vector2Int.up)
            {
                start = new Vector2(minX, maxZ); end = new Vector2(maxX, maxZ);
            }
            else if (dir == Vector2Int.down)
            {
                start = new Vector2(maxX, minZ); end = new Vector2(minX, minZ);
            }
            else if (dir == Vector2Int.left)
            {
                start = new Vector2(minX, minZ); end = new Vector2(minX, maxZ);
            }
            else
            {
                start = new Vector2(maxX, maxZ); end = new Vector2(maxX, minZ);
            }
        }

        private void SpawnStructureTiles()
        {
            if (_preset == null || _preset.StructureGroups == null) return;

            foreach (var coord in _activeGridCoords)
            {
                int x = coord.x;
                int z = coord.y;

                Vector2 cellCenter2D = new Vector2((x + 0.5f) * _tileSize, (z + 0.5f) * _tileSize);
                float ratio = GetNormalizedDistanceRatio(transform.TransformPoint(new Vector3(cellCenter2D.x, 0, cellCenter2D.y)));

                // 1. 地板 (Down 暴露面 - 核心中点 0.5, 0.5)
                TrySpawnStructureFace(x, z, 0f, Quaternion.identity, VoxelFaceDirection.Down, ratio, new Vector3(0.5f, 0f, 0.5f));

                // 2. 天花板 (Up 暴露面 - 核心中点 0.5, 0.5)
                TrySpawnStructureFace(x, z, _roofHeight, Quaternion.identity, VoxelFaceDirection.Up, ratio, new Vector3(0.5f, 0f, 0.5f));

                // 3. 4 向侧墙 (遵循旧版精确的 offsetFraction 与旋转角度)
                bool isDoorCell = _doorGridCoords.Contains(coord);
                if (!isDoorCell)
                {
                    bool southEmpty = !_activeGridCoords.Contains(new Vector2Int(x, z - 1));
                    bool westEmpty = !_activeGridCoords.Contains(new Vector2Int(x - 1, z));
                    bool northEmpty = !_activeGridCoords.Contains(new Vector2Int(x, z + 1));
                    bool eastEmpty = !_activeGridCoords.Contains(new Vector2Int(x + 1, z));

                    // 南面墙体 (Back) - (0.5, 0, 0.0) 0度
                    if (southEmpty)
                    {
                        TrySpawnStructureFace(x, z, 0f, Quaternion.Euler(0f, 0f, 0f), VoxelFaceDirection.Back, ratio, new Vector3(0.5f, 0f, 0.0f));
                    }
                    // 西面墙体 (Left) - (0.0, 0, 0.5) 90度
                    if (westEmpty)
                    {
                        TrySpawnStructureFace(x, z, 0f, Quaternion.Euler(0f, 90f, 0f), VoxelFaceDirection.Left, ratio, new Vector3(0.0f, 0f, 0.5f));
                    }
                    // 北面墙体 (Forward) - (0.5, 0, 1.0) 180度
                    if (northEmpty)
                    {
                        TrySpawnStructureFace(x, z, 0f, Quaternion.Euler(0f, 180f, 0f), VoxelFaceDirection.Forward, ratio, new Vector3(0.5f, 0f, 1.0f));
                    }
                    // 东面墙体 (Right) - (1.0, 0, 0.5) 270度
                    if (eastEmpty)
                    {
                        TrySpawnStructureFace(x, z, 0f, Quaternion.Euler(0f, 270f, 0f), VoxelFaceDirection.Right, ratio, new Vector3(1.0f, 0f, 0.5f));
                    }
                }
            }
        }

        private void TrySpawnStructureFace(
            int gridX,
            int gridZ,
            float height,
            Quaternion baseRotation,
            VoxelFaceDirection direction,
            float distanceRatio,
            Vector3 offsetFraction)
        {
            List<(StructureGroup group, StructureElement elem)> candidates = new List<(StructureGroup, StructureElement)>();
            int totalWeight = 0;

            foreach (var group in _preset.StructureGroups)
            {
                if (group == null || (group.SupportedDirections & direction) == 0) continue;
                if (distanceRatio < group.DistanceRatioRange.x || distanceRatio > group.DistanceRatioRange.y) continue;

                foreach (var elem in group.Elements)
                {
                    if (elem != null && elem.Prefab != null && elem.Weight > 0)
                    {
                        candidates.Add((group, elem));
                        totalWeight += elem.Weight;
                    }
                }
            }

            if (candidates.Count == 0 || totalWeight <= 0) return;

            int randVal = Random.Range(0, totalWeight);
            StructureGroup selectedGroup = null;
            StructureElement selectedElem = null;
            int curSum = 0;

            foreach (var item in candidates)
            {
                curSum += item.elem.Weight;
                if (randVal < curSum)
                {
                    selectedGroup = item.group;
                    selectedElem = item.elem;
                    break;
                }
            }

            if (selectedElem == null) return;

            // 1. 计算出该贴面中点的物理世界位置 (结合组的 heightOffset 高低差)
            float finalY = (direction == VoxelFaceDirection.Down) ? selectedGroup.HeightOffset : height;
            Vector3 localPos = new Vector3(
                (gridX + offsetFraction.x) * _tileSize,
                finalY,
                (gridZ + offsetFraction.z) * _tileSize
            );
            Vector3 worldPos = transform.TransformPoint(localPos);

            // 2. 创建用于解耦坐标旋转与偏移的空“逻辑父锚点 Pivot”
            string pivotName = $"{direction}_{gridX}_{gridZ}_Pivot";
            GameObject pivotObj = new GameObject(pivotName);
            pivotObj.transform.SetParent(GetGroupContainer(selectedGroup), false);
            pivotObj.transform.position = worldPos;

            // 3. 根据 Group 的 FollowSdfGradient 确立参考坐标系基准轴
            Quaternion targetRot = transform.rotation * baseRotation;
            if (selectedGroup.FollowSdfGradient)
            {
                Vector2 grad = GetDiscreteGradientVector2D(new Vector2(localPos.x, localPos.z));
                if (grad != Vector2.zero)
                {
                    Vector3 inwardDir = transform.TransformDirection(new Vector3(grad.x, 0f, grad.y));
                    targetRot = Quaternion.LookRotation(inwardDir, transform.up);
                }
            }

            // 地板或天花板等可开启二向或四向离散纹理打乱
            if (direction == VoxelFaceDirection.Up || direction == VoxelFaceDirection.Down)
            {
                if (selectedElem.RotationMode == StructureRotationMode.TwoWay180)
                {
                    targetRot *= Quaternion.Euler(0f, Random.Range(0, 2) * 180f, 0f);
                }
                else if (selectedElem.RotationMode == StructureRotationMode.FourWay90)
                {
                    targetRot *= Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
                }
            }

            pivotObj.transform.rotation = targetRot;

            // 将缩放覆盖 (scaleOverride) 直接赋予逻辑父锚点
            if (selectedElem.ScaleOverride != Vector3.one)
            {
                pivotObj.transform.localScale = selectedElem.ScaleOverride;
            }

            // 4. 在 Editor 模式下使用 PrefabUtility 实例化 Prefab 作为子物体，完美保留 Prefab 链接
            GameObject artObj;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                artObj = UnityEditor.PrefabUtility.InstantiatePrefab(selectedElem.Prefab) as GameObject;
            }
            else
            {
                artObj = Instantiate(selectedElem.Prefab);
            }
#else
            artObj = Instantiate(selectedElem.Prefab);
#endif

            if (artObj != null)
            {
                artObj.transform.SetParent(pivotObj.transform, false);
                artObj.transform.localPosition = selectedElem.PositionOffset;
                artObj.transform.localRotation = Quaternion.Euler(selectedElem.RotationOffset);
                artObj.transform.localScale = Vector3.one;

                if (selectedGroup.Layer != 0) SetLayerRecursively(pivotObj, MathLog2(selectedGroup.Layer.value));

                _spawnedObjects.Add(pivotObj);
                _spawnedObjects.Add(artObj);
                _spawnedStructures.Add((artObj, selectedGroup));
            }
        }

        private void SpawnDecorations()
        {
            if (_preset == null || _preset.DecorationGroups == null) return;

            List<(DecorationGroup group, DecorationElement decor)> allDecorConfigs = new List<(DecorationGroup, DecorationElement)>();
            Dictionary<DecorationElement, int> spawnCounts = new Dictionary<DecorationElement, int>();

            foreach (var group in _preset.DecorationGroups)
            {
                if (group == null || group.Decorations == null) continue;
                foreach (var decor in group.Decorations)
                {
                    if (decor == null || decor.Prefab == null) continue;
                    allDecorConfigs.Add((group, decor));
                    int targetCount = Random.Range(decor.AmountRange.x, decor.AmountRange.y + 1);
                    spawnCounts[decor] = targetCount;
                }
            }

            List<VirtualSlot> currentSlots = new List<VirtualSlot>();
            foreach (var item in _spawnedStructures)
            {
                if (item.artObj == null || item.group == null || item.group.ProvidedSlotType == null) continue;
                List<VirtualSlot> slots = GenerateVirtualSlotsForObject(item.artObj, item.group.ProvidedSlotType);
                currentSlots.AddRange(slots);
            }

            SpawnDecorationsRecursive(currentSlots, allDecorConfigs, spawnCounts, 1);
        }

        private void SpawnDecorationsRecursive(List<VirtualSlot> slotsToProcess, List<(DecorationGroup group, DecorationElement decor)> allDecorConfigs, Dictionary<DecorationElement, int> spawnCounts, int currentDepth)
        {
            if (currentDepth > _maxRecursionDepth || slotsToProcess == null || slotsToProcess.Count == 0) return;

            List<VirtualSlot> nextLevelSlots = new List<VirtualSlot>();

            foreach (var decorItem in allDecorConfigs)
            {
                DecorationGroup decorGroup = decorItem.group;
                DecorationElement decorElem = decorItem.decor;

                if (!spawnCounts.ContainsKey(decorElem) || spawnCounts[decorElem] <= 0) continue;

                List<VirtualSlot> eligibleSlots = new List<VirtualSlot>();
                foreach (var slot in slotsToProcess)
                {
                    if (slot == null || slot.SlotType == null) continue;
                    if (decorGroup.AllowedSlots != null && decorGroup.AllowedSlots.Contains(slot.SlotType))
                    {
                        float slotRatio = GetNormalizedDistanceRatio(slot.Position);
                        if (slotRatio >= decorGroup.DistanceRatioRange.x && slotRatio <= decorGroup.DistanceRatioRange.y)
                        {
                            eligibleSlots.Add(slot);
                        }
                    }
                }

                if (eligibleSlots.Count == 0) continue;

                // 根据 Group 的 DistributionPattern 执行分布策略
                if (decorGroup.DistributionPattern == DistributionPattern.Random)
                {
                    // 1. 随机打乱策略
                    for (int i = eligibleSlots.Count - 1; i > 0; i--)
                    {
                        int r = Random.Range(0, i + 1);
                        var temp = eligibleSlots[i];
                        eligibleSlots[i] = eligibleSlots[r];
                        eligibleSlots[r] = temp;
                    }
                }
                else if (decorGroup.DistributionPattern == DistributionPattern.Uniform)
                {
                    // 2. 规律等距策略：按空间几何顺序排序后按步长采样
                    eligibleSlots.Sort((a, b) =>
                    {
                        int cmpX = a.Position.x.CompareTo(b.Position.x);
                        if (cmpX != 0) return cmpX;
                        int cmpZ = a.Position.z.CompareTo(b.Position.z);
                        if (cmpZ != 0) return cmpZ;
                        return a.Position.y.CompareTo(b.Position.y);
                    });

                    int stride = Mathf.Max(1, decorGroup.UniformStride);
                    List<VirtualSlot> steppedSlots = new List<VirtualSlot>();
                    for (int idx = 0; idx < eligibleSlots.Count; idx += stride)
                    {
                        steppedSlots.Add(eligibleSlots[idx]);
                    }
                    eligibleSlots = steppedSlots;
                }

                List<VirtualSlot> usedSlotsThisGroup = new List<VirtualSlot>();

                foreach (var slot in eligibleSlots)
                {
                    if (spawnCounts[decorElem] <= 0) break;

                    // 计算预期世界位置 (结合 PositionOffset)
                    Vector3 localSpawnPos = slot.Position + slot.Rotation * decorElem.PositionOffset;
                    Quaternion worldRot = slot.Rotation;

                    // 根据 Group 的 FollowSdfGradient 确立参考坐标系基准轴
                    if (decorGroup.FollowSdfGradient)
                    {
                        Vector3 localPos = transform.InverseTransformPoint(slot.Position);
                        Vector2 grad = GetDiscreteGradientVector2D(new Vector2(localPos.x, localPos.z));
                        if (grad != Vector2.zero)
                        {
                            Vector3 inDir = transform.TransformDirection(new Vector3(grad.x, 0f, grad.y));
                            worldRot = Quaternion.LookRotation(inDir, transform.up);
                        }
                    }

                    float randScale = Random.Range(decorElem.ScaleRange.x, decorElem.ScaleRange.y);
                    Vector3 finalScale = decorElem.Prefab.transform.localScale * randScale;

                    // 0. 门通道避让与墙角避让校验
                    if (decorGroup.AvoidDoorPassage && IsPositionInDoorPassage(localSpawnPos)) continue;
                    if (decorGroup.WallCornerPadding > 0f && IsNearWallCorner(slot.Position, decorGroup.WallCornerPadding)) continue;

                    // 计算当前物体预测的 3D 世界包围盒与离散/连续随机朝向
                    float randomY = decorElem.RotationMode switch
                    {
                        DecorationRotationMode.TwoWay180 => Random.Range(0, 2) * 180f,
                        DecorationRotationMode.FourWay90 => Random.Range(0, 4) * 90f,
                        DecorationRotationMode.EightWay45 => Random.Range(0, 8) * 45f,
                        DecorationRotationMode.FullContinuous => Random.Range(0f, 360f),
                        _ => 0f
                    };
                    Quaternion finalArtRot = worldRot * Quaternion.Euler(decorElem.RotationOffset) * Quaternion.Euler(0f, randomY, 0f);
                    Bounds predictedBounds = CalculatePredictedWorldBounds(decorElem.Prefab, localSpawnPos, finalArtRot, finalScale);

                    // 1. 真实 3D 几何包围盒相交硬互斥 + Spacing 稀疏度软策略校验
                    if (!CheckSpacingAndVolumeValid(localSpawnPos, decorElem, decorGroup, predictedBounds)) continue;

                    // 2. 3D OBB 安全边界防穿墙校验
                    if (!ValidateObbSafety(decorElem, decorGroup, localSpawnPos, worldRot, finalScale)) continue;

                    // 3. 创建解耦的逻辑父锚点 Pivot
                    GameObject pivotObj = new GameObject($"{decorElem.Prefab.name}_Pivot");
                    pivotObj.transform.SetParent(GetGroupContainer(decorGroup), false);
                    pivotObj.transform.position = slot.Position;
                    pivotObj.transform.rotation = worldRot;

                    // 4. 在 Editor 模式下使用 PrefabUtility 实例化 Prefab
                    GameObject artObj;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        artObj = UnityEditor.PrefabUtility.InstantiatePrefab(decorElem.Prefab) as GameObject;
                    }
                    else
                    {
                        artObj = Instantiate(decorElem.Prefab);
                    }
#else
                    artObj = Instantiate(decorElem.Prefab);
#endif

                    if (artObj != null)
                    {
                        artObj.transform.SetParent(pivotObj.transform, false);
                        artObj.transform.localPosition = decorElem.PositionOffset;

                        artObj.transform.localRotation = Quaternion.Euler(decorElem.RotationOffset) * Quaternion.Euler(0f, randomY, 0f);
                        artObj.transform.localScale = Vector3.one * randScale;

                        if (decorGroup.Layer != 0) SetLayerRecursively(pivotObj, MathLog2(decorGroup.Layer.value));

                        _spawnedObjects.Add(pivotObj);
                        _spawnedObjects.Add(artObj);

                        _occupiedDecorPositions.Add(new OccupiedDecorData
                        {
                            position = localSpawnPos,
                            spacing = decorElem.Spacing,
                            config = decorElem,
                            isVolumeObject = decorGroup.IsVolumeObject,
                            worldBounds = predictedBounds
                        });

                        spawnCounts[decorElem]--;
                        usedSlotsThisGroup.Add(slot);

                        if (decorGroup.ProvidedSlotType != null)
                        {
                            List<VirtualSlot> childSlots = GenerateVirtualSlotsForObject(artObj, decorGroup.ProvidedSlotType);
                            nextLevelSlots.AddRange(childSlots);
                        }
                    }
                }

                // 移除已被此种饰品占用的插槽，防止后续物品在相同位置重复堆叠
                foreach (var usedSlot in usedSlotsThisGroup)
                {
                    slotsToProcess.Remove(usedSlot);
                }
            }

            SpawnDecorationsRecursive(nextLevelSlots, allDecorConfigs, spawnCounts, currentDepth + 1);
        }

        private bool IsPositionInDoorPassage(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            int gridX = Mathf.FloorToInt(localPos.x / _tileSize);
            int gridZ = Mathf.FloorToInt(localPos.z / _tileSize);
            return _doorGridCoords.Contains(new Vector2Int(gridX, gridZ));
        }

        private bool IsNearWallCorner(Vector3 worldPos, float cornerPadding)
        {
            if (_wallCornerPoints.Count == 0 || cornerPadding <= 0f) return false;

            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            Vector2 p2D = new Vector2(localPos.x, localPos.z);
            float paddingSq = cornerPadding * cornerPadding;

            foreach (var corner in _wallCornerPoints)
            {
                if ((p2D - corner).sqrMagnitude < paddingSq)
                {
                    return true;
                }
            }
            return false;
        }

        private List<VirtualSlot> GenerateVirtualSlotsForObject(GameObject obj, SlotType providedSlotType)
        {
            List<VirtualSlot> slots = new List<VirtualSlot>();
            if (providedSlotType == null || obj == null) return slots;

            Bounds localBounds = CalculateLocalBounds(obj);
            Vector3 size = localBounds.size;
            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;

            SlotDirection dir = providedSlotType.DefaultDirection;
            Transform trans = obj.transform;

            // 1. 若方向为 Up，在物体顶面切分生成虚拟插槽
            if ((dir & SlotDirection.Up) != 0)
            {
                int countX = Mathf.FloorToInt(size.x / _slotStepSize);
                int countZ = Mathf.FloorToInt(size.z / _slotStepSize);

                float startX = countX > 0 ? min.x + (size.x - (countX - 1) * _slotStepSize) * 0.5f : (min.x + max.x) * 0.5f;
                float startZ = countZ > 0 ? min.z + (size.z - (countZ - 1) * _slotStepSize) * 0.5f : (min.z + max.z) * 0.5f;

                int loopX = Mathf.Max(1, countX);
                int loopZ = Mathf.Max(1, countZ);

                for (int i = 0; i < loopX; i++)
                {
                    for (int j = 0; j < loopZ; j++)
                    {
                        float localX = startX + (countX > 0 ? i * _slotStepSize : 0f);
                        float localZ = startZ + (countZ > 0 ? j * _slotStepSize : 0f);
                        Vector3 localPos = new Vector3(localX, max.y, localZ);
                        Vector3 worldPos = trans.TransformPoint(localPos);

                        worldPos.x += Random.Range(-_slotJitter, _slotJitter);
                        worldPos.z += Random.Range(-_slotJitter, _slotJitter);

                        slots.Add(new VirtualSlot
                        {
                            SlotType = providedSlotType,
                            Position = worldPos,
                            Rotation = trans.rotation
                        });
                    }
                }
            }

            // 2. 若方向为 Down，在物体底面切分生成虚拟插槽
            if ((dir & SlotDirection.Down) != 0)
            {
                int countX = Mathf.FloorToInt(size.x / _slotStepSize);
                int countZ = Mathf.FloorToInt(size.z / _slotStepSize);

                float startX = countX > 0 ? min.x + (size.x - (countX - 1) * _slotStepSize) * 0.5f : (min.x + max.x) * 0.5f;
                float startZ = countZ > 0 ? min.z + (size.z - (countZ - 1) * _slotStepSize) * 0.5f : (min.z + max.z) * 0.5f;

                int loopX = Mathf.Max(1, countX);
                int loopZ = Mathf.Max(1, countZ);

                for (int i = 0; i < loopX; i++)
                {
                    for (int j = 0; j < loopZ; j++)
                    {
                        float localX = startX + (countX > 0 ? i * _slotStepSize : 0f);
                        float localZ = startZ + (countZ > 0 ? j * _slotStepSize : 0f);
                        Vector3 localPos = new Vector3(localX, min.y, localZ);
                        Vector3 worldPos = trans.TransformPoint(localPos);

                        worldPos.x += Random.Range(-_slotJitter, _slotJitter);
                        worldPos.z += Random.Range(-_slotJitter, _slotJitter);

                        slots.Add(new VirtualSlot
                        {
                            SlotType = providedSlotType,
                            Position = worldPos,
                            Rotation = trans.rotation * Quaternion.Euler(180f, 0f, 0f)
                        });
                    }
                }
            }

            // 3. 若方向为 Horizontal，在物体 4 个侧立面切分生成虚拟插槽
            if ((dir & SlotDirection.Horizontal) != 0)
            {
                int countX = Mathf.FloorToInt(size.x / _slotStepSize);
                int countY = Mathf.FloorToInt(size.y / _slotStepSize);
                int countZ = Mathf.FloorToInt(size.z / _slotStepSize);

                float startX = countX > 0 ? min.x + (size.x - (countX - 1) * _slotStepSize) * 0.5f : (min.x + max.x) * 0.5f;
                float startY = countY > 0 ? min.y + (size.y - (countY - 1) * _slotStepSize) * 0.5f : (min.y + max.y) * 0.5f;
                float startZ = countZ > 0 ? min.z + (size.z - (countZ - 1) * _slotStepSize) * 0.5f : (min.z + max.z) * 0.5f;

                int loopX = Mathf.Max(1, countX);
                int loopY = Mathf.Max(1, countY);
                int loopZ = Mathf.Max(1, countZ);

                // 前面 (North/Forward, Z = max.z)
                for (int i = 0; i < loopX; i++)
                {
                    for (int j = 0; j < loopY; j++)
                    {
                        float lx = startX + (countX > 0 ? i * _slotStepSize : 0f);
                        float ly = startY + (countY > 0 ? j * _slotStepSize : 0f);
                        Vector3 worldPos = trans.TransformPoint(new Vector3(lx, ly, max.z));
                        slots.Add(new VirtualSlot
                        {
                            SlotType = providedSlotType,
                            Position = worldPos,
                            Rotation = trans.rotation * Quaternion.FromToRotation(Vector3.up, Vector3.forward)
                        });
                    }
                }

                // 后面 (South/Back, Z = min.z)
                for (int i = 0; i < loopX; i++)
                {
                    for (int j = 0; j < loopY; j++)
                    {
                        float lx = startX + (countX > 0 ? i * _slotStepSize : 0f);
                        float ly = startY + (countY > 0 ? j * _slotStepSize : 0f);
                        Vector3 worldPos = trans.TransformPoint(new Vector3(lx, ly, min.z));
                        slots.Add(new VirtualSlot
                        {
                            SlotType = providedSlotType,
                            Position = worldPos,
                            Rotation = trans.rotation * Quaternion.FromToRotation(Vector3.up, Vector3.back)
                        });
                    }
                }

                // 左面 (West/Left, X = min.x)
                for (int i = 0; i < loopZ; i++)
                {
                    for (int j = 0; j < loopY; j++)
                    {
                        float lz = startZ + (countZ > 0 ? i * _slotStepSize : 0f);
                        float ly = startY + (countY > 0 ? j * _slotStepSize : 0f);
                        Vector3 worldPos = trans.TransformPoint(new Vector3(min.x, ly, lz));
                        slots.Add(new VirtualSlot
                        {
                            SlotType = providedSlotType,
                            Position = worldPos,
                            Rotation = trans.rotation * Quaternion.FromToRotation(Vector3.up, Vector3.left)
                        });
                    }
                }

                // 右面 (East/Right, X = max.x)
                for (int i = 0; i < loopZ; i++)
                {
                    for (int j = 0; j < loopY; j++)
                    {
                        float lz = startZ + (countZ > 0 ? i * _slotStepSize : 0f);
                        float ly = startY + (countY > 0 ? j * _slotStepSize : 0f);
                        Vector3 worldPos = trans.TransformPoint(new Vector3(max.x, ly, lz));
                        slots.Add(new VirtualSlot
                        {
                            SlotType = providedSlotType,
                            Position = worldPos,
                            Rotation = trans.rotation * Quaternion.FromToRotation(Vector3.up, Vector3.right)
                        });
                    }
                }
            }

            return slots;
        }

        private bool CheckSpacingAndVolumeValid(Vector3 pos, DecorationElement elem, DecorationGroup group, Bounds currentWorldBounds)
        {
            foreach (var occ in _occupiedDecorPositions)
            {
                // 1. 物理层硬互斥约束：若双方均为实体体积摆件，全自动基于真实 3D 包围盒进行严密的几何相交碰撞检测
                if (group.IsVolumeObject && occ.isVolumeObject)
                {
                    // 给予 0.05m 的容差微缩，避免物体共面贴合接触时被误杀
                    Bounds expandedOcc = occ.worldBounds;
                    expandedOcc.Expand(-0.05f);
                    if (currentWorldBounds.Intersects(expandedOcc))
                    {
                        return false;
                    }
                }

                // 2. 策略层软约束：Spacing 仅对同一种预制件 (同款物品) 生效，保证同款物品在空间中均匀分布，不误伤其他物体
                if (elem.Spacing > 0f && occ.config == elem)
                {
                    float dist = Vector3.Distance(pos, occ.position);
                    if (dist < elem.Spacing) return false;
                }
            }
            return true;
        }

        private Bounds CalculatePredictedWorldBounds(GameObject prefab, Vector3 worldPos, Quaternion worldRot, Vector3 localScale)
        {
            Bounds localB = CalculateLocalBounds(prefab);
            Vector3[] corners = GetBoundsCorners(localB);
            Bounds worldBounds = new Bounds();
            bool hasBounds = false;

            Matrix4x4 m = Matrix4x4.TRS(worldPos, worldRot, localScale);
            foreach (var corner in corners)
            {
                Vector3 worldCorner = m.MultiplyPoint3x4(corner);
                if (!hasBounds)
                {
                    worldBounds = new Bounds(worldCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(worldCorner);
                }
            }
            return worldBounds;
        }

        private bool ValidateObbSafety(DecorationElement decor, DecorationGroup group, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (!group.PreventWallPenetration || _allWallSegments.Count == 0) return true;

            Vector3 localCenterPos = transform.InverseTransformPoint(pos);
            Vector2 p2D = new Vector2(localCenterPos.x, localCenterPos.z);
            float distToWall = GetSdfDistance(p2D);

            Vector2 grad = GetGradientVector2D(p2D);
            if (grad == Vector2.zero) return true;

            Vector2 toWallDir = -grad;

            Bounds localBounds = CalculateLocalBounds(decor.Prefab);
            Vector3[] localCorners = GetBoundsCorners(localBounds);

            Quaternion localRot = Quaternion.Inverse(transform.rotation) * rot;

            float maxProjection = 0f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 rotatedCorner = localRot * Vector3.Scale(localCorners[i], scale);
                Vector2 corner2D = new Vector2(rotatedCorner.x, rotatedCorner.z);
                float proj = Vector2.Dot(corner2D, toWallDir);
                if (proj > maxProjection)
                {
                    maxProjection = proj;
                }
            }

            if (distToWall < maxProjection * 0.95f)
            {
                return false;
            }

            return true;
        }

        private Transform GetGroupContainer(ScriptableObject group)
        {
            if (group is StructureGroup sg)
            {
                Transform container = _roomContainer.transform.Find(sg.name);
                if (container == null)
                {
                    GameObject go = new GameObject(sg.name);
                    go.transform.SetParent(_roomContainer.transform, false);
                    container = go.transform;
                }
                return container;
            }
            else if (group is DecorationGroup dg)
            {
                Transform container = _roomContainer.transform.Find(dg.name);
                if (container == null)
                {
                    GameObject go = new GameObject(dg.name);
                    go.transform.SetParent(_roomContainer.transform, false);
                    container = go.transform;
                }
                return container;
            }
            return _roomContainer.transform;
        }

        private Bounds CalculateLocalBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers != null && renderers.Length > 0)
            {
                Bounds localBounds = new Bounds();
                bool hasBounds = false;
                Matrix4x4 worldToLocal = obj.transform.worldToLocalMatrix;

                foreach (Renderer r in renderers)
                {
                    if (r == null) continue;
                    Bounds worldB = r.bounds;
                    Vector3[] corners = GetBoundsCorners(worldB);
                    foreach (var corner in corners)
                    {
                        Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corner);
                        if (!hasBounds)
                        {
                            localBounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(localCorner);
                        }
                    }
                }
                return localBounds;
            }
            return new Bounds(Vector3.zero, Vector3.one);
        }

        private Vector3[] GetBoundsCorners(Bounds b)
        {
            return new Vector3[]
            {
                new Vector3(b.min.x, b.min.y, b.min.z),
                new Vector3(b.min.x, b.min.y, b.max.z),
                new Vector3(b.min.x, b.max.y, b.min.z),
                new Vector3(b.min.x, b.max.y, b.max.z),
                new Vector3(b.max.x, b.min.y, b.min.z),
                new Vector3(b.max.x, b.min.y, b.max.z),
                new Vector3(b.max.x, b.max.y, b.min.z),
                new Vector3(b.max.x, b.max.y, b.max.z)
            };
        }

        private float PointToSegmentDistanceSquare(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ap = p - a;
            float sqLen = ab.sqrMagnitude;
            if (sqLen < 0.0001f) return ap.sqrMagnitude;

            float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / sqLen);
            Vector2 proj = a + t * ab;
            return (p - proj).sqrMagnitude;
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private int MathLog2(int mask)
        {
            int layer = 0;
            while (mask > 1) { mask >>= 1; layer++; }
            return layer;
        }

        #endregion
    }
}
