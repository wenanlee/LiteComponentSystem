using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(EntityComponentVolume))]
public class ToggleGroupExamplesEditor : Editor
{
    // ��������
    private const string PROP_ENABLED = "enabled";
    private const float PROPERTY_INDENT = 15f;
    private const float VERTICAL_PADDING = 2f;
    private const float TOGGLE_WIDTH = 20f;
    private const int TITLE_FONT_SIZE = 14;

    // ���л�����
    private SerializedProperty _componentsProperty;

    // �۵�״̬����
    private List<bool> _foldoutStates = new List<bool>();

    // ���������б�
    private ReorderableList _reorderableList;

    // ������Զ����������
    private static List<Type> _cachedComponentTypes;

    /// <summary>
    /// ��ȡ���м̳���CustomComponent�ķǳ�������
    /// </summary>
    private static List<Type> CustomComponentTypes
    {
        get
        {
            if (_cachedComponentTypes != null)
                return _cachedComponentTypes;

            _cachedComponentTypes = new List<Type>();
            Type baseType = typeof(EntityComponent);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes()
                        .Where(t => t.IsSubclassOf(baseType) && !t.IsAbstract))
                    {
                        _cachedComponentTypes.Add(type);
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    Debug.LogWarning($"Skipped assembly due to load error: {assembly.FullName}");
                }
            }
            return _cachedComponentTypes;
        }
    }

    private void OnEnable()
    {
        // ��ʼ�����л�����
        _componentsProperty = serializedObject.FindProperty("Components");
        InitializeFoldoutStates();
        CreateReorderableList();
    }

    /// <summary>
    /// ��ʼ���۵�״̬�б�
    /// </summary>
    private void InitializeFoldoutStates()
    {
        _foldoutStates = new List<bool>(_componentsProperty.arraySize);
        for (int i = 0; i < _componentsProperty.arraySize; i++)
        {
            _foldoutStates.Add(false);
        }
    }

    /// <summary>
    /// �������������б�
    /// </summary>
    private void CreateReorderableList()
    {
        _reorderableList = new ReorderableList(
            serializedObject,
            _componentsProperty,
            draggable: true,
            displayHeader: true,
            displayAddButton: true,
            displayRemoveButton: true
        );

        // �б�������
        _reorderableList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Components");
        };

        // Ԫ�ػ��ƻص�
        _reorderableList.drawElementCallback = DrawListElement;

        // Ԫ�ظ߶ȼ���
        _reorderableList.elementHeightCallback = CalculateElementHeight;

        // ���Ԫ�ػص�
        _reorderableList.onAddCallback = _ => ShowComponentSelectionMenu();
    }

    /// <summary>
    /// ���Ƶ����б�Ԫ��
    /// </summary>
    private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        // �߽���
        if (index >= _foldoutStates.Count) return;

        var element = _componentsProperty.GetArrayElementAtIndex(index);
        var enabledProp = element.FindPropertyRelative(PROP_ENABLED);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        var headerRect = new Rect(rect.x, rect.y, rect.width, lineHeight);

        // �������ÿ���
        var toggleRect = new Rect(headerRect.x, headerRect.y, TOGGLE_WIDTH, lineHeight);
        enabledProp.boolValue = EditorGUI.Toggle(toggleRect, enabledProp.boolValue);

        // �����������
        var labelRect = new Rect(
            toggleRect.xMax + 5,
            headerRect.y,
            rect.width - TOGGLE_WIDTH - 10,
            lineHeight
        );

        var labelStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = TITLE_FONT_SIZE
        };

        EditorGUI.LabelField(labelRect, GetComponentDisplayName(element), labelStyle);

        // ����������¼�
        HandleHeaderClick(headerRect, toggleRect, index);

        // ����չ���������
        if (_foldoutStates[index])
        {
            DrawComponentProperties(rect, element, headerRect.yMax + VERTICAL_PADDING);
        }
    }

    /// <summary>
    /// �������������¼����л��۵�״̬��
    /// </summary>
    private void HandleHeaderClick(Rect headerRect, Rect toggleRect, int index)
    {
        if (Event.current.type == EventType.MouseDown &&
            headerRect.Contains(Event.current.mousePosition) &&
            !toggleRect.Contains(Event.current.mousePosition))
        {
            _foldoutStates[index] = !_foldoutStates[index];
            Event.current.Use();
        }
    }

    /// <summary>
    /// �����������
    /// </summary>
    private void DrawComponentProperties(Rect containerRect, SerializedProperty element, float startY)
    {
        float currentY = startY;
        SerializedProperty iterator = element.Copy();
        SerializedProperty endProperty = element.GetEndProperty();

        bool enterChildren = true;
        iterator.NextVisible(enterChildren); // ������������

        while (iterator.NextVisible(enterChildren) &&
              !SerializedProperty.EqualContents(iterator, endProperty))
        {
            if (iterator.name == PROP_ENABLED) continue;

            float propertyHeight = EditorGUI.GetPropertyHeight(iterator, includeChildren: true);
            var propertyRect = new Rect(
                containerRect.x + PROPERTY_INDENT,
                currentY,
                containerRect.width - PROPERTY_INDENT,
                propertyHeight
            );

            EditorGUI.PropertyField(propertyRect, iterator, includeChildren: true);
            currentY += propertyHeight + VERTICAL_PADDING;
            enterChildren = false;
        }
    }

    /// <summary>
    /// ����Ԫ�ظ߶�
    /// </summary>
    private float CalculateElementHeight(int index)
    {
        if (index >= _foldoutStates.Count)
            return EditorGUIUtility.singleLineHeight;

        float height = EditorGUIUtility.singleLineHeight + VERTICAL_PADDING;

        if (_foldoutStates[index])
        {
            var element = _componentsProperty.GetArrayElementAtIndex(index);
            SerializedProperty iterator = element.Copy();
            SerializedProperty endProperty = element.GetEndProperty();

            bool enterChildren = true;
            iterator.NextVisible(enterChildren); // ������������

            while (iterator.NextVisible(enterChildren) &&
                  !SerializedProperty.EqualContents(iterator, endProperty))
            {
                if (iterator.name == PROP_ENABLED) continue;
                height += EditorGUI.GetPropertyHeight(iterator, true) + VERTICAL_PADDING;
                enterChildren = false;
            }
        }

        return height;
    }

    /// <summary>
    /// ��ȡ�����ʾ����
    /// </summary>
    private string GetComponentDisplayName(SerializedProperty element)
    {
        return element.managedReferenceValue switch
        {
            EntityComponent comp => comp.name,
            null => "Missing Component",
            _ => element.managedReferenceValue.GetType().Name
        };
    }

    /// <summary>
    /// ��ʾ���ѡ��˵�
    /// </summary>
    private void ShowComponentSelectionMenu()
    {
        var menu = new GenericMenu();

        foreach (var type in CustomComponentTypes)
        {
            string menuPath = type.Namespace != null
                ? $"{type.Namespace.Replace('.', '/')}/{type.Name}"
                : type.Name;

            menu.AddItem(new GUIContent(menuPath), false, () => AddNewComponent(type));
        }

        menu.ShowAsContext();
    }

    /// <summary>
    /// �����������б�
    /// </summary>
    private void AddNewComponent(Type componentType)
    {
        serializedObject.Update();

        int newIndex = _componentsProperty.arraySize;
        _componentsProperty.arraySize++;
        serializedObject.ApplyModifiedPropertiesWithoutUndo(); // ������Ⱦ����ջ

        serializedObject.Update();
        var newElement = _componentsProperty.GetArrayElementAtIndex(newIndex);
        newElement.managedReferenceValue = Activator.CreateInstance(componentType);
        _foldoutStates.Add(true); // Ĭ��չ�������

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    /// <summary>
    /// �������б�
    /// </summary>
    private void ClearComponentList()
    {
        serializedObject.Update();
        _componentsProperty.ClearArray();
        _foldoutStates.Clear();
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ͬ���۵�״̬�������С
        while (_foldoutStates.Count < _componentsProperty.arraySize)
            _foldoutStates.Add(false);
        while (_foldoutStates.Count > _componentsProperty.arraySize)
            _foldoutStates.RemoveAt(_foldoutStates.Count - 1);

        // ������ť����
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("AddItem", GUILayout.Width(100)))
            ShowComponentSelectionMenu();
        if (GUILayout.Button("ClearList", GUILayout.Width(100)))
            ClearComponentList();
        EditorGUILayout.EndHorizontal();

        // �����б�
        _reorderableList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}