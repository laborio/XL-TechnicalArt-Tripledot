using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIIdleRotateFadeLoop : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform targetRectTransform;
    [SerializeField] private Graphic targetGraphic;

    [Header("Rotation")]
    [SerializeField] private float rotationDegreesPerSecond = 25f;
    [SerializeField] private bool clockwise = true;

    [Header("Opacity")]
    [SerializeField] private float alphaMax = 1f;
    [SerializeField] private float alphaMin = 0.75f;
    [SerializeField] private float alphaHalfCycleDuration = 0.8f;

    [Header("Behavior")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool resetOnDisable = true;

    private float _baseZRotation;
    private float _alphaPhase;
    private bool _hasBaseState;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        EnsureReferences();

        if (targetRectTransform != null)
        {
            _baseZRotation = targetRectTransform.localEulerAngles.z;
            _hasBaseState = true;
        }

        _alphaPhase = 0f;
        ApplyAlpha(alphaMax);
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }

        UpdateRotation(dt);
        UpdateOpacity(dt);
    }

    private void OnDisable()
    {
        if (!resetOnDisable)
        {
            return;
        }

        if (targetRectTransform != null && _hasBaseState)
        {
            Vector3 angles = targetRectTransform.localEulerAngles;
            angles.z = _baseZRotation;
            targetRectTransform.localEulerAngles = angles;
        }

        ApplyAlpha(alphaMax);
    }

    private void UpdateRotation(float dt)
    {
        if (targetRectTransform == null)
        {
            return;
        }

        float direction = clockwise ? -1f : 1f;
        Vector3 angles = targetRectTransform.localEulerAngles;
        angles.z += direction * rotationDegreesPerSecond * dt;
        targetRectTransform.localEulerAngles = angles;
    }

    private void UpdateOpacity(float dt)
    {
        if (targetGraphic == null)
        {
            return;
        }

        float halfCycle = Mathf.Max(0.0001f, alphaHalfCycleDuration);
        _alphaPhase += dt / halfCycle;
        float pingPong = Mathf.PingPong(_alphaPhase, 1f);
        float alpha = Mathf.Lerp(alphaMax, alphaMin, pingPong);
        ApplyAlpha(alpha);
    }

    private void ApplyAlpha(float alpha)
    {
        if (targetGraphic == null)
        {
            return;
        }

        Color color = targetGraphic.color;
        color.a = Mathf.Clamp01(alpha);
        targetGraphic.color = color;
    }

    private void EnsureReferences()
    {
        if (targetRectTransform == null)
        {
            targetRectTransform = transform as RectTransform;
        }

        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        alphaMax = Mathf.Clamp01(alphaMax);
        alphaMin = Mathf.Clamp01(alphaMin);
        alphaHalfCycleDuration = Mathf.Max(0.01f, alphaHalfCycleDuration);

        if (alphaMin > alphaMax)
        {
            alphaMin = alphaMax;
        }

        EnsureReferences();
    }
#endif
}
