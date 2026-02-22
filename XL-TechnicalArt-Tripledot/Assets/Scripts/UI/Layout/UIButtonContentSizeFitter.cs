using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private RectTransform contentRect;

    [Header("Sizing")]
    [SerializeField] private float horizontalPadding = 48f;
    [SerializeField] private float verticalPadding = 20f;
    [SerializeField] private float minWidth = 120f;
    [SerializeField] private float minHeight = 56f;
    [SerializeField] private float maxWidth = 0f;
    [SerializeField] private float maxHeight = 0f;

    [Header("Runtime")]
    [SerializeField] private bool updateContinuously = false;
    [SerializeField] private bool refreshOnTextChanged = true;

    private RectTransform _selfRect;
    private string _lastText = string.Empty;
    private float _lastFontSize = -1f;
    private bool _lastEnabled;
    private float _lastContentWidth = -1f;
    private float _lastContentHeight = -1f;
#if UNITY_EDITOR
    private bool _pendingEditorRefresh;
#endif
    private bool _isListeningForTextChanges;
    private bool _isRefreshingSize;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        SubscribeTextChangeEvent();
        RequestRefresh();
    }

    private void OnDisable()
    {
        UnsubscribeTextChangeEvent();

#if UNITY_EDITOR
        if (_pendingEditorRefresh)
        {
            EditorApplication.delayCall -= HandleEditorDelayRefresh;
            _pendingEditorRefresh = false;
        }
#endif
    }

    private void OnDestroy()
    {
        UnsubscribeTextChangeEvent();
    }

    private void OnApplicationQuit()
    {
        UnsubscribeTextChangeEvent();
    }

    private void LateUpdate()
    {
        if (!updateContinuously)
        {
            return;
        }

        bool enabledStateChanged = label != null && _lastEnabled != label.enabled;
        bool textChanged = label != null && _lastText != label.text;
        bool fontSizeChanged = label != null && !Mathf.Approximately(_lastFontSize, label.fontSize);
        Vector2 contentSize = GetContentSize(contentRect);
        bool contentRectChanged = contentRect != null &&
            (!Mathf.Approximately(_lastContentWidth, contentSize.x) ||
             !Mathf.Approximately(_lastContentHeight, contentSize.y));

        if (!enabledStateChanged && !textChanged && !fontSizeChanged && !contentRectChanged)
        {
            return;
        }

        RefreshSize();
    }

    private void HandleTextChanged(UnityEngine.Object changedObject)
    {
        // During play mode exit, TMP can dispatch text events while scene objects are being
        // destroyed. Avoid accessing Behaviour properties until we know this instance is valid.
        if (!this || !enabled || label == null || changedObject != label || _isRefreshingSize)
        {
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        RefreshSize();
    }

    private void SubscribeTextChangeEvent()
    {
        if (!refreshOnTextChanged)
        {
            return;
        }

        // Ensure this callback is registered at most once.
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
        _isListeningForTextChanges = true;
    }

    private void UnsubscribeTextChangeEvent()
    {
        // Always attempt to remove so we're resilient to play mode/domain reload edge cases.
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
        _isListeningForTextChanges = false;
    }

    [ContextMenu("Refresh Size")]
    public void RefreshSize()
    {
        if (_isRefreshingSize)
        {
            return;
        }

        _isRefreshingSize = true;

        try
        {
            CacheReferences();
            if (targetRect == null || (label == null && contentRect == null))
            {
                return;
            }

            float preferredWidth = 0f;
            if (label != null)
            {
                label.ForceMeshUpdate();
                preferredWidth = label.GetPreferredValues(label.text, Mathf.Infinity, Mathf.Infinity).x;
            }

            Vector2 contentSize = GetContentSize(contentRect);
            float widthSource = Mathf.Max(preferredWidth, contentSize.x);

            float width = Mathf.Max(minWidth, widthSource + horizontalPadding);
            if (maxWidth > 0f)
            {
                width = Mathf.Min(width, maxWidth);
            }

            float availableTextWidth = Mathf.Max(0f, width - horizontalPadding);
            float preferredHeight = 0f;
            if (label != null)
            {
                preferredHeight = label.GetPreferredValues(label.text, availableTextWidth, Mathf.Infinity).y;
            }

            float heightSource = Mathf.Max(preferredHeight, contentSize.y);
            float height = Mathf.Max(minHeight, heightSource + verticalPadding);
            if (maxHeight > 0f)
            {
                height = Mathf.Min(height, maxHeight);
            }

            targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            CacheLastState();
        }
        finally
        {
            _isRefreshingSize = false;
        }
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

    private static Vector2 GetContentSize(RectTransform rect)
    {
        if (rect == null)
        {
            return Vector2.zero;
        }

        float width = rect.rect.width;
        float height = rect.rect.height;

        float preferredWidth = LayoutUtility.GetPreferredSize(rect, 0);
        float preferredHeight = LayoutUtility.GetPreferredSize(rect, 1);
        if (preferredWidth > 0f)
        {
            width = Mathf.Max(width, preferredWidth);
        }

        if (preferredHeight > 0f)
        {
            height = Mathf.Max(height, preferredHeight);
        }

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rect);
        width = Mathf.Max(width, bounds.size.x);
        height = Mathf.Max(height, bounds.size.y);

        return new Vector2(width, height);
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
        }
        else
        {
            _lastText = label.text;
            _lastFontSize = label.fontSize;
            _lastEnabled = label.enabled;
        }

        Vector2 contentSize = GetContentSize(contentRect);
        _lastContentWidth = contentSize.x;
        _lastContentHeight = contentSize.y;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();

        if (Application.isPlaying)
        {
            UnsubscribeTextChangeEvent();
            SubscribeTextChangeEvent();
        }

        RequestRefresh();
    }
#endif
}
