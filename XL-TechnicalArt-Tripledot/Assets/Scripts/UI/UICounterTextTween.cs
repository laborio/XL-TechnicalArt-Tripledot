using DG.Tweening;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class UICounterTextTween : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private int targetValue;

    [Header("Format")]
    [SerializeField] private string prefix = string.Empty;
    [SerializeField] private string suffix = string.Empty;
    [SerializeField] private bool useThousandsSeparator = true;

    [Header("Animation")]
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private Ease ease = Ease.OutCubic;
    [SerializeField] private bool useUnscaledTime = true;

    public int CurrentValue => _currentValue;
    public int TargetValue => targetValue;

    private Tween _valueTween;
    private int _currentValue;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }

    private void OnDisable()
    {
        _valueTween?.Kill();
        _valueTween = null;
    }

    public void SetValueImmediate(int value)
    {
        _valueTween?.Kill();
        _valueTween = null;
        ApplyValue(value);
    }

    public void SetTargetValue(int value)
    {
        targetValue = value;
    }

    public void Play()
    {
        PlayTo(targetValue);
    }

    public void PlayTo(int target)
    {
        Play(0, target, duration);
    }

    public void Play(int fromValue, int target, float customDuration)
    {
        _valueTween?.Kill();
        _valueTween = null;

        ApplyValue(fromValue);

        if (Mathf.Approximately(customDuration, 0f) || fromValue == target)
        {
            ApplyValue(target);
            return;
        }

        float tweenValue = fromValue;
        _valueTween = DOTween.To(
                () => tweenValue,
                tween =>
                {
                    tweenValue = tween;
                    ApplyValue(Mathf.RoundToInt(tweenValue));
                },
                target,
                Mathf.Max(0f, customDuration))
            .SetEase(ease)
            .SetUpdate(useUnscaledTime)
            .OnComplete(() => ApplyValue(target));
    }

    [ContextMenu("Refresh Text")]
    public void RefreshText()
    {
        ApplyValue(_currentValue);
    }

    private void ApplyValue(int value)
    {
        _currentValue = value;

        if (targetText == null)
        {
            return;
        }

        string numberText = useThousandsSeparator
            ? value.ToString("N0")
            : value.ToString();

        targetText.text = $"{prefix}{numberText}{suffix}";
    }
}
