#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class UIThemeJsonImporterWindow : EditorWindow
{
    [Serializable]
    private class ThemeJsonRoot
    {
        public List<ColorJsonEntry> colors;
        public List<TextStyleJsonEntry> textStyles;
    }

    [Serializable]
    private class ColorJsonEntry
    {
        public string key;
        public string hex;
    }

    [Serializable]
    private class TextStyleJsonEntry
    {
        public string key;
        public string fontAssetPath;
        public float fontSize = 36f;
        public string color;

        public bool outlineEnabled;
        public string outlineColor;
        public float outlineWidth;

        public bool shadowEnabled;
        public string shadowColor;
        public float shadowOffsetX;
        public float shadowOffsetY;
        public bool shadowUseGraphicAlpha = true;
    }

    private UITheme _targetTheme;
    private TextAsset _jsonFile;

    [MenuItem("Tools/UI Theme/Import JSON")]
    private static void OpenWindow()
    {
        GetWindow<UIThemeJsonImporterWindow>("UI Theme JSON Import");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        _targetTheme = (UITheme)EditorGUILayout.ObjectField("UITheme", _targetTheme, typeof(UITheme), false);
        _jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", _jsonFile, typeof(TextAsset), false);

        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(_targetTheme == null || _jsonFile == null))
        {
            if (GUILayout.Button("Import JSON Into Theme"))
            {
                Import(_targetTheme, _jsonFile.text);
            }
        }

        EditorGUILayout.HelpBox(
            "JSON format expects two arrays: colors and textStyles. " +
            "Invalid or missing values are replaced with the existing token value (or default if token is new).",
            MessageType.Info);
    }

    private static void Import(UITheme theme, string json)
    {
        if (theme == null)
        {
            Debug.LogError("UITheme import failed: theme reference is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("UITheme import failed: JSON is empty.");
            return;
        }

        ThemeJsonRoot root;
        try
        {
            root = JsonUtility.FromJson<ThemeJsonRoot>(json);
        }
        catch (Exception exception)
        {
            Debug.LogError($"UITheme import failed: invalid JSON. {exception.Message}");
            return;
        }

        if (root == null)
        {
            Debug.LogError("UITheme import failed: could not parse JSON root object.");
            return;
        }

        List<UITheme.ColorTokenEntry> importedColors = BuildColorTokens(theme, root.colors);
        List<UITheme.TextStyleTokenEntry> importedTextStyles = BuildTextStyleTokens(theme, root.textStyles);

        Undo.RecordObject(theme, "Import UI Theme JSON");
        theme.ReplaceTokens(importedColors, importedTextStyles);
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();

        Debug.Log($"UITheme import completed: {importedColors.Count} color token(s), {importedTextStyles.Count} text style token(s).", theme);
    }

    private static List<UITheme.ColorTokenEntry> BuildColorTokens(UITheme theme, List<ColorJsonEntry> entries)
    {
        var result = new List<UITheme.ColorTokenEntry>();

        if (entries == null)
        {
            return new List<UITheme.ColorTokenEntry>(theme.ColorTokens);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ColorJsonEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            string key = (entry.key ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"UITheme import: skipped color at index {i} because key is missing.");
                continue;
            }

            // Invalid/missing values keep the existing token value when available.
            Color fallback = ResolveColorFallback(theme, key);
            Color color = ParseHexWithFallback(entry.hex, fallback, $"colors[{i}] ({key})");

            result.Add(new UITheme.ColorTokenEntry
            {
                key = key,
                color = color
            });
        }

        if (result.Count == 0)
        {
            return new List<UITheme.ColorTokenEntry>(theme.ColorTokens);
        }

        return result;
    }

    private static List<UITheme.TextStyleTokenEntry> BuildTextStyleTokens(UITheme theme, List<TextStyleJsonEntry> entries)
    {
        var result = new List<UITheme.TextStyleTokenEntry>();

        if (entries == null)
        {
            return new List<UITheme.TextStyleTokenEntry>(theme.TextStyleTokens);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            TextStyleJsonEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            string key = (entry.key ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"UITheme import: skipped text style at index {i} because key is missing.");
                continue;
            }

            // Start from fallback and only override valid incoming fields.
            UITheme.ThemeTextStyle fallback = ResolveTextStyleFallback(theme, key);
            UITheme.ThemeTextStyle style = fallback;

            if (!string.IsNullOrWhiteSpace(entry.fontAssetPath))
            {
                TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(entry.fontAssetPath.Trim());
                if (fontAsset != null)
                {
                    style.fontAsset = fontAsset;
                }
                else
                {
                    Debug.LogWarning($"UITheme import: invalid font path '{entry.fontAssetPath}' for textStyles[{i}] ({key}). Using fallback font.");
                }
            }

            style.fontSize = entry.fontSize > 0f ? entry.fontSize : fallback.fontSize;
            if (entry.fontSize <= 0f)
            {
                Debug.LogWarning($"UITheme import: invalid fontSize for textStyles[{i}] ({key}). Using fallback size {fallback.fontSize}.");
            }

            style.color = ParseHexWithFallback(entry.color, fallback.color, $"textStyles[{i}].color ({key})");

            style.outlineEnabled = entry.outlineEnabled;
            style.outlineColor = ParseHexWithFallback(entry.outlineColor, fallback.outlineColor, $"textStyles[{i}].outlineColor ({key})");
            style.outlineWidth = Mathf.Clamp01(entry.outlineWidth >= 0f ? entry.outlineWidth : fallback.outlineWidth);

            style.shadowEnabled = entry.shadowEnabled;
            style.shadowColor = ParseHexWithFallback(entry.shadowColor, fallback.shadowColor, $"textStyles[{i}].shadowColor ({key})");
            style.shadowOffset = new Vector2(entry.shadowOffsetX, entry.shadowOffsetY);
            style.shadowUseGraphicAlpha = entry.shadowUseGraphicAlpha;

            result.Add(new UITheme.TextStyleTokenEntry
            {
                key = key,
                style = style
            });
        }

        if (result.Count == 0)
        {
            return new List<UITheme.TextStyleTokenEntry>(theme.TextStyleTokens);
        }

        return result;
    }

    private static Color ResolveColorFallback(UITheme theme, string key)
    {
        return theme.TryGetColor(key, out Color color) ? color : Color.white;
    }

    private static UITheme.ThemeTextStyle ResolveTextStyleFallback(UITheme theme, string key)
    {
        return theme.TryGetTextStyle(key, out UITheme.ThemeTextStyle style)
            ? style
            : UITheme.ThemeTextStyle.Default;
    }

    private static Color ParseHexWithFallback(string hex, Color fallback, string context)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback;
        }

        string normalized = hex.Trim();
        if (!normalized.StartsWith("#", StringComparison.Ordinal))
        {
            normalized = $"#{normalized}";
        }

        if (ColorUtility.TryParseHtmlString(normalized, out Color parsed))
        {
            return parsed;
        }

        Debug.LogWarning($"UITheme import: invalid color '{hex}' in {context}. Using fallback value.");
        return fallback;
    }
}
#endif
