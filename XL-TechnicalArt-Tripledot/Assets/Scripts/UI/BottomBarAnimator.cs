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
    private BottomBarVisibilityController _visibilityController;
    private BottomBarLayoutProjector _layoutProjector;

    private Tween _highlightMoveTween;
    private Tween _highlightWidthTween;
    private Tween _highlightScaleTween;
    private Tween _highlightFadeTween;
    private CanvasGroup _highlightCanvasGroup;

    public bool IsShown => _visibilityController == null || _visibilityController.IsShown;

    public void Initialize(IReadOnlyList<TabButtonView> buttons)
    {
        EnsureControllersConfigured();

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
        EnsureControllersConfigured();
        _visibilityController.Show(immediate);
    }

    public void Hide(bool immediate = false)
    {
        EnsureControllersConfigured();
        _visibilityController.Hide(immediate);
    }

    public void AnimateSelect(TabButtonView previousButton, TabButtonView currentButton)
    {
        EnsureControllersConfigured();

        if (currentButton == null)
        {
            return;
        }

        _layoutProjector.EnsureLayoutUpToDate();

        if (selectionHighlight != null && buttonsContainer != null)
        {
            KillHighlightTweens();

            float targetWidth = _layoutProjector.GetHighlightTargetWidth(currentButton, _baseWidths, selected: true);
            Vector2 targetPosition = _layoutProjector.GetProjectedHighlightTargetPosition(previousButton, currentButton, _baseWidths);

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
        EnsureControllersConfigured();
        _layoutProjector.EnsureLayoutUpToDate();

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
        EnsureControllersConfigured();
        return _layoutProjector.GetHighlightTargetPosition(tabRect);
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

        _layoutProjector.EnsureLayoutUpToDate();

        for (int i = 0; i < buttons.Count; i++)
        {
            TabButtonView button = buttons[i];
            if (button == null)
            {
                continue;
            }

            float baseWidth = _layoutProjector.GetCurrentButtonWidth(button);
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

        float targetWidth = _layoutProjector.GetTargetButtonWidth(button, selected, _baseWidths);

        if (_buttonWidthTweens.TryGetValue(button, out Tween existingTween) && existingTween.IsActive())
        {
            existingTween.Kill();
        }

        if (layoutElement.preferredWidth < 0f)
        {
            layoutElement.preferredWidth = _layoutProjector.GetCurrentButtonWidth(button);
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

    private void EnsureControllersConfigured()
    {
        if (_visibilityController == null)
        {
            _visibilityController = new BottomBarVisibilityController();
        }

        _visibilityController.Configure(
            transform,
            barRoot,
            barBackgroundRoot,
            fadeBar,
            showDuration,
            hideDuration,
            showEase,
            hideEase,
            hiddenExtraOffset);

        if (_layoutProjector == null)
        {
            _layoutProjector = new BottomBarLayoutProjector();
        }

        _layoutProjector.Configure(
            buttonsContainer,
            selectionHighlight,
            selectedWidthExtra,
            highlightHorizontalInset,
            highlightMinWidth);
    }

    private void OnDestroy()
    {
        _visibilityController?.Dispose();
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
