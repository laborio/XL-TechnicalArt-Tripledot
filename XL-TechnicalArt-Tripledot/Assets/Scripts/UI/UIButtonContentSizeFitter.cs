using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIButtonContentSizeFitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private TMP_Text label;

    [Header("Sizing")]
    [SerializeField] private float horizontalPadding = 48f;
    [SerializeField] private float verticalPadding = 20f;
    [SerializeField] private float minWidth = 120f;
    [SerializeField] private float minHeight = 56f;
    [SerializeField] private float maxWidth = 0f;
    [SerializeField] private float maxHeight = 0f;

    [Header("Runtime")]
    [SerializeField] private bool updateContinuously = true;

    private RectTransform _selfRect;
    private string _lastText = string.Empty;
    private float _lastFontSize = -1f;
    private bool _lastEnabled;
#if UNITY_EDITOR
    private bool _pendingEditorRefresh;
#endif

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        RequestRefresh();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        if (_pendingEditorRefresh)
        {
            EditorApplication.delayCall -= HandleEditorDelayRefresh;
            _pendingEditorRefresh = false;
        }
#endif
    }

    private void LateUpdate()
    {
        if (!updateContinuously || label == null)
        {
            return;
        }

        bool enabledStateChanged = _lastEnabled != label.enabled;
        bool textChanged = _lastText != label.text;
        bool fontSizeChanged = !Mathf.Approximately(_lastFontSize, label.fontSize);

        if (!enabledStateChanged && !textChanged && !fontSizeChanged)
        {
            return;
        }

        RefreshSize();
    }

    [ContextMenu("Refresh Size")]
    public void RefreshSize()
    {
        CacheReferences();
        if (targetRect == null || label == null)
        {
            return;
        }

        label.ForceMeshUpdate();
        float preferredWidth = label.GetPreferredValues(label.text, Mathf.Infinity, Mathf.Infinity).x;

        float width = Mathf.Max(minWidth, preferredWidth + horizontalPadding);
        if (maxWidth > 0f)
        {
            width = Mathf.Min(width, maxWidth);
        }

        float availableTextWidth = Mathf.Max(0f, width - horizontalPadding);
        float preferredHeight = label.GetPreferredValues(label.text, availableTextWidth, Mathf.Infinity).y;
        float height = Mathf.Max(minHeight, preferredHeight + verticalPadding);
        if (maxHeight > 0f)
        {
            height = Mathf.Min(height, maxHeight);
        }

        targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        CacheLastState();
    }

    private void RequestRefresh()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (_pendingEditorRefresh)
            {
                return;
            }

            _pendingEditorRefresh = true;
            EditorApplication.delayCall += HandleEditorDelayRefresh;
            return;
        }
#endif

        RefreshSize();
    }

#if UNITY_EDITOR
    private void HandleEditorDelayRefresh()
    {
        EditorApplication.delayCall -= HandleEditorDelayRefresh;
        _pendingEditorRefresh = false;

        if (this == null || !isActiveAndEnabled)
        {
            return;
        }

        RefreshSize();
    }
#endif

    [ContextMenu("Refresh Width")]
    public void RefreshWidth()
    {
        RefreshSize();
    }

    private void CacheReferences()
    {
        if (_selfRect == null)
        {
            _selfRect = GetComponent<RectTransform>();
        }

        if (targetRect == null)
        {
            targetRect = _selfRect;
        }
    }

    private void CacheLastState()
    {
        if (label == null)
        {
            _lastText = string.Empty;
            _lastFontSize = -1f;
            _lastEnabled = false;
            return;
        }

        _lastText = label.text;
        _lastFontSize = label.fontSize;
        _lastEnabled = label.enabled;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
        RequestRefresh();
    }
#endif
}
