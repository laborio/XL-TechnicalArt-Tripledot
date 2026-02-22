using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class UIFPSDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] [Min(0.05f)] private float refreshInterval = 0.25f;
    [Tooltip("Lerp factor: 1 = raw frame time, 0 = holds previous sample.")]
    [SerializeField] [Range(0f, 1f)] private float smoothing = 0.1f;
    [SerializeField] private bool showMilliseconds = true;

    private float _smoothedDeltaTime = 1f / 60f;
    private float _nextRefreshTime;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        _smoothedDeltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        _nextRefreshTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (targetText == null)
        {
            return;
        }

        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        _smoothedDeltaTime = Mathf.Lerp(_smoothedDeltaTime, dt, smoothing);

        if (Time.unscaledTime < _nextRefreshTime)
        {
            return;
        }

        float fps = 1f / _smoothedDeltaTime;
        float ms = _smoothedDeltaTime * 1000f;

        targetText.text = showMilliseconds
            ? $"{fps:0} FPS ({ms:0.0} ms)"
            : $"{fps:0} FPS";

        _nextRefreshTime = Time.unscaledTime + refreshInterval;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        refreshInterval = Mathf.Max(0.05f, refreshInterval);
    }
#endif
}
