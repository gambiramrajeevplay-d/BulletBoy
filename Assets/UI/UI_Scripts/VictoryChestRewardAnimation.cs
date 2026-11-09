
using System.Collections;
using UnityEngine;

public class VictoryChestRewardAnimation : MonoBehaviour
{
    [Header("Reward Animation")]
    public GameObject rewardObject;
    public Transform targetPoint;
    public float startScale = 0.25f;
    public float endScale = 1f;
    public float animationDuration = 0.7f;

    [Header("Reward Objects")]
    public GameObject lockedObject;
    public GameObject rewardIndicator;

    [Header("Panel Settings")]
    public GameObject rewardPanel;
    public float panelCloseDelay = 0.5f;

    private bool animationPlaying;
    private bool rewardClaimed;

    public void PlayRewardAnimation()
    {
        rewardObject.SetActive(true);

        if (animationPlaying || rewardClaimed)
            return;

        if (PlayerPrefs.GetInt("InteractableRewardClaimed", 0) == 1)
        {
            rewardClaimed = true;
            return;
        }

        if (rewardObject == null || targetPoint == null)
        {
            Debug.LogWarning(
                "Assign Reward Object and Target Point in the Inspector."
            );
            return;
        }

        StartCoroutine(AnimateReward());
    }

    private IEnumerator AnimateReward()
    {
        animationPlaying = true;

        rewardObject.SetActive(true);

        Transform rewardTransform = rewardObject.transform;

        rewardTransform.localScale = Vector3.one * startScale;

        Vector3 startPosition = rewardTransform.position;
        Vector3 endPosition = targetPoint.position;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / animationDuration
            );

            float smoothProgress = Mathf.SmoothStep(
                0f, 1f, progress
            );

            rewardTransform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                smoothProgress
            );

            rewardTransform.localScale = Vector3.one * Mathf.Lerp(
                startScale,
                endScale,
                smoothProgress
            );

            yield return null;
        }

        rewardTransform.position = endPosition;
        rewardTransform.localScale = Vector3.one * endScale;

        // First, update the lock and indicator.
        if (lockedObject != null)
            lockedObject.SetActive(false);
        else
            Debug.LogWarning("Locked Object is not assigned.");

        if (rewardIndicator != null)
            rewardIndicator.SetActive(true);
        else
            Debug.LogWarning("Reward Indicator is not assigned.");

        // Save the claimed state.
        rewardClaimed = true;

        PlayerPrefs.SetInt("InteractableRewardClaimed", 1);
        PlayerPrefs.SetInt("ShowInteractableRewardOnMenuReturn", 0);
        PlayerPrefs.Save();

        // Keep the panel visible briefly so the changes can be seen.
        if (panelCloseDelay > 0f)
            yield return new WaitForSecondsRealtime(panelCloseDelay);

        animationPlaying = false;

        // Continue to the title and daily reward flow.
        if (MainUI.Instance != null)
        {
            MainUI.Instance.OnInteractableRewardAnimationComplete();
        }
        else
        {
            Debug.LogWarning("MainUI instance was not found.");
        }
    }
}
