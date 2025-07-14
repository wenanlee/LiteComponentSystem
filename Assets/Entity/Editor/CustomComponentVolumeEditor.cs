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
    // 常量定义
    private const string PROP_ENABLED = "enabled";
    private const float PROPERTY_INDENT = 15f;
    private const float VERTICAL_PADDING = 2f;
    private const float TOGGLE_WIDTH = 20f;
    private const int TITLE_FONT_SIZE = 14;

    // 序列化属性
    private SerializedProperty _componentsProperty;

    // 折叠状态管理
    private List<bool> _foldoutStates = new List<bool>();

    // 可重排序列表
    private ReorderableList _reorderableList;

    // 缓存的自定义组件类型
    private static List<Type> _cachedComponentTypes;

    /// <summary>
    /// 获取所有继承自CustomComponent的非抽象类型
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
        // 初始化序列化属性
        _componentsProperty = serializedObject.FindProperty("Components");
        InitializeFoldoutStates();
        CreateReorderableList();
    }

    /// <summary>
    /// 初始化折叠状态列表
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
    /// 创建可重排序列表
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

        // 列表标题绘制
        _reorderableList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Components");
        };

        // 元素绘制回调
        _reorderableList.drawElementCallback = DrawListElement;

        // 元素高度计算
        _reorderableList.elementHeightCallback = CalculateElementHeight;

        // 添加元素回调
        _reorderableList.onAddCallback = _ => ShowComponentSelectionMenu();
    }

    /// <summary>
    /// 绘制单个列表元素
    /// </summary>
    private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        // 边界检查
        if (index >= _foldoutStates.Count) return;

        var element = _componentsProperty.GetArrayElementAtIndex(index);
        var enabledProp = element.FindPropertyRelative(PROP_ENABLED);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        var headerRect = new Rect(rect.x, rect.y, rect.width, lineHeight);

        // 绘制启用开关
        var toggleRect = new Rect(headerRect.x, headerRect.y, TOGGLE_WIDTH, lineHeight);
        enabledProp.boolValue = EditorGUI.Toggle(toggleRect, enabledProp.boolValue);

        // 绘制组件标题
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

        // 处理标题点击事件
        HandleHeaderClick(headerRect, toggleRect, index);

        // 绘制展开后的属性
        if (_foldoutStates[index])
        {
            DrawComponentProperties(rect, element, headerRect.yMax + VERTICAL_PADDING);
        }
    }

    /// <summary>
    /// 处理标题栏点击事件（切换折叠状态）
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
    /// 绘制组件属性
    /// </summary>
    private void DrawComponentProperties(Rect containerRect, SerializedProperty element, float startY)
    {
        float currentY = startY;
        SerializedProperty iterator = element.Copy();
        SerializedProperty endProperty = element.GetEndProperty();

        bool enterChildren = true;
        iterator.NextVisible(enterChildren); // 跳过基类属性

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
    /// 计算元素高度
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
            iterator.NextVisible(enterChildren); // 跳过基类属性

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
    /// 获取组件显示名称
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
    /// 显示组件选择菜单
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
    /// 添加新组件到列表
    /// </summary>
    private void AddNewComponent(Type componentType)
    {
        serializedObject.Update();

        int newIndex = _componentsProperty.arraySize;
        _componentsProperty.arraySize++;
        serializedObject.ApplyModifiedPropertiesWithoutUndo(); // 避免污染撤销栈

        serializedObject.Update();
        var newElement = _componentsProperty.GetArrayElementAtIndex(newIndex);
        newElement.managedReferenceValue = Activator.CreateInstance(componentType);
        _foldoutStates.Add(true); // 默认展开新组件

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    /// <summary>
    /// 清空组件列表
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

        // 同步折叠状态与数组大小
        while (_foldoutStates.Count < _componentsProperty.arraySize)
            _foldoutStates.Add(false);
        while (_foldoutStates.Count > _componentsProperty.arraySize)
            _foldoutStates.RemoveAt(_foldoutStates.Count - 1);

        // 操作按钮区域
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("AddItem", GUILayout.Width(100)))
            ShowComponentSelectionMenu();
        if (GUILayout.Button("ClearList", GUILayout.Width(100)))
            ClearComponentList();
        EditorGUILayout.EndHorizontal();

        // 绘制列表
        _reorderableList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}