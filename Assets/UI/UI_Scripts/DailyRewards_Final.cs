
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

public class DailyRewards_Final : MonoBehaviour
{
    public enum TestDay
    {
        Day1 = 0,
        Day2 = 1,
        Day3 = 2,
        Day4 = 3,
        Day5 = 4,
        Day6 = 5,
        Day7 = 6
    }

    [Header("Daily Reward Testing")]
    public bool enableDayTesting = true;
    public TestDay testDay = TestDay.Day1;

    private int dayValue;

    public GameObject rewards;

    private DateTime NextRewardTime;
    private DateTime FirstRewardTime;

    private const string LastClaimDateKey = "DailyRewardLastClaimDate";


    public List<Button> rewardButtons = new List<Button>();

    public List<int> dailyCoins = new List<int>
    {
        100, 150, 200, 250, 300, 350, 400
    };

    public List<Sprite> dailySprites = new List<Sprite>();
    [Header("Daily Reward Locked Objects")]
    public GameObject[] lockedObjects;

    [Header("Reward Popup")]
    public GameObject rewardPopupPanel;
    public TMP_Text rewardPopupText;

    private void Start()
    {
        if (enableDayTesting)
        {
            // Use the day selected in the Inspector.
            dayValue = (int)testDay;
        }
        else
        {
            // Use the actual saved daily reward progress.
            dayValue = PlayerPrefs.GetInt("DayValue", 0);
        }

        
        SetDailyRewardButtonInteraction();

        if (rewardPopupPanel != null)
            rewardPopupPanel.SetActive(false);

        if (rewards != null)
            rewards.SetActive(false);

        UpdateLockedObjects();
    }

    public void Close()
    {
        if (MainUI.Instance != null)
            MainUI.Instance.mainMenu.SetActive(true);

        if (rewards != null)
            rewards.SetActive(false);
    }

    public void CheckDailyRewards()
    {
        // TEST MODE: Always allow the selected day to be tested.
        if (enableDayTesting)
        {
            if (rewards != null)
                rewards.SetActive(true);

            ButtonHighlighter highlighter =
                rewards != null
                    ? rewards.GetComponent<ButtonHighlighter>()
                    : null;

            if (highlighter != null &&
                rewardButtons != null &&
                dayValue >= 0 &&
                dayValue < rewardButtons.Count &&
                rewardButtons[dayValue] != null)
            {
                highlighter.SetDefaultButton(
                    rewardButtons[dayValue].gameObject
                );
            }

            Debug.Log("TEST MODE: Testing Day " + (dayValue + 1));
            return;
        }

        // NORMAL DAILY REWARD MODE.
        if (dayValue < 0 || dayValue >= dailyCoins.Count)
        {
            Debug.LogError("Invalid daily reward day!");
            return;
        }

        if (DateLoader.Date == null && dayValue < 7)
        {
            if (PlayerPrefs.GetString("Day", "") == "")
            {
                OpenCurrentReward();
            }
            else
            {
                string savedDate = PlayerPrefs.GetString("Day");

                if (DateTime.TryParse(savedDate, out NextRewardTime))
                {
                    if (DateTime.Now >= NextRewardTime)
                    {
                        OpenCurrentReward();
                    }
                    else
                    {
                        OpenMainMenu();
                        Debug.Log("Reward already claimed today.");
                    }
                }
                else
                {
                    OpenCurrentReward();
                }
            }

            DateLoader.Date = PlayerPrefs.GetString("Day");
        }
        else
        {
            OpenMainMenu();
        }
    }


    public void ShowRewardIfAvailable()
    {
        if (enableDayTesting)
        {
            if (rewards != null)
                rewards.SetActive(true);

            if (MainUI.Instance != null && MainUI.Instance.mainMenu != null)
                MainUI.Instance.mainMenu.SetActive(false);

            ButtonHighlighter highlighter =
                rewards != null ? rewards.GetComponent<ButtonHighlighter>() : null;

            if (highlighter != null &&
                rewardButtons != null &&
                dayValue >= 0 &&
                dayValue < rewardButtons.Count &&
                rewardButtons[dayValue] != null)
            {
                highlighter.SetDefaultButton(rewardButtons[dayValue].gameObject);
            }

            Debug.Log("TEST MODE: Testing Day " + (dayValue + 1));
            return;
        }

        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string lastClaimDate = PlayerPrefs.GetString(LastClaimDateKey, "");

        if (lastClaimDate == today)
        {
            // Already claimed today's reward.
            if (rewards != null)
                rewards.SetActive(false);

            if (MainUI.Instance != null && MainUI.Instance.mainMenu != null)
                MainUI.Instance.mainMenu.SetActive(true);

            Debug.Log("Today's daily reward has already been claimed.");
        }
        else
        {
            // No reward claimed today, so show the available reward.
            if (rewards != null)
                rewards.SetActive(true);

            if (MainUI.Instance != null && MainUI.Instance.mainMenu != null)
                MainUI.Instance.mainMenu.SetActive(false);

            ButtonHighlighter highlighter =
                rewards != null ? rewards.GetComponent<ButtonHighlighter>() : null;

            if (highlighter != null &&
                rewardButtons != null &&
                dayValue >= 0 &&
                dayValue < rewardButtons.Count &&
                rewardButtons[dayValue] != null)
            {
                highlighter.SetDefaultButton(rewardButtons[dayValue].gameObject);
            }

            Debug.Log("Daily reward is available for Day " + (dayValue + 1));
        }
    }


