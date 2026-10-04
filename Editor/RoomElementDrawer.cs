using UnityEditor;
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder.Editor
{
    /// <summary>
    /// RoomElement 类及其派生类的通用 Inspector 属性绘制器。
    /// 能够在 Inspector 折叠栏中直观显示序号、预制件名称、权重/数量，并支持一键 Reset 恢复默认值。
    /// </summary>
    [CustomPropertyDrawer(typeof(RoomElement), true)]
    public class RoomElementDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(position, label, property);

            // 1. 解析当前元素数组索引
            int index = 0;
            string path = property.propertyPath;
            int lastBracketOpen = path.LastIndexOf('[');
            int lastBracketClose = path.LastIndexOf(']');
            if (lastBracketOpen >= 0 && lastBracketClose > lastBracketOpen)
            {
                string indexStr = path.Substring(lastBracketOpen + 1, lastBracketClose - lastBracketOpen - 1);
                int.TryParse(indexStr, out index);
            }

            // 2. 解析分配预制件名称
            string prefabName = "Unassigned Prefab";
            SerializedProperty prefabProp = property.FindPropertyRelative("_prefab");
            if (prefabProp != null && prefabProp.objectReferenceValue != null)
            {
                prefabName = prefabProp.objectReferenceValue.name;
            }

            SerializedProperty weightProp = property.FindPropertyRelative("_weight");
            SerializedProperty amountRangeProp = property.FindPropertyRelative("_amountRange");

            float rightX = position.x + position.width;

            Rect maxRect = default;
            Rect minRect = default;
            Rect wRect = default;

            if (amountRangeProp != null)
            {
                rightX -= 38f;
                maxRect = new Rect(rightX, position.y, 38f, EditorGUIUtility.singleLineHeight);
                rightX -= 4f;
                rightX -= 46f;
                minRect = new Rect(rightX, position.y, 46f, EditorGUIUtility.singleLineHeight);
                rightX -= 4f;
            }

            if (weightProp != null)
            {
                rightX -= 46f;
                wRect = new Rect(rightX, position.y, 46f, EditorGUIUtility.singleLineHeight);
                rightX -= 4f;
            }

            float foldoutWidth = rightX - position.x;
            Rect foldoutRect = new Rect(position.x, position.y, foldoutWidth, EditorGUIUtility.singleLineHeight);

            string titleLabel = $"[{index}] {prefabName}";
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, titleLabel, true);

            float prevLabelWidth = EditorGUIUtility.labelWidth;

            if (weightProp != null)
            {
                EditorGUIUtility.labelWidth = 16f;
                weightProp.intValue = EditorGUI.IntField(wRect, "W:", weightProp.intValue);
            }

            if (amountRangeProp != null)
            {
                Vector2Int range = amountRangeProp.vector2IntValue;
                EditorGUIUtility.labelWidth = 16f;
                int minVal = EditorGUI.IntField(minRect, "R:", range.x);
                EditorGUIUtility.labelWidth = 10f;
                int maxVal = EditorGUI.IntField(maxRect, "~", range.y);

                if (minVal < 0) minVal = 0;
                if (maxVal < minVal) maxVal = minVal;
                amountRangeProp.vector2IntValue = new Vector2Int(minVal, maxVal);
            }

            EditorGUIUtility.labelWidth = prevLabelWidth;

            // 3. 绘制展开的主体属性
            if (property.isExpanded)
            {
                float yOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // 绘制 Reset 按钮
                Rect resetRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                Rect indentedResetRect = EditorGUI.IndentedRect(resetRect);
                if (GUI.Button(indentedResetRect, "Reset to Default"))
                {
                    ResetElementProperties(property);
                }

                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.indentLevel++;
                SerializedProperty p = property.Copy();
                SerializedProperty endProperty = property.GetEndProperty();

                p.NextVisible(true);
                while (!SerializedProperty.EqualContents(p, endProperty))
                {
                    float propHeight = EditorGUI.GetPropertyHeight(p, true);
                    Rect propRect = new Rect(position.x, position.y + yOffset, position.width, propHeight);
                    EditorGUI.PropertyField(propRect, p, true);
                    yOffset += propHeight + EditorGUIUtility.standardVerticalSpacing;

                    if (!p.NextVisible(false)) break;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private void ResetElementProperties(SerializedProperty property)
        {
            try
            {
                object targetObj = GetTargetObjectWithPropertyPaths(property);
                if (targetObj is RoomElement element)
                {
                    Undo.RecordObject(property.serializedObject.targetObject, "Reset Element Properties");
                    element.Reset();
                    property.serializedObject.Update();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Reset RoomElement properties failed: {ex.Message}");
            }
        }

        private static object GetTargetObjectWithPropertyPaths(SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetFieldValue(obj, elementName, index);
                }
                else
                {
                    obj = GetFieldValue(obj, element);
                }
            }
            return obj;
        }

        private static object GetFieldValue(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();
            while (type != null)
            {
                var f = type.GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (f != null) return f.GetValue(source);
                var p = type.GetProperty(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (p != null) return p.GetValue(source, null);
                type = type.BaseType;
            }
            return null;
        }

        private static object GetFieldValue(object source, string name, int index)
        {
            var enumerable = GetFieldValue(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var en = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
            {
                if (!en.MoveNext()) return null;
            }
            return en.Current;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;

            // Foldout 行 + Reset 按钮行 + 间距
            float totalHeight = (EditorGUIUtility.singleLineHeight * 2) + (EditorGUIUtility.standardVerticalSpacing * 2);
            SerializedProperty p = property.Copy();
            SerializedProperty endProperty = property.GetEndProperty();

            p.NextVisible(true);
            while (!SerializedProperty.EqualContents(p, endProperty))
            {
                totalHeight += EditorGUI.GetPropertyHeight(p, true) + EditorGUIUtility.standardVerticalSpacing;
                if (!p.NextVisible(false)) break;
            }

            return totalHeight;
        }
    }
}
