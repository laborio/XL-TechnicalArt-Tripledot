using DG.Tweening;
using UnityEngine;

internal sealed class UIBottomBarVisibilityController
{
    private Transform _ownerTransform;
    private RectTransform _barRoot;
    private RectTransform _barBackgroundRoot;

    private bool _fadeBar;
    private float _showDuration;
    private float _hideDuration;
    private Ease _showEase;
    private Ease _hideEase;
    private float _hiddenExtraOffset;

    private Tween _barMoveTween;
    private Tween _barFadeTween;
    private Tween _barBackgroundMoveTween;
    private Tween _barBackgroundFadeTween;
    private CanvasGroup _barCanvasGroup;
    private CanvasGroup _barBackgroundCanvasGroup;
    private Vector2 _barShownAnchoredPosition;
    private Vector2 _barBackgroundShownAnchoredPosition;
    private bool _barPositionInitialized;
    private bool _barBackgroundPositionInitialized;

    public bool IsShown { get; private set; } = true;

    public void Configure(
        Transform ownerTransform,
        RectTransform barRoot,
        RectTransform barBackgroundRoot,
        bool fadeBar,
        float showDuration,
        float hideDuration,
        Ease showEase,
        Ease hideEase,
        float hiddenExtraOffset)
    {
        _ownerTransform = ownerTransform;
        _fadeBar = fadeBar;
        _showDuration = showDuration;
        _hideDuration = hideDuration;
        _showEase = showEase;
        _hideEase = hideEase;
        _hiddenExtraOffset = hiddenExtraOffset;

        ResolveRoots(barRoot, barBackgroundRoot);
        EnsureCanvasGroups();
    }

    public void Show(bool immediate)
    {
        if (_barRoot == null)
        {
            return;
        }

        KillTweens();
        IsShown = true;

        if (immediate)
        {
            _barRoot.anchoredPosition = _barShownAnchoredPosition;
            if (_barBackgroundRoot != null)
            {
                _barBackgroundRoot.anchoredPosition = _barBackgroundShownAnchoredPosition;
            }

            SetBarAlpha(1f);
            SetBarInteractable(true);
            return;
        }

        SetBarInteractable(false);
        _barMoveTween = _barRoot.DOAnchorPos(_barShownAnchoredPosition, _showDuration)
            .SetEase(_showEase)
            .OnComplete(() => SetBarInteractable(true));

        if (_barBackgroundRoot != null)
        {
            _barBackgroundMoveTween = _barBackgroundRoot.DOAnchorPos(_barBackgroundShownAnchoredPosition, _showDuration)
                .SetEase(_showEase);
        }

        if (_fadeBar && _barCanvasGroup != null)
        {
            _barFadeTween = _barCanvasGroup.DOFade(1f, _showDuration).SetEase(_showEase);
        }

        if (_fadeBar && _barBackgroundCanvasGroup != null)
        {
            _barBackgroundFadeTween = _barBackgroundCanvasGroup.DOFade(1f, _showDuration).SetEase(_showEase);
        }
    }

    public void Hide(bool immediate)
    {
        if (_barRoot == null)
        {
            return;
        }

        KillTweens();
        IsShown = false;

        Vector2 hiddenPosition = GetBarHiddenAnchoredPosition();
        Vector2 backgroundHiddenPosition = GetBarBackgroundHiddenAnchoredPosition();

        if (immediate)
        {
            _barRoot.anchoredPosition = hiddenPosition;
            if (_barBackgroundRoot != null)
            {
                _barBackgroundRoot.anchoredPosition = backgroundHiddenPosition;
            }

            SetBarAlpha(_fadeBar ? 0f : 1f);
            SetBarInteractable(false);
            return;
        }

        SetBarInteractable(false);
        _barMoveTween = _barRoot.DOAnchorPos(hiddenPosition, _hideDuration).SetEase(_hideEase);

        if (_barBackgroundRoot != null)
        {
            _barBackgroundMoveTween = _barBackgroundRoot.DOAnchorPos(backgroundHiddenPosition, _hideDuration)
                .SetEase(_hideEase);
        }

        if (_fadeBar && _barCanvasGroup != null)
        {
            _barFadeTween = _barCanvasGroup.DOFade(0f, _hideDuration).SetEase(_hideEase);
        }

        if (_fadeBar && _barBackgroundCanvasGroup != null)
        {
            _barBackgroundFadeTween = _barBackgroundCanvasGroup.DOFade(0f, _hideDuration).SetEase(_hideEase);
        }
    }

    public void Dispose()
    {
        KillTweens();
    }

