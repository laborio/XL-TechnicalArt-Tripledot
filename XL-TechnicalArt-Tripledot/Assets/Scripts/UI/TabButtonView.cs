using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class TabButtonView : MonoBehaviour
{
    public bool IsLocked;
    public BottomBarContent Content;

    public event Action<TabButtonView> Clicked;

    [SerializeField] private Button button;
    [SerializeField] private RectTransform iconTransform;
    [SerializeField] private RectTransform iconLiftTarget;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectedState;
    [SerializeField] private bool ensureButtonRaycastSurface = true;
    [SerializeField] private float selectedIconYOffset = 14f;
    [SerializeField] private bool forceLiftTargetIgnoreLayout = true;
    [SerializeField] private bool debugLiftLogs = false;
    [SerializeField] private Color normalIconColor = Color.white;
    [SerializeField] private Color selectedIconColor = Color.white;

    private RectTransform _rectTransform;
    private LayoutElement _layoutElement;
    private Vector2 _iconLiftBaseAnchoredPosition;
    private bool _hasIconLiftBaseAnchoredPosition;

    public RectTransform RectTransform
    {
        get
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            return _rectTransform;
        }
    }

    public RectTransform IconTransform
    {
        get
        {
            if (iconTransform != null)
            {
                return iconTransform;
            }

            return RectTransform;
        }
    }

    public LayoutElement LayoutElement
    {
        get
        {
            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
            }

            return _layoutElement;
        }
    }

    private void Awake()
    {
        CacheButtonReference();

        _rectTransform = GetComponent<RectTransform>();
        _layoutElement = GetComponent<LayoutElement>();

        EnsureButtonRaycastSurface();
        EnsureLiftTargetIgnoreLayout();
        CacheIconLiftBasePosition(force: true);
    }

    private void OnEnable()
    {
        CacheButtonReference();
        EnsureButtonRaycastSurface();
        EnsureLiftTargetIgnoreLayout();

        if (button != null)
        {
            button.onClick.AddListener(HandleButtonClicked);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedState != null)
        {
            selectedState.SetActive(selected);
        }

        RectTransform liftTarget = ResolveIconLiftTarget();
        if (liftTarget != null)
        {
            if (!_hasIconLiftBaseAnchoredPosition)
            {
                CacheIconLiftBasePosition(force: true);
            }

            Vector3 previousLocalPosition = liftTarget.localPosition;
            Vector2 previousAnchoredPosition = liftTarget.anchoredPosition;
            Vector2 targetPosition = _iconLiftBaseAnchoredPosition;
            targetPosition.y += selected ? selectedIconYOffset : 0f;
            liftTarget.anchoredPosition = targetPosition;

            LogLift(
                $"SetSelected({selected}) target='{liftTarget.name}' prevLocalY={previousLocalPosition.y:F2} prevAnchoredY={previousAnchoredPosition.y:F2} " +
                $"baseAnchoredY={_iconLiftBaseAnchoredPosition.y:F2} offsetY={(selected ? selectedIconYOffset : 0f):F2} resultLocalY={liftTarget.localPosition.y:F2} resultAnchoredY={liftTarget.anchoredPosition.y:F2}");
        }

        if (iconImage != null)
        {
            iconImage.color = selected ? selectedIconColor : normalIconColor;
        }
    }

    private void HandleButtonClicked()
    {
        // Debug.Log($"[BottomBar] Clicked '{name}' content={Content} locked={IsLocked}", this);
        Clicked?.Invoke(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheButtonReference();
        EnsureButtonRaycastSurface();

        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (_layoutElement == null)
        {
            _layoutElement = GetComponent<LayoutElement>();
        }

        CacheIconLiftBasePosition(force: true);
    }
#endif

    private void CacheButtonReference()
    {
        if (button != null && button.transform != transform)
        {
            return;
        }

        Button[] buttonsInChildren = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttonsInChildren.Length; i++)
        {
            Button candidate = buttonsInChildren[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.transform != transform)
            {
                button = candidate;
                return;
            }
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void CacheIconLiftBasePosition(bool force = false)
    {
        if (_hasIconLiftBaseAnchoredPosition && !force)
        {
            return;
        }

        RectTransform liftTarget = ResolveIconLiftTarget();
        if (liftTarget == null)
        {
            LogLift("CacheIconLiftBasePosition skipped (no lift target)");
            return;
        }

        _iconLiftBaseAnchoredPosition = liftTarget.anchoredPosition;
        _hasIconLiftBaseAnchoredPosition = true;
        LogLift(
            $"CacheIconLiftBasePosition(force:{force}) target='{liftTarget.name}' baseAnchoredY={_iconLiftBaseAnchoredPosition.y:F2} baseLocalY={liftTarget.localPosition.y:F2}");
    }

    private void EnsureButtonRaycastSurface()
    {
        if (!ensureButtonRaycastSurface || button == null)
        {
            return;
        }

        Graphic raycastGraphic = button.targetGraphic;
        if (raycastGraphic != null)
        {
            raycastGraphic.raycastTarget = true;
            return;
        }

        raycastGraphic = button.GetComponent<Graphic>();
        if (raycastGraphic == null)
        {
            Image hitImage = button.gameObject.AddComponent<Image>();
            hitImage.color = new Color(1f, 1f, 1f, 0f);
            hitImage.raycastTarget = true;
            raycastGraphic = hitImage;
        }
        else
        {
            raycastGraphic.raycastTarget = true;
        }

        button.targetGraphic = raycastGraphic;
    }

    private void EnsureLiftTargetIgnoreLayout()
    {
        if (!forceLiftTargetIgnoreLayout)
        {
            return;
        }

        RectTransform liftTarget = ResolveIconLiftTarget();
        if (liftTarget == null)
        {
            return;
        }

        LayoutElement liftLayoutElement = liftTarget.GetComponent<LayoutElement>();
        if (liftLayoutElement == null)
        {
            liftLayoutElement = liftTarget.gameObject.AddComponent<LayoutElement>();
        }

        liftLayoutElement.ignoreLayout = true;
    }

    private RectTransform ResolveIconLiftTarget()
    {
        if (iconLiftTarget != null)
        {
            if (iconLiftTarget == RectTransform)
            {
                LogLift("ResolveIconLiftTarget rejected because iconLiftTarget == root RectTransform");
                return null;
            }

            return iconLiftTarget;
        }

        if (iconTransform != null && iconTransform != RectTransform)
        {
            return iconTransform;
        }

        return null;
    }

    private void LogLift(string message)
    {
        if (!debugLiftLogs)
        {
            return;
        }

        // Debug.Log($"[BottomBar][Lift] {name}: {message}", this);
    }
}
