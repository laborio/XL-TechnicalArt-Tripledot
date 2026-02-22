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
        if (!TryGetTextComponent(out TextMeshProUGUI tmpText) || theme == null)
        {
            return;
        }

        string key = ResolveStyleKey();
        if (!theme.TryGetTextStyle(key, out UITheme.ThemeTextStyle style))
        {
            return;
        }

        if (style.fontAsset != null)
        {
            tmpText.font = style.fontAsset;
        }

        // Keep font and shared material in sync. If these diverge, TMP can sample
        // the wrong atlas and render fragmented glyphs.
        if (!TrySyncSharedMaterialWithFont(tmpText))
        {
            return;
        }

        tmpText.fontSize = style.fontSize;
        tmpText.color = style.color;

        // In edit-time OnValidate, TMP may not have a shared material yet.
        // Outline setters create material instances and will throw if source is null.
        if (TryEnsureCanvasRenderer(tmpText))
        {
            tmpText.outlineColor = style.outlineColor;
            tmpText.outlineWidth = style.outlineEnabled ? Mathf.Clamp01(style.outlineWidth) : 0f;
        }

        // Shadow is applied using the standard UI Shadow component for compatibility.
        Shadow shadow = tmpText.GetComponent<Shadow>();
        if (style.shadowEnabled)
        {
            if (shadow == null)
            {
                shadow = tmpText.gameObject.AddComponent<Shadow>();
            }

            shadow.enabled = true;
            shadow.effectColor = style.shadowColor;
            shadow.effectDistance = style.shadowOffset;
            shadow.useGraphicAlpha = style.shadowUseGraphicAlpha;
        }
        else if (shadow != null)
        {
            shadow.enabled = false;
        }

        if (TryEnsureCanvasRenderer(tmpText))
        {
            tmpText.UpdateMeshPadding();
            tmpText.SetAllDirty();
            tmpText.ForceMeshUpdate();
        }
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

    private static bool TrySyncSharedMaterialWithFont(TextMeshProUGUI tmpText)
    {
        if (tmpText == null)
        {
            return false;
        }

        if (tmpText.font == null || tmpText.font.material == null)
        {
            return false;
        }

        Material expected = tmpText.font.material;
        if (tmpText.fontSharedMaterial != expected)
        {
            tmpText.fontSharedMaterial = expected;
        }

        return tmpText.fontSharedMaterial != null;
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
}
