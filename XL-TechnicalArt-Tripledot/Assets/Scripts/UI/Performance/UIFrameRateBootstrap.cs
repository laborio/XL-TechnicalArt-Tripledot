using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-10000)]
public class UIFrameRateBootstrap : MonoBehaviour
{
    private const int DefaultTargetFps = 60;

    [SerializeField] private int targetFps = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyFrameRateSettingsBeforeSceneLoad()
    {
        QualitySettings.vSyncCount = 0;

        // Ensure we never fall back to platform defaults (commonly 30 FPS on mobile)
        // before scene objects get a chance to initialize.
        if (Application.targetFrameRate <= 0)
        {
            Application.targetFrameRate = DefaultTargetFps;
        }
    }

    private void Awake()
    {
        ApplyFrameRateSettings();
    }

    [ContextMenu("Apply Frame Rate Settings")]
    public void ApplyFrameRateSettings()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Mathf.Max(1, targetFps);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        targetFps = Mathf.Max(1, targetFps);
    }
#endif
}
