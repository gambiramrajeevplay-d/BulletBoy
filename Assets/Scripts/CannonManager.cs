using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
 * Runs after BulletCannon (-100) and before BulletBoyPlayer (0), so the
 * launch direction is read from the cannon's up-to-date rotation.
 */
[DefaultExecutionOrder(-50)]
public class CannonManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private BulletBoyPlayer player;

    [Header("Cannons")]
    [SerializeField] private BulletCannon[] cannons;

    [Header("Cannon Order")]
    [Tooltip("Only used when the Cannons list above is EMPTY (auto-find). An assigned list is always used exactly as written.")]
    [SerializeField] private bool useHierarchyOrder = true;

    [Header("Input")]
    [Tooltip("A tap that happens while the player is still entering a cannon is remembered for this long (seconds). Set 0 to disable.")]
    [SerializeField] private float inputBufferTime = 0.12f;

    [Header("Next Cannon UI")]
    [Tooltip("Assign a TextMeshPro text (or the legacy Text below).")]
    [SerializeField] private TMP_Text nextCannonTMPText;
    [SerializeField] private Text nextCannonLegacyText;
    [SerializeField] private string nextCannonPrefix = "Next Cannon: ";
    [SerializeField] private bool showCannonNumber = true;
    [SerializeField] private string finalCannonMessage = "Final cannon - tap to launch!";

    [Tooltip("Optional arrow / ring object that floats above the next cannon.")]
    [SerializeField] private Transform nextCannonMarker;
    [SerializeField] private Vector3 markerOffset = new Vector3(0f, 2.2f, 0f);

    [Header("Finish UI")]
    [SerializeField] private GameObject finishPanel;
    [SerializeField] private float finishPanelDelay = 1f;

    private int currentCannonIndex;
    private bool levelFinished;
    private bool finishPanelRoutineStarted;
    private float bufferedInputTimer;


    // =========================================================
    // AWAKE / START
    // =========================================================

    private void Awake()
    {
        FindPlayer();
        FindCannons();

        if (finishPanel != null)
            finishPanel.SetActive(false);
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("CannonManager: Player not found.");
            return;
        }

        if (cannons == null || cannons.Length == 0)
        {
            Debug.LogError("CannonManager: No cannons found.");
            return;
        }

        currentCannonIndex = 0;

        PrintCannonOrder();

        EnterCurrentCannon();

        UpdateNextCannonUI();
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
         * IMPORTANT:
         *
         * If the Cannons list is assigned in the Inspector, that list IS
         * the path. It is used exactly as written and is NEVER re-sorted.
         *
         * Sorting by Hierarchy order used to overwrite the Inspector order
         * (Array.Sort is also not guaranteed to behave identically on every
         * platform), which made cannons 4 and 5 swap on the TV build.
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

                // Deterministic tie-breaker on every platform.
                return string.CompareOrdinal(a.name, b.name);
            }
        );
    }

    private void PrintCannonOrder()
    {
        Debug.Log("========== CANNON ORDER ==========");

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

        Debug.Log("==================================");
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (levelFinished || player == null)
            return;

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
                // Remember the tap so it is not lost during the capture animation.
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

    private void LateUpdate()
    {
        UpdateMarker();
    }

    private bool ReadPressed()
    {
        if (Input.GetMouseButtonDown(0))
            return true;

        if (Input.GetKeyDown(KeyCode.Space))
            return true;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return true;
        }

        if (Input.GetKeyDown(KeyCode.JoystickButton0))
            return true;

        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            return true;
        }

        return false;
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

            // Level complete. Remove this line if you do not want the
            // finish panel after the final launch.
            FinishLevel();

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

        UpdateNextCannonUI();
    }

    public void PlayerMissedCannon()
    {
        if (levelFinished)
            return;

        Debug.Log("Player missed Cannon " + (currentCannonIndex + 1));

        FinishLevel();
    }


    // =========================================================
    // NEXT CANNON UI
    // =========================================================

    private void UpdateNextCannonUI()
    {
        if (levelFinished)
        {
            SetNextCannonText(string.Empty);
            return;
        }

        BulletCannon next = GetNextCannon();

        if (next == null)
        {
            SetNextCannonText(finalCannonMessage);
            return;
        }

        string message = nextCannonPrefix + next.gameObject.name;

        if (showCannonNumber)
        {
            message +=
                "  [" + (currentCannonIndex + 2) +
                "/" + cannons.Length + "]";
        }

        SetNextCannonText(message);
    }

    private void SetNextCannonText(string message)
    {
        if (nextCannonTMPText != null)
            nextCannonTMPText.text = message;

        if (nextCannonLegacyText != null)
            nextCannonLegacyText.text = message;
    }

    private void UpdateMarker()
    {
        if (nextCannonMarker == null)
            return;

        BulletCannon next = levelFinished ? null : GetNextCannon();

        if (next == null)
        {
            if (nextCannonMarker.gameObject.activeSelf)
                nextCannonMarker.gameObject.SetActive(false);

            return;
        }

        if (!nextCannonMarker.gameObject.activeSelf)
            nextCannonMarker.gameObject.SetActive(true);

        nextCannonMarker.position =
            next.transform.position + markerOffset;
    }


    // =========================================================
    // FINISH
    // =========================================================

    private void FinishLevel()
    {
        if (finishPanelRoutineStarted)
            return;

        finishPanelRoutineStarted = true;
        levelFinished = true;

        UpdateNextCannonUI();

        StartCoroutine(ShowFinishPanelAfterDelay());
    }

    private IEnumerator ShowFinishPanelAfterDelay()
    {
        yield return new WaitForSeconds(finishPanelDelay);

        if (finishPanel != null)
        {
            finishPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "CannonManager: Finish Panel is not assigned."
            );
        }
    }

    public void RestartLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        SceneManager.LoadScene(currentScene.buildIndex);
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