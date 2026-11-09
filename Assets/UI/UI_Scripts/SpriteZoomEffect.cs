
using UnityEngine;
using System.Collections;

public class SpriteZoomEffect : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float targetScale = 3f;
    public float zoomDuration = 2f;
    public bool zoomOnEnable = true;

    private Vector3 originalScale;
    private Coroutine zoomCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (zoomOnEnable)
        {
            StartZoom();
        }
    }

    public void StartZoom()
    {
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
            zoomCoroutine = null;
        }

        transform.localScale = originalScale;
        zoomCoroutine = StartCoroutine(ZoomIn());
    }

    private IEnumerator ZoomIn()
    {
        float elapsed = 0f;
        Vector3 startScale = originalScale;
        Vector3 endScale = originalScale * targetScale;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / zoomDuration);

            transform.localScale = Vector3.Lerp(
                startScale,
                endScale,
                progress
            );

            yield return null;
        }

        transform.localScale = endScale;
        zoomCoroutine = null;
    }

    public void ResetZoom()
    {
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
            zoomCoroutine = null;
        }

        transform.localScale = originalScale;
    }
}
