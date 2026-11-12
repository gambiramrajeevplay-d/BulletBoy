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
    private bool hierarchyWarningShown;

    // Delayed selection of the Tap To Claim / Done button (see SelectButton).
    private Coroutine selectButtonCoroutine;

    private void Awake()
    {
        if (Instance == null)
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
        ValidateReferences();

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

    private void OnDisable()
    {
        // A coroutine stops when this object is disabled, so clear the handle.
        selectButtonCoroutine = null;
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

    // ---------------- INSPECTOR VALIDATION ----------------

    // Logs Inspector mistakes that make panels switch off or never appear.
    private void ValidateReferences()
    {
        if (victoryChest == null)
            Debug.LogWarning("BonusRewardManager: 'Victory Chest' panel is not assigned.");

        if (gotReward == null)
            Debug.LogWarning("BonusRewardManager: 'Got Reward' panel is not assigned.");

        if (rewardStore == null)
            Debug.LogWarning("BonusRewardManager: 'Reward Store' panel is not assigned.");

        if (bonusRewards == null)
            return;

        for (int i = 0; i < bonusRewards.Length; i++)
        {
            BonusReward r = bonusRewards[i];

            if (r == null)
            {
                Debug.LogWarning("BonusRewardManager: Element " + i + " is empty.");
                continue;
            }

            if (r.rewardButton == null)
                Debug.LogWarning("BonusRewardManager: Element " + i + " has no Reward Button.");

            if (r.victoryChestCard == null)
                Debug.LogWarning("BonusRewardManager: Element " + i + " has no Victory Chest Card.");

            if (r.tapToClaimButton == null)
                Debug.LogWarning("BonusRewardManager: Element " + i + " has no Tap To Claim Button.");

            if (r.gotRewardCard == null)
                Debug.LogWarning("BonusRewardManager: Element " + i + " has no Got Reward Card.");

            if (r.doneButton == null)
                Debug.LogWarning("BonusRewardManager: Element " + i + " has no Done Button.");

            for (int j = i + 1; j < bonusRewards.Length; j++)
            {
                BonusReward o = bonusRewards[j];

                if (o == null)
                    continue;

                if (r.victoryChestCard != null && r.victoryChestCard == o.victoryChestCard)
                    Debug.LogWarning("BonusRewardManager: Elements " + i + " and " + j + " share the same Victory Chest Card.");

                if (r.tapToClaimButton != null && r.tapToClaimButton == o.tapToClaimButton)
                    Debug.LogWarning("BonusRewardManager: Elements " + i + " and " + j + " share the same Tap To Claim Button.");

                if (r.gotRewardCard != null && r.gotRewardCard == o.gotRewardCard)
                    Debug.LogWarning("BonusRewardManager: Elements " + i + " and " + j + " share the same Got Reward Card.");

                if (r.doneButton != null && r.doneButton == o.doneButton)
                    Debug.LogWarning("BonusRewardManager: Elements " + i + " and " + j + " share the same Done Button.");
            }
        }
    }

    // ---------------- SAFE PANEL HELPERS ----------------

    // Turns an object ON together with every inactive parent, so it is
    // really visible even if a parent was switched off earlier.
    private void ActivateWithParents(GameObject target)
    {
        if (target == null)
            return;

        Transform t = target.transform;

        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);

            t = t.parent;
        }
    }

    // Hides a panel, but NEVER if that would also hide the panel we are
    // about to show (the next panel is inside it) or this script itself.
    private void HidePanelSafely(GameObject panel, GameObject nextPanel)
    {
        if (panel == null || !panel.activeSelf)
            return;

        bool containsNextPanel =
            nextPanel != null &&
            nextPanel.transform.IsChildOf(panel.transform);

        bool containsThisManager =
            transform.IsChildOf(panel.transform);

        if (containsNextPanel || containsThisManager)
        {
            // Keep it on and draw the next panel on top of it.
            if (nextPanel != null)
                nextPanel.transform.SetAsLastSibling();

            if (!hierarchyWarningShown)
            {
                hierarchyWarningShown = true;

                Debug.LogWarning(
                    "BonusRewardManager: '" + panel.name +
                    "' contains '" + (nextPanel != null ? nextPanel.name : "the next panel") +
                    "' (or this script), so it was left active. " +
                    "For a clean result make Reward Store, Victory Chest and " +
                    "Got Reward SIBLINGS under Parent Reward Store.");
            }

            return;
        }

        panel.SetActive(false);
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

        // 1) Hide every card / button first.
        HideAllStageCards();

        // 2) Show the Victory Chest BEFORE hiding the store, so it can
        //    never be switched off by hiding a parent panel.
        ActivateWithParents(victoryChest);

        // 3) Hide the Reward Store and Got Reward panels.
        HidePanelSafely(rewardStore, victoryChest);
        HidePanelSafely(gotReward, victoryChest);

        // 4) Show only the selected reward's Victory Chest card.
        ShowVictoryChestCard(index);
    }

    // ---------------- VICTORY CHEST ----------------

    private void ShowVictoryChestCard(int selectedIndex)
    {
        if (!IsValidReward(selectedIndex))
            return;

        BonusReward reward = bonusRewards[selectedIndex];

        // Everything was already hidden by HideAllStageCards().
        // Only turn the SELECTED reward ON, so shared objects always stay on.
        if (reward.victoryChestCard != null)
            ActivateWithParents(reward.victoryChestCard);

        if (reward.tapToClaimButton != null)
        {
            ActivateWithParents(reward.tapToClaimButton.gameObject);
            SelectButton(reward.tapToClaimButton, victoryChest);
        }
        else
        {
            Debug.LogWarning(
                "BonusRewardManager: Element " + selectedIndex +
                " has no Tap To Claim Button.");
        }
    }

    public void TapClaimButton(int index)
    {
        if (!rewardClaimInProgress ||
            index != selectedRewardIndex ||
            !IsValidReward(index))
            return;

        // 1) Hide every card / button first.
        HideAllStageCards();

        // 2) Show Got Reward BEFORE hiding the Victory Chest.
        ActivateWithParents(gotReward);
        HidePanelSafely(victoryChest, gotReward);

        // 3) Show only the selected reward's Got Reward card.
        ShowGotRewardCard(index);
    }


    private void ShowGotRewardCard(int selectedIndex)
    {
        if (!IsValidReward(selectedIndex))
            return;

        BonusReward reward = bonusRewards[selectedIndex];

        if (reward.gotRewardCard != null)
            ActivateWithParents(reward.gotRewardCard);

        if (reward.doneButton != null)
        {
            // Also turns on the done_bg parent if the button sits inside it.
            ActivateWithParents(reward.doneButton.gameObject);
            reward.doneButton.interactable = true;

            SelectButton(reward.doneButton, gotReward);
        }
        else
        {
            Debug.LogWarning(
                "BonusRewardManager: Element " + selectedIndex +
                " has no Done Button.");
        }
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
        HideAllStageCards();

        // Show the Reward Store BEFORE hiding the other panels.
        ActivateWithParents(rewardStore);

        HidePanelSafely(victoryChest, rewardStore);
        HidePanelSafely(gotReward, rewardStore);

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

        SetRewardState(reward, true, false, false, true);

        // A coroutine can only start while this object is active.
        if (isActiveAndEnabled)
        {
            reward.messageCoroutine =
                StartCoroutine(HideCompleteLevelMessage(index));
        }
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
        // Reset the flow so the reward buttons keep working next time,
        // even if the store was closed in the middle of a claim.
        selectedRewardIndex = -1;
        rewardClaimInProgress = false;

        if (selectButtonCoroutine != null)
        {
            StopCoroutine(selectButtonCoroutine);
            selectButtonCoroutine = null;
        }

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


    // ---------------- BUTTON SELECTION / HIGHLIGHT ----------------

    // Selects a button so the highlighter shows on it.
    // The selection is done a frame AFTER the panel is switched on, because:
    //  - a ButtonHighlighter on the panel runs its OnEnable (and may select its
    //    own default button) in the same frame the panel is activated;
    //  - the button was only just activated, so the EventSystem and the
    //    highlighter have not seen it yet.
    // 'panel' is the panel the button lives in (Victory Chest / Got Reward).
    private void SelectButton(Button button, GameObject panel)
    {
        if (button == null)
            return;

        if (!isActiveAndEnabled)
        {
            SelectButtonNow(button, panel);
            return;
        }

        if (selectButtonCoroutine != null)
            StopCoroutine(selectButtonCoroutine);

        selectButtonCoroutine =
            StartCoroutine(SelectButtonNextFrame(button, panel));
    }

    private IEnumerator SelectButtonNextFrame(Button button, GameObject panel)
    {
        // Let every OnEnable / Start on the freshly activated panel run first.
        yield return null;

        SelectButtonNow(button, panel);

        // Make sure nothing stole the selection (e.g. a default-button script).
        yield return null;

        if (button != null &&
            button.gameObject.activeInHierarchy &&
            EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject != button.gameObject)
        {
            SelectButtonNow(button, panel);
        }

        selectButtonCoroutine = null;
    }

    private void SelectButtonNow(Button button, GameObject panel)
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

        EnsureHighlighter(button, panel);

        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(button.gameObject);
    }

    // The highlight is drawn by a ButtonHighlighter. If the panel the button
    // sits in has no active one (for example the only highlighter lives on the
    // Reward Store panel, which is hidden at this point), nothing would show.
    // This makes sure one exists and is running.
    private void EnsureHighlighter(Button button, GameObject panel)
    {
        ButtonHighlighter existing =
            button.GetComponentInParent<ButtonHighlighter>();

        if (existing != null)
        {
            if (!existing.enabled)
                existing.enabled = true;

            return;
        }

        GameObject host =
            panel != null ? panel : button.transform.parent.gameObject;

        if (host != null && host.GetComponent<ButtonHighlighter>() == null)
            host.AddComponent<ButtonHighlighter>();
    }

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