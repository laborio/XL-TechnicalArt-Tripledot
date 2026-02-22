using System;
using System.Globalization;
using TMPro;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class UILocalizationKeyText : MonoBehaviour
{
    // Optional global resolver. If null, the key itself is displayed.
    public static Func<string, string> ResolveText;
    private static readonly CultureInfo CommaGroupingCulture = CultureInfo.GetCultureInfo("en-US");

    [SerializeField] private string localizationKey;
    [Header("Number Formatting (Optional)")]
    [SerializeField] private bool formatAsGroupedInteger;

    private TextMeshProUGUI _tmpText;

    public string LocalizationKey => localizationKey;

    public void SetLocalizationKey(string key, bool refresh = true)
    {
        localizationKey = key;
        if (refresh)
        {
            RefreshText();
        }
    }

    [ContextMenu("Refresh Localization Text")]
    public void RefreshText()
    {
        if (!TryGetText(out TextMeshProUGUI tmpText))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(localizationKey))
        {
            tmpText.text = string.Empty;
            return;
        }

        if (ResolveText != null)
        {
            string resolved = ResolveText(localizationKey);
            string displayText = string.IsNullOrEmpty(resolved) ? localizationKey : resolved;
            tmpText.text = FormatDisplayText(displayText);
            return;
        }

        tmpText.text = FormatDisplayText(localizationKey);
    }

    private void Awake()
    {
        TryGetText(out _);
    }

    private void OnEnable()
    {
        RefreshText();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RefreshText();
    }
#endif

    private bool TryGetText(out TextMeshProUGUI tmpText)
    {
        if (_tmpText == null)
        {
            _tmpText = GetComponent<TextMeshProUGUI>();
        }

        tmpText = _tmpText;
        return tmpText != null;
    }

    private string FormatDisplayText(string rawText)
    {
        if (!formatAsGroupedInteger || string.IsNullOrWhiteSpace(rawText))
        {
            return rawText;
        }

        if (!TryParseNumber(rawText, out double parsedValue))
        {
            return rawText;
        }

        long rounded = Convert.ToInt64(Math.Round(parsedValue, MidpointRounding.AwayFromZero));
        return rounded.ToString("#,0", CommaGroupingCulture);
    }

    private static bool TryParseNumber(string rawText, out double value)
    {
        const NumberStyles styles = NumberStyles.AllowLeadingSign |
                                    NumberStyles.AllowDecimalPoint |
                                    NumberStyles.AllowThousands |
                                    NumberStyles.AllowExponent;

        string trimmed = rawText.Trim();
        if (double.TryParse(trimmed, styles, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        if (double.TryParse(trimmed, styles, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        string compact = trimmed.Replace(" ", string.Empty).Replace("\u00A0", string.Empty);
        return double.TryParse(compact, styles, CultureInfo.InvariantCulture, out value);
    }
}
