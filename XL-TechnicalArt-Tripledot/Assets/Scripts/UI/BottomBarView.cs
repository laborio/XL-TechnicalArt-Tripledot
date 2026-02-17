using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BottomBarView : MonoBehaviour
{
    public event Action<BottomBarContent> ContentActivated;
    public event Action Closed;

    [SerializeField] private List<TabButtonView> tabButtons = new List<TabButtonView>(5);
    [SerializeField] private BottomBarAnimator animator;
    [SerializeField] private bool closeContentWhenToggledOff = true;
    [Header("Visibility Toggle Button")]
    [SerializeField] private Button visibilityToggleButton;
    [SerializeField] private RectTransform visibilityToggleArrow;
    [SerializeField] private float visibilityToggleArrowDuration = 0.2f;
    [SerializeField] private Ease visibilityToggleArrowEase = Ease.OutCubic;

    private int _selectedIndex = -1;
    private Tween _visibilityToggleArrowTween;

    public int SelectedIndex => _selectedIndex;

    private void Awake()
    {
        if (tabButtons.Count == 0)
        {
            tabButtons.AddRange(GetComponentsInChildren<TabButtonView>(true));
        }

        for (int i = 0; i < tabButtons.Count; i++)
        {
            TabButtonView button = tabButtons[i];
            if (button != null)
            {
                button.SetSelected(false);
            }
        }

        if (animator != null)
        {
            animator.Initialize(tabButtons);
        }

        SetVisibilityToggleArrowRotation(isBarHidden: animator != null && !animator.IsShown, immediate: true);
    }

    private void OnEnable()
    {
        if (visibilityToggleButton != null)
        {
            visibilityToggleButton.onClick.AddListener(ToggleVisibilityFromButton);
        }

        for (int i = 0; i < tabButtons.Count; i++)
        {
            TabButtonView button = tabButtons[i];
            if (button != null)
            {
                button.Clicked += HandleButtonClicked;
            }
        }
    }

    private void OnDisable()
    {
        if (visibilityToggleButton != null)
        {
            visibilityToggleButton.onClick.RemoveListener(ToggleVisibilityFromButton);
        }

        for (int i = 0; i < tabButtons.Count; i++)
        {
            TabButtonView button = tabButtons[i];
            if (button != null)
            {
                button.Clicked -= HandleButtonClicked;
            }
        }
    }

    private void OnDestroy()
    {
        _visibilityToggleArrowTween?.Kill();
    }

    public void Select(BottomBarContent content)
    {
        int index = tabButtons.FindIndex(button => button != null && button.Content == content);
        if (index >= 0)
        {
            SelectIndex(index);
        }
    }

    public void Deselect()
    {
        DeselectCurrent();
    }

    public void Show(bool immediate = false)
    {
        if (animator != null)
        {
            animator.Show(immediate);
        }

        SetVisibilityToggleArrowRotation(isBarHidden: false, immediate: immediate);
    }

    public void Hide(bool immediate = false, bool closeContent = true)
    {
        if (closeContent)
        {
            DeselectCurrent();
        }

        if (animator != null)
        {
            animator.Hide(immediate);
        }

        SetVisibilityToggleArrowRotation(isBarHidden: true, immediate: immediate);
    }

    public void ToggleVisibilityFromButton()
    {
        if (animator == null)
        {
            return;
        }

        if (animator.IsShown)
        {
            Hide(immediate: false, closeContent: closeContentWhenToggledOff);
            return;
        }

        Show(immediate: false);
    }

    private void SetVisibilityToggleArrowRotation(bool isBarHidden, bool immediate)
    {
        if (visibilityToggleArrow == null)
        {
            return;
        }

        Vector3 currentRotation = visibilityToggleArrow.localEulerAngles;
        Vector3 targetRotation = new Vector3(currentRotation.x, currentRotation.y, isBarHidden ? 180f : 0f);

        if (immediate)
        {
            _visibilityToggleArrowTween?.Kill();
            visibilityToggleArrow.localEulerAngles = targetRotation;
            return;
        }

        _visibilityToggleArrowTween?.Kill();
        _visibilityToggleArrowTween = visibilityToggleArrow
            .DOLocalRotate(targetRotation, visibilityToggleArrowDuration, RotateMode.Fast)
            .SetEase(visibilityToggleArrowEase);
    }

    private void HandleButtonClicked(TabButtonView clickedButton)
    {
        if (clickedButton == null)
        {
            return;
        }

        if (clickedButton.IsLocked)
        {
            clickedButton.PlayLockedWiggle();
            return;
        }

        int clickedIndex = tabButtons.IndexOf(clickedButton);
        if (clickedIndex < 0)
        {
            return;
        }

        if (_selectedIndex == clickedIndex)
        {
            DeselectCurrent();
            return;
        }

        SelectIndex(clickedIndex);
    }

    private void SelectIndex(int index)
    {
        if (index < 0 || index >= tabButtons.Count)
        {
            return;
        }

        TabButtonView currentButton = tabButtons[index];
        if (currentButton == null || currentButton.IsLocked)
        {
            return;
        }

        TabButtonView previousButton = null;
        if (_selectedIndex >= 0 && _selectedIndex < tabButtons.Count)
        {
            previousButton = tabButtons[_selectedIndex];
            if (previousButton != null)
            {
                previousButton.SetSelected(false);
            }
        }

        _selectedIndex = index;
        currentButton.SetSelected(true);

        if (animator != null)
        {
            animator.AnimateSelect(previousButton, currentButton);
        }

        ContentActivated?.Invoke(currentButton.Content);
    }

    private void DeselectCurrent()
    {
        if (_selectedIndex < 0 || _selectedIndex >= tabButtons.Count)
        {
            return;
        }

        TabButtonView selectedButton = tabButtons[_selectedIndex];
        _selectedIndex = -1;

        if (selectedButton != null)
        {
            selectedButton.SetSelected(false);
        }

        if (animator != null)
        {
            animator.AnimateClose(selectedButton);
        }

        Closed?.Invoke();
    }
}
