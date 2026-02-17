using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class BottomBarSelectionHighlightSafeAreaCompensator : MonoBehaviour
{
    [SerializeField] private RectTransform bottomBarRect;
    [SerializeField] private bool includeBottomSafeArea = true;
    [SerializeField] private float extensionOffset = 0f;

    private RectTransform _rectTransform;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;
    private float _lastBottomBarHeight = -1f;
    private float _lastAppliedHeight = -1f;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    private void OnEnable()
    {
        Apply();
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
            Apply();
        }
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
