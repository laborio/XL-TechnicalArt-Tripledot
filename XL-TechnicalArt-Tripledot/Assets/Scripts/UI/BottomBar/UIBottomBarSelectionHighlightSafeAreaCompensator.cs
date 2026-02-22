using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(RectTransform))]
public class UIBottomBarSelectionHighlightSafeAreaCompensator : MonoBehaviour
{
    [SerializeField] private RectTransform bottomBarRect;
    [SerializeField] private bool includeBottomSafeArea = true;
    [SerializeField] private float extensionOffset = 0f;

    private RectTransform _rectTransform;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;
    private float _lastBottomBarHeight = -1f;
    private float _lastAppliedHeight = -1f;
#if UNITY_EDITOR
    private bool _editorApplyQueued;
#endif

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            CancelEditorApply();
        }
#endif
    }

    private void LateUpdate()
    {
        if (NeedsApply())
        {
            Apply();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (!Application.isPlaying)
        {
            QueueEditorApply();
        }
    }

    private void QueueEditorApply()
    {
        if (_editorApplyQueued)
        {
            return;
        }

        _editorApplyQueued = true;
        EditorApplication.delayCall += ApplyFromEditorDelayCall;
    }

    private void CancelEditorApply()
    {
        if (!_editorApplyQueued)
        {
            return;
        }

        _editorApplyQueued = false;
        EditorApplication.delayCall -= ApplyFromEditorDelayCall;
    }

    private void ApplyFromEditorDelayCall()
    {
        EditorApplication.delayCall -= ApplyFromEditorDelayCall;
        _editorApplyQueued = false;

        if (this == null || !isActiveAndEnabled || Application.isPlaying)
        {
            return;
        }

        Apply();
    }
#endif

    private bool NeedsApply()
    {
        if (bottomBarRect == null)
        {
            return false;
        }

        if (_lastSafeArea != Screen.safeArea)
        {
            return true;
        }

        if (_lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height)
        {
            return true;
        }

        float currentBottomBarHeight = GetBottomBarHeight();
        if (!Mathf.Approximately(currentBottomBarHeight, _lastBottomBarHeight))
        {
            return true;
        }

        return false;
    }

    private void Apply()
    {
        if (_rectTransform == null || bottomBarRect == null)
        {
            return;
        }

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        float bottomBarHeight = GetBottomBarHeight();
        float bottomInset = includeBottomSafeArea ? Screen.safeArea.yMin : 0f;
        float targetHeight = Mathf.Max(0f, bottomBarHeight + bottomInset + extensionOffset);

        if (!Mathf.Approximately(_lastAppliedHeight, targetHeight))
        {
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            _lastAppliedHeight = targetHeight;
        }

        _lastBottomBarHeight = bottomBarHeight;
        _lastSafeArea = Screen.safeArea;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private float GetBottomBarHeight()
    {
        return bottomBarRect.rect.height;
    }
}
