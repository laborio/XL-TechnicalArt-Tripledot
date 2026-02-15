using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class BottomBarBackgroundSafeAreaCompensator : MonoBehaviour
{
    [Tooltip("Extra pixels added on top of the safe-area inset, if you want more breathing room.")]
    [SerializeField] private float extraPadding = 0f;

    private RectTransform _rt;
    private float _baseHeight = -1f;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();

        // Store initial (design-time) height as the baseline.
        _baseHeight = _rt.sizeDelta.y;

        Apply();
    }

    private void OnEnable() => Apply();

    private void Update()
    {
        // Safe, cheap check. You can optimize later if needed.
        Apply();
    }

    private void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;

        // Bottom inset in pixels (distance from screen bottom to safe area bottom)
        float bottomInset = Screen.safeArea.yMin;

        // Ensure bar background extends upward by inset so it visually meets safe content
        float targetHeight = _baseHeight + bottomInset + extraPadding;

        // Keep anchored to bottom (pivot should be bottom)
        _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

        // If your pivot is (0.5, 0), you do NOT need to move Y.
        // Height increase goes upward automatically.
        // If pivot isn't bottom, fix pivot to (0.5, 0).
    }
}
