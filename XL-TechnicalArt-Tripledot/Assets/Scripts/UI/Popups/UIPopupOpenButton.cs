using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIPopupOpenButton : MonoBehaviour
{
    [SerializeField] private UIPopupManager popupManager;
    [SerializeField] private UIBasePopupView popupToOpen;
    [SerializeField] private bool openImmediate = false;

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
        if (popupManager == null || popupToOpen == null)
        {
            return;
        }

        popupManager.OpenPopup(popupToOpen, openImmediate);
    }
}
