using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-10000)]
public class UIFrameRateBootstrap : MonoBehaviour
{
    [SerializeField] private int targetFps = 60;
    [SerializeField] private bool logOnApply = true;

    private void Awake()
    {
        ApplyFrameRateSettings();
    }

    [ContextMenu("Apply Frame Rate Settings")]
    public void ApplyFrameRateSettings()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Mathf.Max(1, targetFps);

        if (!logOnApply)
        {
            return;
        }

#if UNITY_2022_2_OR_NEWER
        float refresh = (float)Screen.currentResolution.refreshRateRatio.value;
#else
        int refresh = Screen.currentResolution.refreshRate;
#endif
        Debug.Log(
            $"[UIFrameRateBootstrap] Applied vSync={QualitySettings.vSyncCount}, " +
            $"targetFPS={Application.targetFrameRate}, displayRefresh={refresh}",
            this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        targetFps = Mathf.Max(1, targetFps);
    }
#endif
}