    private void OpenCurrentReward()
    {
        if (rewards != null)
            rewards.SetActive(true);

        ButtonHighlighter highlighter =
            rewards != null
                ? rewards.GetComponent<ButtonHighlighter>()
                : null;

        if (highlighter != null &&
            rewardButtons != null &&
            dayValue >= 0 &&
            dayValue < rewardButtons.Count &&
            rewardButtons[dayValue] != null)
        {
            highlighter.SetDefaultButton(
                rewardButtons[dayValue].gameObject
            );
        }

        Debug.Log("You can claim today's reward.");
    }

    private void OpenMainMenu()
    {
        if (MainUI.Instance != null)
            MainUI.Instance.mainMenu.SetActive(true);
    }



    public void RewardClaimButton()
    {
        if (dayValue < 0 || dayValue >= dailyCoins.Count || dayValue >= 7)
        {
            Debug.LogError("Invalid daily reward day!");
            return;
        }

        if (!enableDayTesting)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string lastClaimDate = PlayerPrefs.GetString(LastClaimDateKey, "");

            if (lastClaimDate == today)
            {
                Debug.Log("Today's daily reward has already been claimed.");
                return;
            }

            PlayerPrefs.SetString(LastClaimDateKey, today);
        }

        int coinsForToday = dailyCoins[dayValue];
        int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);

        totalCoins += coinsForToday;

        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.Save();

        // Immediately refresh all coin text fields in MainUI.
        if (MainUI.Instance != null)
        {
            MainUI.Instance.UpdateTotalCoins();
        }

        ShowRewardPopup(coinsForToday);

        dayValue++;

        if (dayValue >= 7)
            dayValue = 0;

        PlayerPrefs.SetInt("DayValue", dayValue);
        PlayerPrefs.Save();

        Debug.Log("Daily reward claimed: " + coinsForToday +
                  " coins. Total coins: " + totalCoins);

    }


    private void UpdateLockedObjects()
    {
        if (lockedObjects == null || lockedObjects.Length == 0)
            return;

        // dayValue is the next reward day.
        // In normal mode, it is loaded from PlayerPrefs.
        // In testing mode, use the selected test day.
        int currentDay = enableDayTesting
            ? (int)testDay
            : PlayerPrefs.GetInt("DayValue", 0);

        currentDay = Mathf.Clamp(currentDay, 0, 6);

        for (int i = 0; i < lockedObjects.Length; i++)
        {
            if (lockedObjects[i] == null)
                continue;

            // Unlock every reward up to and including the current day.
            // Earlier rewards stay unlocked after being collected.
            bool shouldUnlock = i <= currentDay;

            // After Day 7 is claimed, DayValue resets to 0,
            // so the next cycle starts again from Day 1.
            lockedObjects[i].SetActive(!shouldUnlock);
        }
    }


    private void SetDailyRewardButtonInteraction()
    {
        if (rewardButtons == null || rewardButtons.Count == 0)
            return;

        foreach (Button button in rewardButtons)
        {
            if (button != null)
                button.interactable = false;
        }

        if (dayValue >= 0 &&
            dayValue < rewardButtons.Count &&
            rewardButtons[dayValue] != null)
        {
            rewardButtons[dayValue].interactable = true;
        }
    }

    private void ShowRewardPopup(int coins)
    {
        if (rewardPopupText != null)
        {
            rewardPopupText.text =
                $"DAILY REWARD {coins} Coins RECEIVED!";
        }

        if (rewardPopupPanel != null)
            rewardPopupPanel.SetActive(true);

        foreach (Button button in rewardButtons)
        {
            if (button != null)
                button.interactable = false;
        }
    }

    public void Okay()
    {
        if (rewardPopupPanel != null)
            rewardPopupPanel.SetActive(false);

        if (MainUI.Instance != null)
            MainUI.Instance.mainMenu.SetActive(true);

        if (rewards != null)
            rewards.SetActive(false);
        UpdateLockedObjects();
    }

    public int GetCoinsForToday()
    {
        if (dayValue < 0 || dayValue >= dailyCoins.Count)
            return 0;

        return dailyCoins[dayValue];
    }
}

public static class DateLoader
{
    public static string Date = null;
    public static int volume = 1;
}
