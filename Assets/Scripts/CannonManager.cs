using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CannonManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private BulletBoyPlayer player;

    [Header("Cannons")]
    [SerializeField] private BulletCannon[] cannons;

    [Header("Finish UI")]
    [SerializeField] private GameObject finishPanel;
    [SerializeField] private float finishPanelDelay = 1f;

    private int currentCannonIndex;
    private bool levelFinished;
    private bool finishPanelRoutineStarted;


    // =========================================================
    // INITIALIZE
    // =========================================================

    private void Awake()
    {
        FindPlayer();
        FindCannons();

        if (finishPanel != null)
        {
            finishPanel.SetActive(false);
        }
    }


    private void Start()
    {
        if (player == null)
        {
            Debug.LogError(
                "CannonManager: Player not found."
            );

            return;
        }

        if (cannons == null ||
            cannons.Length == 0)
        {
            Debug.LogError(
                "CannonManager: No cannons found."
            );

            return;
        }

        currentCannonIndex = 0;

        // Put player into the first cannon.
        EnterCurrentCannon();
    }


    // =========================================================
    // FIND PLAYER
    // =========================================================

    private void FindPlayer()
    {
        if (player != null)
            return;

        player = FindObjectOfType<BulletBoyPlayer>();
    }


    // =========================================================
    // FIND CANNONS
    // =========================================================

    private void FindCannons()
    {
        cannons = FindObjectsOfType<BulletCannon>();

        // Sort cannons by X position.
        // Smallest X = Cannon 0.
        System.Array.Sort(
            cannons,
            (a, b) =>
                a.transform.position.x.CompareTo(
                    b.transform.position.x
                )
        );
    }


    // =========================================================
    // INPUT
    // =========================================================

    private void Update()
    {
        if (levelFinished)
            return;

        if (player == null)
            return;


        // =========================================================
        // MOUSE
        // =========================================================

        if (Input.GetMouseButtonDown(0))
        {
            PlayerInput();
        }


        // =========================================================
        // KEYBOARD
        // =========================================================

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayerInput();
        }


        // =========================================================
        // JOYSTICK / CONTROLLER BUTTON 0
        // =========================================================

        if (Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            PlayerInput();
        }


        // =========================================================
        // MOBILE TOUCH
        // =========================================================

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                PlayerInput();
            }
        }
    }

    // =========================================================
    // PLAYER INPUT
    // =========================================================

    private void PlayerInput()
    {
        // Player can only shoot while inside a cannon.
        if (!player.IsInsideCannon())
            return;

        LaunchCurrentCannon();
    }


    // =========================================================
    // LAUNCH CURRENT CANNON
    // =========================================================

    private void LaunchCurrentCannon()
    {
        if (currentCannonIndex < 0)
            return;

        if (currentCannonIndex >= cannons.Length)
            return;

        BulletCannon currentCannon =
            cannons[currentCannonIndex];

        if (currentCannon == null)
            return;


        // =====================================================
        // FINAL CANNON
        // =====================================================

        if (currentCannon.IsFinalCannon())
        {
            /*
             * Do NOT finish the level here.
             *
             * The level finishes when the player
             * actually enters the final cannon.
             */

            player.ClearTargetCannon();

            currentCannon.LaunchPlayer();

            return;
        }


        // =====================================================
        // FIND NEXT CANNON
        // =====================================================

        int nextIndex =
            currentCannonIndex + 1;

        if (nextIndex >= cannons.Length)
        {
            currentCannon.LaunchPlayer();

            return;
        }


        BulletCannon nextCannon =
            cannons[nextIndex];

        if (nextCannon == null)
            return;


        // Tell player which cannon it needs to reach.
        player.SetTargetCannon(nextCannon);


        // Fire.
        currentCannon.LaunchPlayer();
    }


    // =========================================================
    // ENTER CURRENT CANNON
    // =========================================================

    private void EnterCurrentCannon()
    {
        if (currentCannonIndex < 0)
            return;

        if (currentCannonIndex >= cannons.Length)
            return;

        BulletCannon cannon =
            cannons[currentCannonIndex];

        if (cannon == null)
            return;

        cannon.EnterCannon(player);
    }


    // =========================================================
    // PLAYER ENTERED NEXT CANNON
    // =========================================================

    public void PlayerEnteredCannon(
        BulletCannon cannon)
    {
        if (levelFinished)
            return;

        int nextIndex =
            currentCannonIndex + 1;


        if (nextIndex >= cannons.Length)
        {
            return;
        }


        BulletCannon expectedCannon =
            cannons[nextIndex];


        // Make sure this is actually
        // the cannon we expected.
        if (cannon != expectedCannon)
        {
            return;
        }


        // Player successfully entered
        // the next cannon.
        currentCannonIndex++;


        Debug.Log(
            "Player entered Cannon " +
            currentCannonIndex
        );


        // =====================================================
        // LAST CANNON REACHED
        // =====================================================

        if (currentCannonIndex ==
            cannons.Length - 1)
        {
            FinishLevel();
        }
    }


    // =========================================================
    // PLAYER MISSED CANNON
    // =========================================================

    public void PlayerMissedCannon()
    {
        if (levelFinished)
            return;

        Debug.Log(
            "Player missed Cannon " +
            (currentCannonIndex + 1)
        );

        FinishLevel();
    }


    // =========================================================
    // FINISH LEVEL
    // =========================================================

    private void FinishLevel()
    {
        if (finishPanelRoutineStarted)
            return;

        finishPanelRoutineStarted = true;
        levelFinished = true;

        StartCoroutine(
            ShowFinishPanelAfterDelay()
        );
    }


    // =========================================================
    // SHOW FINISH PANEL
    // =========================================================

    private IEnumerator ShowFinishPanelAfterDelay()
    {
        yield return new WaitForSeconds(
            finishPanelDelay
        );

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


    // =========================================================
    // RESTART LEVEL
    // =========================================================

    public void RestartLevel()
    {
        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            currentScene.buildIndex
        );
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
}