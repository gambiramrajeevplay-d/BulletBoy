using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using Script;

/*
 * Runs after BulletCannon (-100) and before BulletBoyPlayer (0), so the
 * launch direction is read from the cannon's up-to-date rotation.
 */
[DefaultExecutionOrder(-50)]
public class CannonManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private BulletBoyPlayer player;

    [Header("Player Spawning")]
    [Tooltip("If the player is spawned at runtime, CannonManager waits this many seconds for it to appear.")]
    [SerializeField] private float playerWaitTimeout = 10f;

    [Header("Cannons")]
    [SerializeField] private BulletCannon[] cannons;

    [Header("Cannon Order")]
    [Tooltip("Only used when the Cannons list above is EMPTY (auto-find). An assigned list is always used exactly as written.")]
    [SerializeField] private bool useHierarchyOrder = true;

    [Header("Smoothness")]
    [Tooltip("Android / Fire TV builds often run at 30 FPS by default, which looks like snapping. 60 is recommended. Set 0 to leave it untouched.")]
    [SerializeField] private int targetFrameRate = 60;

    [Header("Input")]
    [Tooltip("A tap that happens while the player is still entering a cannon is remembered for this long (seconds). Set 0 to disable.")]
    [SerializeField] private float inputBufferTime = 0.12f;

    [Tooltip("Pressing Escape (Android / Fire TV Back button) pauses and resumes the game.")]
    [SerializeField] private bool escapeTogglesPause = true;

    [Header("Level Result Panels")]
    [SerializeField] private GameObject levelPassPanel;
    [SerializeField] private GameObject levelFailPanel;
    [SerializeField] private float resultPanelDelay = 1f;

    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;

    [Header("Pause Sound Toggle")]
    [Tooltip("Assign the Image component on the sound toggle button inside the Pause UI.")]
    [SerializeField] private Image pauseSoundButton;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;

    [Header("Platform UI")]
    [Tooltip("Assign the in-game Pause button. It is hidden on Android TV / Fire TV and shown on mobile / tablet.")]
    [SerializeField] private GameObject pauseButtonGameObject;

    [Tooltip("Optional remote UI/GameObject. It is shown only in the Game_Lobby scene on Android TV / Fire TV.")]
    [SerializeField] private GameObject remoteGameObject;

    [Header("Result Coin Text")]
    [Tooltip("Assign the TMP text that displays the reward on the Level Pass panel.")]
    [SerializeField] private TMP_Text levelPassCoinText;

    [Tooltip("Assign the TMP text that displays 0 on the Level Fail panel.")]
    [SerializeField] private TMP_Text levelFailCoinText;

    [Tooltip("Assign the TMP text that displays 0 on the Tutorial Complete panel.")]
    [SerializeField] private TMP_Text tutorialCoinText;

    [Tooltip("Used only if CoinManager is not present in this scene.")]
    [SerializeField] private int fallbackLevelPassCoins = 100;

    private bool resultCoinsApplied;

    [Header("Level Object")]
    [Tooltip("Gameplay objects to hide when the Level Pass or Level Fail panel appears. Do not assign the GameObject containing CannonManager or the result panels.")]
    [SerializeField] private GameObject levelGameObject;

    [Header("Level Result Audio")]
    [Tooltip("Audio clip played when the Level Pass panel appears.")]
    [SerializeField] private AudioClip levelPassClip;

    [Tooltip("Audio clip played when the Level Fail panel appears.")]
    [SerializeField] private AudioClip levelFailClip;

    [Header("Tutorial Result")]
    [SerializeField] private GameObject tutorialPassPanel;
    [SerializeField] private float tutorialResultDuration = 3f;

    private AudioSource levelPassAudioSource;
    private AudioSource levelFailAudioSource;

    private int currentCannonIndex;
    private bool levelFinished;
    private bool resultRoutineStarted;
    private bool tutorialResultShowing;
    private bool isPaused;
    private float bufferedInputTimer;

    // Prevent the TV remote's Resume/Submit press from also launching the player.
    private bool blockLaunchInputUntilReleased;

    // True once the player exists and the first cannon has been entered.
    private bool levelStarted;

    // Reused list for UI raycasts (avoids allocating every tap).
    private static readonly List<RaycastResult> uiRaycastResults =
        new List<RaycastResult>();


    // =========================================================
    // AWAKE / START
    // =========================================================

    private void Awake()
    {
        ApplyFrameRate();
        ApplyPlatformUI();
        UpdatePauseSoundUI();

        // The player may be spawned at runtime, so this can legitimately
        // find nothing here. Start() waits for the player if needed.
        FindPlayer();
        FindCannons();

        CreateResultAudioSources();

        if (levelPassPanel != null)
            levelPassPanel.SetActive(false);

        if (levelFailPanel != null)
            levelFailPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (tutorialPassPanel != null)
            tutorialPassPanel.SetActive(false);
    }

    private void ApplyPlatformUI()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        bool isTV = AndroidTV.IsAndroidOrFireTv();
        bool isTutorial = currentSceneName == "Tutorial";
        bool isGameLobby = currentSceneName == "Game_Lobby";

        // Pause button: hide on TV / Fire TV, Tutorial, and Game_Lobby.
        // Show it only in other scenes on mobile / tablet.
        if (pauseButtonGameObject != null)
            pauseButtonGameObject.SetActive(!isTV && !isTutorial && !isGameLobby);

        // Remote: show only in Game_Lobby on Android TV / Fire TV.
        // This reference is optional, so scenes without a remote are safe.
        if (remoteGameObject != null)
            remoteGameObject.SetActive(isTV && isGameLobby);
    }

    // Returns true when the active scene is the Tutorial gameplay scene.
    private bool IsTutorialScene()
    {
        return SceneManager.GetActiveScene().name == "Tutorial";
    }

    // Returns true when the active scene is the Game Lobby.
    private bool IsGameLobbyScene()
    {
        return SceneManager.GetActiveScene().name == "Game_Lobby";
    }
    private void ApplyFrameRate()
    {
        if (targetFrameRate <= 0)
            return;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }

    private void Start()
    {
        if (cannons == null || cannons.Length == 0)
        {
            Debug.LogError("CannonManager: No cannons found.");
            return;
        }

        PrintCannonOrder();

        // Player already exists (placed in the scene or registered before Start).
        if (player != null)
        {
            BeginLevel();
            return;
        }

        // Player is spawned at runtime: wait for it to appear.
        StartCoroutine(WaitForPlayerAndBegin());
    }

    private IEnumerator WaitForPlayerAndBegin()
    {
        float waited = 0f;

        while (player == null && waited < playerWaitTimeout)
        {
            FindPlayer();

            if (player != null)
                break;

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (player == null)
        {
            Debug.LogError(
                "CannonManager: Player was not found after waiting " +
                playerWaitTimeout + " seconds."
            );
            yield break;
        }

        BeginLevel();
    }

    private void BeginLevel()
    {
        if (levelStarted)
            return;

        levelStarted = true;
        currentCannonIndex = 0;

        EnterCurrentCannon();
    }

    // Optional: call this from your spawner right after Instantiate().
    // Example: FindObjectOfType<CannonManager>().RegisterPlayer(spawnedPlayer);
    public void RegisterPlayer(BulletBoyPlayer spawnedPlayer)
    {
        if (spawnedPlayer == null)
            return;

        player = spawnedPlayer;

        // If Start() already ran and is waiting, the wait loop will pick the
        // player up on its next frame. If the level has not started and no
        // wait is running, begin right away.
        if (!levelStarted && cannons != null && cannons.Length > 0)
            BeginLevel();
    }

    private void FindPlayer()
    {
        if (player != null)
            return;

        player = FindObjectOfType<BulletBoyPlayer>();
    }

    private void FindCannons()
    {
        /*
         * If the Cannons list is assigned in the Inspector, that list IS
         * the path. It is used exactly as written and is NEVER re-sorted.
         */

        if (cannons != null && cannons.Length > 0)
        {
            ValidateCannonList();
            return;
        }

        // Nothing assigned: find them automatically.
        cannons = FindObjectsOfType<BulletCannon>();

        SortCannons();
    }

    private void ValidateCannonList()
    {
        for (int i = 0; i < cannons.Length; i++)
        {
            if (cannons[i] == null)
            {
                Debug.LogWarning(
                    "CannonManager: Cannons list has an empty slot at index " + i
                );
                continue;
            }

            for (int j = i + 1; j < cannons.Length; j++)
            {
                if (cannons[i] == cannons[j])
                {
                    Debug.LogWarning(
                        "CannonManager: " + cannons[i].gameObject.name +
                        " appears twice in the Cannons list (index " +
                        i + " and " + j + ")."
                    );
                }
            }
        }
    }

    private void SortCannons()
    {
        if (cannons == null || cannons.Length <= 1)
            return;

        if (!useHierarchyOrder)
            return;

        System.Array.Sort(
            cannons,
            (a, b) =>
            {
                if (a == null) return 1;
                if (b == null) return -1;

                int result =
                    a.transform.GetSiblingIndex()
                        .CompareTo(b.transform.GetSiblingIndex());

                if (result != 0)
                    return result;

                return string.CompareOrdinal(a.name, b.name);
            }
        );
    }

    private void PrintCannonOrder()
    {
        for (int i = 0; i < cannons.Length; i++)
        {
            if (cannons[i] == null)
            {
                Debug.Log("Cannon " + i + " = NULL");
                continue;
            }

            Debug.Log(
                "Cannon " + i + " = " + cannons[i].gameObject.name +
                " | Sequence Final = " + (i == cannons.Length - 1)
            );
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Escape / Android Back toggles pause. Handled first so it still
        // works while paused (Update keeps running at timeScale 0).
        if (escapeTogglesPause &&
            !IsTutorialScene() &&
            !IsGameLobbyScene() &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeButton();
            else
                PauseGame();

            return;
        }

        if (levelFinished || isPaused || player == null || !levelStarted)
            return;

        // A TV remote commonly uses the same Submit button for Resume and
        // gameplay. Do not accept another launch until that button is released.
        if (blockLaunchInputUntilReleased)
        {
            if (!IsLaunchInputHeld())
                blockLaunchInputUntilReleased = false;

            bufferedInputTimer = 0f;
            return;
        }

        bool pressed = ReadPressed();

        if (pressed)
        {
            if (player.IsInsideCannon())
            {
                bufferedInputTimer = 0f;
                LaunchCurrentCannon();
            }
            else if (player.IsEnteringCannon())
            {
                bufferedInputTimer = inputBufferTime;
            }
        }
        else if (bufferedInputTimer > 0f)
        {
            bufferedInputTimer -= Time.unscaledDeltaTime;

            if (player.IsInsideCannon())
            {
                bufferedInputTimer = 0f;
                LaunchCurrentCannon();
            }
        }
    }

    private bool IsLaunchInputHeld()
    {
        if (Input.GetKey(KeyCode.Space) ||
            Input.GetKey(KeyCode.Return) ||
            Input.GetKey(KeyCode.KeypadEnter) ||
            Input.GetKey(KeyCode.JoystickButton0) ||
            Input.GetMouseButton(0))
        {
            return true;
        }

        // Keep the lock active while a touch is still down.
        return Input.touchCount > 0;
    }

    private bool ReadPressed()
    {
        // Keyboard / gamepad (never UI taps).
        if (Input.GetKeyDown(KeyCode.Space))
            return true;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return true;
        }

        if (Input.GetKeyDown(KeyCode.JoystickButton0))
            return true;

        // Touch. On mobile Unity also simulates a mouse click for every
        // touch, so when touches exist the mouse check below is skipped.
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (touch.phase != TouchPhase.Began)
                    continue;

                // A tap on the Pause button (or any UI button) must not launch.
                if (IsPointerOverButton(touch.position))
                    continue;

                return true;
            }

            return false;
        }

        // Mouse (editor / PC).
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverButton(Input.mousePosition))
                return false;

            return true;
        }

        return false;
    }

    // True when the topmost UI element under the screen position belongs to
    // a Button / Toggle / Slider etc. Plain non-interactive overlays do NOT
    // block gameplay taps.
    private bool IsPointerOverButton(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData =
            new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        if (uiRaycastResults.Count == 0)
            return false;

        GameObject topHit = uiRaycastResults[0].gameObject;

        return topHit != null &&
               topHit.GetComponentInParent<Selectable>() != null;
    }


    // =========================================================
    // LAUNCH CURRENT CANNON
    // =========================================================

    private void LaunchCurrentCannon()
    {
        if (cannons == null || cannons.Length == 0)
            return;

        if (currentCannonIndex < 0 ||
            currentCannonIndex >= cannons.Length)
        {
            return;
        }

        BulletCannon currentCannon = cannons[currentCannonIndex];

        if (currentCannon == null)
        {
            Debug.LogError(
                "CannonManager: Current cannon is NULL at index " +
                currentCannonIndex
            );
            return;
        }

        bool isSequenceFinal =
            currentCannonIndex == cannons.Length - 1;

        if (isSequenceFinal)
        {
            player.ClearTargetCannon();

            Debug.Log(
                "Launching FINAL cannon: " + currentCannon.gameObject.name
            );

            currentCannon.LaunchPlayer();

            // Final cannon = Level Pass.
            FinishLevel(true);

            return;
        }

        int nextIndex = currentCannonIndex + 1;

        BulletCannon nextCannon = cannons[nextIndex];

        if (nextCannon == null)
        {
            Debug.LogError(
                "CannonManager: Next cannon is NULL at index " + nextIndex
            );
            return;
        }

        player.SetTargetCannon(nextCannon);

        Debug.Log(
            "Launching " + currentCannon.gameObject.name +
            " -> Target: " + nextCannon.gameObject.name
        );

        currentCannon.LaunchPlayer();
    }

    private void EnterCurrentCannon()
    {
        if (cannons == null || cannons.Length == 0)
            return;

        if (currentCannonIndex < 0 ||
            currentCannonIndex >= cannons.Length)
        {
            return;
        }

        BulletCannon cannon = cannons[currentCannonIndex];

        if (cannon == null)
        {
            Debug.LogError("CannonManager: Cannot enter NULL cannon.");
            return;
        }

        if (player == null)
        {
            Debug.LogError("CannonManager: Cannot enter cannon, player is NULL.");
            return;
        }

        cannon.EnterCannon(player);
    }


    // =========================================================
    // EXPECTED CANNON
    // =========================================================

    public bool IsExpectedCannon(BulletCannon cannon)
    {
        if (cannon == null || levelFinished)
            return false;

        int expectedIndex = currentCannonIndex + 1;

        if (cannons == null ||
            expectedIndex < 0 ||
            expectedIndex >= cannons.Length)
        {
            return false;
        }

        return cannons[expectedIndex] == cannon;
    }

    public BulletCannon GetNextCannon()
    {
        int nextIndex = currentCannonIndex + 1;

        if (cannons == null ||
            nextIndex < 0 ||
            nextIndex >= cannons.Length)
        {
            return null;
        }

        return cannons[nextIndex];
    }


    // =========================================================
    // PLAYER EVENTS
    // =========================================================

    public void PlayerEnteredCannon(BulletCannon cannon)
    {
        if (levelFinished)
            return;

        if (cannon == null)
        {
            Debug.LogWarning(
                "CannonManager: PlayerEnteredCannon received NULL."
            );
            return;
        }

        int nextIndex = currentCannonIndex + 1;

        if (nextIndex >= cannons.Length)
            return;

        BulletCannon expectedCannon = cannons[nextIndex];

        if (cannon != expectedCannon)
        {
            Debug.LogWarning(
                "CannonManager: Player entered " + cannon.gameObject.name +
                " but expected " + expectedCannon.gameObject.name
            );
            return;
        }

        currentCannonIndex++;

        Debug.Log(
            "Player entered Cannon " + currentCannonIndex +
            " : " + cannon.gameObject.name
        );
    }

    /// <summary>
    /// Prepares the ordered NEXT cannon for an automatic cannon launch.
    /// </summary>
    public bool PrepareAutomaticLaunch(BulletCannon launchingCannon)
    {
        if (levelFinished ||
            launchingCannon == null ||
            player == null ||
            cannons == null ||
            cannons.Length == 0)
        {
            return false;
        }

        int launchingIndex = -1;

        for (int i = 0; i < cannons.Length; i++)
        {
            if (cannons[i] == launchingCannon)
            {
                launchingIndex = i;
                break;
            }
        }

        if (launchingIndex < 0)
        {
            Debug.LogError(
                "CannonManager: Automatic launch cannon was not found in the sequence: " +
                launchingCannon.gameObject.name
            );

            return false;
        }

        if (currentCannonIndex != launchingIndex)
        {
            if (currentCannonIndex + 1 == launchingIndex)
            {
                currentCannonIndex = launchingIndex;

                Debug.Log(
                    "CannonManager: Synchronized automatic cannon index to " +
                    launchingCannon.gameObject.name
                );
            }
            else
            {
                Debug.LogError(
                    "CannonManager: Automatic launch sequence mismatch. " +
                    "Current index = " + currentCannonIndex +
                    ", launching cannon index = " + launchingIndex
                );

                return false;
            }
        }

        int nextIndex = currentCannonIndex + 1;

        if (nextIndex >= cannons.Length)
        {
            player.ClearTargetCannon();

            Debug.Log(
                "CannonManager: " +
                launchingCannon.gameObject.name +
                " is the final cannon."
            );

            return true;
        }

        BulletCannon nextCannon = cannons[nextIndex];

        if (nextCannon == null)
        {
            Debug.LogError(
                "CannonManager: Next cannon is NULL at index " +
                nextIndex
            );

            return false;
        }

        player.SetTargetCannon(nextCannon);

        Debug.Log(
            "CannonManager: Automatic launch " +
            launchingCannon.gameObject.name +
            " -> Target: " +
            nextCannon.gameObject.name
        );

        return true;
    }


    public void PlayerMissedCannon()
    {
        if (levelFinished)
            return;

        Debug.Log("Player missed Cannon " + (currentCannonIndex + 1));

        // Missing a cannon = Level Fail.
        FinishLevel(false);
    }


    // =========================================================
    // LEVEL PASS / FAIL
    // =========================================================

    private void FinishLevel(bool passed)
    {
        if (resultRoutineStarted)
            return;

        resultRoutineStarted = true;
        levelFinished = true;
        bufferedInputTimer = 0f;

        StartCoroutine(ShowLevelResultAfterDelay(passed));
    }

    private IEnumerator ShowLevelResultAfterDelay(bool passed)
    {
        if (resultPanelDelay > 0f)
            yield return new WaitForSecondsRealtime(resultPanelDelay);

        // Game_Lobby always uses the Tutorial Completed screen,
        // whether the player passes or fails. Interactable tutorial levels
        // continue to use the same result screen as before.
        bool isGameLobby = SceneManager.GetActiveScene().name == "Game_Lobby";

        if (isGameLobby || PlayerPrefs.GetInt("IsInteractableLevel", 0) == 1)
        {
            yield return StartCoroutine(ShowTutorialResult());
            yield break;
        }

        Time.timeScale = 1f;

        // Hide the gameplay level when either result panel is about to appear.
        if (levelGameObject != null)
            levelGameObject.SetActive(false);
        else
            Debug.LogWarning("CannonManager: Level GameObject is not assigned.");

        if (passed)
        {
            int rewardAmount = AwardLevelPassCoins();
            SetResultCoinText(levelPassCoinText, "+" + rewardAmount);
            SetResultCoinText(levelFailCoinText, "0");
        }
        else
        {
            AwardLevelFailCoins();
            SetResultCoinText(levelFailCoinText, "0");
            SetResultCoinText(levelPassCoinText, "+" + GetConfiguredPassReward());
        }

        if (passed)
        {
            if (levelFailPanel != null)
                levelFailPanel.SetActive(false);

            if (levelPassPanel != null)
                levelPassPanel.SetActive(true);
            else
                Debug.LogWarning(
                    "CannonManager: Level Pass Panel is not assigned."
                );

            PlayResultAudio(
                levelPassAudioSource,
                levelPassClip,
                "Level Pass"
            );
        }
        else
        {
            if (levelPassPanel != null)
                levelPassPanel.SetActive(false);

            if (levelFailPanel != null)
                levelFailPanel.SetActive(true);
            else
                Debug.LogWarning(
                    "CannonManager: Level Fail Panel is not assigned."
                );

            PlayResultAudio(
                levelFailAudioSource,
                levelFailClip,
                "Level Fail"
            );
        }
    }


    // =========================================================
    // TUTORIAL COMPLETION
    // =========================================================

    // Saves tutorial completion + the pending interactable reward immediately,
    // so it can't be lost if the player leaves the scene early (e.g. taps Home
    // before the tutorial result timer finishes).
    private void MarkTutorialCompleted()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);

        if (PlayerPrefs.GetInt("InteractableRewardClaimed", 0) == 0)
            PlayerPrefs.SetInt("ShowInteractableRewardOnMenuReturn", 1);

        PlayerPrefs.SetInt("IsInteractableLevel", 0);
        PlayerPrefs.Save();
    }

    private IEnumerator ShowTutorialResult()
    {
        if (tutorialResultShowing)
            yield break;

        tutorialResultShowing = true;

        // Saved immediately, not after the wait below.
        MarkTutorialCompleted();

        Time.timeScale = 1f;

        // Tutorial completion gives no level-pass coins.
        AwardTutorialCompletionCoins();
        SetResultCoinText(tutorialCoinText, "0");

        if (levelGameObject != null)
            levelGameObject.SetActive(false);

        if (levelPassPanel != null)
            levelPassPanel.SetActive(false);

        if (levelFailPanel != null)
            levelFailPanel.SetActive(false);

        if (tutorialPassPanel != null)
            tutorialPassPanel.SetActive(true);
        else
            Debug.LogWarning(
                "CannonManager: Tutorial Pass Panel is not assigned."
            );

        // Play pass audio when the tutorial completion panel appears.
        PlayResultAudio(
            levelPassAudioSource,
            levelPassClip,
            "Tutorial Pass"
        );

        yield return new WaitForSecondsRealtime(tutorialResultDuration);

        Time.timeScale = 1f;

        // Game_Lobby stays on the tutorial completion screen until Home is
        // clicked. Other interactable tutorial scenes keep their old flow.
        if (SceneManager.GetActiveScene().name == "Game_Lobby")
            yield break;

        SceneManager.LoadScene(0);
    }


    // =========================================================
    // RESULT COIN REWARDS / UI
    // =========================================================

    private int GetConfiguredPassReward()
    {
        if (CoinManager.Instance != null)
            return Mathf.Max(0, CoinManager.Instance.levelPassCoins);

        return Mathf.Max(0, fallbackLevelPassCoins);
    }

    private int AwardLevelPassCoins()
    {
        if (resultCoinsApplied)
            return 0;

        resultCoinsApplied = true;

        int rewardAmount = GetConfiguredPassReward();

        if (CoinManager.Instance != null)
        {
            // AddCoins saves the balance and immediately refreshes the gameplay coin text.
            // resultCoinsApplied in this script prevents this reward being added twice.
            CoinManager.Instance.AddCoins(rewardAmount);
        }
        else if (rewardAmount > 0)
        {
            int total = PlayerPrefs.GetInt("TotalCoins", 0) + rewardAmount;
            PlayerPrefs.SetInt("TotalCoins", total);
            PlayerPrefs.Save();
        }

        return rewardAmount;
    }

    private void AwardLevelFailCoins()
    {
        if (resultCoinsApplied)
            return;

        resultCoinsApplied = true;

        if (CoinManager.Instance != null)
            CoinManager.Instance.RefreshCoins();

        // A failed level awards 0 coins. Do not modify TotalCoins.
    }

    private void AwardTutorialCompletionCoins()
    {
        if (resultCoinsApplied)
            return;

        resultCoinsApplied = true;

        if (CoinManager.Instance != null)
            CoinManager.Instance.RefreshCoins();

        // Tutorial completion awards 0 coins.
    }

    private void SetResultCoinText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }


    // =========================================================
    // AUDIO SOURCES
    // =========================================================

    private void CreateResultAudioSources()
    {
        // Create Level Pass AudioSource.
        levelPassAudioSource = gameObject.AddComponent<AudioSource>();

        levelPassAudioSource.playOnAwake = false;
        levelPassAudioSource.loop = false;
        levelPassAudioSource.spatialBlend = 0f;

        // Create Level Fail AudioSource.
        levelFailAudioSource = gameObject.AddComponent<AudioSource>();

        levelFailAudioSource.playOnAwake = false;
        levelFailAudioSource.loop = false;
        levelFailAudioSource.spatialBlend = 0f;
    }

    private void PlayResultAudio(
        AudioSource source,
        AudioClip clip,
        string resultName)
    {
        if (source == null || clip == null)
        {
            if (clip == null)
            {
                Debug.LogWarning(
                    "CannonManager: " + resultName +
                    " audio clip is not assigned."
                );
            }

            return;
        }

        source.Stop();
        source.clip = clip;
        source.Play();
    }


    // =========================================================
    // PAUSE / RESUME / HOME BUTTONS
    // =========================================================

    // Connect the in-game Pause button to this method.
    // The gameplay level is hidden while the pause panel is displayed.
    public void PauseGame()
    {
        // Do not allow pausing in Tutorial or Game_Lobby, even if this
        // method is called directly by another script or a UI event.
        if (IsTutorialScene() || IsGameLobbyScene())
            return;

        if (levelFinished || tutorialResultShowing || isPaused)
            return;

        isPaused = true;
        bufferedInputTimer = 0f;

        // Keep gameplay objects active. Time.timeScale pauses scaled gameplay
        // without firing OnDisable/OnEnable on cannons and player components.

        if (pausePanel != null)
            pausePanel.SetActive(true);

        UpdatePauseSoundUI();
        Time.timeScale = 0f;
    }

    // Connect the Pause panel's Resume button to this method.
    // The gameplay level is restored when the player resumes.
    public void ResumeButton()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (!levelFinished && !tutorialResultShowing)
        {
            // Require the Resume/Submit control to be released before gameplay
            // input is accepted again. This prevents an accidental cannon shot.
            bufferedInputTimer = 0f;
            blockLaunchInputUntilReleased = true;
            isPaused = false;
        }

        Time.timeScale = 1f;
    }

    // Optional alias if your Button OnClick is named/wired as ResumeGame.
    public void ResumeGame()
    {
        ResumeButton();
    }

    // Connect the Pause UI sound button to this method.
    // This updates the same persistent setting used by MainUI.Mute().
    public void PauseSoundToggle()
    {
        AudioManagerPause.IsMuted = !AudioManagerPause.IsMuted;
        UpdatePauseSoundUI();
    }

    // Refreshes the pause icon from the shared audio manager.
    private void UpdatePauseSoundUI()
    {
        AudioManagerPause.Initialize();

        bool isMusicOn = !AudioManagerPause.IsMuted;

        if (pauseSoundButton != null)
            pauseSoundButton.sprite = isMusicOn ? soundOnSprite : soundOffSprite;
    }

    private void HandleMuteStateChanged()
    {
        UpdatePauseSoundUI();
    }

    private void OnEnable()
    {
        AudioManagerPause.OnMuteStateChanged += HandleMuteStateChanged;
        UpdatePauseSoundUI();
    }

    private void OnDisable()
    {
        AudioManagerPause.OnMuteStateChanged -= HandleMuteStateChanged;
    }

    // Connect the Pause panel's Home button here.
    // Leaving from pause does not mark the level as completed.
    public void PauseHomeButton()
    {
        Time.timeScale = 1f;
        MainUI.LevelFailHomeAndReturnToUI();
    }

    // Alias for a Pause panel button already wired to PauseHome.
    public void PauseHome()
    {
        PauseHomeButton();
    }

    // Connect the Level Pass panel's Home button to this method.
    public void LevelPassHome()
    {
        int currentLevel = PlayerPrefs.GetInt("SelectedLevel", 0);
        int unlocked = PlayerPrefs.GetInt("levelsUnlocked", 1);

        PlayerPrefs.SetInt("LevelCompleted_" + currentLevel, 1);

        // Remember exactly which level was completed.
        PlayerPrefs.SetInt("LastCompletedBonusLevel", currentLevel);

        if (currentLevel + 1 >= unlocked)
            PlayerPrefs.SetInt("levelsUnlocked", currentLevel + 2);

        PlayerPrefs.SetInt("SelectedLevel", currentLevel + 1);
        PlayerPrefs.SetInt("currentHighlighter", currentLevel + 1);
        PlayerPrefs.SetInt("IsInteractableLevel", 0);
        PlayerPrefs.SetInt("ShowVIPOnMenuReturn", 1);
        PlayerPrefs.SetInt("LobbyAdPending", 1);

        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene("UI");

        Time.timeScale = 1f;
        MainUI.LevelPassHomeAndReturnToUI();
    }

    // Alias for Inspector setups using PassHomeButton.
    public void PassHomeButton()
    {
        LevelPassHome();
    }

    // Connect the Level Fail panel's Home button to this method.
    public void LevelFailHome()
    {
        PlayerPrefs.SetInt("IsInteractableLevel", 0);
        PlayerPrefs.SetInt("ShowVIPOnMenuReturn", 1);
        PlayerPrefs.SetInt("LastCompletedBonusLevel", -1);
        PlayerPrefs.SetInt("LobbyAdPending", 1);

        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene("UI");
        Time.timeScale = 1f;
        MainUI.LevelFailHomeAndReturnToUI();
    }

    // Alias for Inspector setups using FailHomeButton.
    public void FailHomeButton()
    {
        LevelFailHome();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();

        SceneManager.LoadScene(currentScene.buildIndex);
    }

    // Connect Tutorial Complete panel's Home button to this.
    public void TutorialCompleteHome()
    {
        Time.timeScale = 1f;

        // Guarantees the tutorial-complete flag and the pending interactable
        // reward are saved even if Home is tapped before the result timer ends.
        MarkTutorialCompleted();

        SceneManager.LoadScene("UI");
    }



    // =========================================================
    // GETTERS
    // =========================================================

    public int GetCurrentCannonIndex()
    {
        return currentCannonIndex;
    }

    public bool IsLevelFinished()
    {
        return levelFinished;
    }

    public BulletCannon[] GetCannons()
    {
        return cannons;
    }
}