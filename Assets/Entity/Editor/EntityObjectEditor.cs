using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EntityObject))]
public class EntityObjectEditor : Editor
{
    private SerializedObject volumeSerializedObject;
    private SerializedProperty volumeProperty;
    private Editor volumeEditor;

    void OnEnable()
    {
        volumeProperty = serializedObject.FindProperty("volume");
        CreateVolumeEditor();
    }

    void OnDisable()
    {
        DestroyVolumeEditor();
    }

    private void CreateVolumeEditor()
    {
        DestroyVolumeEditor();
        if (volumeProperty != null && volumeProperty.objectReferenceValue != null)
        {
            volumeSerializedObject = new SerializedObject(volumeProperty.objectReferenceValue);
            volumeEditor = Editor.CreateEditor(volumeProperty.objectReferenceValue);
        }
    }

    private void DestroyVolumeEditor()
    {
        if (volumeEditor != null)
        {
            DestroyImmediate(volumeEditor);
            volumeEditor = null;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. ����Ĭ�����ԣ��ų�volume�ֶΣ�
        DrawPropertiesExcluding(serializedObject, "volume");

        // 2. ��������volume�ֶ�
        EditorGUILayout.PropertyField(volumeProperty);

        // 3. ���volume���ñ仯
        if (volumeProperty.objectReferenceValue == null)
        {
            DestroyVolumeEditor();
        }
        else if (volumeEditor == null || volumeEditor.target != volumeProperty.objectReferenceValue)
        {
            CreateVolumeEditor();
        }

        // 4. ����volume��������ͼ
        if (volumeProperty.objectReferenceValue != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Volume Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            volumeSerializedObject.Update();
            volumeEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                volumeSerializedObject.ApplyModifiedProperties();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}