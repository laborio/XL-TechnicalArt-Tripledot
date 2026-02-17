using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BottomBarView : MonoBehaviour
{
    public event Action<BottomBarContent> ContentActivated;
    public event Action Closed;

    [SerializeField] private List<TabButtonView> tabButtons = new List<TabButtonView>(5);
    [SerializeField] private BottomBarAnimator animator;

    private int _selectedIndex = -1;

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
    }

    private void OnEnable()
    {
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
        for (int i = 0; i < tabButtons.Count; i++)
        {
            TabButtonView button = tabButtons[i];
            if (button != null)
            {
                button.Clicked -= HandleButtonClicked;
            }
        }
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
    }

    private void HandleButtonClicked(TabButtonView clickedButton)
    {
        if (clickedButton == null || clickedButton.IsLocked)
        {
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
