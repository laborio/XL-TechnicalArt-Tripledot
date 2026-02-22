using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class UIThemeText : MonoBehaviour
{
    [SerializeField]
    private UITheme theme;

    [SerializeField]
    private UITheme.TextStyleToken styleToken = UITheme.TextStyleToken.Body;

    [SerializeField]
    private string customStyleKey = "Body";

    private TextMeshProUGUI _tmpText;
    private bool _hasAppliedAtLeastOnce;

    private void Start()
    {
        ApplyTheme();
    }

    private void OnEnable()
    {
        ApplyTheme();
    }

    private void OnValidate()
    {
        ApplyTheme();
    }

    [ContextMenu("Apply Theme")]
    public void ApplyTheme()
    {
        if (!TryGetTextComponent(out TextMeshProUGUI tmpText))
        {
            return;
        }

        bool hasValidMaterial = TrySyncMaterialWithFont(tmpText, out bool materialChanged);

        if (theme == null)
        {
            RefreshTextMeshIfNeeded(tmpText, hasValidMaterial, materialChanged);
            return;
        }

        string key = ResolveStyleKey();
        if (!theme.TryGetTextStyle(key, out UITheme.ThemeTextStyle style))
        {
            RefreshTextMeshIfNeeded(tmpText, hasValidMaterial, materialChanged);
            return;
        }

        if (style.fontAsset != null)
        {
            if (tmpText.font != style.fontAsset)
            {
                tmpText.font = style.fontAsset;
            }
        }

        // Keep font and materials in sync. If these diverge, TMP can sample
        // the wrong atlas and render fragmented glyphs.
        hasValidMaterial = TrySyncMaterialWithFont(tmpText, out materialChanged);
        if (!hasValidMaterial)
        {
            return;
        }

        bool visualsChanged = materialChanged || !_hasAppliedAtLeastOnce;

        if (!Mathf.Approximately(tmpText.fontSize, style.fontSize))
        {
            tmpText.fontSize = style.fontSize;
            visualsChanged = true;
        }

        if (tmpText.color != style.color)
        {
            tmpText.color = style.color;
            visualsChanged = true;
        }

        // In edit-time OnValidate, TMP may not have a shared material yet.
        // Outline setters create material instances and will throw if source is null.
        if (TryEnsureCanvasRenderer(tmpText))
        {
            float targetOutlineWidth = style.outlineEnabled ? Mathf.Clamp01(style.outlineWidth) : 0f;
            if (tmpText.outlineColor != style.outlineColor)
            {
                tmpText.outlineColor = style.outlineColor;
                visualsChanged = true;
            }

            if (!Mathf.Approximately(tmpText.outlineWidth, targetOutlineWidth))
            {
                tmpText.outlineWidth = targetOutlineWidth;
                visualsChanged = true;
            }
        }

        // Shadow is applied using the standard UI Shadow component for compatibility.
        Shadow shadow = tmpText.GetComponent<Shadow>();
        if (style.shadowEnabled)
        {
            if (shadow == null)
            {
                shadow = tmpText.gameObject.AddComponent<Shadow>();
                visualsChanged = true;
            }

            if (!shadow.enabled)
            {
                shadow.enabled = true;
                visualsChanged = true;
            }

            if (shadow.effectColor != style.shadowColor)
            {
                shadow.effectColor = style.shadowColor;
                visualsChanged = true;
            }

            if (shadow.effectDistance != style.shadowOffset)
            {
                shadow.effectDistance = style.shadowOffset;
                visualsChanged = true;
            }

            if (shadow.useGraphicAlpha != style.shadowUseGraphicAlpha)
            {
                shadow.useGraphicAlpha = style.shadowUseGraphicAlpha;
                visualsChanged = true;
            }
        }
        else if (shadow != null && shadow.enabled)
        {
            shadow.enabled = false;
            visualsChanged = true;
        }

        if (!visualsChanged)
        {
            return;
        }

        if (TryEnsureCanvasRenderer(tmpText))
        {
            tmpText.UpdateMeshPadding();
            tmpText.SetAllDirty();
            tmpText.ForceMeshUpdate();
        }

        _hasAppliedAtLeastOnce = true;
    }

    private string ResolveStyleKey()
    {
        return styleToken == UITheme.TextStyleToken.Custom
            ? customStyleKey
            : UITheme.ToKey(styleToken);
    }

    private bool TryGetTextComponent(out TextMeshProUGUI tmpText)
    {
        if (_tmpText == null)
        {
            _tmpText = GetComponent<TextMeshProUGUI>();
        }

        tmpText = _tmpText;
        return tmpText != null;
    }

    private static bool TrySyncMaterialWithFont(TextMeshProUGUI tmpText, out bool materialChanged)
    {
        materialChanged = false;

        if (tmpText == null)
        {
            return false;
        }

        if (tmpText.font == null || tmpText.font.material == null)
        {
            return false;
        }

        Material expected = tmpText.font.material;
        if (!MaterialUsesFontAtlas(tmpText.fontSharedMaterial, tmpText.font))
        {
            tmpText.fontSharedMaterial = expected;
            materialChanged = true;
        }

        // TMP can keep a separate instance material for per-object overrides.
        // Make sure that instance (if any) also points to the same font atlas.
        Material instanceMaterial = tmpText.fontMaterial;
        if (instanceMaterial != null && !MaterialUsesFontAtlas(instanceMaterial, tmpText.font))
        {
            tmpText.fontMaterial = expected;
            materialChanged = true;
        }

        return MaterialUsesFontAtlas(tmpText.fontSharedMaterial, tmpText.font);
    }

    private static bool MaterialUsesFontAtlas(Material material, TMP_FontAsset fontAsset)
    {
        if (material == null || fontAsset == null || fontAsset.atlasTexture == null)
        {
            return false;
        }

        if (!material.HasProperty(ShaderUtilities.ID_MainTex))
        {
            return false;
        }

        return material.GetTexture(ShaderUtilities.ID_MainTex) == fontAsset.atlasTexture;
    }

    private static bool TryEnsureCanvasRenderer(TextMeshProUGUI tmpText)
    {
        if (tmpText == null)
        {
            return false;
        }

        if (tmpText.canvasRenderer != null)
        {
            return true;
        }

        return tmpText.TryGetComponent(out CanvasRenderer _);
    }

    private static void RefreshTextMeshIfNeeded(TextMeshProUGUI tmpText, bool hasValidMaterial, bool materialChanged)
    {
        if (!materialChanged || !hasValidMaterial)
        {
            return;
        }

        if (!TryEnsureCanvasRenderer(tmpText))
        {
            return;
        }

        tmpText.UpdateMeshPadding();
        tmpText.SetAllDirty();
        tmpText.ForceMeshUpdate();
    }
}
