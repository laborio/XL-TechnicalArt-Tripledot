using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class PopupCloseButton : MonoBehaviour
{
    [SerializeField] private PopupManager popupManager;
    [SerializeField] private BasePopupView popupToClose;
    [SerializeField] private bool closeImmediate = false;

    private Button _button;

    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        _button.onClick.AddListener(HandleClicked);
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClicked);
        }
    }

    private void HandleClicked()
    {
        if (popupManager != null)
        {
            if (popupToClose != null)
            {
                popupManager.ClosePopup(popupToClose, closeImmediate);
            }
            else
            {
                popupManager.CloseCurrent(closeImmediate);
            }

            return;
        }

        if (popupToClose != null)
        {
            popupToClose.Close(closeImmediate);
        }
    }
}
