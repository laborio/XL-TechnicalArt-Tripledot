#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIThemeText))]
[CanEditMultipleObjects]
public class UIThemeTextEditor : Editor
{
    private SerializedProperty _themeProperty;
    private SerializedProperty _styleTokenProperty;
    private SerializedProperty _customStyleKeyProperty;

    private void OnEnable()
    {
        _themeProperty = serializedObject.FindProperty("theme");
        _styleTokenProperty = serializedObject.FindProperty("styleToken");
        _customStyleKeyProperty = serializedObject.FindProperty("customStyleKey");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_themeProperty);
        ForceCustomEnumSelection(_styleTokenProperty, (int)UITheme.TextStyleToken.Custom);
        DrawKeySelector(
            label: "Text Style Key",
            keyProperty: _customStyleKeyProperty,
            keys: GetTextStyleKeys(_themeProperty.objectReferenceValue as UITheme));

        serializedObject.ApplyModifiedProperties();

        DrawApplyButtonForTargets();
    }

    private static List<string> GetTextStyleKeys(UITheme theme)
    {
        var keys = new List<string>();
        if (theme == null || theme.TextStyleTokens == null)
        {
            return keys;
        }

        for (int i = 0; i < theme.TextStyleTokens.Count; i++)
        {
            UITheme.TextStyleTokenEntry entry = theme.TextStyleTokens[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            string key = entry.key.Trim();
            if (!ContainsIgnoreCase(keys, key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    private void DrawApplyButtonForTargets()
    {
        if (!GUILayout.Button("Apply Theme Now"))
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] is UIThemeText textBinder)
            {
                textBinder.ApplyTheme();
                EditorUtility.SetDirty(textBinder);
            }
        }
    }

    private static void DrawKeySelector(string label, SerializedProperty keyProperty, List<string> keys)
    {
        if (keyProperty == null)
        {
            return;
        }

        if (keys == null || keys.Count == 0)
        {
            EditorGUILayout.PropertyField(keyProperty, new GUIContent(label));
            EditorGUILayout.HelpBox("No keys found in the selected UITheme.", MessageType.Info);
            return;
        }

        string currentValue = keyProperty.stringValue ?? string.Empty;
        int selectedIndex = 0;
        for (int i = 0; i < keys.Count; i++)
        {
            if (string.Equals(keys[i], currentValue, StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = i;
                break;
            }
        }

        int newIndex = EditorGUILayout.Popup(label, selectedIndex, keys.ToArray());
        if (newIndex >= 0 && newIndex < keys.Count)
        {
            keyProperty.stringValue = keys[newIndex];
        }
    }

    private static bool ContainsIgnoreCase(List<string> values, string item)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (string.Equals(values[i], item, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ForceCustomEnumSelection(SerializedProperty enumProperty, int customValue)
    {
        if (enumProperty == null)
        {
            return;
        }

        enumProperty.enumValueIndex = customValue;
    }
}

[CustomEditor(typeof(UIThemeImage))]
[CanEditMultipleObjects]
public class UIThemeImageEditor : Editor
{
    private SerializedProperty _themeProperty;
    private SerializedProperty _colorTokenProperty;
    private SerializedProperty _customColorKeyProperty;

    private void OnEnable()
    {
        _themeProperty = serializedObject.FindProperty("theme");
        _colorTokenProperty = serializedObject.FindProperty("colorToken");
        _customColorKeyProperty = serializedObject.FindProperty("customColorKey");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_themeProperty);
        ForceCustomEnumSelection(_colorTokenProperty, (int)UITheme.ColorToken.Custom);
        DrawKeySelector(
            label: "Color Key",
            keyProperty: _customColorKeyProperty,
            keys: GetColorKeys(_themeProperty.objectReferenceValue as UITheme));

        serializedObject.ApplyModifiedProperties();

        DrawApplyButtonForTargets();
    }

    private static List<string> GetColorKeys(UITheme theme)
    {
        var keys = new List<string>();
        if (theme == null || theme.ColorTokens == null)
        {
            return keys;
        }

        for (int i = 0; i < theme.ColorTokens.Count; i++)
        {
            UITheme.ColorTokenEntry entry = theme.ColorTokens[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            string key = entry.key.Trim();
            if (!ContainsIgnoreCase(keys, key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    private void DrawApplyButtonForTargets()
    {
        if (!GUILayout.Button("Apply Theme Now"))
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] is UIThemeImage imageBinder)
            {
                imageBinder.ApplyTheme();
                EditorUtility.SetDirty(imageBinder);
            }
        }
    }

    private static void DrawKeySelector(string label, SerializedProperty keyProperty, List<string> keys)
    {
        if (keyProperty == null)
        {
            return;
        }

        if (keys == null || keys.Count == 0)
        {
            EditorGUILayout.PropertyField(keyProperty, new GUIContent(label));
            EditorGUILayout.HelpBox("No keys found in the selected UITheme.", MessageType.Info);
            return;
        }

        string currentValue = keyProperty.stringValue ?? string.Empty;
        int selectedIndex = 0;
        for (int i = 0; i < keys.Count; i++)
        {
            if (string.Equals(keys[i], currentValue, StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = i;
                break;
            }
        }

        int newIndex = EditorGUILayout.Popup(label, selectedIndex, keys.ToArray());
        if (newIndex >= 0 && newIndex < keys.Count)
        {
            keyProperty.stringValue = keys[newIndex];
        }
    }

    private static bool ContainsIgnoreCase(List<string> values, string item)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (string.Equals(values[i], item, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ForceCustomEnumSelection(SerializedProperty enumProperty, int customValue)
    {
        if (enumProperty == null)
        {
            return;
        }

        enumProperty.enumValueIndex = customValue;
    }
}
#endif
