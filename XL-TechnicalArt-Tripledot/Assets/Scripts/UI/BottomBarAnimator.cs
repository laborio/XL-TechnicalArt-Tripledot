using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BottomBarAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform barRoot;
    [SerializeField] private RectTransform buttonsContainer;
    [SerializeField] private RectTransform selectionHighlight;

    [Header("Bar Visibility")]
    [SerializeField] private bool fadeBar = true;
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.2f;
    [SerializeField] private Ease showEase = Ease.OutCubic;
    [SerializeField] private Ease hideEase = Ease.InCubic;
    [SerializeField] private float hiddenExtraOffset = 24f;

    [Header("Highlight")]
    [SerializeField] private bool fadeHighlight = true;
    [SerializeField] private float highlightVisibleAlpha = 1f;
    [SerializeField] private float highlightHorizontalInset = 8f;
    [SerializeField] private float highlightMinWidth = 16f;
    [SerializeField] private float firstSelectPopDuration = 0.2f;
    [SerializeField] private float switchDuration = 0.22f;
    [SerializeField] private Ease highlightMoveEase = Ease.OutCubic;
    [SerializeField] private Ease highlightWidthEase = Ease.OutCubic;
    [SerializeField] private Ease highlightPopEase = Ease.OutBack;

    [Header("Button Width")]
    [SerializeField] private float selectedWidthExtra = 24f;
    [SerializeField] private float buttonWidthDuration = 0.2f;
    [SerializeField] private Ease buttonWidthEase = Ease.OutCubic;

    [Header("Icon Feedback")]
    [SerializeField] private float iconFeedbackDuration = 0.2f;
    [SerializeField] private Vector3 iconFeedbackPunch = new Vector3(0.15f, 0.15f, 0f);

    private readonly Dictionary<TabButtonView, float> _baseWidths = new Dictionary<TabButtonView, float>();
    private readonly Dictionary<TabButtonView, Tween> _buttonWidthTweens = new Dictionary<TabButtonView, Tween>();
    private readonly Dictionary<TabButtonView, Tween> _iconTweens = new Dictionary<TabButtonView, Tween>();

    private Tween _highlightMoveTween;
    private Tween _highlightWidthTween;
    private Tween _highlightScaleTween;
    private Tween _highlightFadeTween;
    private CanvasGroup _highlightCanvasGroup;
    private Tween _barMoveTween;
    private Tween _barFadeTween;
    private CanvasGroup _barCanvasGroup;
    private Vector2 _barShownAnchoredPosition;
    private bool _barPositionInitialized;
    private bool _isShown = true;

    public bool IsShown => _isShown;

    public void Initialize(IReadOnlyList<TabButtonView> buttons)
    {
        InitializeBarReferences();

        if (selectionHighlight != null && fadeHighlight)
        {
            _highlightCanvasGroup = selectionHighlight.GetComponent<CanvasGroup>();
            if (_highlightCanvasGroup == null)
            {
                _highlightCanvasGroup = selectionHighlight.gameObject.AddComponent<CanvasGroup>();
            }

            _highlightCanvasGroup.alpha = 0f;
        }

        if (selectionHighlight != null)
        {
            Vector3 scale = selectionHighlight.localScale;
            scale.y = 0f;
            selectionHighlight.localScale = scale;
        }

        CacheBaseWidths(buttons);
    }

    public void Show(bool immediate = false)
    {
        InitializeBarReferences();
        if (barRoot == null)
        {
            return;
        }

        KillBarTweens();
        _isShown = true;

        if (immediate)
        {
            barRoot.anchoredPosition = _barShownAnchoredPosition;
            SetBarAlpha(1f);
            SetBarInteractable(true);
            return;
        }

        SetBarInteractable(false);
        _barMoveTween = barRoot.DOAnchorPos(_barShownAnchoredPosition, showDuration)
            .SetEase(showEase)
            .OnComplete(() => SetBarInteractable(true));

        if (fadeBar && _barCanvasGroup != null)
        {
            _barFadeTween = _barCanvasGroup.DOFade(1f, showDuration).SetEase(showEase);
        }
    }

    public void Hide(bool immediate = false)
    {
        InitializeBarReferences();
        if (barRoot == null)
        {
            return;
        }

        KillBarTweens();
        _isShown = false;

        Vector2 hiddenPosition = GetBarHiddenAnchoredPosition();

        if (immediate)
        {
            barRoot.anchoredPosition = hiddenPosition;
            SetBarAlpha(fadeBar ? 0f : 1f);
            SetBarInteractable(false);
            return;
        }

        SetBarInteractable(false);
        _barMoveTween = barRoot.DOAnchorPos(hiddenPosition, hideDuration).SetEase(hideEase);

        if (fadeBar && _barCanvasGroup != null)
        {
            _barFadeTween = _barCanvasGroup.DOFade(0f, hideDuration).SetEase(hideEase);
        }
    }

    public void AnimateSelect(TabButtonView previousButton, TabButtonView currentButton)
    {
        if (currentButton == null)
        {
            return;
        }

        EnsureLayoutUpToDate();

        if (selectionHighlight != null && buttonsContainer != null)
        {
            KillHighlightTweens();

            float targetWidth = GetHighlightTargetWidth(currentButton);
            Vector2 targetPosition = GetProjectedHighlightTargetPosition(previousButton, currentButton);

            if (previousButton == null)
            {
                selectionHighlight.anchoredPosition = targetPosition;
                SetRectWidth(selectionHighlight, targetWidth);

                _highlightScaleTween = selectionHighlight.DOScaleY(1f, firstSelectPopDuration).SetEase(highlightPopEase);

                if (fadeHighlight && _highlightCanvasGroup != null)
                {
                    _highlightFadeTween = _highlightCanvasGroup.DOFade(highlightVisibleAlpha, firstSelectPopDuration).SetEase(Ease.OutCubic);
                }
            }
            else
            {
                Vector3 scale = selectionHighlight.localScale;
                scale.y = 1f;
                selectionHighlight.localScale = scale;

                if (fadeHighlight && _highlightCanvasGroup != null)
                {
                    _highlightCanvasGroup.alpha = highlightVisibleAlpha;
                }

                _highlightMoveTween = selectionHighlight.DOAnchorPosX(targetPosition.x, switchDuration).SetEase(highlightMoveEase);
                _highlightWidthTween = AnimateRectWidth(selectionHighlight, targetWidth, switchDuration, highlightWidthEase);
            }
        }

        if (previousButton != null && previousButton != currentButton)
        {
            AnimateButtonWidth(previousButton, false);
        }

        AnimateButtonWidth(currentButton, true);
        AnimateIconFeedback(currentButton);
    }

    public void AnimateClose(TabButtonView deselectedButton)
    {
        if (selectionHighlight != null)
        {
            KillHighlightTweens();

            _highlightScaleTween = selectionHighlight.DOScaleY(0f, firstSelectPopDuration).SetEase(Ease.InCubic);

            if (fadeHighlight && _highlightCanvasGroup != null)
            {
                _highlightFadeTween = _highlightCanvasGroup.DOFade(0f, firstSelectPopDuration).SetEase(Ease.InCubic);
            }
        }

        if (deselectedButton != null)
        {
            AnimateButtonWidth(deselectedButton, false);
        }
    }

    public Vector2 GetHighlightTargetPosition(RectTransform tabRect)
    {
        if (tabRect == null || buttonsContainer == null || selectionHighlight == null)
        {
            return Vector2.zero;
        }

        Vector3 worldCenter = tabRect.TransformPoint(tabRect.rect.center);
        Vector3 localCenter = buttonsContainer.InverseTransformPoint(worldCenter);

        Vector2 targetPosition = selectionHighlight.anchoredPosition;
        targetPosition.x = localCenter.x;
        return targetPosition;
    }

    public Tween AnimateRectWidth(RectTransform targetRect, float targetWidth, float duration, Ease ease)
    {
        float currentWidth = targetRect.rect.width;
        Tween tween = DOTween.To(
                () => currentWidth,
                value =>
                {
                    currentWidth = value;
                    SetRectWidth(targetRect, value);
                },
                targetWidth,
                duration)
            .SetEase(ease);

        return tween;
    }

    private void CacheBaseWidths(IReadOnlyList<TabButtonView> buttons)
    {
        _baseWidths.Clear();

        if (buttons == null)
        {
            return;
        }

        EnsureLayoutUpToDate();

        for (int i = 0; i < buttons.Count; i++)
        {
            TabButtonView button = buttons[i];
            if (button == null)
            {
                continue;
            }

            float baseWidth = GetCurrentButtonWidth(button);
            _baseWidths[button] = baseWidth;
        }
    }

    private void AnimateButtonWidth(TabButtonView button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        LayoutElement layoutElement = button.LayoutElement;
        if (layoutElement == null)
        {
            return;
        }

        float targetWidth = GetTargetButtonWidth(button, selected);

        if (_buttonWidthTweens.TryGetValue(button, out Tween existingTween) && existingTween.IsActive())
        {
            existingTween.Kill();
        }

        if (layoutElement.preferredWidth < 0f)
        {
            layoutElement.preferredWidth = GetCurrentButtonWidth(button);
        }

        Tween tween = DOTween.To(
                () => layoutElement.preferredWidth,
                value =>
                {
                    layoutElement.preferredWidth = value;
                    LayoutRebuilder.MarkLayoutForRebuild(buttonsContainer);
                },
                targetWidth,
                buttonWidthDuration)
            .SetEase(buttonWidthEase);

        _buttonWidthTweens[button] = tween;
    }

    private void AnimateIconFeedback(TabButtonView button)
    {
        RectTransform icon = button.IconTransform;
        if (icon == null)
        {
            return;
        }

        if (_iconTweens.TryGetValue(button, out Tween existingTween) && existingTween.IsActive())
        {
            existingTween.Kill();
        }

        icon.localScale = Vector3.one;
        Tween tween = icon.DOPunchScale(iconFeedbackPunch, iconFeedbackDuration, 8, 0.8f);
        _iconTweens[button] = tween;
    }

    private float GetTargetButtonWidth(TabButtonView button, bool selected)
    {
        if (!_baseWidths.TryGetValue(button, out float baseWidth))
        {
            baseWidth = GetCurrentButtonWidth(button);
            _baseWidths[button] = baseWidth;
        }

        return selected ? baseWidth + selectedWidthExtra : baseWidth;
    }

    private float GetHighlightTargetWidth(TabButtonView button)
    {
        float selectedButtonWidth = GetTargetButtonWidth(button, true);
        float targetWidth = selectedButtonWidth - (highlightHorizontalInset * 2f);
        return Mathf.Max(highlightMinWidth, targetWidth);
    }

    private Vector2 GetProjectedHighlightTargetPosition(TabButtonView previousButton, TabButtonView currentButton)
    {
        LayoutElement previousLayout = previousButton != null ? previousButton.LayoutElement : null;
        LayoutElement currentLayout = currentButton != null ? currentButton.LayoutElement : null;

        float previousOriginalPreferred = 0f;
        float currentOriginalPreferred = 0f;
        bool previousChanged = false;
        bool currentChanged = false;

        if (previousLayout != null)
        {
            previousOriginalPreferred = previousLayout.preferredWidth;
            previousLayout.preferredWidth = GetTargetButtonWidth(previousButton, false);
            previousChanged = true;
        }

        if (currentLayout != null)
        {
            currentOriginalPreferred = currentLayout.preferredWidth;
            currentLayout.preferredWidth = GetTargetButtonWidth(currentButton, true);
            currentChanged = true;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsContainer);
        Vector2 projectedPosition = GetHighlightTargetPosition(currentButton.RectTransform);

        if (previousChanged)
        {
            previousLayout.preferredWidth = previousOriginalPreferred;
        }

        if (currentChanged)
        {
            currentLayout.preferredWidth = currentOriginalPreferred;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsContainer);
        return projectedPosition;
    }

    private float GetCurrentButtonWidth(TabButtonView button)
    {
        LayoutElement layoutElement = button.LayoutElement;
        if (layoutElement != null && layoutElement.preferredWidth > 0f)
        {
            return layoutElement.preferredWidth;
        }

        float rectWidth = button.RectTransform.rect.width;
        if (rectWidth > 0f)
        {
            return rectWidth;
        }

        return Mathf.Max(button.RectTransform.sizeDelta.x, 1f);
    }

    private void EnsureLayoutUpToDate()
    {
        Canvas.ForceUpdateCanvases();

        if (buttonsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsContainer);
        }
    }

    private void SetRectWidth(RectTransform rectTransform, float width)
    {
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    private void KillHighlightTweens()
    {
        _highlightMoveTween?.Kill();
        _highlightWidthTween?.Kill();
        _highlightScaleTween?.Kill();
        _highlightFadeTween?.Kill();
    }

    private void InitializeBarReferences()
    {
        if (barRoot == null)
        {
            barRoot = transform as RectTransform;
        }

        if (barRoot == null)
        {
            return;
        }

        if (!_barPositionInitialized)
        {
            _barShownAnchoredPosition = barRoot.anchoredPosition;
            _barPositionInitialized = true;
        }

        _barCanvasGroup = barRoot.GetComponent<CanvasGroup>();
        if (_barCanvasGroup == null)
        {
            _barCanvasGroup = barRoot.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private Vector2 GetBarHiddenAnchoredPosition()
    {
        Vector2 hiddenPosition = _barShownAnchoredPosition;
        hiddenPosition.y -= GetBarHideDistance();
        return hiddenPosition;
    }

    private float GetBarHideDistance()
    {
        if (barRoot == null)
        {
            return hiddenExtraOffset;
        }

        float barHeight = barRoot.rect.height;
        if (barHeight <= 0f)
        {
            barHeight = Mathf.Abs(barRoot.sizeDelta.y);
        }

        return barHeight + hiddenExtraOffset;
    }

    private void SetBarAlpha(float alpha)
    {
        if (!fadeBar || _barCanvasGroup == null)
        {
            return;
        }

        _barCanvasGroup.alpha = alpha;
    }

    private void SetBarInteractable(bool interactable)
    {
        if (_barCanvasGroup == null)
        {
            return;
        }

        _barCanvasGroup.interactable = interactable;
        _barCanvasGroup.blocksRaycasts = interactable;
    }

    private void KillBarTweens()
    {
        _barMoveTween?.Kill();
        _barFadeTween?.Kill();
    }

    private void OnDestroy()
    {
        KillBarTweens();
        KillHighlightTweens();

        foreach (KeyValuePair<TabButtonView, Tween> pair in _buttonWidthTweens)
        {
            pair.Value?.Kill();
        }

        foreach (KeyValuePair<TabButtonView, Tween> pair in _iconTweens)
        {
            pair.Value?.Kill();
        }
    }
}
