using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class UIThemeImage : MonoBehaviour
{
    [SerializeField]
    private UITheme theme;

    [SerializeField]
    private UITheme.ColorToken colorToken = UITheme.ColorToken.Primary;

    [SerializeField]
    private string customColorKey = "Primary";

    private Image _image;

    private void Start()
    {
        ApplyTheme();
    }

    private void OnEnable()
    {
        ApplyTheme();
    }

    private void OnValidate()
    {
        ApplyTheme();
    }

    [ContextMenu("Apply Theme")]
    public void ApplyTheme()
    {
        if (!TryGetImage(out Image image) || theme == null)
        {
            return;
        }

        string key = ResolveColorKey();
        if (!theme.TryGetColor(key, out Color color))
        {
            return;
        }

        image.color = color;
    }

    private string ResolveColorKey()
    {
        return colorToken == UITheme.ColorToken.Custom
            ? customColorKey
            : UITheme.ToKey(colorToken);
    }

    private bool TryGetImage(out Image image)
    {
        if (_image == null)
        {
            _image = GetComponent<Image>();
        }

        image = _image;
        return image != null;
    }
}
