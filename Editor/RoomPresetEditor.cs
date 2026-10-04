using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder.Editor
{
    /// <summary>
    /// 自定义 RoomPreset Inspector 编辑器。
    /// 支持在 Preset 面板中直接对所引用的 StructureGroup 和 DecorationGroup 进行原生的可折叠内嵌展开编辑。
    /// </summary>
    [CustomEditor(typeof(RoomPreset))]
    public class RoomPresetEditor : UnityEditor.Editor
    {
        private readonly Dictionary<Object, bool> _soFoldouts = new Dictionary<Object, bool>();
        private readonly Dictionary<Object, SerializedObject> _soSerializedObjects = new Dictionary<Object, SerializedObject>();

        private void OnDisable()
        {
            _soSerializedObjects.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty structGroupsProp = serializedObject.FindProperty("_structureGroups");
            SerializedProperty decorGroupsProp = serializedObject.FindProperty("_decorationGroups");

            if (structGroupsProp != null)
            {
                EditorGUILayout.Space(5);
                DrawSOListWithFoldouts(structGroupsProp, "Structure Groups");
            }

            EditorGUILayout.Space(15);

            if (decorGroupsProp != null)
            {
                DrawSOListWithFoldouts(decorGroupsProp, "Decoration Groups");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSOListWithFoldouts(SerializedProperty listProp, string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty elemProp = listProp.GetArrayElementAtIndex(i);
                Object targetSO = elemProp.objectReferenceValue;

                EditorGUILayout.BeginHorizontal();

                // 1. 折叠展开勾选框
                bool expanded = false;
                if (targetSO != null)
                {
                    if (!_soFoldouts.ContainsKey(targetSO)) _soFoldouts[targetSO] = false;
                    _soFoldouts[targetSO] = EditorGUILayout.Toggle(_soFoldouts[targetSO], EditorStyles.foldout, GUILayout.Width(15));
                    expanded = _soFoldouts[targetSO];
                }
                else
                {
                    GUILayout.Space(15);
                }

                // 2. SO 对象槽位
                EditorGUILayout.PropertyField(elemProp, GUIContent.none);

                // 3. Ping 按钮
                if (targetSO != null)
                {
                    if (GUILayout.Button("Ping", GUILayout.Width(42), GUILayout.Height(18)))
                    {
                        EditorGUIUtility.PingObject(targetSO);
                    }
                }

                // 4. 移除按钮
                if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(18)))
                {
                    listProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                // 5. 内嵌展开编辑区
                if (expanded && targetSO != null)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUI.indentLevel++;

                    if (!_soSerializedObjects.TryGetValue(targetSO, out SerializedObject childSO) || childSO.targetObject == null || childSO.targetObject != targetSO)
                    {
                        childSO = new SerializedObject(targetSO);
                        _soSerializedObjects[targetSO] = childSO;
                    }

                    childSO.Update();

                    SerializedProperty iterator = childSO.GetIterator();
                    bool enterChildren = true;
                    while (iterator.NextVisible(enterChildren))
                    {
                        enterChildren = false;
                        if (iterator.name == "m_Script") continue;
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    childSO.ApplyModifiedProperties();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(2);
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("+ Add Group Element"))
            {
                listProp.arraySize++;
                listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = null;
            }

            EditorGUILayout.EndVertical();
        }
    }
}
