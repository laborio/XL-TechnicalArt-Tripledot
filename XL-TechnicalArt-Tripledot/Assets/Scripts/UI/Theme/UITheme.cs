using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "UITheme", menuName = "UI/UI Theme")]
public class UITheme : ScriptableObject
{
    public enum ColorToken
    {
        Primary,
        Secondary,
        TextDefault,
        TextLight,
        Alert,
        Custom
    }

    public enum TextStyleToken
    {
        Heading,
        Body,
        Caption,
        Alert,
        Custom
    }

    [Serializable]
    public struct ThemeTextStyle
    {
        [Tooltip("Optional TMP font asset. Leave null to keep the current component font.")]
        public TMP_FontAsset fontAsset;

        [Min(1f)]
        public float fontSize;

        public Color color;

        public bool outlineEnabled;
        public Color outlineColor;

        [Range(0f, 1f)]
        public float outlineWidth;

        public bool shadowEnabled;
        public Color shadowColor;
        public Vector2 shadowOffset;
        public bool shadowUseGraphicAlpha;

        public static ThemeTextStyle Default => new ThemeTextStyle
        {
            fontAsset = null,
            fontSize = 36f,
            color = Color.white,
            outlineEnabled = false,
            outlineColor = Color.black,
            outlineWidth = 0f,
            shadowEnabled = false,
            shadowColor = new Color(0f, 0f, 0f, 0.5f),
            shadowOffset = new Vector2(1f, -1f),
            shadowUseGraphicAlpha = true
        };
    }

    [Serializable]
    public class ColorTokenEntry
    {
        public string key = "Primary";
        public Color color = Color.white;
    }

    [Serializable]
    public class TextStyleTokenEntry
    {
        public string key = "Body";
        public ThemeTextStyle style = ThemeTextStyle.Default;
    }

    [SerializeField]
    // String-keyed color tokens make it easy to add new entries without code changes.
    private List<ColorTokenEntry> colorTokens = new List<ColorTokenEntry>
    {
        new ColorTokenEntry { key = "Primary", color = new Color(0.24f, 0.57f, 1f, 1f) },
        new ColorTokenEntry { key = "Secondary", color = new Color(0.15f, 0.82f, 0.72f, 1f) },
        new ColorTokenEntry { key = "TextDefault", color = new Color(0.10f, 0.12f, 0.16f, 1f) },
        new ColorTokenEntry { key = "TextLight", color = Color.white },
        new ColorTokenEntry { key = "Alert", color = new Color(0.95f, 0.29f, 0.22f, 1f) }
    };

    [SerializeField]
    // Text style tokens are also string-keyed, with enum defaults as a convenience.
    private List<TextStyleTokenEntry> textStyleTokens = new List<TextStyleTokenEntry>
    {
        new TextStyleTokenEntry
        {
            key = "Heading",
            style = new ThemeTextStyle
            {
                fontAsset = null,
                fontSize = 52f,
                color = Color.white,
                outlineEnabled = false,
                outlineColor = Color.black,
                outlineWidth = 0f,
                shadowEnabled = false,
                shadowColor = new Color(0f, 0f, 0f, 0.5f),
                shadowOffset = new Vector2(1f, -1f),
                shadowUseGraphicAlpha = true
            }
        },
        new TextStyleTokenEntry
        {
            key = "Body",
            style = new ThemeTextStyle
            {
                fontAsset = null,
                fontSize = 36f,
                color = Color.white,
                outlineEnabled = false,
                outlineColor = Color.black,
                outlineWidth = 0f,
                shadowEnabled = false,
                shadowColor = new Color(0f, 0f, 0f, 0.5f),
                shadowOffset = new Vector2(1f, -1f),
                shadowUseGraphicAlpha = true
            }
        },
        new TextStyleTokenEntry
        {
            key = "Caption",
            style = new ThemeTextStyle
            {
                fontAsset = null,
                fontSize = 26f,
                color = Color.white,
                outlineEnabled = false,
                outlineColor = Color.black,
                outlineWidth = 0f,
                shadowEnabled = false,
                shadowColor = new Color(0f, 0f, 0f, 0.5f),
                shadowOffset = new Vector2(1f, -1f),
                shadowUseGraphicAlpha = true
            }
        },
        new TextStyleTokenEntry
        {
            key = "Alert",
            style = new ThemeTextStyle
            {
                fontAsset = null,
                fontSize = 36f,
                color = new Color(0.95f, 0.29f, 0.22f, 1f),
                outlineEnabled = false,
                outlineColor = Color.black,
                outlineWidth = 0f,
                shadowEnabled = false,
                shadowColor = new Color(0f, 0f, 0f, 0.5f),
                shadowOffset = new Vector2(1f, -1f),
                shadowUseGraphicAlpha = true
            }
        }
    };

