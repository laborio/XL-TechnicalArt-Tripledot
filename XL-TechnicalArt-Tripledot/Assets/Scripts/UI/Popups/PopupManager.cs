using UnityEngine;

[DisallowMultipleComponent]
public class PopupManager : MonoBehaviour
{
    [SerializeField] private PopupBackdropView backdrop;
    [SerializeField] private bool closeOnBackdropClick = true;
    [SerializeField] private bool closeCurrentImmediatelyWhenOpeningAnother = true;

    private BasePopupView _currentPopup;

    public BasePopupView CurrentPopup => _currentPopup;

    private void Awake()
    {
        if (backdrop != null)
        {
            backdrop.Hide(immediate: true);
        }
    }

    private void OnEnable()
    {
        if (backdrop != null)
        {
            backdrop.Clicked += HandleBackdropClicked;
        }
    }

    private void OnDisable()
    {
        if (backdrop != null)
        {
            backdrop.Clicked -= HandleBackdropClicked;
        }

        if (_currentPopup != null)
        {
            UnbindPopup(_currentPopup);
            _currentPopup = null;
        }
    }

    public void OpenPopup(BasePopupView popup)
    {
        OpenPopup(popup, immediate: false);
    }

    public void OpenPopupImmediate(BasePopupView popup)
    {
        OpenPopup(popup, immediate: true);
    }

    public void OpenPopup(BasePopupView popup, bool immediate)
    {
        if (popup == null)
        {
            return;
        }

        if (_currentPopup == popup && popup.IsOpen)
        {
            return;
        }

        if (_currentPopup != null)
        {
            BasePopupView previousPopup = _currentPopup;
            UnbindPopup(previousPopup);
            _currentPopup = null;
            previousPopup.Close(immediate || closeCurrentImmediatelyWhenOpeningAnother);
        }

        _currentPopup = popup;
        _currentPopup.SetOwner(this);
        BindPopup(_currentPopup);

        if (backdrop != null)
        {
            backdrop.Show(immediate);
        }

        _currentPopup.Open(immediate);
    }

    public void CloseCurrent()
    {
        CloseCurrent(immediate: false);
    }

    public void CloseCurrentImmediate()
    {
        CloseCurrent(immediate: true);
    }

    public void CloseCurrent(bool immediate)
    {
        if (_currentPopup == null)
        {
            if (backdrop != null)
            {
                backdrop.Hide(immediate);
            }

            return;
        }

        _currentPopup.Close(immediate);
    }

    public void ClosePopup(BasePopupView popup)
    {
        ClosePopup(popup, immediate: false);
    }

    public void ClosePopup(BasePopupView popup, bool immediate)
    {
        if (popup == null)
        {
            return;
        }

        if (popup == _currentPopup)
        {
            _currentPopup.Close(immediate);
            return;
        }

        popup.Close(immediate);
    }

    private void HandlePopupClosed(BasePopupView popup)
    {
        if (popup != _currentPopup)
        {
            return;
        }

        UnbindPopup(popup);
        popup.SetOwner(null);
        _currentPopup = null;

        if (backdrop != null)
        {
            backdrop.Hide(immediate: false);
        }
    }

    private void BindPopup(BasePopupView popup)
    {
        popup.Closed += HandlePopupClosed;
    }

    private void UnbindPopup(BasePopupView popup)
    {
        popup.Closed -= HandlePopupClosed;
    }

    private void HandleBackdropClicked()
    {
        if (!closeOnBackdropClick)
        {
            return;
        }

        CloseCurrent(immediate: false);
    }
}
