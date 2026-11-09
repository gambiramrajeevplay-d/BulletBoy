using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class BonusRewardManager : MonoBehaviour
{
    public static BonusRewardManager Instance;

    [System.Serializable]
    public class BonusReward
    {
        [Header("Reward Store")]
        public Button rewardButton;

        [Header("Victory Chest")]
        public GameObject victoryChestCard;
        public Button tapToClaimButton;

        [Header("Got Reward")]
        public GameObject gotRewardCard;
        public Button doneButton;

        [Header("Mission Requirement")]
        [Min(1)] public int requiredLevel = 1;

        [Header("Reward Coins")]
        public int rewardCoins = 200;

        [Header("Reward Store States")]
        public GameObject locked;
        public GameObject open;
        public GameObject claimed;
        public GameObject completeLevel;

        [System.NonSerialized]
        public Coroutine messageCoroutine;

        [System.NonSerialized]
        public UnityAction storeListener;

        [System.NonSerialized]
        public UnityAction claimListener;

        [System.NonSerialized]
        public UnityAction doneListener;
    }

    [Header("Main Panels")]
    public GameObject parentRewardStore;
    public GameObject rewardStore;
    public GameObject victoryChest;
    public GameObject gotReward;

    [Header("Rewards - Configure 4 Elements")]
    public BonusReward[] bonusRewards = new BonusReward[4];

    [Header("Settings")]
    public float completeLevelDuration = 3f;

    private const string LevelCompletedKey = "LevelCompleted_";
    private const string RewardClaimedKey = "BonusRewardClaimed_";

    private int selectedRewardIndex = -1;
    private bool rewardClaimInProgress;
    private bool openedFromHomeForBonusReward = false;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        if (bonusRewards == null)
            return;

        for (int i = 0; i < bonusRewards.Length; i++)
        {
            int index = i;
            BonusReward reward = bonusRewards[i];

            if (reward == null)
                continue;

            // Reward Store card button
            if (reward.rewardButton != null)
            {
                reward.storeListener =
                    () => OnRewardButtonPressed(index);

                reward.rewardButton.onClick.AddListener(
                    reward.storeListener);
            }

            // Victory Chest: Tap to Claim button
            if (reward.tapToClaimButton != null)
            {
                reward.claimListener =
                    () => TapClaimButton(index);

                reward.tapToClaimButton.onClick.AddListener(
                    reward.claimListener);
            }

            // Got Reward: Done button
            if (reward.doneButton != null)
            {
                reward.doneListener =
                    () => DoneButton(index);

                reward.doneButton.onClick.AddListener(
                    reward.doneListener);
            }
        }
    }

    private void Start()
    {
        // Start with the reward sub-cards hidden.
        HideAllStageCards();

        if (victoryChest != null)
            victoryChest.SetActive(false);

        if (gotReward != null)
            gotReward.SetActive(false);

        RefreshAllRewards();
    }

    private void OnEnable()
    {
        RefreshAllRewards();
    }

    private void OnDestroy()
    {
        if (bonusRewards == null)
            return;

        foreach (BonusReward reward in bonusRewards)
        {
            if (reward == null)
                continue;

            if (reward.messageCoroutine != null)
                StopCoroutine(reward.messageCoroutine);

            if (reward.rewardButton != null &&
                reward.storeListener != null)
            {
                reward.rewardButton.onClick.RemoveListener(
                    reward.storeListener);
            }

            if (reward.tapToClaimButton != null &&
                reward.claimListener != null)
            {
                reward.tapToClaimButton.onClick.RemoveListener(
                    reward.claimListener);
            }

            if (reward.doneButton != null &&
                reward.doneListener != null)
            {
                reward.doneButton.onClick.RemoveListener(
                    reward.doneListener);
            }
        }
    }

    // ---------------- REWARD STORE ----------------

    public void RefreshAllRewards()
    {
        if (bonusRewards != null)
        {
            for (int i = 0; i < bonusRewards.Length; i++)
            {
                RefreshReward(i);
            }
        }

        if (MainUI.Instance != null)
        {
            MainUI.Instance.UpdateRewardIndicator(
                HasAnyAvailableReward());
        }
    }

    private void RefreshReward(int index)
    {
        if (!IsValidReward(index))
            return;

        BonusReward reward = bonusRewards[index];

        if (IsRewardClaimed(index))
        {
            SetRewardState(reward, false, false, true, false);
        }
        else if (IsRequiredLevelCompleted(reward))
        {
            SetRewardState(reward, false, true, false, false);
        }
        else
        {
            SetRewardState(reward, true, false, false, false);
        }
    }

    private bool IsRequiredLevelCompleted(BonusReward reward)
    {
        if (reward.requiredLevel < 1)
            return false;

        int levelIndex = reward.requiredLevel - 1;

        return PlayerPrefs.GetInt(
            LevelCompletedKey + levelIndex, 0) == 1;
    }

    private void OnRewardButtonPressed(int index)
    {
        if (!IsValidReward(index) || rewardClaimInProgress)
            return;

        BonusReward reward = bonusRewards[index];

        // Already claimed
        if (IsRewardClaimed(index))
        {
            RefreshReward(index);
            return;
        }

        // Required level is not completed
        if (!IsRequiredLevelCompleted(reward))
        {
            ShowCompleteLevelMessage(index);
            return;
        }

        selectedRewardIndex = index;
        rewardClaimInProgress = true;

        // Close Reward Store and Got Reward.
        if (rewardStore != null)
            rewardStore.SetActive(false);

        if (gotReward != null)
            gotReward.SetActive(false);

        // Open Victory Chest with only the selected reward.
        HideAllStageCards();

        if (victoryChest != null)
            victoryChest.SetActive(true);

        ShowVictoryChestCard(index);
    }

    // ---------------- VICTORY CHEST ----------------

    private void ShowVictoryChestCard(int selectedIndex)
    {
        for (int i = 0; i < bonusRewards.Length; i++)
        {
            if (!IsValidReward(i))
                continue;

            BonusReward reward = bonusRewards[i];
            bool selected = i == selectedIndex;

            if (reward.victoryChestCard != null)
                reward.victoryChestCard.SetActive(selected);

            if (reward.tapToClaimButton != null)
                reward.tapToClaimButton.gameObject.SetActive(selected);
        }
        if (IsValidReward(selectedIndex))
        {
            SelectButton(bonusRewards[selectedIndex].tapToClaimButton);
        }
    }

    public void TapClaimButton(int index)
    {
        if (!rewardClaimInProgress ||
            index != selectedRewardIndex ||
            !IsValidReward(index))
            return;

        // Close Victory Chest.
        if (victoryChest != null)
            victoryChest.SetActive(false);

        HideAllStageCards();

        // Open Got Reward with the same selected reward.
        if (gotReward != null)
            gotReward.SetActive(true);

        ShowGotRewardCard(index);
    }


    private void ShowGotRewardCard(int selectedIndex)
    {
        for (int i = 0; i < bonusRewards.Length; i++)
        {
            if (!IsValidReward(i))
                continue;

            BonusReward reward = bonusRewards[i];
            bool selected = i == selectedIndex;

            if (reward.gotRewardCard != null)
                reward.gotRewardCard.SetActive(selected);

            if (reward.doneButton != null)
            {
                Transform doneButtonTransform = reward.doneButton.transform;

                // Activate the done_bg parent if it is the direct parent.
                if (doneButtonTransform.parent != null)
                    doneButtonTransform.parent.gameObject.SetActive(selected);

                reward.doneButton.gameObject.SetActive(selected);
                reward.doneButton.interactable = selected;
            }
        }

        if (IsValidReward(selectedIndex))
            SelectButton(bonusRewards[selectedIndex].doneButton);
    }


    // ---------------- DONE / GIVE COINS ----------------

    public void DoneButton(int index)
    {
        if (!rewardClaimInProgress ||
            index != selectedRewardIndex ||
            !IsValidReward(index))
            return;

        BonusReward reward = bonusRewards[index];

        // Never award coins twice for the same reward.
        if (IsRewardClaimed(index))
        {
            FinishRewardFlow();
            return;
        }

        if (!IsRequiredLevelCompleted(reward))
        {
            FinishRewardFlow();
            return;
        }

        // Save claimed status before awarding coins.
        PlayerPrefs.SetInt(GetRewardClaimKey(reward), 1);

        // Keep the old key for compatibility with existing saved data.
        PlayerPrefs.SetInt(RewardClaimedKey + index, 1);

        PlayerPrefs.Save();

        if (reward.rewardCoins > 0)
        {
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoins(reward.rewardCoins);
            }
            else
            {
                int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
                totalCoins += reward.rewardCoins;

                PlayerPrefs.SetInt("TotalCoins", totalCoins);
                PlayerPrefs.Save();
            }
        }

        if (reward.messageCoroutine != null)
        {
            StopCoroutine(reward.messageCoroutine);
            reward.messageCoroutine = null;
        }

        SetRewardState(reward, false, false, true, false);

        Debug.Log(
            "Bonus reward for required level " +
            reward.requiredLevel +
            " claimed permanently.");

        FinishRewardFlow();
    }

    private void FinishRewardFlow()
    {
        if (victoryChest != null)
            victoryChest.SetActive(false);

        if (gotReward != null)
            gotReward.SetActive(false);

        HideAllStageCards();

        if (rewardStore != null)
            rewardStore.SetActive(true);

        selectedRewardIndex = -1;
        rewardClaimInProgress = false;

        RefreshAllRewards();

        if (MainUI.Instance != null)
            MainUI.Instance.UpdateTotalCoins();
    }

    // ---------------- HIDE ALL SUB-CARDS ----------------

    private void HideAllStageCards()
    {
        if (bonusRewards == null)
            return;

        foreach (BonusReward reward in bonusRewards)
        {
            if (reward == null)
                continue;

            if (reward.victoryChestCard != null)
                reward.victoryChestCard.SetActive(false);

            if (reward.tapToClaimButton != null)
                reward.tapToClaimButton.gameObject.SetActive(false);

            if (reward.gotRewardCard != null)
                reward.gotRewardCard.SetActive(false);

            if (reward.doneButton != null)
                reward.doneButton.gameObject.SetActive(false);
        }
    }

    // ---------------- COMPLETE LEVEL MESSAGE ----------------


    private void ShowCompleteLevelMessage(int index)
    {
        if (!IsValidReward(index))
            return;

        // Stop all previous Complete Level messages.
        for (int i = 0; i < bonusRewards.Length; i++)
        {
            if (!IsValidReward(i))
                continue;

            BonusReward previousReward = bonusRewards[i];

            if (previousReward.messageCoroutine != null)
            {
                StopCoroutine(previousReward.messageCoroutine);
                previousReward.messageCoroutine = null;
            }

            if (previousReward.completeLevel != null)
                previousReward.completeLevel.SetActive(false);
        }

        // Activate only the currently selected reward's message.
        BonusReward reward = bonusRewards[index];

        SetRewardState(reward, false, false, false, true);

        reward.messageCoroutine =
            StartCoroutine(HideCompleteLevelMessage(index));
    }


    private IEnumerator HideCompleteLevelMessage(int index)
    {
        yield return new WaitForSecondsRealtime(
            completeLevelDuration);

        if (IsValidReward(index))
        {
            bonusRewards[index].messageCoroutine = null;
            RefreshReward(index);
        }
    }

    private void SetRewardState(
        BonusReward reward,
        bool showLocked,
        bool showOpen,
        bool showClaimed,
        bool showCompleteLevel)
    {
        if (reward.locked != null)
            reward.locked.SetActive(showLocked);

        if (reward.open != null)
            reward.open.SetActive(showOpen);

        if (reward.claimed != null)
            reward.claimed.SetActive(showClaimed);

        if (reward.completeLevel != null)
            reward.completeLevel.SetActive(showCompleteLevel);
    }

    private bool IsValidReward(int index)
    {
        return bonusRewards != null &&
               index >= 0 &&
               index < bonusRewards.Length &&
               bonusRewards[index] != null;
    }

    // ---------------- CLOSE REWARD STORE ----------------


    public void RewardClose()
    {
        if (parentRewardStore != null)
            parentRewardStore.SetActive(false);

        if (rewardStore != null)
            rewardStore.SetActive(false);

        if (victoryChest != null)
            victoryChest.SetActive(false);

        if (gotReward != null)
            gotReward.SetActive(false);

        HideAllStageCards();

        // Refresh the indicator even when the reward was not collected.
        RefreshAllRewards();

        if (openedFromHomeForBonusReward)
        {
            openedFromHomeForBonusReward = false;

            if (MainUI.Instance != null &&
                MainUI.Instance.mainMenu != null)
            {
                MainUI.Instance.mainMenu.SetActive(true);
            }

            return;
        }

        if (MainUI.Instance != null &&
            MainUI.Instance.mainMenu != null)
        {
            MainUI.Instance.mainMenu.SetActive(true);
        }
    }


    private void SelectButton(Button button)
    {
        if (button == null || !button.gameObject.activeInHierarchy)
            return;

        if (!button.interactable)
            return;

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            Debug.LogWarning("EventSystem is missing from the scene.");
            return;
        }

        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(button.gameObject);
    }

    //public bool HasAvailableReward()
    //{
    //    if (bonusRewards == null)
    //        return false;

    //    for (int i = 0; i < bonusRewards.Length; i++)
    //    {
    //        if (!IsValidReward(i))
    //            continue;

    //        BonusReward reward = bonusRewards[i];

    //        if (!IsRewardClaimed(i) &&
    //            IsRequiredLevelCompleted(reward))
    //        {
    //            return true;
    //        }
    //    }

    //    return false;
    //}


    public bool HasAvailableRewardForLevel(int completedLevelIndex)
    {
        if (completedLevelIndex < 0 || bonusRewards == null)
            return false;

        int completedLevelNumber = completedLevelIndex + 1;

        for (int i = 0; i < bonusRewards.Length; i++)
        {
            if (!IsValidReward(i))
                continue;

            BonusReward reward = bonusRewards[i];

            if (reward.requiredLevel != completedLevelNumber)
                continue;

            return !IsRewardClaimed(i) &&
                   IsRequiredLevelCompleted(reward);
        }

        return false;
    }

    public bool HasAnyAvailableReward()
    {
        if (bonusRewards == null)
            return false;

        for (int i = 0; i < bonusRewards.Length; i++)
        {
            if (!IsValidReward(i))
                continue;

            BonusReward reward = bonusRewards[i];

            if (!IsRewardClaimed(i) &&
                IsRequiredLevelCompleted(reward))
            {
                return true;
            }
        }

        return false;
    }

    public void OpenRewardStoreFromHome()
    {
        // Remember that the reward panel was opened from Main Menu.
        openedFromHomeForBonusReward = true;

        rewardClaimInProgress = false;
        selectedRewardIndex = -1;

        if (victoryChest != null)
            victoryChest.SetActive(false);

        if (gotReward != null)
            gotReward.SetActive(false);

        HideAllStageCards();

        // Close Main Menu.
        if (MainUI.Instance != null && MainUI.Instance.mainMenu != null)
            MainUI.Instance.mainMenu.SetActive(false);

        // Open Reward Panel.
        if (parentRewardStore != null)
            parentRewardStore.SetActive(true);

        if (rewardStore != null)
            rewardStore.SetActive(true);

        RefreshAllRewards();
    }


    //public bool TryOpenAvailableRewardFromHome()
    //{
    //    //if (!HasAvailableReward())
    //    //    return false;

    //    OpenRewardStoreFromHome();
    //    return true;
    //}

    private string GetRewardClaimKey(BonusReward reward)
    {
        return RewardClaimedKey + "Level_" + reward.requiredLevel;
    }

    private bool IsRewardClaimed(int index)
    {
        if (!IsValidReward(index))
            return false;

        BonusReward reward = bonusRewards[index];

        // New permanent key based on the required level.
        string newKey = GetRewardClaimKey(reward);

        if (PlayerPrefs.GetInt(newKey, 0) == 1)
            return true;

        // Preserve claims saved by your previous script.
        string oldKey = RewardClaimedKey + index;

        if (PlayerPrefs.GetInt(oldKey, 0) == 1)
        {
            PlayerPrefs.SetInt(newKey, 1);
            PlayerPrefs.Save();
            return true;
        }

        return false;
    }
}