    private readonly Dictionary<string, Color> _colorLookup = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ThemeTextStyle> _textStyleLookup = new Dictionary<string, ThemeTextStyle>(StringComparer.OrdinalIgnoreCase);
    private bool _isCacheBuilt;

    public IReadOnlyList<ColorTokenEntry> ColorTokens => colorTokens;
    public IReadOnlyList<TextStyleTokenEntry> TextStyleTokens => textStyleTokens;

    public static string ToKey(ColorToken token)
    {
        return token.ToString();
    }

    public static string ToKey(TextStyleToken token)
    {
        return token.ToString();
    }

    public bool TryGetColor(string key, out Color color)
    {
        EnsureCache();

        if (string.IsNullOrWhiteSpace(key))
        {
            color = Color.white;
            return false;
        }

        return _colorLookup.TryGetValue(key.Trim(), out color);
    }

    public bool TryGetColor(ColorToken token, out Color color)
    {
        return TryGetColor(ToKey(token), out color);
    }

    public Color GetColorOrDefault(string key, Color fallback)
    {
        return TryGetColor(key, out Color color) ? color : fallback;
    }

    public bool TryGetTextStyle(string key, out ThemeTextStyle style)
    {
        EnsureCache();

        if (string.IsNullOrWhiteSpace(key))
        {
            style = ThemeTextStyle.Default;
            return false;
        }

        return _textStyleLookup.TryGetValue(key.Trim(), out style);
    }

    public bool TryGetTextStyle(TextStyleToken token, out ThemeTextStyle style)
    {
        return TryGetTextStyle(ToKey(token), out style);
    }

    public ThemeTextStyle GetTextStyleOrDefault(string key, ThemeTextStyle fallback)
    {
        return TryGetTextStyle(key, out ThemeTextStyle style) ? style : fallback;
    }

    public void ReplaceTokens(List<ColorTokenEntry> newColorTokens, List<TextStyleTokenEntry> newTextStyleTokens)
    {
        colorTokens = newColorTokens ?? new List<ColorTokenEntry>();
        textStyleTokens = newTextStyleTokens ?? new List<TextStyleTokenEntry>();
        RebuildCache();
    }

    private void OnValidate()
    {
        RebuildCache();
    }

    private void EnsureCache()
    {
        if (_isCacheBuilt)
        {
            return;
        }

        RebuildCache();
    }

    private void RebuildCache()
    {
        // Build case-insensitive lookups for fast runtime access.
        _colorLookup.Clear();
        _textStyleLookup.Clear();

        for (int i = 0; i < colorTokens.Count; i++)
        {
            ColorTokenEntry entry = colorTokens[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            _colorLookup[entry.key.Trim()] = entry.color;
        }

        for (int i = 0; i < textStyleTokens.Count; i++)
        {
            TextStyleTokenEntry entry = textStyleTokens[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            _textStyleLookup[entry.key.Trim()] = entry.style;
        }

        _isCacheBuilt = true;
    }
}
