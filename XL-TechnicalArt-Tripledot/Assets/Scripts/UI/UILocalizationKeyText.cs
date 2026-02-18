using System;
using TMPro;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class UILocalizationKeyText : MonoBehaviour
{
    // Optional global resolver. If null, the key itself is displayed.
    public static Func<string, string> ResolveText;

    [SerializeField] private string localizationKey;

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
            tmpText.text = string.IsNullOrEmpty(resolved) ? localizationKey : resolved;
            return;
        }

        tmpText.text = localizationKey;
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
}
