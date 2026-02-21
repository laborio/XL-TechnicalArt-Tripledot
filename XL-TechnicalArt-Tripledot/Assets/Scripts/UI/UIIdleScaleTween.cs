using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class UIIdleScaleTween : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float scaleMultiplier = 1.06f;
    [SerializeField] private float halfCycleDuration = 0.45f;
    [SerializeField] private Ease ease = Ease.InOutSine;
    [SerializeField] private bool useUnscaledTime = true;

    private Tween _idleTween;
    private Vector3 _baseScale;
    private bool _hasBaseScale;

    private void Awake()
    {
        if (target == null)
        {
            target = transform;
        }

        if (target != null)
        {
            _baseScale = target.localScale;
            _hasBaseScale = true;
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
        _hasBaseScale = true;
        Vector3 targetScale = _baseScale * Mathf.Max(0f, scaleMultiplier);

        _idleTween = target
            .DOScale(targetScale, Mathf.Max(0f, halfCycleDuration))
            .SetEase(ease)
            .SetUpdate(useUnscaledTime)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void StopIdle()
    {
        _idleTween?.Kill();
        _idleTween = null;

        if (target != null)
        {
            target.localScale = _hasBaseScale ? _baseScale : target.localScale;
        }
    }
}
