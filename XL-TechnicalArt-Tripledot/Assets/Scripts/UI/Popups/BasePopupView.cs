using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class BasePopupView : MonoBehaviour
{
    public event Action<BasePopupView> Opened;
    public event Action<BasePopupView> Closed;

    [Header("References")]
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Open / Close")]
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private Ease openEase = Ease.OutBack;
    [SerializeField] private Ease closeEase = Ease.InCubic;
    [SerializeField] private float hiddenScale = 0.92f;
    [SerializeField] private float hiddenYOffset = 40f;

    [Header("Idle")]
    [SerializeField] private bool playIdleAnimation = false;
    [SerializeField] private float idlePulseScale = 0.01f;
    [SerializeField] private float idlePulseDuration = 1.2f;

    [Header("Behavior")]
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private bool deactivateOnClose = true;
    [SerializeField] private bool forceLayoutRebuildOnOpen = true;

    private PopupManager _owner;
    private Tween _openTween;
    private Tween _closeTween;
    private Tween _idleTween;
    private Vector2 _shownAnchoredPosition;
    private bool _initialized;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        EnsureInitialized();

        if (hideOnAwake)
        {
            ApplyHiddenVisualState();
            SetCanvasInteractable(false);
            IsOpen = false;

            if (deactivateOnClose)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void OnDisable()
    {
        StopIdleAnimation();
        KillOpenCloseTweens();
    }

    private void OnDestroy()
    {
        StopIdleAnimation();
        KillOpenCloseTweens();
    }

    public void SetOwner(PopupManager owner)
    {
        _owner = owner;
    }

    public void Open(bool immediate = false)
    {
        EnsureInitialized();

        gameObject.SetActive(true);
        RebuildLayoutNow();
        StopIdleAnimation();
        KillOpenCloseTweens();

        if (immediate)
        {
            ApplyShownVisualState();
            SetCanvasInteractable(true);
            IsOpen = true;
            StartIdleAnimation();
            Opened?.Invoke(this);
            return;
        }

        PrepareOpenVisualState();
        SetCanvasInteractable(false);

        Sequence sequence = DOTween.Sequence();
        sequence.Join(canvasGroup.DOFade(1f, openDuration).SetEase(Ease.OutCubic));
        sequence.Join(popupRoot.DOAnchorPos(_shownAnchoredPosition, openDuration).SetEase(openEase));
        sequence.Join(popupRoot.DOScale(1f, openDuration).SetEase(openEase));
        _openTween = sequence.OnComplete(() =>
        {
            IsOpen = true;
            SetCanvasInteractable(true);
            StartIdleAnimation();
            Opened?.Invoke(this);
        });
    }

    public void Close(bool immediate = false)
    {
        EnsureInitialized();

        StopIdleAnimation();
        KillOpenCloseTweens();

        if (immediate)
        {
            ApplyHiddenVisualState();
            SetCanvasInteractable(false);
            IsOpen = false;
            Closed?.Invoke(this);

            if (deactivateOnClose)
            {
                gameObject.SetActive(false);
            }

            return;
        }

        SetCanvasInteractable(false);

        Vector2 hiddenPosition = GetHiddenAnchoredPosition();
        Sequence sequence = DOTween.Sequence();
        sequence.Join(canvasGroup.DOFade(0f, closeDuration).SetEase(Ease.InCubic));
        sequence.Join(popupRoot.DOAnchorPos(hiddenPosition, closeDuration).SetEase(closeEase));
        sequence.Join(popupRoot.DOScale(hiddenScale, closeDuration).SetEase(closeEase));
        _closeTween = sequence.OnComplete(() =>
        {
            IsOpen = false;
            Closed?.Invoke(this);

            if (deactivateOnClose)
            {
                gameObject.SetActive(false);
            }
        });
    }

    public void RequestClose(bool immediate = false)
    {
        if (_owner != null)
        {
            _owner.ClosePopup(this, immediate);
            return;
        }

        Close(immediate);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        if (popupRoot == null)
        {
            popupRoot = transform as RectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        _shownAnchoredPosition = popupRoot != null ? popupRoot.anchoredPosition : Vector2.zero;
        _initialized = true;
    }

    private void PrepareOpenVisualState()
    {
        if (popupRoot == null)
        {
            return;
        }

        popupRoot.anchoredPosition = GetHiddenAnchoredPosition();
        popupRoot.localScale = new Vector3(hiddenScale, hiddenScale, 1f);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void ApplyShownVisualState()
    {
        if (popupRoot != null)
        {
            popupRoot.anchoredPosition = _shownAnchoredPosition;
            popupRoot.localScale = Vector3.one;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private void ApplyHiddenVisualState()
    {
        if (popupRoot != null)
        {
            popupRoot.anchoredPosition = GetHiddenAnchoredPosition();
            popupRoot.localScale = new Vector3(hiddenScale, hiddenScale, 1f);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private Vector2 GetHiddenAnchoredPosition()
    {
        return _shownAnchoredPosition + new Vector2(0f, -hiddenYOffset);
    }

    private void SetCanvasInteractable(bool interactable)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    private void StartIdleAnimation()
    {
        if (!playIdleAnimation || popupRoot == null || idlePulseScale <= 0f)
        {
            return;
        }

        StopIdleAnimation();

        Vector3 pulseScale = Vector3.one * (1f + idlePulseScale);
        _idleTween = popupRoot
            .DOScale(pulseScale, idlePulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopIdleAnimation()
    {
        _idleTween?.Kill();
        _idleTween = null;

        if (popupRoot != null && IsOpen)
        {
            popupRoot.localScale = Vector3.one;
        }
    }

    private void KillOpenCloseTweens()
    {
        _openTween?.Kill();
        _closeTween?.Kill();
        _openTween = null;
        _closeTween = null;
    }

    private void RebuildLayoutNow()
    {
        if (!forceLayoutRebuildOnOpen || popupRoot == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(popupRoot);
        Canvas.ForceUpdateCanvases();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (popupRoot == null)
        {
            popupRoot = transform as RectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }
#endif
}
