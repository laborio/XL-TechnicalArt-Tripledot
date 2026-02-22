using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIFullscreenVideoButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button triggerButton;
    [SerializeField] private GameObject fullscreenVideoRoot;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Behavior")]
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private bool playFromStart = true;
    [SerializeField] private bool forceNoLoop = true;

    private void Awake()
    {
        if (triggerButton == null)
        {
            triggerButton = GetComponent<Button>();
        }

        if (hideOnAwake && fullscreenVideoRoot != null)
        {
            fullscreenVideoRoot.SetActive(false);
        }

        if (videoPlayer != null)
        {
            if (forceNoLoop)
            {
                videoPlayer.isLooping = false;
            }

            videoPlayer.loopPointReached += HandleVideoFinished;
        }
    }

    private void OnEnable()
    {
        if (triggerButton != null)
        {
            triggerButton.onClick.AddListener(PlayVideo);
        }
    }

    private void OnDisable()
    {
        if (triggerButton != null)
        {
            triggerButton.onClick.RemoveListener(PlayVideo);
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= HandleVideoFinished;
        }
    }

    public void PlayVideo()
    {
        if (fullscreenVideoRoot != null)
        {
            fullscreenVideoRoot.SetActive(true);
        }

        if (videoPlayer == null)
        {
            return;
        }

        if (playFromStart)
        {
            videoPlayer.Stop();
            videoPlayer.time = 0d;
        }

        videoPlayer.Play();
    }

    public void CloseVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        if (fullscreenVideoRoot != null)
        {
            fullscreenVideoRoot.SetActive(false);
        }
    }

    private void HandleVideoFinished(VideoPlayer source)
    {
        CloseVideo();
    }
}