    private void ResolveRoots(RectTransform barRoot, RectTransform barBackgroundRoot)
    {
        RectTransform resolvedBarRoot = barRoot;
        if (resolvedBarRoot == null && _ownerTransform != null)
        {
            resolvedBarRoot = _ownerTransform as RectTransform;
        }

        if (_barRoot != resolvedBarRoot)
        {
            _barRoot = resolvedBarRoot;
            _barPositionInitialized = false;
        }

        if (_barRoot == null)
        {
            _barBackgroundRoot = null;
            _barBackgroundPositionInitialized = false;
            return;
        }

        if (!_barPositionInitialized)
        {
            _barShownAnchoredPosition = _barRoot.anchoredPosition;
            _barPositionInitialized = true;
        }

        RectTransform resolvedBackgroundRoot = barBackgroundRoot;
        if (resolvedBackgroundRoot == null && _barRoot.parent != null)
        {
            Transform sibling = _barRoot.parent.Find("UI_BottomBarBackground");
            if (sibling != null)
            {
                resolvedBackgroundRoot = sibling as RectTransform;
            }
        }

        if (_barBackgroundRoot != resolvedBackgroundRoot)
        {
            _barBackgroundRoot = resolvedBackgroundRoot;
            _barBackgroundPositionInitialized = false;
        }

        if (_barBackgroundRoot != null && !_barBackgroundPositionInitialized)
        {
            _barBackgroundShownAnchoredPosition = _barBackgroundRoot.anchoredPosition;
            _barBackgroundPositionInitialized = true;
        }
    }

    private void EnsureCanvasGroups()
    {
        if (_barRoot == null)
        {
            return;
        }

        _barCanvasGroup = _barRoot.GetComponent<CanvasGroup>();
        if (_barCanvasGroup == null)
        {
            _barCanvasGroup = _barRoot.gameObject.AddComponent<CanvasGroup>();
        }

        if (_barBackgroundRoot == null)
        {
            _barBackgroundCanvasGroup = null;
            return;
        }

        _barBackgroundCanvasGroup = _barBackgroundRoot.GetComponent<CanvasGroup>();
        if (_barBackgroundCanvasGroup == null)
        {
            _barBackgroundCanvasGroup = _barBackgroundRoot.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private Vector2 GetBarHiddenAnchoredPosition()
    {
        Vector2 hiddenPosition = _barShownAnchoredPosition;
        hiddenPosition.y -= GetBarHideDistance(_barRoot);
        return hiddenPosition;
    }

    private Vector2 GetBarBackgroundHiddenAnchoredPosition()
    {
        if (_barBackgroundRoot == null)
        {
            return Vector2.zero;
        }

        Vector2 hiddenPosition = _barBackgroundShownAnchoredPosition;
        hiddenPosition.y -= GetBarHideDistance(_barBackgroundRoot);
        return hiddenPosition;
    }

    private float GetBarHideDistance(RectTransform targetRoot)
    {
        if (targetRoot == null)
        {
            return _hiddenExtraOffset;
        }

        float barHeight = targetRoot.rect.height;
        if (barHeight <= 0f)
        {
            barHeight = Mathf.Abs(targetRoot.sizeDelta.y);
        }

        return barHeight + _hiddenExtraOffset;
    }

    private void SetBarAlpha(float alpha)
    {
        if (!_fadeBar)
        {
            return;
        }

        if (_barCanvasGroup != null)
        {
            _barCanvasGroup.alpha = alpha;
        }

        if (_barBackgroundCanvasGroup != null)
        {
            _barBackgroundCanvasGroup.alpha = alpha;
        }
    }

    private void SetBarInteractable(bool interactable)
    {
        if (_barCanvasGroup == null && _barBackgroundCanvasGroup == null)
        {
            return;
        }

        if (_barCanvasGroup != null)
        {
            _barCanvasGroup.interactable = interactable;
            _barCanvasGroup.blocksRaycasts = interactable;
        }

        if (_barBackgroundCanvasGroup != null)
        {
            _barBackgroundCanvasGroup.interactable = interactable;
            _barBackgroundCanvasGroup.blocksRaycasts = interactable;
        }
    }

    private void KillTweens()
    {
        _barMoveTween?.Kill();
        _barFadeTween?.Kill();
        _barBackgroundMoveTween?.Kill();
        _barBackgroundFadeTween?.Kill();

        _barMoveTween = null;
        _barFadeTween = null;
        _barBackgroundMoveTween = null;
        _barBackgroundFadeTween = null;
    }
}
