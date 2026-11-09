using Script;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainUI : MonoBehaviour
{
    public static MainUI Instance;

    public GameObject privacyPolicy, gameTitle, dailyRewards, mainMenu, store, levelSelection,
        tvLoading, tabLoading, vipEnvelope, vipPass, starterPack, rewardStore, interactableReward,
        rewardIndicator, spinWheel, spinIndicator, adsPlayer;

    // ========================= CAR CONTROLLER =========================

    private const int TOTAL_CARS = 7;
    private const int SPECIAL_CAR_INDEX = 1;
    private const int SPECIAL_CAR_PRICE = 2500;

    [Header("Car Controller Panel")]
    [SerializeField] private GameObject carControllerPanel;

    [Header("Car Preview Objects (Scene Objects)")]
    [SerializeField] private GameObject[] allCars = new GameObject[TOTAL_CARS];

    [Header("Car UI")]
    [SerializeField] private TMP_Text carNameText;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private Image accel;
    [SerializeField] private Image boost;
    [SerializeField] private Image handling;

    [Header("Car Buttons")]
    [SerializeField] private Button leftCarButton;
    [SerializeField] private Button rightCarButton;
    [SerializeField] private Button carCloseButton;
    [SerializeField] private GameObject selectCarButton;
    [SerializeField] private GameObject unlock2500Button;
    [SerializeField] private GameObject[] buyCarButtons;

    [Header("Car Stats")]
    [SerializeField] private float[] accelVal;
    [SerializeField] private float[] boostVal;
    [SerializeField] private float[] handlingVal;

    [Header("Car Not Enough Coins Popup")]
    [SerializeField] private GameObject notEnoughCoinsPopup;
    [SerializeField] private float notEnoughPopupDuration = 5f;

    [Header("Car Names")]
    [SerializeField] private string[] carNames = new string[TOTAL_CARS];

    [Header("Temporary Car Unlock (Session Only)")]
    [SerializeField] private bool tempUnlockAllCars = false;

    public int presentCar = 0;

    private Coroutine notEnoughPopupCoroutine;
    private bool carButtonListenersAdded;

    [Header("Web Video Ad")]
    public WebVideoAd webVideoAdObject;

    [Header("No Ads Settings")]
    public bool NoAds = false;

    private const string VipEnvelopeShownKey = "VipEnvelopeShown";

    [Header("Coins")]
    public List<TextMeshProUGUI> totalCoinsTexts;

    [Header("Level Selection")]
    public int selectedlvl;
    public Button[] lvlButtons;
    public LevelButtonUI[] lvlButtonsUI;
    public RectTransform content;
    public int selectedIndex = 0;
    public float smoothSpeed = 2.5f;
    public GameObject SelectedObj;
    public float[] positions;

   
    [Header("Level Unlock")]
    [SerializeField] private GameObject unlockAllLevelsPanel;

    [System.Serializable]
    public class LevelButtonUI
    {
        [Header("Level Details")]
        public string sceneNameToLoad = "MainGame";
        public int levelToLoad = 1;
        public bool isPaidLevel = false;

        [Header("Level Visuals")]
        public Button button;
        public GameObject lockImg;
        public GameObject playable;
        public GameObject completed;
    }

    private enum LobbyAdLocation
    {
        None = 0,
        VipPassClose = 1,
        MainMenuStart = 2,
        StarterPackClose = 3
    }

    private enum AfterAdAction
    {
        None,
        OpenMainMenu,
        OpenCarController,
        OpenStarterPack,
        OpenLevelSelection
    }

    private const string LobbyAdPendingKey = "LobbyAdPending";

    private LobbyAdLocation selectedLobbyAdLocation = LobbyAdLocation.None;
    private AfterAdAction afterAdAction = AfterAdAction.None;

    private bool lobbyAdUsed;
    private bool lobbyAdPending;
    private WebVideoAd webVideoAd;

    [Header("Interactable Level")]
    [Header("Take Index From UI_Canvas Load Level Array Index Value")]
    public bool loadInteractableLevelOnAgree = true;
    public int interactableLevelIndex = 0;

    private const string InteractableKey = "IsInteractableLevel";
    private const string SelectedLevelKey = "SelectedLevel";
    private const string ShowVIPOnMenuReturnKey = "ShowVIPOnMenuReturn";

    private int levelsUnlocked;

    [Header("Audio")]
    public AudioSource menuSound;

    [Header("Sound Toggle")]
    public Image soundButton;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    [Header("Settings")]
    public GameObject settingsPanel;
    public Button settingsCloseButton;

    private const string PrivacyAcceptedKey = "PrivacyPolicyAccepted";
    private const string TutorialCompletedKey = "TutorialCompleted";

    private const string InteractableRewardClaimedKey = "InteractableRewardClaimed";
    private const string ShowInteractableRewardKey = "ShowInteractableRewardOnMenuReturn";

    private bool vipFlowActive;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeLobbyAd();
        InitializeCarController();

        AudioListener.pause = false;

        UpdateSpinIndicator();
        UpdateSoundUI();
        UpdateLevelButtonsUI();
        UpdateTotalCoins();

        StartCoroutine(RefreshBonusRewardIndicatorNextFrame());

        if (!PlayerPrefs.HasKey("PopUpCounter"))
        {
            PlayerPrefs.SetInt("PopUpCounter", 0);
            PlayerPrefs.Save();
        }

        if (vipEnvelope != null)
            vipEnvelope.SetActive(false);

        if (vipPass != null)
            vipPass.SetActive(false);

        if (carControllerPanel != null)
            carControllerPanel.SetActive(false);

        MenuSoundOn();

        bool privacyAccepted = PlayerPrefs.GetInt(PrivacyAcceptedKey, 0) == 1;
        bool tutorialCompleted = PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;

        if (!privacyAccepted)
        {
            if (privacyPolicy != null)
                privacyPolicy.SetActive(true);

            if (gameTitle != null)
                gameTitle.SetActive(false);

            if (dailyRewards != null)
                dailyRewards.SetActive(false);

            if (mainMenu != null)
                mainMenu.SetActive(false);

            return;
        }

        if (privacyPolicy != null)
            privacyPolicy.SetActive(false);


        bool showInteractableReward =
            PlayerPrefs.GetInt(ShowInteractableRewardKey, 0) == 1;

        if (showInteractableReward)
        {
            // Consume the pending request so it cannot trigger on every launch.
            PlayerPrefs.SetInt(ShowInteractableRewardKey, 0);
            PlayerPrefs.Save();

            if (mainMenu != null)
                mainMenu.SetActive(false);

            if (gameTitle != null)
                gameTitle.SetActive(false);

            if (dailyRewards != null)
                dailyRewards.SetActive(false);

            if (store != null)
                store.SetActive(false);

            if (levelSelection != null)
                levelSelection.SetActive(false);

            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (vipEnvelope != null)
                vipEnvelope.SetActive(false);

            if (vipPass != null)
                vipPass.SetActive(false);

            MenuSoundOn();

            if (interactableReward != null)
            {
                interactableReward.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Interactable Reward panel is not assigned.");
                StartCoroutine(ShowTitleAndDailyRewards());
            }

            return;
        }

        bool showVIP = PlayerPrefs.GetInt("ShowVIPOnMenuReturn", 0) == 1;

        if (showVIP)
        {
            PlayerPrefs.SetInt("ShowVIPOnMenuReturn", 0);

            int lastCompletedLevel =
                PlayerPrefs.GetInt("LastCompletedBonusLevel", -1);

            // Consume this level check so it cannot be reused on a later visit.
            PlayerPrefs.SetInt("LastCompletedBonusLevel", -1);
            PlayerPrefs.Save();

            BonusRewardManager bonusManager =
                FindObjectOfType<BonusRewardManager>(true);

            if (bonusManager != null &&
                bonusManager.HasAvailableRewardForLevel(lastCompletedLevel))
            {
                if (mainMenu != null)
                    mainMenu.SetActive(false);

                if (gameTitle != null)
                    gameTitle.SetActive(false);

                if (dailyRewards != null)
                    dailyRewards.SetActive(false);

                bonusManager.OpenRewardStoreFromHome();
            }
            else
            {
                ShowVipFlow();
            }
        }
        else if (tutorialCompleted)
        {
            StartCoroutine(ShowTitleAndDailyRewards());
        }
        else
        {
            if (mainMenu != null)
                mainMenu.SetActive(true);
        }

    }

    private void Update()
    {
        if (vipFlowActive)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            SelectPrevious();

        if (Input.GetKeyDown(KeyCode.RightArrow))
            SelectNext();

        if (carControllerPanel != null && carControllerPanel.activeInHierarchy)
            UpdateCoinsUI();
    }

    //========================= Ads Positions =========================

    private void InitializeLobbyAd()
    {
        lobbyAdUsed = false;
        afterAdAction = AfterAdAction.None;

        if (NoAds)
        {
            lobbyAdPending = false;
            selectedLobbyAdLocation = LobbyAdLocation.None;

            if (adsPlayer != null)
                adsPlayer.SetActive(false);

            PlayerPrefs.SetInt(LobbyAdPendingKey, 0);
            PlayerPrefs.Save();

            Debug.Log("NoAds Enabled. Lobby ads disabled.");
            return;
        }

        if (webVideoAdObject != null)
        {
            webVideoAd = webVideoAdObject;
        }
        else if (adsPlayer != null)
        {
            webVideoAd = adsPlayer.GetComponent<WebVideoAd>();
        }

        if (adsPlayer != null)
            adsPlayer.SetActive(false);

        lobbyAdPending = PlayerPrefs.GetInt(LobbyAdPendingKey, 0) == 1;

        if (!lobbyAdPending)
        {
            selectedLobbyAdLocation = LobbyAdLocation.None;
            return;
        }

        int lastCompletedLevel =
            PlayerPrefs.GetInt("LastCompletedBonusLevel", -1);

        bool bonusRewardAvailable = false;

        if (PlayerPrefs.GetInt("ShowVIPOnMenuReturn", 0) == 1 &&
            lastCompletedLevel >= 0)
        {
            BonusRewardManager bonusManager =
                FindObjectOfType<BonusRewardManager>(true);

            if (bonusManager != null)
            {
                bonusRewardAvailable =
                    bonusManager.HasAvailableRewardForLevel(
                        lastCompletedLevel);
            }
        }

        if (bonusRewardAvailable)
        {
            selectedLobbyAdLocation =
                (LobbyAdLocation)Random.Range(2, 4);
        }
        else
        {
            selectedLobbyAdLocation =
                (LobbyAdLocation)Random.Range(1, 4);
        }

        PlayerPrefs.SetInt(LobbyAdPendingKey, 0);
        PlayerPrefs.Save();

        Debug.Log(
            "Random lobby ad location: " + selectedLobbyAdLocation +
            " | Bonus reward available: " + bonusRewardAvailable);

        if (webVideoAd == null)
        {
            Debug.LogError(
                "WebVideoAd reference is missing. Assign the WebVideoAd component in MainUI.");
        }
    }



    public void MarkReturnToLobbyForAd()
    {
        PlayerPrefs.SetInt(LobbyAdPendingKey, 1);
        PlayerPrefs.Save();
    }

    private bool TryShowLobbyAd(LobbyAdLocation location, AfterAdAction action)
    {
        if (NoAds)
            return false;

        if (!lobbyAdPending ||
            lobbyAdUsed ||
            selectedLobbyAdLocation != location)
        {
            return false;
        }

        if (adsPlayer == null || webVideoAd == null)
        {
            Debug.LogWarning("AdsPlayer or WebVideoAd is not assigned.");
            lobbyAdPending = false;
            lobbyAdUsed = true;
            return false;
        }

        lobbyAdUsed = true;
        lobbyAdPending = false;
        afterAdAction = action;

        // Register the callback BEFORE activating the ad.
        webVideoAd.SetCloseCallback(OnLobbyAdClosed);

        // Activate the ad immediately.
        adsPlayer.SetActive(true);

        //starting countdownroutine in wegvideoad from here
        webVideoAd.StartCountdown();

        // Close the previous panel in the same frame.
        switch (afterAdAction)
        {
            case AfterAdAction.OpenMainMenu:
                if (vipPass != null)
                    vipPass.SetActive(false);

                MenuSoundOn();

                if (vipEnvelope != null)
                    vipEnvelope.SetActive(false);
                break;

            case AfterAdAction.OpenCarController:
                if (mainMenu != null)
                    mainMenu.SetActive(false);
                break;

            case AfterAdAction.OpenStarterPack:
                if (mainMenu != null)
                    mainMenu.SetActive(false);
                break;

            case AfterAdAction.OpenLevelSelection:
                if (starterPack != null)
                    starterPack.SetActive(false);
                break;
        }

        return true;
    }

    private void OnLobbyAdClosed()
    {
        if (adsPlayer != null)
            adsPlayer.SetActive(false);

        AfterAdAction action = afterAdAction;
        afterAdAction = AfterAdAction.None;

        switch (action)
        {
            case AfterAdAction.OpenMainMenu:
                OpenMainMenuAfterVip();
                break;

            case AfterAdAction.OpenCarController:
                OpenCarControllerPanel();
                break;

            case AfterAdAction.OpenStarterPack:
                OpenStarterPackAfterAd();
                break;

            case AfterAdAction.OpenLevelSelection:
                OpenLevelSelectionAfterAd();
                break;
        }
    }

    private void OpenStarterPackAfterAd()
    {
        if (starterPack != null)
            starterPack.SetActive(true);

        if (mainMenu != null)
            mainMenu.SetActive(false);
    }

    private void OpenLevelSelectionAfterAd()
    {
        if (carControllerPanel != null)
            carControllerPanel.SetActive(false);

        if (starterPack != null)
            starterPack.SetActive(false);

        if (levelSelection != null)
            levelSelection.SetActive(true);

        UpdateTotalCoins();
        UpdateLevelButtonsUI();

        StartCoroutine(SelectFirstLevelButton());
    }

    // ======================== PRIVACY POLICY ========================

    public void Privacy_Policy()
    {
        Application.OpenURL(
            "https://playdgamestudio.com/privacy-policy-2/");
    }

    public void TermsOfUse()
    {
        Application.OpenURL(
            "https://playdgamestudio.com/privacy-policy-2/");
    }

    public void Privacy_Agree()
    {
        PlayerPrefs.SetInt(PrivacyAcceptedKey, 1);

        if (loadInteractableLevelOnAgree)
        {
            PlayerPrefs.SetInt(InteractableKey, 1);
            PlayerPrefs.SetInt(SelectedLevelKey, interactableLevelIndex);
        }
        else
        {
            PlayerPrefs.SetInt(InteractableKey, 0);
        }

        PlayerPrefs.Save();

        SceneManager.LoadScene("Game_Lobby");
    }

    // ======================== Daily Rewards PANEL ========================

    private IEnumerator ShowTitleAndDailyRewards()
    {
        if (gameTitle != null)
            gameTitle.SetActive(true);

        if (dailyRewards != null)
            dailyRewards.SetActive(false);

        if (mainMenu != null)
            mainMenu.SetActive(false);

        yield return new WaitForSeconds(3f);

        if (gameTitle != null)
            gameTitle.SetActive(false);

        DailyRewards_Final dailyRewardScript =
            FindObjectOfType<DailyRewards_Final>();

        if (dailyRewardScript != null)
        {
            dailyRewardScript.ShowRewardIfAvailable();
        }
        else
        {
            if (dailyRewards != null)
                dailyRewards.SetActive(false);

            if (mainMenu != null)
                mainMenu.SetActive(true);
        }
    }

    // ======================== MAIN MENU ========================

    public void MenuSoundOn()
    {
        if (menuSound != null)
            menuSound.Play();
    }

    public void MenuSoundOff()
    {
        if (menuSound != null)
            menuSound.Stop();
    }

    public void OnStartBtnPressed()
    {
        if (TryShowLobbyAd(
            LobbyAdLocation.MainMenuStart,
            AfterAdAction.OpenCarController))
        {
            return;
        }

        OpenCarControllerPanel();
    }

    // Opens the car selection panel from Main Menu Play or Level Selection Back.
    public void OpenCarControllerPanel()
    {
        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (levelSelection != null)
            levelSelection.SetActive(false);

        if (starterPack != null)
            starterPack.SetActive(false);

        if (carControllerPanel != null)
        {
            carControllerPanel.SetActive(true);
            RefreshCar();
        }
        else
        {
            Debug.LogError("MainUI: Car Controller Panel is not assigned.");
        }
    }

    // Called after the player confirms the currently previewed car.
    public void OnCarSelected()
    {
        if (carControllerPanel != null)
            carControllerPanel.SetActive(false);

        TurnOffAllCars();

        if (starterPack != null)
            starterPack.SetActive(true);
        else
            OpenLevelSelectionAfterAd();
    }



    public void StarterPackClose()
    {
        if (TryShowLobbyAd(
            LobbyAdLocation.StarterPackClose,
            AfterAdAction.OpenLevelSelection))
        {
            return;
        }

        OpenLevelSelectionAfterAd();
    }

    public void OnstorePanel()
    {
        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (store != null)
            store.SetActive(true);
    }

    public void OffstorePanel()
    {
        if (mainMenu != null)
            mainMenu.SetActive(true);

        UpdateTotalCoins();

        if (store != null)
            store.SetActive(false);
    }

    public void Moreinfo()
    {
        Application.OpenURL(
            "https://www.amazon.com/Games-PlayD-Game-Studio-Private-Limited/s?rh=n%3A9209902011%2Cp_4%3APlayD+Game+Studio+Private+Limited");
    }


    public void OnChestBox()
    {
        BonusRewardManager bonusManager =
            FindObjectOfType<BonusRewardManager>(true);

        if (bonusManager == null)
        {
            Debug.LogWarning("BonusRewardManager was not found.");
            return;
        }

        bonusManager.OpenRewardStoreFromHome();
    }

    public void OffChestBox()
    {
        rewardStore.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void UpdateRewardIndicator(bool show)
    {
        if (rewardIndicator != null)
        {
            rewardIndicator.SetActive(show);
        }
    }

    public void OnSpinWheel()
    {
        spinWheel.SetActive(true);
        mainMenu.SetActive(false);
    }

    public void OffSpinWheel()
    {
        mainMenu.SetActive(true);
        spinWheel.SetActive(false);

        UpdateSpinIndicator();
    }

    public void UpdateSpinIndicator()
    {
        if (spinIndicator == null)
            return;

        string today = System.DateTime.Now.ToString("yyyy-MM-dd");

        string lastSpinDate = PlayerPrefs.GetString(
            "DailySpinLastClaimDate",
            ""
        );

        bool dailySpinAvailable = lastSpinDate != today;

        SpinWheelManager spinManager = null;

        if (spinWheel != null)
        {
            spinManager = spinWheel.GetComponentInChildren<SpinWheelManager>(true);
        }

        bool extraSpinAvailable =
            spinManager != null && spinManager.HasExtraSpinsAvailable;

        spinIndicator.SetActive(dailySpinAvailable || extraSpinAvailable);
    }

    private IEnumerator RefreshBonusRewardIndicatorNextFrame()
    {
        // Wait until all Start() methods have initialized.
        yield return null;

        BonusRewardManager bonusManager =
            FindObjectOfType<BonusRewardManager>(true);

        if (bonusManager != null)
        {
            UpdateRewardIndicator(
                bonusManager.HasAnyAvailableReward());
        }
        else
        {
            UpdateRewardIndicator(false);
        }
    }

    public void BuyStarterPack()
    {
        AddStoreCoins(2000);
    }


    // ======================== LEVEL SELECTION ========================

    private void UpdateLevelButtonsUI()
    {
        if (lvlButtonsUI == null)
            return;

        int playerLevel = PlayerPrefs.GetInt(StringsData.playerLevel, 1);
        int unlockedAll = PlayerPrefs.GetInt(StringsData.unlockedAllLevels, 0);

        for (int i = 0; i < lvlButtonsUI.Length; i++)
        {
            LevelButtonUI lvl = lvlButtonsUI[i];

            if (lvl == null || lvl.button == null)
                continue;

            // Remove old listeners so refreshing the menu never adds duplicates.
            lvl.button.onClick.RemoveAllListeners();

            bool isLevelOne = lvl.levelToLoad == 1;
            bool isPaidLevelFiveAvailable =
                lvl.isPaidLevel && lvl.levelToLoad == 5 && playerLevel >= 5;

            bool isUnlocked = isLevelOne || playerLevel >= lvl.levelToLoad;

            if (lvl.lockImg != null)
                lvl.lockImg.SetActive(true);

            if (lvl.playable != null)
                lvl.playable.SetActive(false);

            if (lvl.completed != null)
                lvl.completed.SetActive(false);

            // Level 1 is always free.
            if (isLevelOne)
            {
                ConfigureLevelButton(lvl, true, false);
            }
            // Level 5 becomes selectable after level 4 progression,
            // but still opens the unlock panel until all levels are purchased.
            else if (isPaidLevelFiveAvailable)
            {
                ConfigureLevelButton(lvl, true, unlockedAll == 0);

                if (unlockedAll == 1)
                    AddLevelLoadListener(lvl, i);
                else
                    AddUnlockPanelListener(lvl);
            }
            // Other paid levels can be selected to open the unlock panel.
            else if (lvl.isPaidLevel && unlockedAll == 0)
            {
                ConfigureLevelButton(lvl, true, true);
                AddUnlockPanelListener(lvl);
            }
            // Normal progression-based levels.
            else
            {
                ConfigureLevelButton(lvl, isUnlocked, !isUnlocked);

                if (isUnlocked)
                    AddLevelLoadListener(lvl, i);
            }

            if (PlayerPrefs.GetInt("LevelCompleted_" + i, 0) == 1 &&
                lvl.completed != null)
            {
                lvl.completed.SetActive(true);
            }
        }
    }

    private void ConfigureLevelButton(LevelButtonUI lvl, bool interactable, bool showLock)
    {
        lvl.button.interactable = interactable;

        if (lvl.lockImg != null)
            lvl.lockImg.SetActive(showLock);

        if (lvl.playable != null)
            lvl.playable.SetActive(interactable && !showLock);
    }

    private void AddLevelLoadListener(LevelButtonUI lvl, int arrayIndex)
    {
        lvl.button.onClick.AddListener(() => LoadLevel(arrayIndex));
    }

    private void AddUnlockPanelListener(LevelButtonUI lvl)
    {
        lvl.button.onClick.AddListener(() =>
        {
            ShowUnlockAllLevelsPanel();
        });
    }

    private void ShowUnlockAllLevelsPanel()
    {
        if (unlockAllLevelsPanel != null)
        {
            unlockAllLevelsPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("MainUI: Unlock All Levels Panel is not assigned.");
        }
    }

    public void LevelSelectionbackButton()
    {
        OpenCarControllerPanel();
        UpdateTotalCoins();
    }

    private IEnumerator SelectFirstLevelButton()
    {
        yield return null;

        if (levelSelection == null || !levelSelection.activeInHierarchy)
            yield break;

        ButtonHighlighter[] highlighters =
            levelSelection.GetComponentsInChildren<ButtonHighlighter>(true);

        foreach (ButtonHighlighter highlighter in highlighters)
        {
            if (highlighter != null && highlighter.enabled)
                highlighter.DefaultHighlighter_Levels();
        }
    }

    // Connect level buttons through the MainUI listener.
    // The array index identifies the configured LevelButtonUI entry;
    // the scene itself is selected by its Inspector-assigned sceneNameToLoad.
    public void LoadLevel(int levelIndex)
    {
        if (lvlButtonsUI == null ||
            levelIndex < 0 ||
            levelIndex >= lvlButtonsUI.Length)
        {
            Debug.LogWarning("MainUI: Invalid level button index: " + levelIndex);
            return;
        }

        LevelButtonUI level = lvlButtonsUI[levelIndex];

        if (level == null || level.button == null)
        {
            Debug.LogWarning("MainUI: Level button entry is not configured at index " + levelIndex);
            return;
        }

        if (string.IsNullOrWhiteSpace(level.sceneNameToLoad))
        {
            Debug.LogError("MainUI: Scene Name To Load is empty for level " + level.levelToLoad);
            return;
        }

        int unlockedAll = PlayerPrefs.GetInt(StringsData.unlockedAllLevels, 0);

        if (level.isPaidLevel && unlockedAll == 0)
        {
            ShowUnlockAllLevelsPanel();
            return;
        }

        SelectedObj = level.button.gameObject;
        selectedIndex = levelIndex;

        StartCoroutine(LoadLevelCoroutine(level));
    }

    private IEnumerator LoadLevelCoroutine(LevelButtonUI level)
    {
        Loading();

        // Save the actual level number for gameplay progression systems.
        PlayerPrefs.SetInt(StringsData.levelToLoad, Mathf.Max(1, level.levelToLoad));

        // Retain the existing key for any systems that still read it.
        PlayerPrefs.SetInt(SelectedLevelKey, Mathf.Max(0, level.levelToLoad - 1));
        PlayerPrefs.SetInt(InteractableKey, 0);
        PlayerPrefs.Save();

        yield return new WaitForSeconds(3f);

        // Load the scene configured on this level's MainUI Inspector entry.
        SceneManager.LoadScene(level.sceneNameToLoad);
    }

    private void ShowUnlock()
    {
        if (levelSelection != null)
            levelSelection.SetActive(false);
    }

    // ======================== LEVEL CONTENT MOVEMENT ========================

    public void Content_move()
    {
        if (positions == null || positions.Length == 0 ||
            selectedIndex < 0 || selectedIndex >= positions.Length ||
            content == null)
        {
            return;
        }

        Vector2 targetPosition = new Vector2(
            content.anchoredPosition.x,
            positions[selectedIndex]);

        content.anchoredPosition = Vector2.Lerp(
            content.anchoredPosition,
            targetPosition,
            smoothSpeed * Time.deltaTime);
    }

    public void SelectNext()
    {
        if (positions != null && selectedIndex < positions.Length - 1)
            selectedIndex++;
    }

    public void SelectPrevious()
    {
        if (selectedIndex > 0)
            selectedIndex--;
    }

    public void Loading()
    {
        if (levelSelection != null)
            levelSelection.SetActive(false);

        if (AndroidTV.IsAndroidOrFireTv())
        {
            if (tvLoading != null)
                tvLoading.SetActive(true);

            if (tabLoading != null)
                tabLoading.SetActive(false);
        }
        else
        {
            if (tvLoading != null)
                tvLoading.SetActive(false);

            if (tabLoading != null)
                tabLoading.SetActive(true);
        }
    }

    // ======================== STORE PACKS ========================

    public void store200coins()
    {
        AddStoreCoins(200);
    }

    public void store800coins()
    {
        AddStoreCoins(800);
    }

    public void store1000coins()
    {
        AddStoreCoins(1000);
    }

    public void store5000coins()
    {
        AddStoreCoins(5000);
    }

    private void AddStoreCoins(int amount)
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(amount);
        }
        else
        {
            int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);

            PlayerPrefs.SetInt("TotalCoins", totalCoins + amount);
            PlayerPrefs.Save();

            UpdateTotalCoins();
        }
    }

    // ======================== COIN UI ========================

    public void UpdateTotalCoins()
    {
        int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        string displayText = CoinFormatter.FormatCoins(totalCoins);

        if (totalCoinsTexts == null)
            return;

        foreach (TextMeshProUGUI text in totalCoinsTexts)
        {
            if (text != null)
                text.text = displayText;
        }
    }

    // ======================== SETTINGS ========================

    public void SettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        if (mainMenu != null)
            mainMenu.SetActive(false);
    }

    public void SettingsCloseButton()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainMenu != null)
            mainMenu.SetActive(true);

        UpdateTotalCoins();
    }

    public void Mute()
    {
        bool isMusicOn = PlayerPrefs.GetInt("MusicState", 1) == 1;

        isMusicOn = !isMusicOn;

        AudioListener.volume = isMusicOn ? 1f : 0f;

        PlayerPrefs.SetInt("MusicState", isMusicOn ? 1 : 0);
        PlayerPrefs.Save();

        if (soundButton != null)
        {
            soundButton.sprite =
                isMusicOn ? soundOnSprite : soundOffSprite;
        }
    }

    private void UpdateSoundUI()
    {
        bool isMusicOn = PlayerPrefs.GetInt("MusicState", 1) == 1;

        AudioListener.volume = isMusicOn ? 1f : 0f;

        if (soundButton != null)
        {
            soundButton.sprite =
                isMusicOn ? soundOnSprite : soundOffSprite;
        }
    }

    // ======================== VIP FLOW ========================

    private void ShowVipFlow()
    {
        vipFlowActive = true;

        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (gameTitle != null)
            gameTitle.SetActive(false);

        if (dailyRewards != null)
            dailyRewards.SetActive(false);

        if (store != null)
            store.SetActive(false);

        if (levelSelection != null)
            levelSelection.SetActive(false);

        if (carControllerPanel != null)
            carControllerPanel.SetActive(false);

        if (starterPack != null)
            starterPack.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (vipEnvelope != null)
            vipEnvelope.SetActive(false);

        if (vipPass != null)
            vipPass.SetActive(false);

        MenuSoundOn();

        bool envelopeAlreadyShown =
            PlayerPrefs.GetInt(VipEnvelopeShownKey, 0) == 1;

        if (!envelopeAlreadyShown)
        {
            // Save immediately so the envelope cannot appear again
            // after closing the game or loading Scene 0 again.
            PlayerPrefs.SetInt(VipEnvelopeShownKey, 1);
            PlayerPrefs.Save();

            if (vipEnvelope != null)
            {
                vipEnvelope.SetActive(true);
            }
            else
            {
                Debug.LogWarning("VIP Envelope panel is not assigned.");
                OpenMainMenuAfterVip();
            }
        }
        else
        {
            // Every subsequent visit to Scene 0 opens VIP Pass directly.
            if (vipPass != null)
            {
                vipPass.SetActive(true);
                MenuSoundOff();
            }
            else
            {
                Debug.LogWarning("VIP Pass panel is not assigned.");
                OpenMainMenuAfterVip();
            }
        }
    }

    // Connect this function to the Tap To Open button on VIP Envelope.
    public void OnVipEnvelopeTap()
    {
        if (vipEnvelope != null)
            vipEnvelope.SetActive(false);

        if (vipPass != null)
        {
            vipPass.SetActive(true);
            MenuSoundOff();
        }
        else
        {
            Debug.LogWarning("VIP Pass panel is not assigned.");
            OpenMainMenuAfterVip();
        }
    }

    // Connect this function to the Close button on VIP Pass.

    public void CloseVipPass()
    {
        if (TryShowLobbyAd(
            LobbyAdLocation.VipPassClose,
            AfterAdAction.OpenMainMenu))
        {
            return;
        }

        if (vipPass != null)
            vipPass.SetActive(false);

        MenuSoundOn();

        if (vipEnvelope != null)
            vipEnvelope.SetActive(false);

        OpenMainMenuAfterVip();
    }


    private void OpenMainMenuAfterVip()
    {
        vipFlowActive = false;

        if (vipEnvelope != null)
            vipEnvelope.SetActive(false);

        if (vipPass != null)
            vipPass.SetActive(false);

        MenuSoundOn();

        if (mainMenu != null)
            mainMenu.SetActive(true);

        UpdateTotalCoins();
        UpdateLevelButtonsUI();
    }

    public void OpenVipPassFromReward()
    {
        vipFlowActive = true;

        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (vipEnvelope != null)
            vipEnvelope.SetActive(false);

        if (vipPass != null)
            vipPass.SetActive(true);

        MenuSoundOff();
    }

    public void Vip_Annually()
    {
        AddStoreCoins(6000);
    }
    public void Vip_Quaterly()
    {
        AddStoreCoins(4500);
    }
    public void Vip_Monthly()
    {
        AddStoreCoins(3000);
    }
    public void Vip_Weekly()
    {
        AddStoreCoins(1500);
    }


    public void OnInteractableRewardAnimationComplete()
    {
        if (interactableReward != null)
            interactableReward.SetActive(false);

        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (gameTitle != null)
            gameTitle.SetActive(false);

        if (dailyRewards != null)
            dailyRewards.SetActive(false);

        StartCoroutine(ShowTitleAndDailyRewards());
    }


    // Optional: Call this only if you intentionally want to show
    // the envelope again for testing.
    //public void ResetVipEnvelopeForTesting()
    //{
    //    PlayerPrefs.DeleteKey(VipEnvelopeShownKey);
    //    PlayerPrefs.Save();
    //}


    // ========================= CAR CONTROLLER =========================

    private void InitializeCarController()
    {
        if (PlayerPrefs.GetInt("car0", 0) != 1)
        {
            PlayerPrefs.SetInt("car0", 1);
            PlayerPrefs.Save();
        }

        presentCar = Mathf.Clamp(PlayerPrefs.GetInt("selectedTruck", 0), 0, TOTAL_CARS - 1);

        if (!carButtonListenersAdded)
        {
            if (leftCarButton != null)
                leftCarButton.onClick.AddListener(Left);

            if (rightCarButton != null)
                rightCarButton.onClick.AddListener(Right);

            if (carCloseButton != null)
                carCloseButton.onClick.AddListener(OnCarClose);

            carButtonListenersAdded = true;
        }

        RefreshCar();
        UpdateCoinsUI();

        if (carControllerPanel != null)
            carControllerPanel.SetActive(false);
    }

    private void OnEnable()
    {
        UpdateCoinsUI();
    }

    private void UpdateCoinsUI()
    {
        if (currencyText == null)
            return;

        // CoinManager stores the shared balance under the TotalCoins PlayerPrefs key.
        // Read the same balance used by the existing MainUI coin display.
        currencyText.text = PlayerPrefs.GetInt("TotalCoins", 0).ToString();
    }

    public void Left()
    {
        presentCar = (presentCar - 1 + TOTAL_CARS) % TOTAL_CARS;
        RefreshCar();
    }

    public void Right()
    {
        presentCar = (presentCar + 1) % TOTAL_CARS;
        RefreshCar();
    }

    private void RefreshCar()
    {
        if (presentCar < 0 || presentCar >= TOTAL_CARS)
            presentCar = 0;

        TurnOffAllCars();

        if (allCars != null && presentCar < allCars.Length && allCars[presentCar] != null)
            allCars[presentCar].SetActive(true);

        UpdateCarName();
        UpdateCarStatsUI();
        UpdateCarButtons();

        PlayerPrefs.SetInt("selectedTruck", presentCar);
        PlayerPrefs.Save();

        UpdateCoinsUI();
    }

    private void TurnOffAllCars()
    {
        if (allCars == null)
            return;

        foreach (GameObject car in allCars)
        {
            if (car != null)
                car.SetActive(false);
        }
    }

    private void UpdateCarName()
    {
        if (carNameText == null)
            return;

        if (carNames != null && presentCar < carNames.Length &&
            !string.IsNullOrEmpty(carNames[presentCar]))
        {
            carNameText.text = carNames[presentCar];
        }
        else
        {
            carNameText.text = "Car " + (presentCar + 1);
        }
    }

    private void UpdateCarStatsUI()
    {
        if (accel != null && accelVal != null && presentCar < accelVal.Length)
            accel.fillAmount = accelVal[presentCar];

        if (boost != null && boostVal != null && presentCar < boostVal.Length)
            boost.fillAmount = boostVal[presentCar];

        if (handling != null && handlingVal != null && presentCar < handlingVal.Length)
            handling.fillAmount = handlingVal[presentCar];
    }

    private bool IsCarUnlocked(int index)
    {
        if (index < 0 || index >= TOTAL_CARS)
            return false;

        if (tempUnlockAllCars)
            return true;

        if (index == 0)
            return true;

        return PlayerPrefs.GetInt("car" + index, 0) == 1;
    }

    private void UpdateCarButtons()
    {
        bool unlocked = IsCarUnlocked(presentCar);

        if (selectCarButton != null)
            selectCarButton.SetActive(false);

        if (unlock2500Button != null)
            unlock2500Button.SetActive(false);

        if (buyCarButtons != null)
        {
            foreach (GameObject buyButton in buyCarButtons)
            {
                if (buyButton != null)
                    buyButton.SetActive(false);
            }
        }

        if (unlocked)
        {
            if (selectCarButton != null)
                selectCarButton.SetActive(true);
            return;
        }

        if (presentCar == SPECIAL_CAR_INDEX)
        {
            if (unlock2500Button != null)
                unlock2500Button.SetActive(true);
        }

        if (buyCarButtons != null &&
            presentCar < buyCarButtons.Length &&
            buyCarButtons[presentCar] != null)
        {
            buyCarButtons[presentCar].SetActive(true);
        }
    }

    // Retains the original generic unlock button behavior.
    public void buy()
    {
        PlayerPrefs.SetInt("car" + presentCar, 1);
        PlayerPrefs.Save();
        UpdateCarButtons();
    }

    public void UnlockSpecialCar()
    {
        int currentCoins = PlayerPrefs.GetInt("TotalCoins", 0);

        if (currentCoins < SPECIAL_CAR_PRICE)
        {
            ShowNotEnoughCoinsPopup();
            return;
        }

        // Use the same CoinManager already used by MainUI for coin rewards/purchases.
        if (CoinManager.Instance != null)
        {
            if (!CoinManager.Instance.SpendCoins(SPECIAL_CAR_PRICE))
            {
                ShowNotEnoughCoinsPopup();
                UpdateCoinsUI();
                return;
            }
        }
        else
        {
            // Fallback keeps the balance consistent if the CoinManager is absent.
            PlayerPrefs.SetInt("TotalCoins", currentCoins - SPECIAL_CAR_PRICE);
            PlayerPrefs.Save();
        }

        PlayerPrefs.SetInt("car" + SPECIAL_CAR_INDEX, 1);
        PlayerPrefs.Save();

        UpdateCarButtons();
        UpdateCoinsUI();
        UpdateTotalCoins();
        StartCoroutine(ForceSelectCarButton());
    }

    private IEnumerator ForceSelectCarButton()
    {
        yield return null;

        if (selectCarButton != null && selectCarButton.activeInHierarchy &&
            EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selectCarButton);
        }
    }

    private void ShowNotEnoughCoinsPopup()
    {
        if (notEnoughCoinsPopup == null)
            return;

        if (notEnoughPopupCoroutine != null)
            StopCoroutine(notEnoughPopupCoroutine);

        notEnoughPopupCoroutine = StartCoroutine(NotEnoughCoinsRoutine());
    }

    private IEnumerator NotEnoughCoinsRoutine()
    {
        notEnoughCoinsPopup.SetActive(true);
        yield return new WaitForSeconds(notEnoughPopupDuration);

        if (notEnoughCoinsPopup != null)
            notEnoughCoinsPopup.SetActive(false);

        notEnoughPopupCoroutine = null;
    }

    public void SelectCar()
    {
        PlayerPrefs.SetInt("selectedTruck", presentCar);
        PlayerPrefs.Save();

        OnCarSelected();
    }

    public void OnCarClose()
    {
        if (carControllerPanel != null)
            carControllerPanel.SetActive(false);

        if (mainMenu != null)
            mainMenu.SetActive(true);

        if (levelSelection != null)
            levelSelection.SetActive(false);

        TurnOffAllCars();
    }

    // Compatibility method for the original CarControllerUI Close button event.
    public void OnClose()
    {
        OnCarClose();
    }

    public void EnableTempUnlockAllCars()
    {
        tempUnlockAllCars = true;
        UpdateCarButtons();
    }

    public void DisableTempUnlockAllCars()
    {
        tempUnlockAllCars = false;
        UpdateCarButtons();
    }

}