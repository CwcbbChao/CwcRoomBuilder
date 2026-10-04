using UnityEditor;
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder.Editor
{
    /// <summary>
    /// RoomBuilder 组件的自定义 Inspector 编辑器。
    /// 经过深度性能优化：绝无 SetDirty 卡顿与鼠标转圈，画板居中对齐，支持极速拖拽涂抹。
    /// </summary>
    [CustomEditor(typeof(RoomBuilder))]
    public class RoomBuilderEditor : UnityEditor.Editor
    {
        private RoomBuilder _roomBuilder;
        private bool _autoPreview = true;
        private bool _showSettings = true;
        private bool _showPresetInline = true;

        private bool _isDragging = false;
        private Vector2Int _dragStartGridPos;
        private Vector2Int _dragEndGridPos;
        private bool _dragModeDraw = true;

        private UnityEditor.Editor _presetEditor;

        private enum PaintMode
        {
            Room = 1,
            Door = 2
        }
        private PaintMode _currentPaintMode = PaintMode.Room;

        private void OnEnable()
        {
            _roomBuilder = (RoomBuilder)target;
            _autoPreview = EditorPrefs.GetBool("CwcRoomBuilder_AutoPreview", true);

            if (_roomBuilder.gridData == null || _roomBuilder.gridData.Length != _roomBuilder.CanvasWidth * _roomBuilder.CanvasHeight)
            {
                _roomBuilder.ResizeGrid(_roomBuilder.CanvasWidth, _roomBuilder.CanvasHeight);
            }
        }

        private void OnDisable()
        {
            if (_presetEditor != null)
            {
                DestroyImmediate(_presetEditor);
                _presetEditor = null;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawEditorHeader();
            EditorGUILayout.Space(3);

            // 顶部核心快捷控制条（含显眼的 Auto Live Preview 开关）
            DrawTopQuickControls();
            EditorGUILayout.Space(8);

            EditorGUI.BeginChangeCheck();

            // 1. 基础配置与 Preset 内嵌快速编辑区
            DrawSettingsArea();
            EditorGUILayout.Space(10);

            // 2. 网格尺寸控制区
            DrawCanvasSizeControls();
            EditorGUILayout.Space(10);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_roomBuilder);
                if (_autoPreview)
                {
                    _roomBuilder.GenerateWithCurrentSeed();
                }
            }

            // 3. 画板绘制独立于 ChangeCheck，防止拖拽时频繁 SetDirty 导致鼠标转圈卡顿
            DrawPixelCanvas();
        }

        private void DrawEditorHeader()
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, 36);
            EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.3f, 0.45f, 1f));

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(headerRect, "CWC ROOM BUILDER", titleStyle);
        }

        private void DrawTopQuickControls()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = _autoPreview ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);

            EditorGUI.BeginChangeCheck();
            bool newAuto = GUILayout.Toggle(_autoPreview, _autoPreview ? " Live Preview [ON]" : " Live Preview [OFF]", "Button", GUILayout.Height(26));
            if (EditorGUI.EndChangeCheck())
            {
                _autoPreview = newAuto;
                EditorPrefs.SetBool("CwcRoomBuilder_AutoPreview", _autoPreview);
                if (_autoPreview)
                {
                    _roomBuilder.GenerateWithCurrentSeed();
                }
            }

            GUI.backgroundColor = new Color(0.25f, 0.7f, 1f);
            if (GUILayout.Button("Generate", GUILayout.Height(26)))
            {
                _roomBuilder.GenerateWithCurrentSeed();
            }

            GUI.backgroundColor = new Color(0.6f, 0.5f, 1f);
            if (GUILayout.Button("New Seed", GUILayout.Width(75), GUILayout.Height(26)))
            {
                _roomBuilder.GenerateWithNewSeed();
            }

            GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
            if (GUILayout.Button("Clear", GUILayout.Width(50), GUILayout.Height(26)))
            {
                _roomBuilder.ClearGeneratedObjects();
            }

            GUI.backgroundColor = prevBg;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettingsArea()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _showSettings = EditorGUILayout.Foldout(_showSettings, "Basic Configuration & Preset Settings", true);
            if (_showSettings)
            {
                EditorGUI.indentLevel++;

                SerializedProperty presetProp = serializedObject.FindProperty("_preset");
                EditorGUILayout.PropertyField(presetProp);

                DrawInlinePresetSection();

                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_tileSize"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_roofHeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_maxRecursionDepth"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_generatorId"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_slotStepSize"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_slotJitter"));

                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_currentSeed"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawInlinePresetSection()
        {
            if (_roomBuilder.Preset == null) return;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _showPresetInline = EditorGUILayout.Foldout(_showPresetInline, $"Quick Inline Preset Editor: [{_roomBuilder.Preset.name}]", true);
            if (_showPresetInline)
            {
                if (_presetEditor == null || _presetEditor.target != _roomBuilder.Preset)
                {
                    if (_presetEditor != null) DestroyImmediate(_presetEditor);
                    _presetEditor = UnityEditor.Editor.CreateEditor(_roomBuilder.Preset);
                }

                EditorGUI.indentLevel++;
                _presetEditor.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCanvasSizeControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("2D Grid Canvas Dimensions", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int newW = EditorGUILayout.IntSlider("Canvas Width (Columns)", _roomBuilder.CanvasWidth, 1, 100);
            int newH = EditorGUILayout.IntSlider("Canvas Height (Rows)", _roomBuilder.CanvasHeight, 1, 100);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_roomBuilder, "Resize Room Canvas");
                _roomBuilder.ResizeGrid(newW, newH);
                EditorUtility.SetDirty(_roomBuilder);
                if (_autoPreview) _roomBuilder.GenerateWithCurrentSeed();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPixelCanvas()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Grid Painter Canvas [{_roomBuilder.CanvasWidth} × {_roomBuilder.CanvasHeight}]", EditorStyles.boldLabel);

            _currentPaintMode = (PaintMode)GUILayout.Toolbar((int)_currentPaintMode - 1, new string[] { "Room Tile", "Door Passage" }) + 1;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            int width = _roomBuilder.CanvasWidth;
            int height = _roomBuilder.CanvasHeight;
            float maxCanvasPixelWidth = EditorGUIUtility.currentViewWidth - 50f;
            float tileSizeInPixels = Mathf.Clamp(maxCanvasPixelWidth / width, 10f, 32f);

            float totalCanvasWidth = width * tileSizeInPixels;
            Rect canvasBoxRect = EditorGUILayout.GetControlRect(false, height * tileSizeInPixels);

            // 计算居中对齐起始 X 偏移
            float startX = canvasBoxRect.x + Mathf.Max(0f, (canvasBoxRect.width - totalCanvasWidth) * 0.5f);

            // 1. 居中绘制背景
            Rect centeredCanvasRect = new Rect(startX, canvasBoxRect.y, totalCanvasWidth, canvasBoxRect.height);
            EditorGUI.DrawRect(centeredCanvasRect, new Color(0.12f, 0.12f, 0.12f, 1f));

            // 2. 处理极速无卡顿画板事件
            ProcessCanvasEvents(centeredCanvasRect, startX, tileSizeInPixels, width, height);

            // 3. 居中渲染网格单元与网格线
            Color borderColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    int gridIndex = y * width + x;
                    Rect tileRect = new Rect(
                        startX + x * tileSizeInPixels,
                        canvasBoxRect.y + (height - 1 - y) * tileSizeInPixels,
                        tileSizeInPixels,
                        tileSizeInPixels
                    );

                    int tileState = _roomBuilder.gridData[gridIndex];
                    Color fillColor = (tileState == 1) ? new Color(0.2f, 0.75f, 0.35f, 0.95f) :
                                      (tileState == 2) ? new Color(0.95f, 0.65f, 0.1f, 0.95f) :
                                                         new Color(0.18f, 0.18f, 0.18f, 1f);

                    EditorGUI.DrawRect(tileRect, fillColor);
                    Handles.DrawSolidRectangleWithOutline(tileRect, Color.clear, borderColor);
                }
            }

            // 4. 拖拽框选高亮预览
            if (_isDragging)
            {
                int minX = Mathf.Min(_dragStartGridPos.x, _dragEndGridPos.x);
                int maxX = Mathf.Max(_dragStartGridPos.x, _dragEndGridPos.x);
                int minY = Mathf.Min(_dragStartGridPos.y, _dragEndGridPos.y);
                int maxY = Mathf.Max(_dragStartGridPos.y, _dragEndGridPos.y);

                Rect dragBoxRect = new Rect(
                    startX + minX * tileSizeInPixels,
                    canvasBoxRect.y + (height - 1 - maxY) * tileSizeInPixels,
                    (maxX - minX + 1) * tileSizeInPixels,
                    (maxY - minY + 1) * tileSizeInPixels
                );

                Color selectionFill = _dragModeDraw ? new Color(0.2f, 0.6f, 1f, 0.25f) : new Color(1f, 0.2f, 0.2f, 0.25f);
                Color selectionBorder = _dragModeDraw ? new Color(0.3f, 0.7f, 1f, 1f) : new Color(1f, 0.3f, 0.3f, 1f);
                Handles.DrawSolidRectangleWithOutline(dragBoxRect, selectionFill, selectionBorder);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fill All Tiles"))
            {
                Undo.RecordObject(_roomBuilder, "Fill Canvas");
                for (int i = 0; i < _roomBuilder.gridData.Length; i++) _roomBuilder.gridData[i] = (int)_currentPaintMode;
                EditorUtility.SetDirty(_roomBuilder);
                if (_autoPreview) _roomBuilder.GenerateWithCurrentSeed();
            }
            if (GUILayout.Button("Clear All Tiles"))
            {
                Undo.RecordObject(_roomBuilder, "Clear Canvas");
                System.Array.Clear(_roomBuilder.gridData, 0, _roomBuilder.gridData.Length);
                EditorUtility.SetDirty(_roomBuilder);
                if (_autoPreview) _roomBuilder.ClearGeneratedObjects();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ProcessCanvasEvents(Rect canvasRect, float startX, float tileSize, int width, int height)
        {
            Event e = Event.current;
            if (!canvasRect.Contains(e.mousePosition) && !_isDragging) return;

            Vector2 mousePos = e.mousePosition;
            int cellX = Mathf.Clamp(Mathf.FloorToInt((mousePos.x - startX) / tileSize), 0, width - 1);
            int cellY = Mathf.Clamp(height - 1 - Mathf.FloorToInt((mousePos.y - canvasRect.y) / tileSize), 0, height - 1);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                _isDragging = true;
                _dragStartGridPos = new Vector2Int(cellX, cellY);
                _dragEndGridPos = new Vector2Int(cellX, cellY);

                int curVal = _roomBuilder.gridData[cellY * width + cellX];
                int targetVal = (int)_currentPaintMode;
                // 只要绘制起点的值不等于当前要绘制的模式，就绘制目标模式；如果起点已是该模式，则执行清除 (0)
                _dragModeDraw = (curVal != targetVal);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _isDragging)
            {
                Vector2Int newEnd = new Vector2Int(cellX, cellY);
                if (newEnd != _dragEndGridPos)
                {
                    _dragEndGridPos = newEnd;
                    Repaint(); // 拖拽时只轻量触发画面重绘，绝不调用 SetDirty
                }
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _isDragging)
            {
                _isDragging = false;
                ApplyDragPaint(); // 绘制完毕抬起鼠标时，统一统一只做一次 Undo 与 SetDirty
                e.Use();
            }
        }

        private void ApplyDragPaint()
        {
            Undo.RecordObject(_roomBuilder, "Paint Canvas");

            int minX = Mathf.Min(_dragStartGridPos.x, _dragEndGridPos.x);
            int maxX = Mathf.Max(_dragStartGridPos.x, _dragEndGridPos.x);
            int minY = Mathf.Min(_dragStartGridPos.y, _dragEndGridPos.y);
            int maxY = Mathf.Max(_dragStartGridPos.y, _dragEndGridPos.y);

            int valToSet = _dragModeDraw ? (int)_currentPaintMode : 0;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int index = y * _roomBuilder.CanvasWidth + x;
                    if (index >= 0 && index < _roomBuilder.gridData.Length)
                    {
                        _roomBuilder.gridData[index] = valToSet;
                    }
                }
            }

            EditorUtility.SetDirty(_roomBuilder);

            // 只有当开了自动预览时，抬起鼠标后才触发生成
            if (_autoPreview)
            {
                _roomBuilder.GenerateWithCurrentSeed();
            }
        }
    }
}
