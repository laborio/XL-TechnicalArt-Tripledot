using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIPopupBackdropView : MonoBehaviour
{
    public event Action Clicked;

    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button clickCatcherButton;
    [SerializeField] private GameObject blurRoot;

    [Header("Animation")]
    [SerializeField] private float visibleAlpha = 0.75f;
    [SerializeField] private float showDuration = 0.2f;
    [SerializeField] private float hideDuration = 0.2f;
    [SerializeField] private Ease showEase = Ease.OutCubic;
    [SerializeField] private Ease hideEase = Ease.InCubic;

    [Header("Post FX")]
    [SerializeField] private Volume postFxVolume;
    [SerializeField] private float sharpFocusDistance = 10f;
    [SerializeField] private float blurredFocusDistance = 0.1f;

    [Header("Behavior")]
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private bool deactivateOnHide = true;

    private Tween _fadeTween;
    private Tween _volumeTween;

    private void Awake()
    {
        EnsureReferences();

        if (hideOnAwake)
        {
            ApplyHiddenState();
            if (deactivateOnHide)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        if (clickCatcherButton != null)
        {
            clickCatcherButton.onClick.AddListener(HandleBackdropClicked);
        }
    }

    private void OnDisable()
    {
        if (clickCatcherButton != null)
        {
            clickCatcherButton.onClick.RemoveListener(HandleBackdropClicked);
        }

        _fadeTween?.Kill();
        _fadeTween = null;
        _volumeTween?.Kill();
        _volumeTween = null;
    }

    public void Show(bool immediate = false)
    {
        EnsureReferences();

        gameObject.SetActive(true);
        if (blurRoot != null)
        {
            blurRoot.SetActive(true);
        }

        _fadeTween?.Kill();
        _fadeTween = null;
        _volumeTween?.Kill();
        _volumeTween = null;

        SetInteractable(false);

        if (immediate)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visibleAlpha;
            }

            SetPostFxFocusDistance(blurredFocusDistance);
            SetInteractable(true);
            return;
        }

        TweenPostFxFocusDistance(blurredFocusDistance, showDuration, showEase);

        if (canvasGroup == null)
        {
            SetInteractable(true);
            return;
        }

        canvasGroup.alpha = 0f;
        _fadeTween = canvasGroup
            .DOFade(visibleAlpha, showDuration)
            .SetEase(showEase)
            .OnComplete(() => SetInteractable(true));
    }

    public void Hide(bool immediate = false)
    {
        EnsureReferences();

        _fadeTween?.Kill();
        _fadeTween = null;
        _volumeTween?.Kill();
        _volumeTween = null;

        SetInteractable(false);

        if (immediate)
        {
            ApplyHiddenState();
            if (deactivateOnHide)
            {
                gameObject.SetActive(false);
            }

            return;
        }

        TweenPostFxFocusDistance(sharpFocusDistance, hideDuration, hideEase);

        if (canvasGroup == null)
        {
            if (blurRoot != null)
            {
                blurRoot.SetActive(false);
            }

            SetPostFxFocusDistance(sharpFocusDistance);

            if (deactivateOnHide)
            {
                gameObject.SetActive(false);
            }

            return;
        }

        _fadeTween = canvasGroup
            .DOFade(0f, hideDuration)
            .SetEase(hideEase)
            .OnComplete(() =>
            {
                if (blurRoot != null)
                {
                    blurRoot.SetActive(false);
                }

                if (deactivateOnHide)
                {
                    gameObject.SetActive(false);
                }
            });
    }

    private void HandleBackdropClicked()
    {
        Clicked?.Invoke();
    }

    private void EnsureReferences()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (clickCatcherButton == null)
        {
            clickCatcherButton = GetComponent<Button>();
        }
    }

    private void ApplyHiddenState()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (blurRoot != null)
        {
            blurRoot.SetActive(false);
        }

        SetPostFxFocusDistance(sharpFocusDistance);
        SetInteractable(false);
    }

    private void TweenPostFxFocusDistance(float targetFocusDistance, float duration, Ease ease)
    {
        if (!TryGetDepthOfField(out DepthOfField depthOfField))
        {
            return;
        }

        _volumeTween?.Kill();
        _volumeTween = null;

        float clampedTargetFocusDistance = Mathf.Max(0.001f, targetFocusDistance);
        if (duration <= 0f)
        {
            SetPostFxFocusDistance(clampedTargetFocusDistance);
            return;
        }

        SetPostFxVolumeActive();
        _volumeTween = DOTween.To(
                () => depthOfField.focusDistance.value,
                value => depthOfField.focusDistance.value = Mathf.Max(0.001f, value),
                clampedTargetFocusDistance,
                duration)
            .SetEase(ease);
    }

    private void SetPostFxFocusDistance(float focusDistance)
    {
        if (!TryGetDepthOfField(out DepthOfField depthOfField))
        {
            return;
        }

        SetPostFxVolumeActive();
        depthOfField.focusDistance.value = Mathf.Max(0.001f, focusDistance);
    }

    private bool TryGetDepthOfField(out DepthOfField depthOfField)
    {
        depthOfField = null;
        if (postFxVolume == null)
        {
            return false;
        }

        VolumeProfile profile = postFxVolume.profile;
        if (profile == null || !profile.TryGet(out depthOfField))
        {
            return false;
        }

        depthOfField.active = true;
        depthOfField.mode.overrideState = true;
        depthOfField.mode.value = DepthOfFieldMode.Bokeh;
        depthOfField.focusDistance.overrideState = true;
        return true;
    }

    private void SetPostFxVolumeActive()
    {
        if (postFxVolume == null)
        {
            return;
        }

        postFxVolume.enabled = true;
        postFxVolume.weight = 1f;
    }

    private void SetInteractable(bool interactable)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (clickCatcherButton == null)
        {
            clickCatcherButton = GetComponent<Button>();
        }

    }
#endif
}
