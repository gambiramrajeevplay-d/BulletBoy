using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System.Collections;
using System.Text.RegularExpressions;
using System;

public class WebVideoAd : MonoBehaviour, IPointerClickHandler
{

    [Header("JSON API")]
    public string JsonURL =
        "https://playdgamestudio.com/wp-json/wp/v2/posts/3681";

    [Header("Video Components")]
    public VideoPlayer videoPlayer;
    public RawImage rawImage;

    [Header("UI Buttons")]
    public Button closeButton;
    public Button downloadButton;

    [Header("Countdown")]
    public TextMeshProUGUI countdownText;
    public float countdownDuration = 5f;

    private string videoURL;
    private string linkURL;

    private RenderTexture renderTexture;
    private Coroutine countdownCoroutine;
    private bool adClosed;

    private System.Action closeCallback;
    private bool initialized;

    [Serializable]
    private class WordPressPost
    {
        public ContentData content;
    }

    [Serializable]
    private class ContentData
    {
        public string rendered;
    }

    private void OnEnable()
    {
        if (initialized)
            BeginAd();
    }

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
            closeButton.onClick.AddListener(CloseAd);
        }

        if (downloadButton != null)
            downloadButton.onClick.AddListener(OpenGameLink);

        initialized = true;
        BeginAd();
    }

    public void SetCloseCallback(System.Action callback)
    {
        closeCallback = callback;
    }

    private void BeginAd()
    {
        adClosed = false;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        if (videoPlayer != null)
            videoPlayer.Stop();

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);

        if (countdownText != null)
            countdownText.text = "";

        StartCoroutine(LoadAdFromJson());
    }

    private IEnumerator LoadAdFromJson()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(JsonURL))
        {
            request.timeout = 20;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("JSON Error: " + request.error);
                yield break;
            }

            string json = request.downloadHandler.text;

            WordPressPost post = null;

            try
            {
                post = JsonUtility.FromJson<WordPressPost>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("JSON Parse Error: " + e.Message);
            }

            if (post == null || post.content == null)
            {
                Debug.LogError("WordPress content.rendered not found!");
                yield break;
            }

            string content = post.content.rendered;

            // Read URLs directly from JSON content.
            videoURL = ExtractValue(content, "VideoURL");
            linkURL = ExtractValue(content, "LinkURL");

            videoURL = CleanValue(videoURL);
            linkURL = CleanValue(linkURL);

            if (string.IsNullOrEmpty(videoURL))
            {
                Debug.LogError(
                    "VideoURL not found. Content: " + content);
                yield break;
            }

            Debug.Log("VideoURL: " + videoURL);
            Debug.Log("LinkURL: " + linkURL);

            SetupVideo();
            //countdownCoroutine = StartCoroutine(CountdownRoutine());
        }
    }

    private string ExtractValue(string content, string key)
    {
        if (string.IsNullOrEmpty(content))
            return null;

        content = content.Replace("&quot;", "\"")
                         .Replace("&#34;", "\"")
                         .Replace("&#x22;", "\"")
                         .Replace("&amp;", "&")
                         .Replace("\\\"", "\"")
                         .Replace("\\/", "/");

        Match match = Regex.Match(
            content,
            "\"" + Regex.Escape(key) +
            "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"",
            RegexOptions.IgnoreCase
        );

        if (!match.Success)
            return null;

        return match.Groups[1].Value
            .Replace("\\\"", "\"")
            .Replace("\\/", "/");
    }

    private string CleanValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return value.Trim().TrimEnd(';').Trim();
    }

    private void SetupVideo()
    {
        if (adClosed)
            return;

        renderTexture = new RenderTexture(1280, 720, 0);
        renderTexture.Create();

        rawImage.texture = renderTexture;
        rawImage.color = Color.white;
        rawImage.raycastTarget = true;

        videoPlayer.playOnAwake = false;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoURL;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.isLooping = true;

        // Remove previous event subscriptions to prevent duplicates.
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.errorReceived -= OnVideoError;

        // Subscribe to the events.
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;

        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer player)
    {
        if (!adClosed)
            player.Play();
    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        Debug.LogError("Video Error: " + message);
    }

    private IEnumerator CountdownRoutine()
    {
        float remaining = countdownDuration;

        while (remaining > 0 && !adClosed)
        {
            if (countdownText != null)
                countdownText.text =
                    Mathf.CeilToInt(remaining).ToString();

            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        if (countdownText != null)
            countdownText.text = "";

        if (!adClosed && closeButton != null)
            closeButton.gameObject.SetActive(true);

        countdownCoroutine = null;
    }

    public void StartCountdown()
    {
        if (adClosed)
            return;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    public void OpenGameLink()
    {
        if (string.IsNullOrEmpty(linkURL))
        {
            Debug.LogError("LinkURL not found in JSON!");
            return;
        }

        Application.OpenURL(linkURL);
    }

    public void CloseAd()
    {
        if (adClosed)
            return;

        adClosed = true;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        if (videoPlayer != null)
            videoPlayer.Stop();

        if (countdownText != null)
            countdownText.text = "";

        System.Action callback = closeCallback;
        closeCallback = null;

        gameObject.SetActive(false);

        callback?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenGameLink();
    }

    private void OnDestroy()
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseAd);

        if (downloadButton != null)
            downloadButton.onClick.RemoveListener(OpenGameLink);

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.Stop();
            videoPlayer.targetTexture = null;
        }

        if (rawImage != null)
            rawImage.texture = null;

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
    }
}
