using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BottomBarAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform barRoot;
    [SerializeField] private RectTransform barBackgroundRoot;
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
    [SerializeField] private Vector3 iconFeedbackScalePunch = new Vector3(0.12f, 0.12f, 0f);
    [SerializeField] private float iconFeedbackWiggleDistance = 8f;
    [SerializeField] private int iconFeedbackWiggleVibrato = 8;

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
    private Tween _barBackgroundMoveTween;
    private Tween _barBackgroundFadeTween;
    private CanvasGroup _barCanvasGroup;
    private CanvasGroup _barBackgroundCanvasGroup;
    private Vector2 _barShownAnchoredPosition;
    private Vector2 _barBackgroundShownAnchoredPosition;
    private bool _barPositionInitialized;
    private bool _barBackgroundPositionInitialized;
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
            if (barBackgroundRoot != null)
            {
                barBackgroundRoot.anchoredPosition = _barBackgroundShownAnchoredPosition;
            }

            SetBarAlpha(1f);
            SetBarInteractable(true);
            return;
        }

        SetBarInteractable(false);
        _barMoveTween = barRoot.DOAnchorPos(_barShownAnchoredPosition, showDuration)
            .SetEase(showEase)
            .OnComplete(() => SetBarInteractable(true));

        if (barBackgroundRoot != null)
        {
            _barBackgroundMoveTween = barBackgroundRoot.DOAnchorPos(_barBackgroundShownAnchoredPosition, showDuration).SetEase(showEase);
        }

        if (fadeBar && _barCanvasGroup != null)
        {
            _barFadeTween = _barCanvasGroup.DOFade(1f, showDuration).SetEase(showEase);
        }

        if (fadeBar && _barBackgroundCanvasGroup != null)
        {
            _barBackgroundFadeTween = _barBackgroundCanvasGroup.DOFade(1f, showDuration).SetEase(showEase);
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
        Vector2 backgroundHiddenPosition = GetBarBackgroundHiddenAnchoredPosition();

        if (immediate)
        {
            barRoot.anchoredPosition = hiddenPosition;
            if (barBackgroundRoot != null)
            {
                barBackgroundRoot.anchoredPosition = backgroundHiddenPosition;
            }

            SetBarAlpha(fadeBar ? 0f : 1f);
            SetBarInteractable(false);
            return;
        }

        SetBarInteractable(false);
        _barMoveTween = barRoot.DOAnchorPos(hiddenPosition, hideDuration).SetEase(hideEase);

        if (barBackgroundRoot != null)
        {
            _barBackgroundMoveTween = barBackgroundRoot.DOAnchorPos(backgroundHiddenPosition, hideDuration).SetEase(hideEase);
        }

        if (fadeBar && _barCanvasGroup != null)
        {
            _barFadeTween = _barCanvasGroup.DOFade(0f, hideDuration).SetEase(hideEase);
        }

        if (fadeBar && _barBackgroundCanvasGroup != null)
        {
            _barBackgroundFadeTween = _barBackgroundCanvasGroup.DOFade(0f, hideDuration).SetEase(hideEase);
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

                Vector3 scale = selectionHighlight.localScale;
                scale.y = 0f;
                selectionHighlight.localScale = scale;

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
        EnsureLayoutUpToDate();

        if (selectionHighlight != null)
        {
            KillHighlightTweens();

            if (fadeHighlight && _highlightCanvasGroup != null)
            {
                _highlightFadeTween = _highlightCanvasGroup
                    .DOFade(0f, firstSelectPopDuration)
                    .SetEase(Ease.InCubic)
                    .OnComplete(() =>
                    {
                        if (selectionHighlight == null)
                        {
                            return;
                        }

                        Vector3 hiddenScale = selectionHighlight.localScale;
                        hiddenScale.y = 0f;
                        selectionHighlight.localScale = hiddenScale;
                    });
            }
            else
            {
                Vector3 hiddenScale = selectionHighlight.localScale;
                hiddenScale.y = 0f;
                selectionHighlight.localScale = hiddenScale;
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
        RectTransform icon = button.IconFeedbackTransform;
        if (icon == null)
        {
            return;
        }

        if (_iconTweens.TryGetValue(button, out Tween existingTween) && existingTween.IsActive())
        {
            existingTween.Kill();
        }

        icon.localScale = Vector3.one;

        Vector2 baseAnchoredPosition = icon.anchoredPosition;
        Sequence sequence = DOTween.Sequence();

        sequence.Join(icon.DOPunchScale(iconFeedbackScalePunch, iconFeedbackDuration, 8, 0.8f));

        if (iconFeedbackWiggleDistance > 0f)
        {
            Vector2 wigglePunch = new Vector2(iconFeedbackWiggleDistance, 0f);
            sequence.Join(icon.DOPunchAnchorPos(wigglePunch, iconFeedbackDuration, iconFeedbackWiggleVibrato, 0f));
        }

        sequence.OnComplete(() =>
        {
            if (icon != null)
            {
                icon.anchoredPosition = baseAnchoredPosition;
            }
        });
        sequence.OnKill(() =>
        {
            if (icon != null)
            {
                icon.anchoredPosition = baseAnchoredPosition;
            }
        });

        _iconTweens[button] = sequence;
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
        return GetHighlightTargetWidth(button, true);
    }

    private float GetHighlightTargetWidth(TabButtonView button, bool selected)
    {
        float buttonWidth = GetTargetButtonWidth(button, selected);
        float targetWidth = buttonWidth - (highlightHorizontalInset * 2f);
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

    private Vector2 GetProjectedHighlightTargetPositionOnClose(TabButtonView deselectedButton)
    {
        if (deselectedButton == null)
        {
            return selectionHighlight != null ? selectionHighlight.anchoredPosition : Vector2.zero;
        }

        LayoutElement deselectedLayout = deselectedButton.LayoutElement;
        if (deselectedLayout == null)
        {
            return GetHighlightTargetPosition(deselectedButton.RectTransform);
        }

        float deselectedOriginalPreferred = deselectedLayout.preferredWidth;
        deselectedLayout.preferredWidth = GetTargetButtonWidth(deselectedButton, false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsContainer);
        Vector2 projectedPosition = GetHighlightTargetPosition(deselectedButton.RectTransform);

        deselectedLayout.preferredWidth = deselectedOriginalPreferred;
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

        if (barBackgroundRoot == null && barRoot.parent != null)
        {
            Transform sibling = barRoot.parent.Find("UI_BottomBarBackground");
            if (sibling != null)
            {
                barBackgroundRoot = sibling as RectTransform;
            }
        }

        _barCanvasGroup = barRoot.GetComponent<CanvasGroup>();
        if (_barCanvasGroup == null)
        {
            _barCanvasGroup = barRoot.gameObject.AddComponent<CanvasGroup>();
        }

        if (barBackgroundRoot != null)
        {
            if (!_barBackgroundPositionInitialized)
            {
                _barBackgroundShownAnchoredPosition = barBackgroundRoot.anchoredPosition;
                _barBackgroundPositionInitialized = true;
            }

            _barBackgroundCanvasGroup = barBackgroundRoot.GetComponent<CanvasGroup>();
            if (_barBackgroundCanvasGroup == null)
            {
                _barBackgroundCanvasGroup = barBackgroundRoot.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private Vector2 GetBarHiddenAnchoredPosition()
    {
        Vector2 hiddenPosition = _barShownAnchoredPosition;
        hiddenPosition.y -= GetBarHideDistance(barRoot);
        return hiddenPosition;
    }

    private Vector2 GetBarBackgroundHiddenAnchoredPosition()
    {
        if (barBackgroundRoot == null)
        {
            return Vector2.zero;
        }

        Vector2 hiddenPosition = _barBackgroundShownAnchoredPosition;
        hiddenPosition.y -= GetBarHideDistance(barBackgroundRoot);
        return hiddenPosition;
    }

    private float GetBarHideDistance(RectTransform targetRoot)
    {
        if (targetRoot == null)
        {
            return hiddenExtraOffset;
        }

        float barHeight = targetRoot.rect.height;
        if (barHeight <= 0f)
        {
            barHeight = Mathf.Abs(targetRoot.sizeDelta.y);
        }

        return barHeight + hiddenExtraOffset;
    }

    private void SetBarAlpha(float alpha)
    {
        if (!fadeBar || _barCanvasGroup == null)
        {
            if (!fadeBar)
            {
                return;
            }
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
        if (_barCanvasGroup == null)
        {
            if (_barBackgroundCanvasGroup == null)
            {
                return;
            }
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

    private void KillBarTweens()
    {
        _barMoveTween?.Kill();
        _barFadeTween?.Kill();
        _barBackgroundMoveTween?.Kill();
        _barBackgroundFadeTween?.Kill();
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
