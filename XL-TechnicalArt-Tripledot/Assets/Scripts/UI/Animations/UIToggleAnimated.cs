using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Toggle))]
public class UIToggleAnimated : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Toggle toggle;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private RectTransform indicator;
    [SerializeField] private RectTransform trackRect;
    [SerializeField] private UITheme theme;

    [Header("Theme Keys")]
    [SerializeField] private string onColorKey = "AccentGreen";
    [SerializeField] private string offColorKey = "PrimaryDark";
    [SerializeField] private Color onFallbackColor = new Color(0.53f, 0.9f, 0.04f, 1f);
    [SerializeField] private Color offFallbackColor = new Color(0.12f, 0.34f, 0.73f, 1f);

    [Header("Animation")]
    [SerializeField] private float indicatorMargin = 4f;
    [SerializeField] private float moveDuration = 0.18f;
    [SerializeField] private float colorDuration = 0.14f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;
    [SerializeField] private Ease colorEase = Ease.OutCubic;

    private Tween _moveTween;
    private Tween _colorTween;

    private void Awake()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }

        if (trackRect == null && indicator != null)
        {
            trackRect = indicator.parent as RectTransform;
        }
    }

    private void OnEnable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(HandleToggleValueChanged);
        }

        ApplyVisualState(toggle != null && toggle.isOn, true);
    }

    private void OnDisable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
        }

        _moveTween?.Kill();
        _colorTween?.Kill();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || toggle == null)
        {
            return;
        }

        ApplyVisualState(toggle.isOn, true);
    }

    public void SetIsOn(bool isOn, bool immediate)
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(isOn);
        }

        ApplyVisualState(isOn, immediate);
    }

    private void HandleToggleValueChanged(bool isOn)
    {
        ApplyVisualState(isOn, false);
    }

    private void ApplyVisualState(bool isOn, bool immediate)
    {
        if (indicator == null || backgroundImage == null)
        {
            return;
        }

        _moveTween?.Kill();
        _colorTween?.Kill();

        Color targetColor = GetStateColor(isOn);
        float targetX = GetIndicatorTargetX(isOn);

        if (immediate)
        {
            SetIndicatorX(targetX);
            backgroundImage.color = targetColor;
            return;
        }

        _moveTween = indicator.DOAnchorPosX(targetX, moveDuration).SetEase(moveEase);
        _colorTween = backgroundImage.DOColor(targetColor, colorDuration).SetEase(colorEase);
    }

    private float GetIndicatorTargetX(bool isOn)
    {
        RectTransform track = trackRect != null ? trackRect : indicator.parent as RectTransform;
        if (track == null)
        {
            return indicator.anchoredPosition.x;
        }

        float trackWidth = track.rect.width;
        float indicatorWidth = indicator.rect.width;

        float left = indicatorMargin + indicatorWidth * indicator.pivot.x;
        float right = trackWidth - indicatorMargin - indicatorWidth * (1f - indicator.pivot.x);

        return isOn ? right : left;
    }

    private Color GetStateColor(bool isOn)
    {
        string key = isOn ? onColorKey : offColorKey;
        Color fallback = isOn ? onFallbackColor : offFallbackColor;

        if (theme != null && theme.TryGetColor(key, out Color themedColor))
        {
            return themedColor;
        }

        return fallback;
    }

    private void SetIndicatorX(float x)
    {
        Vector2 position = indicator.anchoredPosition;
        position.x = x;
        indicator.anchoredPosition = position;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }

        if (trackRect == null && indicator != null)
        {
            trackRect = indicator.parent as RectTransform;
        }

        if (!Application.isPlaying)
        {
            ApplyVisualState(toggle != null && toggle.isOn, true);
        }
    }
#endif
}
