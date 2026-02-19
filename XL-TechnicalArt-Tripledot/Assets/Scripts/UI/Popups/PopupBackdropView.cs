using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PopupBackdropView : MonoBehaviour
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
    [SerializeField] private float visiblePostFxWeight = 1f;

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

            SetPostFxWeight(visiblePostFxWeight, disableWhenZero: false);
            SetInteractable(true);
            return;
        }

        TweenPostFxWeight(visiblePostFxWeight, showDuration, showEase);

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

        TweenPostFxWeight(0f, hideDuration, hideEase);

        if (canvasGroup == null)
        {
            if (blurRoot != null)
            {
                blurRoot.SetActive(false);
            }

            SetPostFxWeight(0f, disableWhenZero: true);

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

        SetPostFxWeight(0f, disableWhenZero: true);
        SetInteractable(false);
    }

    private void TweenPostFxWeight(float targetWeight, float duration, Ease ease)
    {
        if (postFxVolume == null)
        {
            return;
        }

        _volumeTween?.Kill();
        _volumeTween = null;

        if (duration <= 0f)
        {
            SetPostFxWeight(targetWeight, disableWhenZero: targetWeight <= 0f);
            return;
        }

        postFxVolume.enabled = true;
        _volumeTween = DOTween.To(
                () => postFxVolume.weight,
                value => postFxVolume.weight = value,
                Mathf.Clamp01(targetWeight),
                duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                if (postFxVolume != null && targetWeight <= 0f)
                {
                    postFxVolume.enabled = false;
                }
            });
    }

    private void SetPostFxWeight(float weight, bool disableWhenZero)
    {
        if (postFxVolume == null)
        {
            return;
        }

        postFxVolume.weight = Mathf.Clamp01(weight);
        postFxVolume.enabled = !disableWhenZero || postFxVolume.weight > 0f;
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
