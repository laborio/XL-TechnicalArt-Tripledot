using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class UITweenAnimationIconHighlight : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Scale")]
    [SerializeField] private float scaleMultiplier = 1.06f;
    [SerializeField] private float halfCycleDuration = 0.45f;
    [SerializeField] private Ease ease = Ease.InOutSine;

    [Header("Wiggle")]
    [SerializeField] private bool enableWiggle = true;
    [SerializeField] private float wiggleAngle = 6f;
    [SerializeField] private float wiggleHalfCycleDuration = 0.2f;
    [SerializeField] private Ease wiggleEase = Ease.InOutSine;

    [Header("Behavior")]
    [SerializeField] private bool useUnscaledTime = true;

    private Tween _idleTween;
    private Tween _wiggleTween;
    private Vector3 _baseScale;
    private Vector3 _baseEulerAngles;
    private bool _hasBaseScale;
    private bool _hasBaseRotation;

    private void Awake()
    {
        if (target == null)
        {
            target = transform;
        }

        if (target != null)
        {
            _baseScale = target.localScale;
            _baseEulerAngles = target.localEulerAngles;
            _hasBaseScale = true;
            _hasBaseRotation = true;
        }
    }

    private void OnEnable()
    {
        StartIdle();
    }

    private void OnDisable()
    {
        StopIdle();
    }

    public void StartIdle()
    {
        if (target == null)
        {
            return;
        }

        StopIdle();

        _baseScale = target.localScale;
        _baseEulerAngles = target.localEulerAngles;
        _hasBaseScale = true;
        _hasBaseRotation = true;
        Vector3 targetScale = _baseScale * Mathf.Max(0f, scaleMultiplier);

        _idleTween = target
            .DOScale(targetScale, Mathf.Max(0.01f, halfCycleDuration))
            .SetEase(ease)
            .SetUpdate(useUnscaledTime)
            .SetLoops(-1, LoopType.Yoyo);

        if (!enableWiggle || Mathf.Approximately(wiggleAngle, 0f))
        {
            return;
        }

        Vector3 startEuler = _baseEulerAngles + new Vector3(0f, 0f, -Mathf.Abs(wiggleAngle));
        Vector3 endEuler = _baseEulerAngles + new Vector3(0f, 0f, Mathf.Abs(wiggleAngle));
        target.localEulerAngles = startEuler;

        _wiggleTween = target
            .DOLocalRotate(endEuler, Mathf.Max(0.01f, wiggleHalfCycleDuration))
            .SetEase(wiggleEase)
            .SetUpdate(useUnscaledTime)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void StopIdle()
    {
        _idleTween?.Kill();
        _idleTween = null;
        _wiggleTween?.Kill();
        _wiggleTween = null;

        if (target != null)
        {
            if (_hasBaseScale)
            {
                target.localScale = _baseScale;
            }

            if (_hasBaseRotation)
            {
                target.localEulerAngles = _baseEulerAngles;
            }
        }
    }
}
