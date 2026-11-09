
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpinWheelManager : MonoBehaviour
{
    [System.Serializable]
    public class WheelReward
    {
        public string rewardName;
        public int rewardAmount = 100;
    }

    [Header("Wheel")]
    public RectTransform wheel;
    public Button spinButton;

    [Header("Rewards - Clockwise From Top")]
    public WheelReward[] rewards;

    [Header("Spin Again Reward")]
    public string spinAgainRewardName = "Spin Again";

    private int extraSpinsAvailable = 0;

    [Header("Wheel Alignment")]
    [Tooltip("Angle correction for aligning the reward center with the pointer. Start with 0 and adjust if needed.")]
    public float rewardCenterOffset = 0f;

    [Header("Spin Settings")]
    [Min(1)] public int fullRotations = 6;
    [Min(0.1f)] public float rotationSpeed = 720f;
    [Min(0.1f)] public float spinDuration = 6f;

    [Header("Reward UI")]
    public TMP_Text rewardText;
    public GameObject rewardPanel;

    [Header("Daily Spin Settings")]
    public bool enableTesting = false;
    public bool allowSpinAgainInTesting = false;
    public string lastSpinDateKey = "DailySpinLastClaimDate";

    [Header("Coin Settings")]
    public string coinPlayerPrefsKey = "TotalCoins";
    public int startingCoins = 0;

    [Header("Visit Again UI")]
    public GameObject visitAgain;
    public float visitAgainDuration = 3f;

    [Header("Testing")]
    public bool forceSpinAgainReward = false;

    private bool isSpinning;
    private bool hasSpunToday;
    private int totalCoins;

    private float initialWheelAngle;
    private Vector2 initialAnchoredPosition;
    private Vector3 initialLocalPosition;
    private Vector3 initialLocalScale;

    private void Start()
    {
        if (wheel == null)
        {
            Debug.LogError("SpinWheelManager: Assign the Wheel RectTransform.");
            enabled = false;
            return;
        }

        if (rewards == null || rewards.Length == 0)
        {
            Debug.LogError("SpinWheelManager: Add at least one reward.");
            enabled = false;
            return;
        }

        if (rewards.Length < 2)
        {
            Debug.LogError("SpinWheelManager: Add at least two reward sections.");
            enabled = false;
            return;
        }

        initialWheelAngle = wheel.localEulerAngles.z;
        initialAnchoredPosition = wheel.anchoredPosition;
        initialLocalPosition = wheel.localPosition;
        initialLocalScale = wheel.localScale;

        totalCoins = PlayerPrefs.GetInt(
            coinPlayerPrefsKey,
            startingCoins
        );

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        if (spinButton != null)
            spinButton.onClick.AddListener(SpinWheel);

        if (visitAgain != null)
            visitAgain.SetActive(false);

        CheckDailySpin();
        UpdateDailySpinUI();
    }

    private void CheckDailySpin()
    {
        if (enableTesting)
        {
            hasSpunToday = false;
            return;
        }

        string today = DateTime.Now.ToString("yyyy-MM-dd");

        string lastSpinDate = PlayerPrefs.GetString(
            lastSpinDateKey,
            ""
        );

        hasSpunToday = lastSpinDate == today;
    }

    public void SpinWheel()
    {
        if (isSpinning)
            return;

        if (rewards == null || rewards.Length < 2)
            return;

        CheckDailySpin();

        if (hasSpunToday && extraSpinsAvailable <= 0 && !(enableTesting && allowSpinAgainInTesting))
        {
            UpdateDailySpinUI();

            if (visitAgain != null)
                StartCoroutine(ShowVisitAgain());

            Debug.Log("Today's free spin has already been used.");
            return;
        }

        if (spinButton != null)
            spinButton.interactable = false;

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        if (extraSpinsAvailable > 0)
        {
            extraSpinsAvailable--;
        }
        else if (!enableTesting)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            PlayerPrefs.SetString(lastSpinDateKey, today);
            PlayerPrefs.Save();

            hasSpunToday = true;

            if (MainUI.Instance != null)
                MainUI.Instance.UpdateSpinIndicator();
        }

        StartCoroutine(SpinRoutine());
    }

    private IEnumerator ShowVisitAgain()
    {
        visitAgain.SetActive(true);

        yield return new WaitForSeconds(visitAgainDuration);

        visitAgain.SetActive(false);
    }
    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        UpdateDailySpinUI();

        //int rewardIndex = UnityEngine.Random.Range(0, rewards.Length);


        // this is for testing spin again reward to check the spin indicator activating or not in main ui
        int rewardIndex;

        if (forceSpinAgainReward)
        {
            rewardIndex = Array.FindIndex(
                rewards,
                reward => string.Equals(
                    reward.rewardName.Trim(),
                    spinAgainRewardName.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            );

            if (rewardIndex < 0)
            {
                Debug.LogError("SpinWheelManager: Spin Again reward not found in rewards array.");
                isSpinning = false;
                UpdateDailySpinUI();
                yield break;
            }
        }
        else
        {
            rewardIndex = UnityEngine.Random.Range(0, rewards.Length);
        }

        float sectionAngle = 360f / rewards.Length;

        float desiredAngle = initialWheelAngle
                             + rewardIndex * sectionAngle
                             + rewardCenterOffset;

        float startAngle = wheel.localEulerAngles.z;

        float extraAngle = Mathf.Repeat(
            desiredAngle - startAngle,
            360f
        );

        // Calculate the distance needed for the complete spin.
        float minimumAngle = fullRotations * 360f + extraAngle;

        // Use Inspector rotation speed to calculate the rotation distance.
        // The wheel always completes at least the requested full rotations.
        float speedBasedAngle = rotationSpeed * spinDuration * 0.5f;

        float totalRotation = Mathf.Max(minimumAngle, speedBasedAngle);

        // Keep the selected reward aligned with the pointer.
        totalRotation = extraAngle +
                        Mathf.Max(
                            fullRotations * 360f,
                            Mathf.Ceil((totalRotation - extraAngle) / 360f) * 360f
                        );

        float targetAngle = startAngle + totalRotation;

        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / spinDuration);

            // Start at the configured speed and gradually slow down.
            float easedProgress = 1f - Mathf.Pow(1f - progress, 4f);

            float angle = Mathf.Lerp(
                startAngle,
                targetAngle,
                easedProgress
            );

            wheel.anchoredPosition = initialAnchoredPosition;
            wheel.localPosition = initialLocalPosition;
            wheel.localScale = initialLocalScale;

            wheel.localRotation = Quaternion.Euler(
                0f,
                0f,
                angle
            );

            yield return null;
        }

        wheel.anchoredPosition = initialAnchoredPosition;
        wheel.localPosition = initialLocalPosition;
        wheel.localScale = initialLocalScale;

        wheel.localRotation = Quaternion.Euler(
            0f,
            0f,
            targetAngle
        );

        WheelReward selectedReward = rewards[rewardIndex];

        bool isSpinAgain = string.Equals(
            selectedReward.rewardName.Trim(),
            spinAgainRewardName.Trim(),
            StringComparison.OrdinalIgnoreCase
        );

        if (isSpinAgain)
        {
            extraSpinsAvailable++;

            if (MainUI.Instance != null)
                MainUI.Instance.UpdateSpinIndicator();
        }

        if (selectedReward.rewardAmount > 0)
        {
            totalCoins = PlayerPrefs.GetInt(
                coinPlayerPrefsKey,
                startingCoins
            );

            totalCoins += selectedReward.rewardAmount;

            PlayerPrefs.SetInt(coinPlayerPrefsKey, totalCoins);
            PlayerPrefs.Save();

            if (MainUI.Instance != null)
                MainUI.Instance.UpdateTotalCoins();
        }

        if (rewardText != null)
        {
            rewardText.text = selectedReward.rewardAmount > 0
                ? selectedReward.rewardName + "+" + selectedReward.rewardAmount
                : selectedReward.rewardName;
        }

        if (rewardPanel != null)
            rewardPanel.SetActive(true);

        isSpinning = false;
        UpdateDailySpinUI();

        if (isSpinAgain && spinButton != null)
            spinButton.interactable = true;

        Debug.Log(
            "Spin result: " + selectedReward.rewardName +
            ", Coins: " + selectedReward.rewardAmount
        );
    }


    private void UpdateDailySpinUI()
    {
        if (spinButton != null)
            spinButton.interactable = !isSpinning;
    }

    public void RefreshDailySpin()
    {
        CheckDailySpin();
        UpdateDailySpinUI();
    }

    public void CloseRewardPanel()
    {
        if (rewardPanel != null)
            rewardPanel.SetActive(false);
    }

    public bool HasExtraSpinsAvailable
    {
        get { return extraSpinsAvailable > 0; }
    }

    private void OnDestroy()
    {
        if (spinButton != null)
            spinButton.onClick.RemoveListener(SpinWheel);
    }
}
