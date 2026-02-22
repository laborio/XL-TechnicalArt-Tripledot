using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal sealed class BottomBarLayoutProjector
{
    private RectTransform _buttonsContainer;
    private RectTransform _selectionHighlight;
    private float _selectedWidthExtra;
    private float _highlightHorizontalInset;
    private float _highlightMinWidth;

    public void Configure(
        RectTransform buttonsContainer,
        RectTransform selectionHighlight,
        float selectedWidthExtra,
        float highlightHorizontalInset,
        float highlightMinWidth)
    {
        _buttonsContainer = buttonsContainer;
        _selectionHighlight = selectionHighlight;
        _selectedWidthExtra = selectedWidthExtra;
        _highlightHorizontalInset = highlightHorizontalInset;
        _highlightMinWidth = highlightMinWidth;
    }

    public void EnsureLayoutUpToDate()
    {
        Canvas.ForceUpdateCanvases();

        if (_buttonsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_buttonsContainer);
        }
    }

    public Vector2 GetHighlightTargetPosition(RectTransform tabRect)
    {
        if (tabRect == null || _buttonsContainer == null || _selectionHighlight == null)
        {
            return Vector2.zero;
        }

        Vector3 worldCenter = tabRect.TransformPoint(tabRect.rect.center);
        Vector3 localCenter = _buttonsContainer.InverseTransformPoint(worldCenter);

        Vector2 targetPosition = _selectionHighlight.anchoredPosition;
        targetPosition.x = localCenter.x;
        return targetPosition;
    }

    public float GetCurrentButtonWidth(TabButtonView button)
    {
        if (button == null)
        {
            return 1f;
        }

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

    public float GetTargetButtonWidth(TabButtonView button, bool selected, IDictionary<TabButtonView, float> baseWidths)
    {
        if (button == null)
        {
            return 0f;
        }

        if (baseWidths == null)
        {
            float uncachedBaseWidth = GetCurrentButtonWidth(button);
            return selected ? uncachedBaseWidth + _selectedWidthExtra : uncachedBaseWidth;
        }

        if (!baseWidths.TryGetValue(button, out float baseWidth))
        {
            baseWidth = GetCurrentButtonWidth(button);
            baseWidths[button] = baseWidth;
        }

        return selected ? baseWidth + _selectedWidthExtra : baseWidth;
    }

    public float GetHighlightTargetWidth(TabButtonView button, IDictionary<TabButtonView, float> baseWidths, bool selected)
    {
        float buttonWidth = GetTargetButtonWidth(button, selected, baseWidths);
        float targetWidth = buttonWidth - (_highlightHorizontalInset * 2f);
        return Mathf.Max(_highlightMinWidth, targetWidth);
    }

    public Vector2 GetProjectedHighlightTargetPosition(
        TabButtonView previousButton,
        TabButtonView currentButton,
        IDictionary<TabButtonView, float> baseWidths)
    {
        if (currentButton == null)
        {
            return Vector2.zero;
        }

        if (_buttonsContainer == null)
        {
            return GetHighlightTargetPosition(currentButton.RectTransform);
        }

        LayoutElement previousLayout = previousButton != null ? previousButton.LayoutElement : null;
        LayoutElement currentLayout = currentButton.LayoutElement;

        float previousOriginalPreferred = 0f;
        float currentOriginalPreferred = 0f;
        bool previousChanged = false;
        bool currentChanged = false;

        if (previousLayout != null)
        {
            previousOriginalPreferred = previousLayout.preferredWidth;
            previousLayout.preferredWidth = GetTargetButtonWidth(previousButton, false, baseWidths);
            previousChanged = true;
        }

        if (currentLayout != null)
        {
            currentOriginalPreferred = currentLayout.preferredWidth;
            currentLayout.preferredWidth = GetTargetButtonWidth(currentButton, true, baseWidths);
            currentChanged = true;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_buttonsContainer);
        Vector2 projectedPosition = GetHighlightTargetPosition(currentButton.RectTransform);

        if (previousChanged)
        {
            previousLayout.preferredWidth = previousOriginalPreferred;
        }

        if (currentChanged)
        {
            currentLayout.preferredWidth = currentOriginalPreferred;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_buttonsContainer);
        return projectedPosition;
    }
}
